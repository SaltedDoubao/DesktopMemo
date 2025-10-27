using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DesktopMemo.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace DesktopMemo.Infrastructure.Services;

/// <summary>
/// 备忘录元数据迁移服务（index.json + .md 文件 -> SQLite 索引 + .md 文件）。
/// </summary>
public sealed class MemoMetadataMigrationService
{
    private readonly ILogger<MemoMetadataMigrationService>? _logger;

    public MemoMetadataMigrationService(ILogger<MemoMetadataMigrationService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 将备忘录元数据从 index.json 迁移到 SQLite 数据库。
    /// Markdown 文件保持不变。
    /// </summary>
    /// <param name="dataDirectory">数据目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>迁移结果</returns>
    public async Task<MigrationResult> MigrateToSqliteIndexAsync(
        string dataDirectory,
        CancellationToken cancellationToken = default)
    {
        var contentDir = Path.Combine(dataDirectory, "content");
        var indexFile = Path.Combine(contentDir, "index.json");
        var memosDb = Path.Combine(dataDirectory, "memos.db");
        var backupFile = Path.Combine(contentDir, $"index_backup_{DateTime.Now:yyyyMMddHHmmss}.json");

        // 如果 SQLite 数据库已存在，则认为已经迁移过
        if (File.Exists(memosDb))
        {
            _logger?.LogInformation("备忘录 SQLite 索引已存在，跳过迁移");
            return new MigrationResult(false, 0, "SQLite 索引已存在");
        }

        // 如果 content 目录不存在，说明没有数据
        if (!Directory.Exists(contentDir))
        {
            _logger?.LogInformation("content 目录不存在，无需迁移");
            
            // 即使没有数据，也创建 SQLite 数据库
            using var emptyRepo = new SqliteIndexedMemoRepository(dataDirectory);
            
            return new MigrationResult(true, 0, "content 目录不存在");
        }

        try
        {
            _logger?.LogInformation("开始迁移备忘录元数据到 SQLite 索引...");

            // 1. 使用旧的 Repository 读取所有备忘录
            var oldRepository = new FileMemoRepository(dataDirectory);
            var memos = await oldRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation("读取到 {Count} 条备忘录", memos.Count);

            if (memos.Count == 0)
            {
                _logger?.LogInformation("没有备忘录需要迁移");
                
                // 创建空的 SQLite 数据库
                using var emptyRepo = new SqliteIndexedMemoRepository(dataDirectory);
                
                // 备份 index.json（如果存在）
                if (File.Exists(indexFile))
                {
                    File.Move(indexFile, backupFile);
                    _logger?.LogInformation("已备份空 index.json 至: {BackupFile}", backupFile);
                }
                
                return new MigrationResult(true, 0, "没有备忘录需要迁移");
            }

            // 2. 创建 SQLite Repository 并写入数据
            using (var sqliteRepository = new SqliteIndexedMemoRepository(dataDirectory))
            {
                foreach (var memo in memos)
                {
                    await sqliteRepository.AddAsync(memo, cancellationToken).ConfigureAwait(false);
                }
            }

            // 3. 备份原 index.json 文件
            if (File.Exists(indexFile))
            {
                File.Move(indexFile, backupFile);
                _logger?.LogInformation("已备份 index.json 至: {BackupFile}", backupFile);
            }

            _logger?.LogInformation("✅ 成功迁移 {Count} 条备忘录元数据", memos.Count);
            _logger?.LogInformation("📝 Markdown 文件保持不变");

            return new MigrationResult(true, memos.Count, $"成功迁移 {memos.Count} 条备忘录");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "迁移失败");
            return new MigrationResult(false, 0, $"迁移失败: {ex.Message}");
        }
    }
}

