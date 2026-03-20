using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using XTranslate.Models;
using XTranslate.Services;
using XTranslate.ViewModels;
using XTranslate.Views;

namespace XTranslate;

public partial class App : Application
{
    public static App Instance => (App)System.Windows.Application.Current;

    // --- Services ---
    public SettingsService SettingsService { get; private set; } = null!;
    public TranslationEngineRegistry EngineRegistry { get; private set; } = null!;
    public TranslationService TranslationService { get; private set; } = null!;
    public HotkeyService HotkeyService { get; private set; } = null!;
    public ClipboardService ClipboardService { get; private set; } = null!;
    public TextSelectionMonitor? TextSelectionMonitor { get; private set; }

    public AppSettings Settings => SettingsService.Settings;

    // --- UI ---
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private FloatingIconWindow? _floatingIcon;
    private System.Windows.Forms.ToolStripMenuItem? _hotkeyToggle;
    private System.Windows.Forms.ToolStripMenuItem? _mouseToggle;

    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        SettingsService = new SettingsService();
        SettingsService.Load();

        EngineRegistry = new TranslationEngineRegistry();
        EngineRegistry.Register(new GoogleTranslateEngine());
        EngineRegistry.Register(new MyMemoryTranslateEngine());

        TranslationService = new TranslationService(EngineRegistry);
        ClipboardService = new ClipboardService();

        var mainViewModel = new MainViewModel(TranslationService);
        _mainWindow = new MainWindow(mainViewModel);

        // Hotkey
        HotkeyService = new HotkeyService();
        SetupHotkey();

        // Floating icon (lightweight — no Ctrl+C until user clicks)
        _floatingIcon = new FloatingIconWindow();
        _floatingIcon.TranslateRequested += OnFloatingIconClicked;

        // Text selection monitor
        if (Settings.ShowFloatingIcon)
        {
            StartTextSelectionMonitor();
        }

        SetupSystemTray();

        if (!Settings.StartMinimized)
            _mainWindow.Show();

