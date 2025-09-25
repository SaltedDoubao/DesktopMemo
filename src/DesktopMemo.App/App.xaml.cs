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

        services.AddSingleton<IMemoRepository>(_ => new FileMemoRepository(dataDirectory));
        services.AddSingleton<ISettingsService>(_ => new JsonSettingsService(dataDirectory));

        services.AddSingleton<MainViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var viewModel = Services.GetRequiredService<MainViewModel>();
        viewModel.InitializeAsync().GetAwaiter().GetResult();

        var window = new MainWindow(viewModel);
        window.Show();
    }
}

