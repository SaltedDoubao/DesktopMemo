using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DesktopMemo.Core.Interfaces;
using DesktopMemo.Core.Models;

namespace DesktopMemo.Core.Services
{
    public class AdvancedMemoService : IMemoService
    {
        private readonly string _appDataDir;
        private readonly string _noteFilePath;
        private readonly string _memosFilePath;
        private readonly string _memosMetadataFilePath;
        private readonly string _contentDir;

        private List<MemoModel> _memos = new List<MemoModel>();

        public event EventHandler<MemoChangedEventArgs>? MemoChanged;

        public AdvancedMemoService()
        {
            _appDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _noteFilePath = Path.Combine(_appDataDir, "notes.json");
            _memosFilePath = Path.Combine(_appDataDir, "memos.json");
            _memosMetadataFilePath = Path.Combine(_appDataDir, "memos_metadata.json");
            _contentDir = Path.Combine(_appDataDir, "content");

            Directory.CreateDirectory(_appDataDir);
            Directory.CreateDirectory(_contentDir);
        }

        public async Task<List<MemoModel>> LoadMemosAsync()
        {
            try
            {
                // 优先尝试加载Markdown文件
                if (Directory.Exists(_contentDir))
                {
                    await LoadMemosFromMarkdownAsync();

                    // 如果有数据，返回
                    if (_memos.Any())
                    {
                        return _memos.OrderByDescending(m => m.UpdatedAt).ToList();
                    }
                }

                // 如果没有Markdown文件，尝试从旧格式迁移
                await MigrateFromOldNoteFormat();

                return _memos.OrderByDescending(m => m.UpdatedAt).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载备忘录失败: {ex.Message}");

                // 如果加载失败，创建默认备忘录
                if (!_memos.Any())
                {
                    await CreateDefaultMemoAsync();
                }

                return _memos;
            }
        }

        public async Task SaveMemoAsync(MemoModel memo)
        {
            try
            {
                memo.UpdatedAt = DateTime.Now;

                // 保存到Markdown文件
                var fileName = $"{memo.Id}.md";
                var filePath = Path.Combine(_contentDir, fileName);

                var content = $"# {memo.Title}\n\n{memo.Content}";
                await File.WriteAllTextAsync(filePath, content);

                // 更新内存中的备忘录
                var existingMemo = _memos.FirstOrDefault(m => m.Id == memo.Id);
                if (existingMemo != null)
                {
                    existingMemo.Title = memo.Title;
                    existingMemo.Content = memo.Content;
                    existingMemo.UpdatedAt = memo.UpdatedAt;
                    existingMemo.IsPinned = memo.IsPinned;
                    existingMemo.Tags = memo.Tags;
                    existingMemo.Category = memo.Category;
                    existingMemo.Priority = memo.Priority;
                }
                else
                {
                    _memos.Add(memo);
                }

                // 保存元数据
                await SaveMetadataAsync();

                MemoChanged?.Invoke(this, new MemoChangedEventArgs(memo, MemoChangeType.Updated));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存备忘录失败: {ex.Message}", ex);
            }
        }

        public async Task DeleteMemoAsync(string memoId)
        {
            try
            {
                var memo = _memos.FirstOrDefault(m => m.Id == memoId);
                if (memo == null) return;

                // 删除Markdown文件
                var fileName = $"{memoId}.md";
                var filePath = Path.Combine(_contentDir, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // 从内存中移除
                _memos.Remove(memo);

                // 保存元数据
                await SaveMetadataAsync();

                MemoChanged?.Invoke(this, new MemoChangedEventArgs(memo, MemoChangeType.Deleted));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"删除备忘录失败: {ex.Message}", ex);
            }
        }

        public async Task<MemoModel> CreateMemoAsync(string title = "", string content = "")
        {
            try
            {
                var memo = MemoModel.CreateNew(
                    string.IsNullOrEmpty(title) ? "新建备忘录" : title,
                    string.IsNullOrEmpty(content) ? "" : content
                );

                await SaveMemoAsync(memo);

                MemoChanged?.Invoke(this, new MemoChangedEventArgs(memo, MemoChangeType.Added));

                return memo;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"创建备忘录失败: {ex.Message}", ex);
            }
        }

