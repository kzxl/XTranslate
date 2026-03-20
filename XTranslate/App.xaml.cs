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
    public TranslationEngineRegistry EngineRegistry { get; private set; } = null!;
    public TranslationService TranslationService { get; private set; } = null!;
    public HotkeyService HotkeyService { get; private set; } = null!;
    public ClipboardService ClipboardService { get; private set; } = null!;
    public TextSelectionMonitor TextSelectionMonitor { get; private set; } = null!;

    public AppSettings Settings => SettingsService.Settings;

    // --- UI ---
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private FloatingIconWindow? _floatingIcon;

    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Initialize services
        SettingsService = new SettingsService();
        SettingsService.Load();

        // Engine registry — add new engines here
        EngineRegistry = new TranslationEngineRegistry();
        EngineRegistry.Register(new GoogleTranslateEngine());
        // Future: EngineRegistry.Register(new BingTranslateEngine());
        // Future: EngineRegistry.Register(new DeepLTranslateEngine());

        TranslationService = new TranslationService(EngineRegistry);
        ClipboardService = new ClipboardService();
        HotkeyService = new HotkeyService();

        // Create main window
        var mainViewModel = new MainViewModel(TranslationService);
        _mainWindow = new MainWindow(mainViewModel);

        // Setup floating icon
        _floatingIcon = new FloatingIconWindow();
        _floatingIcon.TranslateRequested += OnFloatingIconTranslateRequested;

        // Setup text selection monitor
        TextSelectionMonitor = new TextSelectionMonitor();
        TextSelectionMonitor.TextSelected += OnTextSelected;
        TextSelectionMonitor.SelectionCleared += OnSelectionCleared;
        TextSelectionMonitor.Start();

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

    // --- Text Selection → Floating Icon ---

    private void OnTextSelected(string text, int x, int y)
    {
        Dispatcher.Invoke(() =>
        {
            _floatingIcon?.ShowAt(x, y, text);
        });
    }

    private void OnSelectionCleared()
    {
        Dispatcher.Invoke(() =>
        {
            _floatingIcon?.HideIcon();
        });
    }

    private async void OnFloatingIconTranslateRequested(string text)
    {
        await ShowTranslationPopup(text);
    }

    // --- System Tray ---

    private void SetupSystemTray()
    {
        // Load custom icon, fallback to system icon
        System.Drawing.Icon appIcon;
        try
        {
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.ico");
            if (System.IO.File.Exists(iconPath))
                appIcon = new System.Drawing.Icon(iconPath);
            else
                appIcon = System.Drawing.Icon.ExtractAssociatedIcon(
                    System.Reflection.Assembly.GetExecutingAssembly().Location)
                    ?? System.Drawing.SystemIcons.Application;
        }
        catch
        {
            appIcon = System.Drawing.SystemIcons.Application;
        }

        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "XTranslate — Dịch nhanh (Ctrl+Q)",
            Icon = appIcon,
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

    // --- Global Hotkey ---

    private void SetupHotkey()
    {
        HotkeyService.HotkeyPressed += OnTranslateHotkeyPressed;

        if (!HotkeyService.RegisterHotkey(Settings.TranslateHotkey))
        {
            System.Windows.MessageBox.Show(
                "Không thể đăng ký phím tắt Ctrl+Q.\nCó thể phím tắt đã được sử dụng bởi ứng dụng khác.",
                "XTranslate", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    private async void OnTranslateHotkeyPressed()
    {
        try
        {
            // Disable text selection monitor briefly to avoid interference
            TextSelectionMonitor.IsEnabled = false;
            _floatingIcon?.HideIcon();

            var selectedText = await ClipboardService.GetSelectedTextAsync();

            TextSelectionMonitor.IsEnabled = true;

            if (!string.IsNullOrWhiteSpace(selectedText))
            {
                await ShowTranslationPopup(selectedText);
            }
        }
        catch
        {
            TextSelectionMonitor.IsEnabled = true;
        }
    }

    private async Task ShowTranslationPopup(string text)
    {
        await Dispatcher.InvokeAsync(async () =>
        {
            var popupVm = new PopupViewModel(TranslationService);
            var popup = new PopupWindow(popupVm);
            popup.Show();
            await popupVm.TranslateAsync(text, Settings.DefaultTargetLanguage);
        });
    }

    // --- Window Management ---

    private void ShowMainWindow()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.WindowState = System.Windows.WindowState.Normal;
            _mainWindow.Activate();
        }
    }

    private void ExitApplication()
    {
        TextSelectionMonitor?.Dispose();
        HotkeyService?.Dispose();
        _floatingIcon?.Close();
        _trayIcon?.Dispose();
        _mainWindow?.Close();
        Shutdown();
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        TextSelectionMonitor?.Dispose();
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
