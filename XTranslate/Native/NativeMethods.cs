using System.Runtime.InteropServices;

namespace XTranslate.Native;

/// <summary>
/// Minimal Win32 P/Invoke declarations needed for XTranslate.
/// </summary>
internal static partial class NativeMethods
{
    // --- Hotkey Registration ---

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    // --- Foreground Window ---

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(IntPtr hWnd);

    // --- Cursor Position ---

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    // --- Keyboard Input Simulation (for Ctrl+C) ---

    [LibraryImport("user32.dll")]
    public static partial short GetAsyncKeyState(int vKey);

    [LibraryImport("user32.dll")]
    public static partial uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    public const int INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const ushort VK_SHIFT = 0x10;
    public const ushort VK_CONTROL = 0x11;
    public const ushort VK_MENU = 0x12; // Alt
    public const ushort VK_LWIN = 0x5B;
    public const ushort VK_RWIN = 0x5C;
    public const ushort VK_C = 0x43;

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public int Type;
        public INPUTUNION Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION
    {
        [FieldOffset(0)]
        public KEYBDINPUT Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort Vk;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    // --- Window Messages ---
    public const int WM_HOTKEY = 0x0312;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP = 0x0202;

    // --- GDI Object Cleanup ---
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject(IntPtr hObject);

    // --- Low-level Mouse Hook ---
    public const int WH_MOUSE_LL = 14;

    public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    // --- Helper: Simulate Ctrl+C ---

    /// <summary>
    /// Simulate Ctrl+C keystroke to copy selected text to clipboard.
    /// Releases any held modifier keys first to avoid conflicts.
    /// </summary>
    public static void SendCtrlC()
    {
        // Release modifier keys that might be held (from hotkey)
        var releaseInputs = new List<INPUT>();
        if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
            releaseInputs.Add(MakeKeyInput(VK_CONTROL, KEYEVENTF_KEYUP));
        if ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0)
            releaseInputs.Add(MakeKeyInput(VK_SHIFT, KEYEVENTF_KEYUP));
        if ((GetAsyncKeyState(VK_MENU) & 0x8000) != 0)
            releaseInputs.Add(MakeKeyInput(VK_MENU, KEYEVENTF_KEYUP));

        if (releaseInputs.Count > 0)
        {
            SendInput((uint)releaseInputs.Count, releaseInputs.ToArray(), Marshal.SizeOf<INPUT>());
            Thread.Sleep(30);
        }

        // Send Ctrl+C
        var inputs = new INPUT[]
        {
            MakeKeyInput(VK_CONTROL, 0),
            MakeKeyInput(VK_C, 0),
            MakeKeyInput(VK_C, KEYEVENTF_KEYUP),
            MakeKeyInput(VK_CONTROL, KEYEVENTF_KEYUP),
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static INPUT MakeKeyInput(ushort vk, uint flags) => new()
    {
        Type = INPUT_KEYBOARD,
        Union = new INPUTUNION
        {
            Keyboard = new KEYBDINPUT { Vk = vk, Flags = flags }
        }
    };
}
