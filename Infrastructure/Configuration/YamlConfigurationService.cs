using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using DesktopMemo.Core.Interfaces;

namespace DesktopMemo.Infrastructure.Configuration
{
    public interface IYamlConfigurationService
    {
        Task<T?> LoadConfigAsync<T>(string fileName) where T : class;
        Task SaveConfigAsync<T>(string fileName, T config) where T : class;
        Task<AppConfiguration> LoadAppConfigurationAsync();
        Task SaveAppConfigurationAsync(AppConfiguration config);
    }

    public class YamlConfigurationService : IYamlConfigurationService
    {
        private readonly string _configDirectory;
        private readonly ISerializer _serializer;
        private readonly IDeserializer _deserializer;

        public YamlConfigurationService()
        {
            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DesktopMemo",
                "Config");

            EnsureConfigDirectoryExists();

            _serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        private void EnsureConfigDirectoryExists()
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
        }

        public async Task<T?> LoadConfigAsync<T>(string fileName) where T : class
        {
            try
            {
                var filePath = Path.Combine(_configDirectory, fileName);
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var yaml = await File.ReadAllTextAsync(filePath);
                return _deserializer.Deserialize<T>(yaml);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load config {fileName}: {ex.Message}");
                return null;
            }
        }

        public async Task SaveConfigAsync<T>(string fileName, T config) where T : class
        {
            try
            {
                var filePath = Path.Combine(_configDirectory, fileName);
                var yaml = _serializer.Serialize(config);
                await File.WriteAllTextAsync(filePath, yaml);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save config {fileName}: {ex.Message}");
                throw;
            }
        }

        public async Task<AppConfiguration> LoadAppConfigurationAsync()
        {
            var config = await LoadConfigAsync<AppConfiguration>("app.config.yaml");
            if (config == null)
            {
                config = GetDefaultConfiguration();
                await SaveAppConfigurationAsync(config);
            }
            return config;
        }

        public async Task SaveAppConfigurationAsync(AppConfiguration config)
        {
            await SaveConfigAsync("app.config.yaml", config);
        }

        private AppConfiguration GetDefaultConfiguration()
        {
            return new AppConfiguration
            {
                App = new AppSettings
                {
                    Version = "1.3.1",
                    Name = "DesktopMemo",
                    AutoStart = false
                },
                Window = new WindowSettings
                {
                    DefaultSize = new SizeConfig { Width = 380, Height = 300 },
                    PresetSizes = new List<PresetSize>
                    {
                        new PresetSize { Name = "小窗口", Width = 300, Height = 200 },
                        new PresetSize { Name = "中等窗口", Width = 400, Height = 300 },
                        new PresetSize { Name = "大窗口", Width = 500, Height = 400 }
                    },
                    DefaultTopmost = "Desktop",
                    RememberPosition = true,
                    ShowInTaskbar = false,
                    AllowsTransparency = true
                },
                Editor = new EditorSettings
                {
                    FontSize = 14,
                    FontFamily = "Segoe UI, Microsoft YaHei",
                    TabSize = 4,
                    WordWrap = true,
                    SpellCheck = false,
                    AutoSave = true,
                    AutoSaveInterval = 1000
                },
                Theme = new ThemeSettings
                {
                    Current = "Dark",
                    AutoSwitch = false,
                    FollowSystem = false
                },
                Localization = new LocalizationSettings
                {
                    Current = "zh-CN",
                    AutoDetect = true
                },
                Features = new FeatureSettings
                {
                    MultiSelect = true,
                    SearchHighlight = true,
                    KeyboardShortcuts = true,
                    TrayIcon = true,
                    Notifications = true
                },
                Performance = new PerformanceSettings
                {
                    MaxSearchMatches = 1000,
                    VirtualizedList = true,
                    DebounceDelay = 300
                },
                Logging = new LoggingSettings
                {
                    Level = "Info",
                    EnableFileLogging = true,
                    MaxLogFiles = 5
                }
            };
        }
    }

    // Configuration models
    public class AppConfiguration
    {
        public AppSettings App { get; set; } = new();
        public WindowSettings Window { get; set; } = new();
        public EditorSettings Editor { get; set; } = new();
        public ThemeSettings Theme { get; set; } = new();
        public LocalizationSettings Localization { get; set; } = new();
        public FeatureSettings Features { get; set; } = new();
        public PerformanceSettings Performance { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
    }

    public class AppSettings
    {
        public string Version { get; set; } = "";
        public string Name { get; set; } = "";
        public bool AutoStart { get; set; }
    }

    public class WindowSettings
    {
        public SizeConfig DefaultSize { get; set; } = new();
        public List<PresetSize> PresetSizes { get; set; } = new();
        public string DefaultTopmost { get; set; } = "";
        public bool RememberPosition { get; set; }
        public bool ShowInTaskbar { get; set; }
        public bool AllowsTransparency { get; set; }
    }

    public class SizeConfig
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class PresetSize
    {
        public string Name { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class EditorSettings
    {
        public int FontSize { get; set; }
        public string FontFamily { get; set; } = "";
        public int TabSize { get; set; }
        public bool WordWrap { get; set; }
        public bool SpellCheck { get; set; }
        public bool AutoSave { get; set; }
        public int AutoSaveInterval { get; set; }
    }

    public class ThemeSettings
    {
        public string Current { get; set; } = "";
        public bool AutoSwitch { get; set; }
        public bool FollowSystem { get; set; }
    }

    public class LocalizationSettings
    {
        public string Current { get; set; } = "";
        public bool AutoDetect { get; set; }
    }

    public class FeatureSettings
    {
        public bool MultiSelect { get; set; }
        public bool SearchHighlight { get; set; }
        public bool KeyboardShortcuts { get; set; }
        public bool TrayIcon { get; set; }
        public bool Notifications { get; set; }
    }

    public class PerformanceSettings
    {
        public int MaxSearchMatches { get; set; }
        public bool VirtualizedList { get; set; }
        public int DebounceDelay { get; set; }
    }

    public class LoggingSettings
    {
        public string Level { get; set; } = "";
        public bool EnableFileLogging { get; set; }
        public int MaxLogFiles { get; set; }
    }
}