        Debug.WriteLine("[XTranslate] Startup complete.");
    }

    // --- Text Selection Monitor ---

    private void StartTextSelectionMonitor()
    {
        TextSelectionMonitor = new TextSelectionMonitor();
        TextSelectionMonitor.PossibleSelection += OnPossibleTextSelection;
        TextSelectionMonitor.SelectionCleared += () => Dispatcher.Invoke(() => _floatingIcon?.HideIcon());
        TextSelectionMonitor.Start();
    }

    private void OnPossibleTextSelection(int x, int y)
    {
        // Just show the icon — no clipboard capture yet
        Dispatcher.Invoke(() => _floatingIcon?.ShowAt(x, y, ""));
    }

    /// <summary>
    /// User clicked the floating icon → now capture text and translate.
    /// </summary>
    private async void OnFloatingIconClicked(string _)
    {
        try
        {
            if (TextSelectionMonitor != null)
                TextSelectionMonitor.IsEnabled = false;

            var text = await ClipboardService.GetSelectedTextAsync();
            Debug.WriteLine($"[XTranslate] FloatingIcon click → text='{text?.Substring(0, Math.Min(text?.Length ?? 0, 30))}'");

            if (TextSelectionMonitor != null)
                TextSelectionMonitor.IsEnabled = true;

            if (!string.IsNullOrWhiteSpace(text))
            {
                await ShowTranslationPopup(text);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[XTranslate] FloatingIcon error: {ex.Message}");
            if (TextSelectionMonitor != null)
                TextSelectionMonitor.IsEnabled = true;
        }
    }

    // --- Hotkey ---

    private void SetupHotkey()
    {
        HotkeyService.HotkeyPressed += OnTranslateHotkeyPressed;

        var hotkey = Settings.TranslateHotkey;
        bool ok = HotkeyService.RegisterHotkey(hotkey);
        Debug.WriteLine($"[XTranslate] RegisterHotKey({hotkey}) = {ok}");

        if (!ok)
        {
            System.Windows.MessageBox.Show(
                $"Phím tắt {FormatHotkey(hotkey)} đã bị ứng dụng khác sử dụng.\n\n" +
                "Vào Cài đặt → Phím tắt để chọn phím tắt khác.\n" +
                "Bạn vẫn có thể dùng Ctrl+Enter trong cửa sổ chính.",
                "XTranslate — Phím tắt",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// Called after Settings saved to re-register hotkey with new key combo.
    /// </summary>
    public void ReRegisterHotkey()
    {
        HotkeyService.UnregisterHotkey();
        var hotkey = Settings.TranslateHotkey;
        bool ok = HotkeyService.RegisterHotkey(hotkey);
        Debug.WriteLine($"[XTranslate] ReRegisterHotKey({hotkey}) = {ok}");

        if (!ok)
        {
            System.Windows.MessageBox.Show(
                $"Không thể đăng ký phím tắt {FormatHotkey(hotkey)}.\n" +
                "Phím có thể đang được sử dụng bởi ứng dụng khác.",
                "XTranslate", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }

        // Update tray tooltip
        if (_trayIcon != null)
            _trayIcon.Text = $"XTranslate — Dịch nhanh ({FormatHotkey(hotkey)})";

        // Update floating icon
        if (Settings.ShowFloatingIcon && TextSelectionMonitor == null)
        {
            StartTextSelectionMonitor();
        }
        else if (!Settings.ShowFloatingIcon && TextSelectionMonitor != null)
        {
            TextSelectionMonitor.Dispose();
            TextSelectionMonitor = null;
            _floatingIcon?.HideIcon();
        }
    }

    private static string FormatHotkey(System.Windows.Forms.Keys hotkey)
    {
        var parts = new System.Collections.Generic.List<string>();
        if (hotkey.HasFlag(System.Windows.Forms.Keys.Control)) parts.Add("Ctrl");
        if (hotkey.HasFlag(System.Windows.Forms.Keys.Alt)) parts.Add("Alt");
        if (hotkey.HasFlag(System.Windows.Forms.Keys.Shift)) parts.Add("Shift");
        var key = hotkey & System.Windows.Forms.Keys.KeyCode;
        if (key != System.Windows.Forms.Keys.None) parts.Add(key.ToString());
        return string.Join("+", parts);
    }

    private async void OnTranslateHotkeyPressed()
    {
        Debug.WriteLine("[XTranslate] Hotkey pressed!");
        try
        {
            if (TextSelectionMonitor != null)
                TextSelectionMonitor.IsEnabled = false;
            _floatingIcon?.HideIcon();

            var text = await ClipboardService.GetSelectedTextAsync();

            if (TextSelectionMonitor != null)
                TextSelectionMonitor.IsEnabled = true;

            if (!string.IsNullOrWhiteSpace(text))
                await ShowTranslationPopup(text);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[XTranslate] Hotkey error: {ex.Message}");
            if (TextSelectionMonitor != null)
                TextSelectionMonitor.IsEnabled = true;
        }
    }

    private async Task ShowTranslationPopup(string text)
    {
        await Dispatcher.InvokeAsync(async () =>
        {
            var vm = new PopupViewModel(TranslationService);
            var popup = new PopupWindow(vm);
            popup.Show();
            await vm.TranslateAsync(text, Settings.DefaultTargetLanguage);
        });
    }

    // --- System Tray ---

    private void SetupSystemTray()
    {
        System.Drawing.Icon appIcon;
        try
        {
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.ico");
            appIcon = System.IO.File.Exists(iconPath)
                ? new System.Drawing.Icon(iconPath)
                : System.Drawing.SystemIcons.Application;
        }
        catch { appIcon = System.Drawing.SystemIcons.Application; }

        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = $"XTranslate — Dịch nhanh ({FormatHotkey(Settings.TranslateHotkey)})",
            Icon = appIcon,
            Visible = true,
            ContextMenuStrip = CreateTrayMenu()
        };
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private System.Windows.Forms.ContextMenuStrip CreateTrayMenu()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();

        // --- Title ---
        var titleItem = new System.Windows.Forms.ToolStripMenuItem("XTranslate");
        titleItem.Font = new System.Drawing.Font(titleItem.Font.FontFamily, titleItem.Font.Size + 1, System.Drawing.FontStyle.Bold);
        titleItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(titleItem);
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        // --- Main actions ---
        var showItem = new System.Windows.Forms.ToolStripMenuItem("Hiện cửa sổ chính");
        showItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(showItem);

        var historyItem = new System.Windows.Forms.ToolStripMenuItem("Lịch sử dịch");
        historyItem.Enabled = false; // Future feature
        menu.Items.Add(historyItem);

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var settingsItem = new System.Windows.Forms.ToolStripMenuItem("Cài đặt");
        settingsItem.Click += (_, _) =>
        {
            ShowMainWindow();
            if (_mainWindow != null)
            {
                var sw = new SettingsWindow(SettingsService) { Owner = _mainWindow };
                if (sw.ShowDialog() == true) ReRegisterHotkey();
            }
        };
        menu.Items.Add(settingsItem);

        var aboutItem = new System.Windows.Forms.ToolStripMenuItem("Về XTranslate");
        aboutItem.Click += (_, _) =>
        {
            System.Windows.MessageBox.Show(
                "XTranslate v1.0\n\n" +
                "Phần mềm dịch thuật nhanh cho Windows.\n" +
                "Thay thế QTranslate — hiện đại, nhẹ nhàng.\n\n" +
                $"Engine: {EngineRegistry.ActiveEngineName}\n" +
                $"Hotkey: {FormatHotkey(Settings.TranslateHotkey)}\n" +
                $".NET {Environment.Version}",
                "Về XTranslate", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        };
        menu.Items.Add(aboutItem);

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        // --- Toggle items ---
        _hotkeyToggle = new System.Windows.Forms.ToolStripMenuItem("Bật phím tắt toàn cục");
        _hotkeyToggle.Checked = true;
        _hotkeyToggle.CheckOnClick = true;
        _hotkeyToggle.CheckedChanged += (_, _) =>
        {
            if (_hotkeyToggle.Checked)
            {
                HotkeyService.RegisterHotkey(Settings.TranslateHotkey);
            }
            else
            {
                HotkeyService.UnregisterHotkey();
            }
        };
        menu.Items.Add(_hotkeyToggle);

        _mouseToggle = new System.Windows.Forms.ToolStripMenuItem("Chế độ chuột (floating icon)");
        _mouseToggle.Checked = Settings.ShowFloatingIcon;
        _mouseToggle.CheckOnClick = true;
        _mouseToggle.CheckedChanged += (_, _) =>
        {
            Settings.ShowFloatingIcon = _mouseToggle.Checked;
            if (_mouseToggle.Checked && TextSelectionMonitor == null)
            {
                StartTextSelectionMonitor();
            }
            else if (!_mouseToggle.Checked)
            {
                TextSelectionMonitor?.Dispose();
                TextSelectionMonitor = null;
                _floatingIcon?.HideIcon();
            }
        };
        menu.Items.Add(_mouseToggle);

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        // --- Exit ---
        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Thoát");
        exitItem.Click += (_, _) => ExitApplication();
        menu.Items.Add(exitItem);

        return menu;
    }

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
        Debug.WriteLine($"[XTranslate] UNHANDLED: {e.Exception}");
        e.Handled = true;
    }
}
