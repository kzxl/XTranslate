using System.Diagnostics;
using System.Windows;
using XTranslate.Core.Interfaces;
using XTranslate.Native;

namespace XTranslate.Services;

/// <summary>
/// Captures selected text from any application using clipboard automation.
/// </summary>
public class ClipboardService : IClipboardService
{
    /// <summary>
    /// Captures the currently selected text by simulating Ctrl+C.
    /// </summary>
    public async Task<string?> GetSelectedTextAsync()
    {
        string? original = null;

        try
        {
            // Save current clipboard content
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try { original = Clipboard.GetText(); }
                catch { original = null; }
            });

            // Clear clipboard
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try { Clipboard.Clear(); }
                catch { /* ignore */ }
            });

            // Simulate Ctrl+C
            NativeMethods.SendCtrlC();
            await Task.Delay(100);

            // Read clipboard
            string? text = null;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try { text = Clipboard.GetText(); }
                catch { text = null; }
            });

            // Restore original clipboard
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(original))
                        Clipboard.SetText(original);
                    else
                        Clipboard.Clear();
                }
                catch { /* ignore */ }
            });

            return text;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClipboardService] Error: {ex.Message}");
            return null;
        }
    }
}
