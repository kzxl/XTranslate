using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using XTranslate.Native;

namespace XTranslate.Services;

/// <summary>
/// Monitors mouse activity to detect text selection across all applications.
/// When a mouse-up is detected after a drag, checks clipboard for selected text
/// and fires TextSelected event to show a floating translate icon.
/// </summary>
public class TextSelectionMonitor : IDisposable
{
    public event Action<string, int, int>? TextSelected; // text, cursorX, cursorY
    public event Action? SelectionCleared;

    private IntPtr _mouseHookId = IntPtr.Zero;
    private NativeMethods.LowLevelMouseProc? _mouseProc;
    private bool _isMouseDown;
    private readonly DispatcherTimer _debounceTimer;
    private bool _disposed;

    public bool IsEnabled { get; set; } = true;

    public TextSelectionMonitor()
    {
        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _debounceTimer.Tick += OnDebounceTimerTick;
    }

    public void Start()
    {
        if (_mouseHookId != IntPtr.Zero) return;

        _mouseProc = MouseHookCallback;
        _mouseHookId = SetMouseHook(_mouseProc);
    }

    public void Stop()
    {
        if (_mouseHookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }
    }

    private IntPtr SetMouseHook(NativeMethods.LowLevelMouseProc proc)
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        return NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL, proc,
            NativeMethods.GetModuleHandle(module.ModuleName), 0);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && IsEnabled)
        {
            int msg = wParam.ToInt32();

            if (msg == NativeMethods.WM_LBUTTONDOWN)
            {
                _isMouseDown = true;
                _debounceTimer.Stop();
                SelectionCleared?.Invoke();
            }
            else if (msg == NativeMethods.WM_LBUTTONUP && _isMouseDown)
            {
                _isMouseDown = false;
                // Debounce: wait a moment before checking selection
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private async void OnDebounceTimerTick(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();

        if (!IsEnabled) return;

        try
        {
            // Get cursor position
            if (!NativeMethods.GetCursorPos(out var point))
                return;

            // Try to get selected text via Ctrl+C
            var clipboardService = new ClipboardService();
            var text = await clipboardService.GetSelectedTextAsync();

            if (!string.IsNullOrWhiteSpace(text) && text.Length >= 1 && text.Length <= 5000)
            {
                TextSelected?.Invoke(text.Trim(), point.X, point.Y);
            }
        }
        catch
        {
            // Silently fail
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _debounceTimer.Stop();
        Stop();
    }
}
