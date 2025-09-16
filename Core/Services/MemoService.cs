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
    public class MemoService : IMemoService
    {
        private readonly string _dataDirectory;
        private readonly JsonSerializerOptions _jsonOptions;
        public event EventHandler<MemoChangedEventArgs>? MemoChanged;

        public MemoService()
        {
            _dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DesktopMemo",
                "Data");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

            EnsureDataDirectoryExists();
        }

        private void EnsureDataDirectoryExists()
        {
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
        }

        public async Task<List<MemoModel>> LoadMemosAsync()
        {
            try
            {
                var memos = new List<MemoModel>();
                var files = Directory.GetFiles(_dataDirectory, "*.json");

                foreach (var file in files)
                {
                    var json = await File.ReadAllTextAsync(file);
                    var memo = JsonSerializer.Deserialize<MemoModel>(json, _jsonOptions);
                    if (memo != null)
                    {
                        memos.Add(memo);
                    }
                }

                // 按更新时间排序
                return memos.OrderByDescending(m => m.UpdatedAt).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载备忘录失败: {ex.Message}");
                return new List<MemoModel>();
            }
        }

        public async Task SaveMemoAsync(MemoModel memo)
        {
            try
            {
                memo.UpdatedAt = DateTime.Now;
                var fileName = $"{memo.Id}.json";
                var filePath = Path.Combine(_dataDirectory, fileName);
                var json = JsonSerializer.Serialize(memo, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);

                MemoChanged?.Invoke(this, new MemoChangedEventArgs(memo, MemoChangeType.Updated));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存备忘录失败: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteMemoAsync(string memoId)
        {
            try
            {
                var fileName = $"{memoId}.json";
                var filePath = Path.Combine(_dataDirectory, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    var memo = new MemoModel { Id = memoId };
                    MemoChanged?.Invoke(this, new MemoChangedEventArgs(memo, MemoChangeType.Deleted));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除备忘录失败: {ex.Message}");
                throw;
            }
        }

        public async Task<MemoModel> CreateMemoAsync(string title = "", string content = "")
        {
            var memo = new MemoModel
            {
                Id = Guid.NewGuid().ToString(),
                Title = string.IsNullOrEmpty(title) ? "新建备忘录" : title,
                Content = content,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsPinned = false
            };

            await SaveMemoAsync(memo);
            MemoChanged?.Invoke(this, new MemoChangedEventArgs(memo, MemoChangeType.Added));
            return memo;
        }

        public async Task SaveAllMemosAsync(List<MemoModel> memos)
        {
            foreach (var memo in memos)
            {
                await SaveMemoAsync(memo);
            }
        }
    }
}