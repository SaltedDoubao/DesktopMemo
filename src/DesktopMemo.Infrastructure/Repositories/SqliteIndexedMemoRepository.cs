using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Core.Models;
using Microsoft.Data.Sqlite;

namespace DesktopMemo.Infrastructure.Repositories;

/// <summary>
/// 混合存储架构的备忘录 Repository：
/// - SQLite 存储元数据索引（快速查询）
/// - Markdown 文件存储完整内容（可移植性）
/// </summary>
public sealed class SqliteIndexedMemoRepository : IMemoRepository, IDisposable
{
    private readonly string _connectionString;
    private readonly string _contentDirectory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SqliteIndexedMemoRepository(string dataDirectory)
    {
        _contentDirectory = Path.Combine(dataDirectory, "content");
        Directory.CreateDirectory(_contentDirectory);

        var dbPath = Path.Combine(dataDirectory, "memos.db");
        _connectionString = $"Data Source={dbPath}";

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS memos (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL DEFAULT '',
                preview TEXT NOT NULL DEFAULT '',
                is_pinned INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                file_path TEXT NOT NULL,
                
                -- 云同步扩展字段
                version INTEGER NOT NULL DEFAULT 1,
                sync_status INTEGER NOT NULL DEFAULT 0,
                deleted_at TEXT,
                
                CHECK (is_pinned IN (0, 1))
            );

            CREATE TABLE IF NOT EXISTS memo_tags (
                memo_id TEXT NOT NULL,
                tag TEXT NOT NULL,
                PRIMARY KEY (memo_id, tag),
                FOREIGN KEY (memo_id) REFERENCES memos(id) ON DELETE CASCADE
            );

            -- 性能索引
            CREATE INDEX IF NOT EXISTS idx_memos_updated 
                ON memos(updated_at DESC);
            CREATE INDEX IF NOT EXISTS idx_memos_pinned 
                ON memos(is_pinned DESC, updated_at DESC);
            CREATE INDEX IF NOT EXISTS idx_tags_tag 
                ON memo_tags(tag);
            CREATE INDEX IF NOT EXISTS idx_memos_sync_status 
                ON memos(sync_status);
        ";
        command.ExecuteNonQuery();
    }

    public async Task<IReadOnlyList<Memo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // 查询元数据（快速）
            const string sql = @"
                SELECT 
                    m.id,
                    m.title,
                    m.preview,
                    m.is_pinned,
                    m.created_at,
                    m.updated_at,
                    m.file_path,
                    m.version,
                    m.sync_status,
                    m.deleted_at,
                    GROUP_CONCAT(t.tag) AS tags
                FROM memos m
                LEFT JOIN memo_tags t ON m.id = t.memo_id
                WHERE m.deleted_at IS NULL
                GROUP BY m.id
                ORDER BY m.is_pinned DESC, m.updated_at DESC";

            var dtos = await connection.QueryAsync<MemoMetadataDto>(sql).ConfigureAwait(false);

            var memos = new List<Memo>();
            foreach (var dto in dtos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 从 Markdown 文件读取内容
                var content = await LoadContentFromFileAsync(dto.FilePath, cancellationToken).ConfigureAwait(false);
                if (content != null)
                {
                    memos.Add(dto.ToMemo(content));
                }
            }

            return memos;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Memo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            const string sql = @"
                SELECT 
                    m.id,
                    m.title,
                    m.preview,
                    m.is_pinned,
                    m.created_at,
                    m.updated_at,
                    m.file_path,
                    m.version,
                    m.sync_status,
                    m.deleted_at,
                    GROUP_CONCAT(t.tag) AS tags
                FROM memos m
                LEFT JOIN memo_tags t ON m.id = t.memo_id
                WHERE m.id = @Id AND m.deleted_at IS NULL
                GROUP BY m.id";

            var dto = await connection.QuerySingleOrDefaultAsync<MemoMetadataDto>(sql, new { Id = id.ToString() }).ConfigureAwait(false);
            if (dto == null)
            {
                return null;
            }

