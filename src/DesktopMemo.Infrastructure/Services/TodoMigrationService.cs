using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DesktopMemo.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace DesktopMemo.Infrastructure.Services;

/// <summary>
/// TodoList 数据迁移服务（JSON -> SQLite）。
/// </summary>
public sealed class TodoMigrationService
{
    private readonly ILogger<TodoMigrationService>? _logger;

    public TodoMigrationService(ILogger<TodoMigrationService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 将 JSON 格式的待办事项数据迁移到 SQLite 数据库。
    /// </summary>
    /// <param name="dataDirectory">数据目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>迁移结果</returns>
    public async Task<MigrationResult> MigrateFromJsonToSqliteAsync(
        string dataDirectory,
        CancellationToken cancellationToken = default)
    {
        var jsonFile = Path.Combine(dataDirectory, "todos.json");
        var sqliteDb = Path.Combine(dataDirectory, "todos.db");
        var backupFile = Path.Combine(dataDirectory, $"todos_backup_{DateTime.Now:yyyyMMddHHmmss}.json");

        // 如果 SQLite 数据库已存在，则认为已经迁移过
        if (File.Exists(sqliteDb))
        {
            _logger?.LogInformation("SQLite 数据库已存在，跳过迁移");
            return new MigrationResult(false, 0, "SQLite 数据库已存在");
        }

        // 如果 JSON 文件不存在，则没有数据需要迁移
        if (!File.Exists(jsonFile))
        {
            _logger?.LogInformation("未找到 JSON 文件，无需迁移");
            return new MigrationResult(true, 0, "未找到 JSON 文件");
        }

        try
        {
            _logger?.LogInformation("开始从 JSON 迁移到 SQLite...");

            // 1. 读取 JSON 数据
            var jsonRepository = new JsonTodoRepository(dataDirectory);
            var todos = await jsonRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

            if (todos.Count == 0)
            {
                _logger?.LogInformation("JSON 文件为空，无需迁移");
                
                // 即使数据为空，也创建 SQLite 数据库并备份 JSON 文件
                var emptyRepo = new SqliteTodoRepository(dataDirectory);
                emptyRepo.Dispose();
                
                if (File.Exists(jsonFile))
                {
                    File.Move(jsonFile, backupFile);
                    _logger?.LogInformation("已备份空 JSON 文件至: {BackupFile}", backupFile);
                }
                
                return new MigrationResult(true, 0, "JSON 文件为空");
            }

            // 2. 创建 SQLite 数据库并写入数据
            using (var sqliteRepository = new SqliteTodoRepository(dataDirectory))
            {
                foreach (var todo in todos)
                {
                    await sqliteRepository.AddAsync(todo, cancellationToken).ConfigureAwait(false);
                }
            }

            // 3. 备份原 JSON 文件
            File.Move(jsonFile, backupFile);

            _logger?.LogInformation("✅ 成功迁移 {Count} 条待办事项", todos.Count);
            _logger?.LogInformation("📦 原文件已备份至: {BackupFile}", backupFile);

            return new MigrationResult(true, todos.Count, $"成功迁移 {todos.Count} 条数据");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "迁移失败");
            return new MigrationResult(false, 0, $"迁移失败: {ex.Message}");
        }
    }
}

/// <summary>
/// 迁移结果。
/// </summary>
/// <param name="Success">是否成功</param>
/// <param name="MigratedCount">迁移的数据条数</param>
/// <param name="Message">结果消息</param>
public record MigrationResult(bool Success, int MigratedCount, string Message);

