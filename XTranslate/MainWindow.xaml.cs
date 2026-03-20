using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using XTranslate.ViewModels;

namespace XTranslate;

/// <summary>
/// Main translation window.
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>
    /// Handle Ctrl+Enter at Window level via PreviewKeyDown.
    /// This works even when TextBox has focus (AcceptsReturn=True eats normal Enter).
    /// </summary>
    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Return && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (ViewModel.TranslateCommand.CanExecute(null))
            {
                ViewModel.TranslateCommand.Execute(null);
                e.Handled = true;
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
        settingsWindow.ShowDialog();
    }
}