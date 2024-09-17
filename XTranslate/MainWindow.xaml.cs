using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;

namespace XTranslate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 1;
        private const int MOD_CTRL = 0x0002;
        private const int WH_MOUSE = 14;

        private IntPtr _hwnd;
        private IntPtr _hook;

        public MainWindow()
        {
            InitializeComponent();
            RegisterGlobalMouseHook();
            RegisterGlobalHotkey();
            _hwnd = new WindowInteropHelper(this).Handle;
        }

        protected override void OnSourceInitialized(EventArgs e)

        {

            base.OnSourceInitialized(e);
            try

            {
                HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
                source.AddHook(new System.Windows.Interop.HwndSourceHook(WindowHook));
            }

            catch (Exception ex)

            {

                // Handle the exception here

                // You can log the error, display an error message to the user, etc.

                Console.WriteLine("An EngineException occurred: " + ex.Message);

            }

        }
        private IntPtr WindowHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)

        {

            return WndProc(hwnd, msg, wParam, lParam);

        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)

        {

            if (msg == WM_HOTKEY)
            {
                ActivateFunction();
            }
            return IntPtr.Zero;

        }
        public delegate IntPtr HwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);

        private void RegisterGlobalHotkey()
        {
            RegisterHotKey(_hwnd, HOTKEY_ID, MOD_CTRL, 0);
        }

        private void UnregisterGlobalHotkey()
        {
            UnregisterHotKey(_hwnd, HOTKEY_ID);
        }

        private void RegisterGlobalMouseHook()
        {
            _hook = SetWindowsHookEx(WH_MOUSE, MouseHookProc, IntPtr.Zero, 0);
        }

        private void UnregisterGlobalMouseHook()
        {
            UnhookWindowsHookEx(_hook);
        }

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0) return CallNextHookEx(_hook, nCode, wParam, lParam);

            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Mouse.LeftButton != MouseButtonState.Pressed) return CallNextHookEx(_hook, nCode, wParam, lParam);

            var selectedText = GetSelectedText();
            if (string.IsNullOrEmpty(selectedText)) return CallNextHookEx(_hook, nCode, wParam, lParam);

            ActivateFunction();
            return CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        private string GetSelectedText()
        {
            var textSelection = (TextSelection)Clipboard.GetData("System.Windows.Forms.Text");
            return textSelection.Text;
        }

        private void ActivateFunction()
        {
            MessageBox.Show("Hotkey pressed!");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UnregisterGlobalHotkey();
            UnregisterGlobalMouseHook();
        }

        [DllImport("user32")]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }

}