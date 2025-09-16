using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using DesktopMemo.Core.Interfaces;
using DesktopMemo.Core.Services;
using DesktopMemo.Infrastructure;
using DesktopMemo.Infrastructure.Configuration;
using DesktopMemo.Infrastructure.Migration;
using DesktopMemo.Infrastructure.Performance;
using DesktopMemo.UI.ViewModels;

namespace DesktopMemo
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ConfigureServices();

            // Perform data migration if needed
            _ = Task.Run(async () => await PerformDataMigrationAsync());

            // Create and show main window
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private async Task PerformDataMigrationAsync()
        {
            try
            {
                var migrationService = ServiceLocator.GetService<IDataMigrationService>();

                if (await migrationService.NeedsMigrationAsync())
                {
                    var result = await migrationService.MigrateDataAsync();

                    if (result.Success)
                    {
                        System.Diagnostics.Debug.WriteLine("数据迁移成功完成");
                        foreach (var message in result.Messages)
                        {
                            System.Diagnostics.Debug.WriteLine($"迁移: {message}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"数据迁移失败: {result.Error}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"数据迁移异常: {ex.Message}");
            }
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Register core services
            services.AddSingleton<IMemoService, MemoService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ISearchService, SearchService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<IWindowManagementService, WindowManagementService>();

            // Register infrastructure services
            services.AddSingleton<IYamlConfigurationService, YamlConfigurationService>();
            services.AddSingleton<IDataMigrationService, DataMigrationService>();
            services.AddSingleton<IPerformanceService, PerformanceService>();
            services.AddSingleton<IBackgroundTaskService, BackgroundTaskService>();

            // Register UI services
            services.AddSingleton<ITrayService>(provider =>
            {
                var settingsService = provider.GetRequiredService<ISettingsService>();
                var windowService = provider.GetRequiredService<IWindowManagementService>();
                return new TrayService(settingsService, windowService);
            });

            // Register ViewModels
            services.AddTransient<MainViewModel>(provider =>
            {
                return new MainViewModel(
                    provider.GetRequiredService<IMemoService>(),
                    provider.GetRequiredService<IWindowManagementService>(),
                    provider.GetRequiredService<ISettingsService>(),
                    provider.GetRequiredService<ILocalizationService>());
            });

            // Build service provider
            ServiceLocator.Initialize(services);

            // Initialize theme and localization on startup
            var themeService = ServiceLocator.GetService<IThemeService>();
            var localizationService = ServiceLocator.GetService<ILocalizationService>();

            // Apply saved theme
            var settingsService = ServiceLocator.GetService<ISettingsService>();
            var savedTheme = settingsService.GetSetting("Theme", "Dark");
            themeService.SetTheme(savedTheme);

            // Apply saved language
            var savedLanguage = settingsService.GetSetting("Language", "zh-CN");
            localizationService.SetLanguage(savedLanguage);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ServiceLocator.Dispose();
            base.OnExit(e);
        }
    }

}
