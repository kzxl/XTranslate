using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using XTranslate.Native;

namespace XTranslate.Services;

/// <summary>
/// Manages global hotkey registration via Win32 RegisterHotKey/UnregisterHotKey.
/// Uses a hidden WPF window as message sink.
/// </summary>
public class HotkeyService : IDisposable
{
    public event Action? HotkeyPressed;

    private readonly Window _messageWindow;
    private IntPtr _windowHandle;
    private HwndSource? _hwndSource;
    private bool _isRegistered;
    private const int HotkeyId = 9000;

    public HotkeyService()
    {
        // Create a hidden window to receive WM_HOTKEY messages
        _messageWindow = new Window
        {
            Width = 0,
            Height = 0,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None,
            Visibility = Visibility.Hidden
        };
        _messageWindow.SourceInitialized += (_, _) =>
        {
            _windowHandle = new WindowInteropHelper(_messageWindow).Handle;
            _hwndSource = HwndSource.FromHwnd(_windowHandle);
            _hwndSource?.AddHook(WndProc);
        };
        _messageWindow.Show();
        _messageWindow.Hide();
    }

    public bool RegisterHotkey(Keys hotkey)
    {
        UnregisterHotkey();

        var modifiers = GetModifiers(hotkey);
        var vk = (uint)(hotkey & Keys.KeyCode);

        _isRegistered = NativeMethods.RegisterHotKey(_windowHandle, HotkeyId, modifiers, vk);
        return _isRegistered;
    }

    public void UnregisterHotkey()
    {
        if (_isRegistered && _windowHandle != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(_windowHandle, HotkeyId);
            _isRegistered = false;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private static uint GetModifiers(Keys hotkey)
    {
        uint modifiers = 0;
        if (hotkey.HasFlag(Keys.Alt)) modifiers |= 0x0001;      // MOD_ALT
        if (hotkey.HasFlag(Keys.Control)) modifiers |= 0x0002;   // MOD_CONTROL
        if (hotkey.HasFlag(Keys.Shift)) modifiers |= 0x0004;     // MOD_SHIFT
        return modifiers;
    }

    public void Dispose()
    {
        UnregisterHotkey();
        _hwndSource?.RemoveHook(WndProc);
        _messageWindow.Close();
    }
}
