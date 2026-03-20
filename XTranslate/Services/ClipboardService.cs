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

        // Small delay for stability
        await Task.Delay(50);

        // Simulate Ctrl+C
        SimulateCtrlC();

        // Wait for clipboard to be populated
        await Task.Delay(150);

        // Read new clipboard content
        string selectedText = "";
        await RunOnSTAThread(() =>
        {
            if (Clipboard.ContainsText())
                selectedText = Clipboard.GetText();
        });

        // Restore previous clipboard content
        await RunOnSTAThread(() =>
        {
            if (previousText != null)
                Clipboard.SetText(previousText);
            else
                Clipboard.Clear();
        });

        return selectedText;
    }

    private static void SimulateCtrlC()
    {
        var inputs = new NativeMethods.INPUT[]
        {
            // Ctrl down
            CreateKeyInput(NativeMethods.VK_CONTROL, false),
            // C down
            CreateKeyInput(NativeMethods.VK_C, false),
            // C up
            CreateKeyInput(NativeMethods.VK_C, true),
            // Ctrl up
            CreateKeyInput(NativeMethods.VK_CONTROL, true)
        };

        NativeMethods.SendInput((uint)inputs.Length, inputs,
            Marshal.SizeOf(typeof(NativeMethods.INPUT)));
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
