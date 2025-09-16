using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using DesktopMemo.Core.Interfaces;
using DesktopMemo.Core.Services;
using DesktopMemo.Infrastructure;

namespace DesktopMemo
{
    /// <summary>
    /// 应用程序主类，配置依赖注入并启动主窗口
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 配置依赖注入
                var services = new ServiceCollection();
                ConfigureServices(services);
                ServiceLocator.Initialize(services);

                // 创建并显示主窗口
                var memoService = ServiceLocator.GetService<IMemoService>();
                var settingsService = ServiceLocator.GetService<ISettingsService>();
                var searchService = ServiceLocator.GetService<ISearchService>();
                
                var mainWindow = new MainWindow(memoService, null!, settingsService, searchService);
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"启动错误:\n{ex.Message}\n\n{ex.StackTrace}",
                    "DesktopMemo Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);

                Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 注册核心服务
            services.AddSingleton<IMemoService, AdvancedMemoService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<ITrayService, TrayService>();
            
            // SearchService 需要 IMemoService
            services.AddSingleton<ISearchService, SearchService>();
            
            // WindowManagementService需要特殊处理，因为它需要Window实例
            services.AddTransient<IWindowManagementService, WindowManagementService>();

            // 注册主窗口
            services.AddTransient<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ServiceLocator.Dispose();
            base.OnExit(e);
        }
    }
}