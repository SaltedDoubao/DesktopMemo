using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DesktopMemo.Core.Interfaces;
using DesktopMemo.Core.Models;

namespace DesktopMemo.Infrastructure.Migration
{
    public interface IDataMigrationService
    {
        Task<bool> NeedsMigrationAsync();
        Task<MigrationResult> MigrateDataAsync();
        Task BackupOldDataAsync();
    }

    public class DataMigrationService : IDataMigrationService
    {
        private readonly IMemoService _memoService;
        private readonly ISettingsService _settingsService;
        private readonly string _appDataDir;
        private readonly string _backupDir;

        public DataMigrationService(IMemoService memoService, ISettingsService settingsService)
        {
            _memoService = memoService ?? throw new ArgumentNullException(nameof(memoService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            _appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DesktopMemo");

            _backupDir = Path.Combine(_appDataDir, "Backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        }

        public async Task<bool> NeedsMigrationAsync()
        {
            // Check if old format files exist
            var oldNoteFile = Path.Combine(_appDataDir, "note.txt");
            var oldMemosFile = Path.Combine(_appDataDir, "memos.json");
            var oldSettingsFile = Path.Combine(_appDataDir, "settings.json");

            return File.Exists(oldNoteFile) || File.Exists(oldMemosFile) ||
                   (File.Exists(oldSettingsFile) && !await HasNewFormatDataAsync());
        }

        private async Task<bool> HasNewFormatDataAsync()
        {
            try
            {
                var memos = await _memoService.LoadMemosAsync();
                return memos.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<MigrationResult> MigrateDataAsync()
        {
            var result = new MigrationResult
            {
                StartTime = DateTime.Now,
                Success = true,
                Messages = new List<string>()
            };

            try
            {
                // Create backup
                await BackupOldDataAsync();
                result.Messages.Add("旧数据已备份");

                // Migrate memos
                await MigrateMemosAsync(result);

                // Migrate settings
                await MigrateSettingsAsync(result);

                result.EndTime = DateTime.Now;
                result.Messages.Add($"迁移完成，耗时: {(result.EndTime - result.StartTime).TotalSeconds:F1}秒");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                result.Messages.Add($"迁移失败: {ex.Message}");
            }

            return result;
        }

        private async Task MigrateMemosAsync(MigrationResult result)
        {
            var migratedCount = 0;

            // Migrate from old note.txt format
            var oldNoteFile = Path.Combine(_appDataDir, "note.txt");
            if (File.Exists(oldNoteFile))
            {
                try
                {
                    var content = await File.ReadAllTextAsync(oldNoteFile);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var memo = MemoModel.CreateNew("导入的备忘录", content);
                        await _memoService.SaveMemoAsync(memo);
                        migratedCount++;
                        result.Messages.Add("已迁移 note.txt 内容");
                    }
                }
                catch (Exception ex)
                {
                    result.Messages.Add($"迁移 note.txt 失败: {ex.Message}");
                }
            }

            // Migrate from old memos.json format
            var oldMemosFile = Path.Combine(_appDataDir, "memos.json");
            if (File.Exists(oldMemosFile))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(oldMemosFile);
                    var oldMemos = JsonSerializer.Deserialize<List<OldMemoFormat>>(json);

                    if (oldMemos != null)
                    {
                        foreach (var oldMemo in oldMemos)
                        {
                            var newMemo = new MemoModel
                            {
                                Id = string.IsNullOrEmpty(oldMemo.Id) ? Guid.NewGuid().ToString() : oldMemo.Id,
                                Title = oldMemo.Title ?? "无标题",
                                Content = oldMemo.Content ?? "",
                                CreatedAt = oldMemo.CreatedAt != default ? oldMemo.CreatedAt : DateTime.Now,
                                UpdatedAt = oldMemo.UpdatedAt != default ? oldMemo.UpdatedAt : DateTime.Now,
                                IsPinned = oldMemo.IsPinned
                            };

                            await _memoService.SaveMemoAsync(newMemo);
                            migratedCount++;
                        }
                        result.Messages.Add($"已迁移 {oldMemos.Count} 个备忘录从 memos.json");
                    }
                }
                catch (Exception ex)
                {
                    result.Messages.Add($"迁移 memos.json 失败: {ex.Message}");
                }
            }

            result.MigratedMemosCount = migratedCount;
        }

        private async Task MigrateSettingsAsync(MigrationResult result)
        {
            var oldSettingsFile = Path.Combine(_appDataDir, "settings.json");
            if (!File.Exists(oldSettingsFile)) return;

            try
            {
                var json = await File.ReadAllTextAsync(oldSettingsFile);
                var oldSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (oldSettings != null)
                {
                    var migratedCount = 0;
                    foreach (var setting in oldSettings)
                    {
                        _settingsService.SetSetting(setting.Key, setting.Value);
                        migratedCount++;
                    }

                    await _settingsService.SaveSettingsAsync();
                    result.Messages.Add($"已迁移 {migratedCount} 个设置项");
                    result.MigratedSettingsCount = migratedCount;
                }
            }
            catch (Exception ex)
            {
                result.Messages.Add($"迁移设置失败: {ex.Message}");
            }
        }

        public async Task BackupOldDataAsync()
        {
            if (!Directory.Exists(_backupDir))
            {
                Directory.CreateDirectory(_backupDir);
            }

            var filesToBackup = new[]
            {
                "note.txt", "memos.json", "settings.json", "memos_metadata.json"
            };

            foreach (var fileName in filesToBackup)
            {
                var sourcePath = Path.Combine(_appDataDir, fileName);
                if (File.Exists(sourcePath))
                {
                    var backupPath = Path.Combine(_backupDir, fileName);
                    File.Copy(sourcePath, backupPath, true);
                }
            }

            // Backup entire Data directory if exists
            var dataDir = Path.Combine(_appDataDir, "Data");
            if (Directory.Exists(dataDir))
            {
                var backupDataDir = Path.Combine(_backupDir, "Data");
                await CopyDirectoryAsync(dataDir, backupDataDir);
            }
        }

        private async Task CopyDirectoryAsync(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourceDir, file);
                var targetPath = Path.Combine(targetDir, relativePath);
                var targetFileDir = Path.GetDirectoryName(targetPath);

                if (!string.IsNullOrEmpty(targetFileDir))
                {
                    Directory.CreateDirectory(targetFileDir);
                }

                File.Copy(file, targetPath, true);
            }

            await Task.CompletedTask;
        }
    }

    // Legacy data models for migration
    public class OldMemoFormat
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsPinned { get; set; }
    }

    public class MigrationResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public List<string> Messages { get; set; } = new();
        public int MigratedMemosCount { get; set; }
        public int MigratedSettingsCount { get; set; }
    }
}