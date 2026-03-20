using System.Windows;
using System.Windows.Input;
using XTranslate.Helpers;
using XTranslate.Services;

namespace XTranslate.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private System.Windows.Forms.Keys _capturedHotkey;
    private bool _isRecordingHotkey;

    public SettingsWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Settings;

        // Hotkey
        _capturedHotkey = settings.TranslateHotkey;
        txtHotkey.Text = FormatHotkey(_capturedHotkey);

        // Language
        cboTargetLang.ItemsSource = LanguageDatabase.TargetLanguages;
        cboTargetLang.SelectedItem = LanguageDatabase.FindByCode(settings.DefaultTargetLanguage)
                                     ?? LanguageDatabase.FindByCode("vi");

        // Behavior
        chkMinimizeToTray.IsChecked = settings.MinimizeToTray;
        chkStartMinimized.IsChecked = settings.StartMinimized;
        chkShowFloatingIcon.IsChecked = settings.ShowFloatingIcon;

        // Advanced
        var registry = App.Instance.EngineRegistry;
        cboEngine.ItemsSource = registry.AvailableEngines;
        cboEngine.SelectedItem = registry.ActiveEngineName;

        chkStartWithWindows.IsChecked = settings.StartWithWindows;
        txtPopupDelay.Text = settings.PopupAutoCloseSeconds.ToString();
    }

    // --- Hotkey Recorder ---

    private void HotkeyBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _isRecordingHotkey = true;
        txtHotkey.Text = "Nhấn tổ hợp phím...";
        txtHotkey.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x81, 0x8C, 0xF8)); // accent
        txtHotkeyHint.Text = "Đang ghi...";
    }

    private void HotkeyBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _isRecordingHotkey = false;
        txtHotkey.Text = FormatHotkey(_capturedHotkey);
        txtHotkey.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0xF1, 0xF5, 0xF9)); // white
        txtHotkeyHint.Text = "Click để đổi";
    }

    private void HotkeyBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_isRecordingHotkey) return;

        e.Handled = true;

        // Ignore standalone modifier keys
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return; // Wait for actual key
        }

        // Build Keys enum from WPF modifiers + key
        var wpfKey = KeyInterop.VirtualKeyFromKey(key);
        var formsKey = (System.Windows.Forms.Keys)wpfKey;

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            formsKey |= System.Windows.Forms.Keys.Control;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            formsKey |= System.Windows.Forms.Keys.Alt;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            formsKey |= System.Windows.Forms.Keys.Shift;

        // Must have at least one modifier
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
            !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            txtHotkey.Text = "Cần ít nhất Ctrl hoặc Alt";
            return;
        }

        _capturedHotkey = formsKey;
        txtHotkey.Text = FormatHotkey(_capturedHotkey);
        txtHotkey.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x22, 0xC5, 0x5E)); // green = success
        txtHotkeyHint.Text = "✓ Đã ghi";
        _isRecordingHotkey = false;

        // Move focus away
        Keyboard.ClearFocus();
    }

    private static string FormatHotkey(System.Windows.Forms.Keys hotkey)
    {
        var parts = new List<string>();
        if (hotkey.HasFlag(System.Windows.Forms.Keys.Control)) parts.Add("Ctrl");
        if (hotkey.HasFlag(System.Windows.Forms.Keys.Alt)) parts.Add("Alt");
        if (hotkey.HasFlag(System.Windows.Forms.Keys.Shift)) parts.Add("Shift");

        var keyCode = hotkey & System.Windows.Forms.Keys.KeyCode;
        if (keyCode != System.Windows.Forms.Keys.None)
            parts.Add(keyCode.ToString());

        return string.Join(" + ", parts);
    }

    // --- Save / Cancel ---

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Settings;

        // Hotkey
        settings.TranslateHotkey = _capturedHotkey;

        // Language
        if (cboTargetLang.SelectedItem is Models.Language lang)
            settings.DefaultTargetLanguage = lang.Code;

        // Behavior
        settings.MinimizeToTray = chkMinimizeToTray.IsChecked ?? true;
        settings.StartMinimized = chkStartMinimized.IsChecked ?? false;
        settings.ShowFloatingIcon = chkShowFloatingIcon.IsChecked ?? true;

        // Advanced
        settings.StartWithWindows = chkStartWithWindows.IsChecked ?? false;
        if (int.TryParse(txtPopupDelay.Text, out var delay) && delay >= 0)
            settings.PopupAutoCloseSeconds = delay;
        if (cboEngine.SelectedItem is string engineName)
            App.Instance.EngineRegistry.ActiveEngineName = engineName;

        // Apply floating icon
        if (App.Instance.TextSelectionMonitor != null)
            App.Instance.TextSelectionMonitor.IsEnabled = settings.ShowFloatingIcon;

        _settingsService.Save();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
