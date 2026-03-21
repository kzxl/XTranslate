using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using XTranslate.Core;
using XTranslate.Core.Interfaces;
using XTranslate.ViewModels;

namespace XTranslate;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private AppOrchestrator? _orchestrator;

    /// <summary>
    /// Service provider for resolving dependencies.
    /// Used by Views that need to resolve services (e.g. SettingsWindow).
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Setup DI container
        var services = new ServiceCollection();
        services.AddXTranslateServices();

        // Register factory for transient PopupViewModel
        services.AddTransient<Func<PopupViewModel>>(sp => () => sp.GetRequiredService<PopupViewModel>());

        _serviceProvider = services.BuildServiceProvider();
        Services = _serviceProvider;

        // Load settings
        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        settingsService.Load();

        // Apply saved engine preference
        var registry = _serviceProvider.GetRequiredService<Services.TranslationEngineRegistry>();
        registry.ActiveEngineName = settingsService.Settings.ActiveEngineName;

        // Initialize orchestrator
        _orchestrator = _serviceProvider.GetRequiredService<AppOrchestrator>();
        _orchestrator.Initialize();

        Debug.WriteLine("[XTranslate] App startup complete with DI container.");
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        _orchestrator?.Shutdown();
        _serviceProvider?.Dispose();
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Debug.WriteLine($"[XTranslate] UNHANDLED: {e.Exception}");
        e.Handled = true;
    }
}
