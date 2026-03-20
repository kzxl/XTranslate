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
    private bool _canClose;

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
            var left = (double)point.X + 15;
            var top = (double)point.Y + 25; // Cách mũi tên chuột một đoạn xuống dưới để né đoạn text đang bôi đen

            // Nếu popup bị vượt qua mép phải màn hình
            if (left + ActualWidth > screen.Right)
                left = screen.Right - ActualWidth - 10;
                
            // Nếu popup bị vượt qua mép dưới màn hình (đáy), đảo nó lên phía TRÊN con trỏ chuột
            if (top + ActualHeight > screen.Bottom)
                top = point.Y - ActualHeight - 15;

            Left = Math.Max(0, left);
            Top = Math.Max(0, top);
        }

        // Focus for keyboard input (Esc to close)
        Activate();
        Focus();
        
        // Prevent auto-closing immediately due to initial focus glitches
        Task.Delay(200).ContinueWith(_ => _canClose = true);
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // Auto-close when popup loses focus, but only after initial delay
        if (_canClose)
        {
            Close();
        }
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