            var content = await LoadContentFromFileAsync(dto.FilePath, cancellationToken).ConfigureAwait(false);
            return content != null ? dto.ToMemo(content) : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task AddAsync(Memo memo, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // 1. 保存 Markdown 文件
            var filePath = GetMemoPath(memo.Id);
            await SaveContentToFileAsync(filePath, memo, cancellationToken).ConfigureAwait(false);

            // 2. 保存元数据到 SQLite
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // 插入备忘录元数据
                const string insertMemoSql = @"
                    INSERT INTO memos (
                        id, title, preview, is_pinned, created_at, updated_at, 
                        file_path, version, sync_status
                    ) VALUES (
                        @Id, @Title, @Preview, @IsPinned, @CreatedAt, @UpdatedAt,
                        @FilePath, @Version, @SyncStatus
                    )";

                await connection.ExecuteAsync(insertMemoSql, new
                {
                    Id = memo.Id.ToString(),
                    memo.Title,
                    memo.Preview,
                    IsPinned = memo.IsPinned ? 1 : 0,
                    CreatedAt = memo.CreatedAt.ToString("o"),
                    UpdatedAt = memo.UpdatedAt.ToString("o"),
                    FilePath = filePath,
                    memo.Version,
                    SyncStatus = (int)memo.SyncStatus
                }, transaction).ConfigureAwait(false);

                // 插入标签
                if (memo.Tags.Any())
                {
                    const string insertTagSql = "INSERT INTO memo_tags (memo_id, tag) VALUES (@MemoId, @Tag)";
                    foreach (var tag in memo.Tags.Distinct())
                    {
                        await connection.ExecuteAsync(insertTagSql, new
                        {
                            MemoId = memo.Id.ToString(),
                            Tag = tag
                        }, transaction).ConfigureAwait(false);
                    }
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateAsync(Memo memo, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // 1. 更新 Markdown 文件
            var filePath = GetMemoPath(memo.Id);
            await SaveContentToFileAsync(filePath, memo, cancellationToken).ConfigureAwait(false);

            // 2. 更新 SQLite 元数据
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // 更新备忘录元数据
                const string updateMemoSql = @"
                    UPDATE memos SET
                        title = @Title,
                        preview = @Preview,
                        is_pinned = @IsPinned,
                        updated_at = @UpdatedAt,
                        version = @Version,
                        sync_status = @SyncStatus
                    WHERE id = @Id AND deleted_at IS NULL";

                var affected = await connection.ExecuteAsync(updateMemoSql, new
                {
                    Id = memo.Id.ToString(),
                    memo.Title,
                    memo.Preview,
                    IsPinned = memo.IsPinned ? 1 : 0,
                    UpdatedAt = memo.UpdatedAt.ToString("o"),
                    memo.Version,
                    SyncStatus = (int)memo.SyncStatus
                }, transaction).ConfigureAwait(false);

                if (affected == 0)
                {
                    throw new InvalidOperationException($"Memo {memo.Id} 不存在或已删除");
                }

                // 删除旧标签
                const string deleteTagsSql = "DELETE FROM memo_tags WHERE memo_id = @MemoId";
                await connection.ExecuteAsync(deleteTagsSql, new { MemoId = memo.Id.ToString() }, transaction).ConfigureAwait(false);

                // 插入新标签
                if (memo.Tags.Any())
                {
                    const string insertTagSql = "INSERT INTO memo_tags (memo_id, tag) VALUES (@MemoId, @Tag)";
                    foreach (var tag in memo.Tags.Distinct())
                    {
                        await connection.ExecuteAsync(insertTagSql, new
                        {
                            MemoId = memo.Id.ToString(),
                            Tag = tag
                        }, transaction).ConfigureAwait(false);
                    }
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // 物理删除（本地模式）
            const string deleteMemoSql = "DELETE FROM memos WHERE id = @Id";
            const string deleteTagsSql = "DELETE FROM memo_tags WHERE memo_id = @Id";

            await connection.ExecuteAsync(deleteTagsSql, new { Id = id.ToString() }).ConfigureAwait(false);
            await connection.ExecuteAsync(deleteMemoSql, new { Id = id.ToString() }).ConfigureAwait(false);

            // 删除 Markdown 文件
            var filePath = GetMemoPath(id);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    // ==================== Markdown 文件操作 ====================

    private string GetMemoPath(Guid id) => Path.Combine(_contentDirectory, $"{id:N}.md");

    private async Task<string?> LoadContentFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            // 跳过 YAML Front Matter
            var firstLine = await reader.ReadLineAsync().ConfigureAwait(false);
            if (firstLine?.Equals("---", StringComparison.Ordinal) == true)
            {
                string? line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (line.Equals("---", StringComparison.Ordinal))
                    {
                        break;
                    }
                }
            }

            // 读取内容
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveContentToFileAsync(string filePath, Memo memo, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine("---");
        builder.AppendLine($"id: {memo.Id}");
        builder.AppendLine($"title: {EscapeYamlString(memo.Title)}");
        builder.AppendLine($"createdAt: {memo.CreatedAt.ToString("O", CultureInfo.InvariantCulture)}");
        builder.AppendLine($"updatedAt: {memo.UpdatedAt.ToString("O", CultureInfo.InvariantCulture)}");
        builder.AppendLine($"isPinned: {memo.IsPinned.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine("tags:");
        foreach (var tag in memo.Tags.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"  - {EscapeYamlString(tag)}");
        }
        builder.AppendLine("---");
        builder.Append(memo.Content);

        await File.WriteAllTextAsync(filePath, builder.ToString(), Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

    private static string EscapeYamlString(string value)
    {
        return value.Contains(':', StringComparison.Ordinal) || value.Contains('"')
            ? $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : value;
    }

    public void Dispose()
    {
        _lock?.Dispose();
    }

    // ==================== DTO 类 ====================

    private class MemoMetadataDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Preview { get; set; } = string.Empty;
        public int IsPinned { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string UpdatedAt { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int Version { get; set; }
        public int SyncStatus { get; set; }
        public string? DeletedAt { get; set; }
        public string? Tags { get; set; }

        public Memo ToMemo(string content)
        {
            var tagList = string.IsNullOrEmpty(Tags)
                ? Array.Empty<string>()
                : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries);

            return new Memo(
                Guid.Parse(Id),
                Title,
                content,
                Preview,
                DateTimeOffset.Parse(CreatedAt),
                DateTimeOffset.Parse(UpdatedAt),
                tagList,
                IsPinned == 1,
                Version,
                (SyncStatus)SyncStatus,
                DeletedAt != null ? DateTimeOffset.Parse(DeletedAt) : null
            );
        }
    }
}

