using System.Windows;
using XTranslate.Core.Interfaces;
using XTranslate.Native;

namespace XTranslate.Services;

/// <summary>
/// Captures selected text from any application using clipboard automation.
/// Uses adaptive polling on the clipboard sequence number instead of fixed
/// delays, so capture completes as soon as the target app responds to Ctrl+C
/// (typically 20-80ms) rather than always waiting ~200ms.
/// </summary>
public class ClipboardService : IClipboardService
{
    // Tunables: total wait is bounded by MaxWaitMs; we poll every PollIntervalMs.
    private const int MaxWaitMs = 400;
    private const int PollIntervalMs = 15;
    private const int SettleDelayMs = 10;

    /// <summary>
    /// Captures the currently selected text by simulating Ctrl+C.
    /// </summary>
    public async Task<string?> GetSelectedTextAsync()
    {
        var dispatcher = Application.Current.Dispatcher;

        try
        {
            // Snapshot original clipboard text and the current sequence number,
            // then clear, in a single dispatcher hop (clipboard APIs need STA).
            var (original, seqBefore) = await dispatcher.InvokeAsync(() =>
            {
                string? orig = null;
                try { orig = Clipboard.GetText(); }
                catch { orig = null; }

                uint seq = NativeMethods.GetClipboardSequenceNumber();

                try { Clipboard.Clear(); }
                catch { /* ignore */ }

                return (orig, seq);
            });

            // Let the Clear settle, then simulate Ctrl+C to the foreground window.
            // Run on a background thread: SendCtrlC may Thread.Sleep while releasing
            // held modifier keys, and we must not block the UI thread.
            await Task.Delay(SettleDelayMs);
            await Task.Run(NativeMethods.SendCtrlC);

            // Poll until the clipboard sequence number advances past the snapshot
            // (the Clear already bumped it once, so wait for a further change),
            // or until we hit the max wait.
            string? text = null;
            int waited = 0;
            while (waited < MaxWaitMs)
            {
                await Task.Delay(PollIntervalMs);
                waited += PollIntervalMs;

                var (changed, current) = await dispatcher.InvokeAsync(() =>
                {
                    uint seq = NativeMethods.GetClipboardSequenceNumber();
                    if (seq == seqBefore)
                        return (false, (string?)null);

                    string? t = null;
                    try { t = Clipboard.GetText(); }
                    catch { t = null; }
                    return (true, t);
                });

                if (changed)
                {
                    text = current;
                    // A non-empty result means the copy succeeded; stop early.
                    if (!string.IsNullOrEmpty(text))
                        break;
                }
            }

            Log.Debug(() => $"[Clipboard] Got ({waited}ms): '{Truncate(text, 50)}'");

            // Restore original clipboard content.
            await dispatcher.InvokeAsync(() =>
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
            Log.Error($"ClipboardService: {ex.Message}");
            return null;
        }
    }

    private static string Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? "" : value.Substring(0, Math.Min(value.Length, max));
}