        public async Task SaveAllMemosAsync(List<MemoModel> memos)
        {
            try
            {
                foreach (var memo in memos)
                {
                    await SaveMemoAsync(memo);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"批量保存备忘录失败: {ex.Message}", ex);
            }
        }

        #region 私有方法 - 从原始代码提取

        private async Task LoadMemosFromMarkdownAsync()
        {
            try
            {
                _memos.Clear();

                // 加载元数据
                await LoadMetadataAsync();

                // 扫描内容目录中的所有.md文件
                var mdFiles = Directory.GetFiles(_contentDir, "*.md");

                foreach (var filePath in mdFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var content = await File.ReadAllTextAsync(filePath);

                    // 解析标题和内容
                    var (title, memoContent) = ParseMarkdownContent(content);

                    // 查找对应的元数据
                    var existingMemo = _memos.FirstOrDefault(m => m.Id == fileName);

                    if (existingMemo != null)
                    {
                        existingMemo.Title = title;
                        existingMemo.Content = memoContent;
                    }
                    else
                    {
                        var memo = new MemoModel
                        {
                            Id = fileName,
                            Title = title,
                            Content = memoContent,
                            CreatedAt = File.GetCreationTime(filePath),
                            UpdatedAt = File.GetLastWriteTime(filePath)
                        };
                        _memos.Add(memo);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从Markdown加载备忘录失败: {ex.Message}");
            }
        }

        private async Task LoadMetadataAsync()
        {
            try
            {
                if (File.Exists(_memosMetadataFilePath))
                {
                    var json = await File.ReadAllTextAsync(_memosMetadataFilePath);
                    var memos = JsonSerializer.Deserialize<List<MemoModel>>(json);
                    if (memos != null)
                    {
                        _memos = memos;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载元数据失败: {ex.Message}");
            }
        }

        private async Task SaveMetadataAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_memos, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                await File.WriteAllTextAsync(_memosMetadataFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存元数据失败: {ex.Message}");
            }
        }

        private (string title, string content) ParseMarkdownContent(string markdown)
        {
            var lines = markdown.Split('\n');
            var title = "无标题";
            var content = "";

            if (lines.Length > 0 && lines[0].StartsWith("# "))
            {
                title = lines[0].Substring(2).Trim();
                content = string.Join("\n", lines.Skip(2)).Trim();
            }
            else
            {
                content = markdown;
            }

            return (title, content);
        }

        private async Task MigrateFromOldNoteFormat()
        {
            try
            {
                // 尝试从旧的notes.json格式迁移
                if (File.Exists(_noteFilePath))
                {
                    var content = await File.ReadAllTextAsync(_noteFilePath);

                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var memo = MemoModel.CreateNew("导入的备忘录", content);
                        _memos.Add(memo);
                        await SaveMemoAsync(memo);
                    }
                }

                // 尝试从旧的memos.json格式迁移
                if (File.Exists(_memosFilePath))
                {
                    var json = await File.ReadAllTextAsync(_memosFilePath);
                    var oldMemos = JsonSerializer.Deserialize<List<MemoModel>>(json);

                    if (oldMemos != null)
                    {
                        foreach (var memo in oldMemos)
                        {
                            _memos.Add(memo);
                            await SaveMemoAsync(memo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"迁移旧格式失败: {ex.Message}");
            }
        }

        private async Task CreateDefaultMemoAsync()
        {
            try
            {
                var defaultContent = @"欢迎使用 DesktopMemo！

这是一个功能强大的桌面便签应用程序。

## 主要功能
- **多备忘录管理**: 创建、编辑和删除多个备忘录
- **Markdown支持**: 支持Markdown格式，让你的笔记更美观
- **窗口置顶**: 三种置顶模式，适应不同工作场景
- **托盘集成**: 系统托盘功能，随时访问
- **位置记忆**: 自动记住窗口位置

## 快捷键
- `Ctrl+N`: 新建备忘录
- `Ctrl+S`: 保存当前备忘录
- `Ctrl+D`: 删除当前备忘录
- `Ctrl+Tab`: 切换到下一个备忘录

开始在这里记录你的想法吧！";

                var defaultMemo = MemoModel.CreateNew("欢迎使用 DesktopMemo", defaultContent);
                await SaveMemoAsync(defaultMemo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建默认备忘录失败: {ex.Message}");
            }
        }

        #endregion
    }
}