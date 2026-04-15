using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SyncBrowser.App.ViewModels;
using SyncBrowser.Core.Interfaces;
using SyncBrowser.Core.Services;
using SyncBrowser.Infrastructure.Dispatchers;
using SyncBrowser.Infrastructure.Factories;
using SyncBrowser.Infrastructure.Repositories;

namespace SyncBrowser.App;

/// <summary>
/// Application entry point. Configures the dependency injection container
/// and launches the main window.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Exposes the DI container so DataTemplate-created views can resolve services.
    /// </summary>
    public IServiceProvider Services => _serviceProvider!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // ── Core Services ──
        services.AddSingleton<IProfileRepository, JsonProfileRepository>();
        services.AddSingleton<IExtensionManager, JsonExtensionManager>();
        services.AddSingleton<IInputDispatcher, CdpInputDispatcher>();
        services.AddSingleton<IBrowserFactory, WebView2BrowserFactory>();
        services.AddSingleton<ISyncMediator, SyncMediator>();
        services.AddSingleton<ProfileManager>();

        // ── ViewModels ──
        services.AddSingleton<MainViewModel>();
        services.AddTransient<ProfileManagerViewModel>();
        services.AddTransient<ExtensionManagerViewModel>();

        // ── Views ──
        services.AddSingleton<MainWindow>();

        // ── Service Provider (for resolving transient ViewModels in Views) ──
        services.AddSingleton<IServiceProvider>(sp => sp);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
