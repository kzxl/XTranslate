using System.Windows;
using XTranslate.Helpers;
using XTranslate.Services;

namespace XTranslate.Views;

/// <summary>
/// Settings dialog with General, Behavior, and Advanced sections.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;

    public SettingsWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Settings;

        // Target language
        cboTargetLang.ItemsSource = LanguageDatabase.TargetLanguages;
        cboTargetLang.SelectedItem = LanguageDatabase.FindByCode(settings.DefaultTargetLanguage)
                                     ?? LanguageDatabase.FindByCode("vi");

        // Behavior
        chkMinimizeToTray.IsChecked = settings.MinimizeToTray;
        chkStartMinimized.IsChecked = settings.StartMinimized;
        chkShowFloatingIcon.IsChecked = settings.ShowFloatingIcon;

        // Advanced - Engine
        var registry = App.Instance.EngineRegistry;
        cboEngine.ItemsSource = registry.AvailableEngines;
        cboEngine.SelectedItem = registry.ActiveEngineName;

        // Advanced - Auto-start
        chkStartWithWindows.IsChecked = settings.StartWithWindows;
        txtPopupDelay.Text = settings.PopupAutoCloseSeconds.ToString();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Settings;

        if (cboTargetLang.SelectedItem is Models.Language lang)
            settings.DefaultTargetLanguage = lang.Code;

        settings.MinimizeToTray = chkMinimizeToTray.IsChecked ?? true;
        settings.StartMinimized = chkStartMinimized.IsChecked ?? false;
        settings.ShowFloatingIcon = chkShowFloatingIcon.IsChecked ?? true;
        settings.StartWithWindows = chkStartWithWindows.IsChecked ?? false;

        if (int.TryParse(txtPopupDelay.Text, out var delay) && delay >= 0)
            settings.PopupAutoCloseSeconds = delay;

        // Update engine
        if (cboEngine.SelectedItem is string engineName)
            App.Instance.EngineRegistry.ActiveEngineName = engineName;

        // Apply floating icon toggle
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
