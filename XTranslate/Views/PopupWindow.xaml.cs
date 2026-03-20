using System.Windows;
using System.Windows.Input;
using XTranslate.Native;
using XTranslate.ViewModels;

namespace XTranslate.Views;

/// <summary>
/// Translation popup that appears near the cursor.
/// </summary>
public partial class PopupWindow : Window
{
    public PopupWindow(PopupViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Position near cursor
        if (NativeMethods.GetCursorPos(out var point))
        {
            // Ensure popup doesn't go off-screen
            var screen = SystemParameters.WorkArea;
            var left = (double)point.X + 10;
            var top = (double)point.Y + 10;

            if (left + ActualWidth > screen.Right)
                left = screen.Right - ActualWidth - 10;
            if (top + ActualHeight > screen.Bottom)
                top = point.Y - ActualHeight - 10;

            Left = Math.Max(0, left);
            Top = Math.Max(0, top);
        }

        // Focus for keyboard input (Esc to close)
        Focus();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // Auto-close when popup loses focus
        Close();
    }

    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}
