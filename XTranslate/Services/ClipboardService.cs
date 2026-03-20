using System.Runtime.InteropServices;
using System.Windows;
using XTranslate.Native;

namespace XTranslate.Services;

/// <summary>
/// Gets selected text from the foreground application by simulating Ctrl+C.
/// Saves and restores the clipboard content.
/// </summary>
public class ClipboardService
{
    /// <summary>
    /// Captures the currently selected text in the foreground window.
    /// </summary>
    public async Task<string> GetSelectedTextAsync()
    {
        // Save current clipboard content
        string? previousText = null;

        await RunOnSTAThread(() =>
        {
            if (Clipboard.ContainsText())
                previousText = Clipboard.GetText();
            Clipboard.Clear();
        });

        // 1. Wait until user releases modifiers (Ctrl, Shift, Alt, Windows) to prevent stuck keys. Max wait: 1 second
        int waitLoops = 0;
        while (IsModifierPressed() && waitLoops < 20)
        {
            await Task.Delay(50);
            waitLoops++;
        }

        // 2. Simulate Ctrl+C
        SimulateCtrlC();

        // 3. Retry loop to wait for the target application to populate the clipboard
        string selectedText = "";
        for (int i = 0; i < 20; i++) // Max wait: 1 second
        {
            await Task.Delay(50);
            await RunOnSTAThread(() =>
            {
                if (Clipboard.ContainsText())
                    selectedText = Clipboard.GetText();
            });

            if (!string.IsNullOrEmpty(selectedText))
                break;
        }

        // 4. Restore previous clipboard content
        await RunOnSTAThread(() =>
        {
            // Only restore if we didn't just copy the exact same string
            if (previousText != null && previousText != selectedText)
                Clipboard.SetText(previousText);
            else if (previousText == null)
                Clipboard.Clear();
        });

        return selectedText;
    }

    private static bool IsModifierPressed()
    {
        return IsKeyPressed(NativeMethods.VK_CONTROL) ||
               IsKeyPressed(NativeMethods.VK_SHIFT) ||
               IsKeyPressed(NativeMethods.VK_MENU) ||
               IsKeyPressed(NativeMethods.VK_LWIN) ||
               IsKeyPressed(NativeMethods.VK_RWIN);
    }

    private static bool IsKeyPressed(ushort vk)
    {
        return (NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0;
    }

    private static void SimulateCtrlC()
    {
        var inputs = new NativeMethods.INPUT[]
        {
            // Release any pressed modifiers just in case
            CreateKeyInput(NativeMethods.VK_CONTROL, true),
            CreateKeyInput((ushort)System.Windows.Forms.Keys.ShiftKey, true),
            CreateKeyInput((ushort)System.Windows.Forms.Keys.Enter, true),
            
            // Ctrl down
            CreateKeyInput(NativeMethods.VK_CONTROL, false),
            // C down
            CreateKeyInput(NativeMethods.VK_C, false),
            // C up
            CreateKeyInput(NativeMethods.VK_C, true),
            // Ctrl up
            CreateKeyInput(NativeMethods.VK_CONTROL, true)
        };

        NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
    }

    private static NativeMethods.INPUT CreateKeyInput(ushort vk, bool keyUp)
    {
        return new NativeMethods.INPUT
        {
            Type = NativeMethods.INPUT_KEYBOARD,
            Union = new NativeMethods.INPUTUNION
            {
                Keyboard = new NativeMethods.KEYBDINPUT
                {
                    Vk = vk,
                    Flags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0
                }
            }
        };
    }

    private static Task RunOnSTAThread(Action action)
    {
        if (Application.Current?.Dispatcher != null)
        {
            return Application.Current.Dispatcher.InvokeAsync(action).Task;
        }
        
        var tcs = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            action();
            tcs.SetResult();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }
}
