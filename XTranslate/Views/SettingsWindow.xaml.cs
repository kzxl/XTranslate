using System.Windows;
using XTranslate.Helpers;
using XTranslate.Services;

namespace XTranslate.Views;

/// <summary>
/// Settings dialog.
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

        // Populate target language combo
        cboTargetLang.ItemsSource = LanguageDatabase.TargetLanguages;
        cboTargetLang.SelectedItem = LanguageDatabase.FindByCode(settings.DefaultTargetLanguage)
                                     ?? LanguageDatabase.FindByCode("vi");

        chkMinimizeToTray.IsChecked = settings.MinimizeToTray;
        chkStartMinimized.IsChecked = settings.StartMinimized;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.Settings;

        if (cboTargetLang.SelectedItem is Models.Language lang)
            settings.DefaultTargetLanguage = lang.Code;

        settings.MinimizeToTray = chkMinimizeToTray.IsChecked ?? true;
        settings.StartMinimized = chkStartMinimized.IsChecked ?? false;

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
