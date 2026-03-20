using System.ComponentModel;
using System.Windows;
using XTranslate.ViewModels;

namespace XTranslate;

/// <summary>
/// Main translation window.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // Minimize to tray instead of closing
        if (App.Instance.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && App.Instance.Settings.MinimizeToTray)
        {
            Hide();
            WindowState = WindowState.Normal;
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new Views.SettingsWindow(App.Instance.SettingsService)
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
    }
}