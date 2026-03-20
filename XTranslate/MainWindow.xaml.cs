using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using XTranslate.ViewModels;

namespace XTranslate;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>
    /// Ctrl+Enter: call translate directly, bypassing command CanExecute.
    /// </summary>
    private async void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Return && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;

            if (!string.IsNullOrWhiteSpace(ViewModel.SourceText) && !ViewModel.IsTranslating)
            {
                await ViewModel.TranslateAsync();
            }
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (App.Instance.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == System.Windows.WindowState.Minimized && App.Instance.Settings.MinimizeToTray)
        {
            Hide();
            WindowState = System.Windows.WindowState.Normal;
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new Views.SettingsWindow(App.Instance.SettingsService)
        {
            Owner = this
        };
        if (settingsWindow.ShowDialog() == true)
        {
            // Re-register hotkey if changed
            App.Instance.ReRegisterHotkey();
        }
    }
}