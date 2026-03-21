using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using XTranslate.Native;

namespace XTranslate.Services;

/// <summary>
/// Monitors mouse activity to detect text selection.
/// When mouse-up after drag detected, shows floating icon at cursor position.
/// Does NOT capture clipboard — that happens only when user clicks the icon.
/// </summary>
public class TextSelectionMonitor : IDisposable
{
    /// <summary>Fired when user potentially selected text (mouse up after drag).</summary>
    public event Action<int, int>? PossibleSelection; // cursorX, cursorY

    /// <summary>Fired when selection is likely cleared (click without drag).</summary>
    public event Action? SelectionCleared;

    private IntPtr _mouseHookId = IntPtr.Zero;
    private NativeMethods.LowLevelMouseProc? _mouseProc;
    private bool _isMouseDown;
    private NativeMethods.POINT _mouseDownPoint;
    private readonly DispatcherTimer _debounceTimer;
    private bool _disposed;

    public bool IsEnabled { get; set; } = true;

    private const int MinDragDistance = 10; // pixels — must drag at least this far

    public TextSelectionMonitor()
    {
        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _debounceTimer.Tick += OnDebounceTimerTick;
    }

    public void Start()
    {
        if (_mouseHookId != IntPtr.Zero) return;

        _mouseProc = MouseHookCallback;
        _mouseHookId = SetMouseHook(_mouseProc);
        Console.WriteLine($"[XTranslate] TextSelectionMonitor started. Hook={_mouseHookId}");
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
                NativeMethods.GetCursorPos(out _mouseDownPoint);
                _debounceTimer.Stop();
                SelectionCleared?.Invoke();
            }
            else if (msg == NativeMethods.WM_LBUTTONUP && _isMouseDown)
            {
                _isMouseDown = false;

                // Check if mouse moved enough (was it a drag or just a click?)
                if (NativeMethods.GetCursorPos(out var upPoint))
                {
                    int dx = Math.Abs(upPoint.X - _mouseDownPoint.X);
                    int dy = Math.Abs(upPoint.Y - _mouseDownPoint.Y);

                    if (dx > MinDragDistance || dy > MinDragDistance)
                    {
                        // Likely text selection — debounce
                        _debounceTimer.Stop();
                        _debounceTimer.Start();
                    }
                }
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private void OnDebounceTimerTick(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();

        if (!IsEnabled) return;

        if (NativeMethods.GetCursorPos(out var point))
        {
            Console.WriteLine($"[XTranslate] Possible selection at ({point.X}, {point.Y})");
            PossibleSelection?.Invoke(point.X, point.Y);
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
