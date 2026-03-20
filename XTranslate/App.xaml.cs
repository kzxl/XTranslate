using System.Windows;
using System.Windows.Threading;
using XTranslate.Models;
using XTranslate.Services;
using XTranslate.ViewModels;
using XTranslate.Views;

namespace XTranslate;

/// <summary>
/// Application entry point. Manages services, system tray, and global hotkey.
/// </summary>
public partial class App : Application
{
    public static App Instance => (App)System.Windows.Application.Current;

    // --- Services ---
    public SettingsService SettingsService { get; private set; } = null!;
    public TranslationService TranslationService { get; private set; } = null!;
    public HotkeyService HotkeyService { get; private set; } = null!;
    public ClipboardService ClipboardService { get; private set; } = null!;

    public AppSettings Settings => SettingsService.Settings;

    // --- System Tray ---
    private System.Windows.Forms.NotifyIcon? _trayIcon;

    private MainWindow? _mainWindow;

    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Initialize services
        SettingsService = new SettingsService();
        SettingsService.Load();

        var engine = new GoogleTranslateEngine();
        TranslationService = new TranslationService(engine);
        ClipboardService = new ClipboardService();
        HotkeyService = new HotkeyService();

        // Create main window
        var mainViewModel = new MainViewModel(TranslationService);
        _mainWindow = new MainWindow(mainViewModel);

        // Setup system tray
        SetupSystemTray();

        // Setup global hotkey
        SetupHotkey();

        // Show main window
        if (!Settings.StartMinimized)
        {
            _mainWindow.Show();
        }
    }

    private void SetupSystemTray()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "XTranslate — Dịch nhanh (Ctrl+Q)",
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = CreateTrayMenu()
        };

        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private System.Windows.Forms.ContextMenuStrip CreateTrayMenu()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();

        var showItem = new System.Windows.Forms.ToolStripMenuItem("Hiện cửa sổ chính");
        showItem.Click += (_, _) => ShowMainWindow();
        showItem.Font = new System.Drawing.Font(showItem.Font, System.Drawing.FontStyle.Bold);
        menu.Items.Add(showItem);

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var settingsItem = new System.Windows.Forms.ToolStripMenuItem("Cài đặt");
        settingsItem.Click += (_, _) =>
        {
            ShowMainWindow();
            if (_mainWindow != null)
            {
                var settingsWindow = new SettingsWindow(SettingsService) { Owner = _mainWindow };
                settingsWindow.ShowDialog();
            }
        };
        menu.Items.Add(settingsItem);

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Thoát");
        exitItem.Click += (_, _) => ExitApplication();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void SetupHotkey()
    {
        HotkeyService.HotkeyPressed += OnTranslateHotkeyPressed;

        if (!HotkeyService.RegisterHotkey(Settings.TranslateHotkey))
        {
            MessageBox.Show(
                $"Không thể đăng ký phím tắt Ctrl+Q.\nCó thể phím tắt đã được sử dụng bởi ứng dụng khác.",
                "XTranslate", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void OnTranslateHotkeyPressed()
    {
        try
        {
            // Get selected text from foreground application
            var selectedText = await ClipboardService.GetSelectedTextAsync();

            if (string.IsNullOrWhiteSpace(selectedText))
                return;

            // Create popup
            await Dispatcher.InvokeAsync(async () =>
            {
                var popupVm = new PopupViewModel(TranslationService);
                var popup = new PopupWindow(popupVm);
                popup.Show();

                // Start translation
                await popupVm.TranslateAsync(selectedText, Settings.DefaultTargetLanguage);
            });
        }
        catch
        {
            // Silently fail — hotkey popup is non-critical
        }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }
    }

    private void ExitApplication()
    {
        HotkeyService?.Dispose();
        _trayIcon?.Dispose();
        _mainWindow?.Close();
        Shutdown();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        HotkeyService?.Dispose();
        _trayIcon?.Dispose();
        SettingsService?.Save();
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Console.WriteLine("Unhandled exception: " + e.Exception.Message);
        e.Handled = true;
    }
}
