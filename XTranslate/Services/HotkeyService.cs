using System.Windows.Forms;
using System.Windows.Interop;
using XTranslate.Native;

namespace XTranslate.Services;

/// <summary>
/// Manages global hotkey registration via Win32 RegisterHotKey/UnregisterHotKey.
/// Uses HwndSource directly as message sink (more reliable than hidden WPF Window).
/// </summary>
public class HotkeyService : IDisposable
{
    public event Action? HotkeyPressed;

    private HwndSource? _hwndSource;
    private bool _isRegistered;
    private const int HotkeyId = 9000;

    public bool IsRegistered => _isRegistered;

    public HotkeyService()
    {
        // Create a message-only window via HwndSource — no WPF Window needed
        var parameters = new HwndSourceParameters("XTranslateHotkeyMsgSink")
        {
            Width = 0,
            Height = 0,
            PositionX = -100,
            PositionY = -100,
            WindowStyle = 0 // no visible style
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    public bool RegisterHotkey(Keys hotkey)
    {
        UnregisterHotkey();

        if (_hwndSource == null) return false;

        var modifiers = GetModifiers(hotkey);
        var vk = (uint)(hotkey & Keys.KeyCode);

        _isRegistered = NativeMethods.RegisterHotKey(_hwndSource.Handle, HotkeyId, modifiers, vk);
        return _isRegistered;
    }

    public void UnregisterHotkey()
    {
        if (_isRegistered && _hwndSource != null)
        {
            NativeMethods.UnregisterHotKey(_hwndSource.Handle, HotkeyId);
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
        _hwndSource?.Dispose();
        _hwndSource = null;
    }
}
