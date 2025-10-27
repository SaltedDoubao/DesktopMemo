using System;
using System.IO;
using System.Windows;
using DesktopMemo.App.Localization;
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
        services.AddSingleton<IMemoRepository>(_ => new SqliteIndexedMemoRepository(dataDirectory));
        services.AddSingleton<ITodoRepository>(_ => new SqliteTodoRepository(dataDirectory));
        services.AddSingleton<ISettingsService>(_ => new JsonSettingsService(dataDirectory));
        services.AddSingleton<IMemoSearchService, MemoSearchService>();
        services.AddSingleton(_ => new MemoMigrationService(dataDirectory, appDirectory));
        
        // 数据迁移服务
        services.AddSingleton<TodoMigrationService>();
        services.AddSingleton<MemoMetadataMigrationService>();

        // 窗口和托盘服务
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<ITrayService, TrayService>();

        // 本地化服务
        services.AddSingleton<ILocalizationService, LocalizationService>();

        // ViewModel
        services.AddSingleton<TodoListViewModel>();
        services.AddSingleton<MainViewModel>();

        return services.BuildServiceProvider();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // 执行数据迁移
            var appDirectory = AppContext.BaseDirectory;
            var dataDirectory = Path.Combine(appDirectory, ".memodata");
            
            // 1. Todo 数据迁移（JSON -> SQLite）
            var todoMigrationService = Services.GetRequiredService<TodoMigrationService>();
            var todoMigrationResult = await todoMigrationService.MigrateFromJsonToSqliteAsync(dataDirectory);
            if (todoMigrationResult.Success && todoMigrationResult.MigratedCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"TodoList 迁移成功: {todoMigrationResult.Message}");
            }
            
            // 2. 备忘录元数据迁移（index.json -> SQLite 索引）
            var memoMetadataMigrationService = Services.GetRequiredService<MemoMetadataMigrationService>();
            var memoMigrationResult = await memoMetadataMigrationService.MigrateToSqliteIndexAsync(dataDirectory);
            if (memoMigrationResult.Success && memoMigrationResult.MigratedCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"备忘录索引迁移成功: {memoMigrationResult.Message}");
            }

            // 加载语言设置
            var settingsService = Services.GetRequiredService<ISettingsService>();
            var localizationService = Services.GetRequiredService<ILocalizationService>();
            
            var settings = await settingsService.LoadAsync();
            if (!string.IsNullOrEmpty(settings.PreferredLanguage))
            {
                localizationService.ChangeLanguage(settings.PreferredLanguage);
            }

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
            }
            catch
            {
                // 托盘服务初始化失败，但应用程序仍然可以运行
            }

            // 在显示窗口之前先加载配置（配置中会根据设置决定是否显示托盘）
            await viewModel.InitializeAsync();

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

