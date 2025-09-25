using System;
using System.IO;
using System.Windows;
using DesktopMemo.App.ViewModels;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Infrastructure.Repositories;
using DesktopMemo.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopMemo.App;

public partial class App : Application
{
    public IServiceProvider Services { get; }

    public App()
    {
        Services = ConfigureServices();
    }

    private IServiceProvider ConfigureServices()
    {
        var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopMemo");

        var services = new ServiceCollection();

        // 核心服务
        services.AddSingleton<IMemoRepository>(_ => new FileMemoRepository(dataDirectory));
        services.AddSingleton<ISettingsService>(_ => new JsonSettingsService(dataDirectory));

        // 新增的窗口和托盘服务
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<ITrayService, TrayService>();

        // ViewModel
        services.AddSingleton<MainViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var viewModel = Services.GetRequiredService<MainViewModel>();
        var windowService = Services.GetRequiredService<IWindowService>();
        var trayService = Services.GetRequiredService<ITrayService>();

        var window = new MainWindow(viewModel, windowService, trayService);

        // 初始化窗口服务
        if (windowService is WindowService ws)
        {
            ws.Initialize(window);
        }

        // 初始化托盘服务
        trayService.Initialize();
        trayService.Show();

        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 清理托盘图标
        Services.GetService<ITrayService>()?.Dispose();
        base.OnExit(e);
    }
}

