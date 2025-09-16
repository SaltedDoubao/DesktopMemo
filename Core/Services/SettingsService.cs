using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DesktopMemo.Core.Interfaces;

namespace DesktopMemo.Core.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        private Dictionary<string, object> _settings;
        private readonly JsonSerializerOptions _jsonOptions;

        public event EventHandler<SettingChangedEventArgs>? SettingChanged;

        public SettingsService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DesktopMemo");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _settingsFilePath = Path.Combine(appDataPath, "settings.json");
            _settings = new Dictionary<string, object>();

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

            LoadSettingsAsync().Wait();
        }

        public T GetSetting<T>(string key, T defaultValue)
        {
            if (_settings.TryGetValue(key, out var value))
            {
                if (value is JsonElement jsonElement)
                {
                    try
                    {
                        var result = JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), _jsonOptions);
                        return result ?? defaultValue;
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }

                if (value is T typedValue)
                {
                    return typedValue;
                }

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        public void SetSetting<T>(string key, T value)
        {
            if (value != null)
            {
                _settings[key] = value;
                SettingChanged?.Invoke(this, new SettingChangedEventArgs(key, value));
            }
        }

        public async Task SaveSettingsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, _jsonOptions);
                await File.WriteAllTextAsync(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
            }
        }

        public async Task LoadSettingsAsync()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = await File.ReadAllTextAsync(_settingsFilePath);
                    _settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions)
                               ?? new Dictionary<string, object>();
                }
                else
                {
                    // 设置默认值
                    InitializeDefaultSettings();
                    await SaveSettingsAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载设置失败: {ex.Message}");
                InitializeDefaultSettings();
            }
        }

        private void InitializeDefaultSettings()
        {
            _settings = new Dictionary<string, object>
            {
                ["ShowExitPrompt"] = true,
                ["ShowDeletePrompt"] = true,
                ["BackgroundOpacity"] = 0.1,
                ["AutoSaveInterval"] = 1000,
                ["TopmostMode"] = "Desktop",
                ["WindowPinned"] = false,
                ["Theme"] = "Dark",
                ["Language"] = "zh-CN",
                ["FontSize"] = 14.0,
                ["FontFamily"] = "Segoe UI, Microsoft YaHei"
            };
        }
    }
}