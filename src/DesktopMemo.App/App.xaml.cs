using System;
using System.IO;
using System.Windows;
using DesktopMemo.App.ViewModels;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Infrastructure.Repositories;
using DesktopMemo.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using WpfApp = System.Windows.Application;

namespace DesktopMemo.App;

public partial class App : WpfApp
{
    public IServiceProvider Services { get; }

    public App()
    {
        Services = ConfigureServices();
    }

    private IServiceProvider ConfigureServices()
    {
        var appDirectory = AppContext.BaseDirectory;
        var dataDirectory = Path.Combine(appDirectory, ".memodata");

        var services = new ServiceCollection();

        // 核心服务
        services.AddSingleton<IMemoRepository>(_ => new FileMemoRepository(dataDirectory));
        services.AddSingleton<ISettingsService>(_ => new JsonSettingsService(dataDirectory));
        services.AddSingleton<IMemoSearchService, MemoSearchService>();
        services.AddSingleton(_ => new MemoMigrationService(dataDirectory, appDirectory));

        // 窗口和托盘服务
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<ITrayService, TrayService>();

        // ViewModel
        services.AddSingleton<MainViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var viewModel = Services.GetRequiredService<MainViewModel>();
            var windowService = Services.GetRequiredService<IWindowService>();
            var trayService = Services.GetRequiredService<ITrayService>();

            var window = new MainWindow(viewModel, windowService, trayService);

            if (windowService is WindowService ws)
            {
                ws.Initialize(window);
            }

            // 初始化托盘服务，但不要因为失败而停止应用程序
            try
            {
                trayService.Initialize();
                trayService.Show();
            }
            catch
            {
                // 托盘服务初始化失败，但应用程序仍然可以运行
            }

            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"应用程序启动失败: {ex.Message}\n\n请尝试删除 .memodata 目录后重新启动应用程序。", 
                "DesktopMemo 启动错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Services.GetService<ITrayService>()?.Dispose();
        base.OnExit(e);
    }
}

