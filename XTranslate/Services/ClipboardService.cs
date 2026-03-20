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
        string? previousText = null;

        await RunOnSTAThread(() =>
        {
            if (Clipboard.ContainsText())
                previousText = Clipboard.GetText();
            Clipboard.Clear();
        });

        // 1. Gửi lệnh Copy mạnh mẽ nhất của WinForms
        // Không quan tâm state bàn phím hiện tại, thư viện này tự route message qua OS pipe rất chuẩn.
        await RunOnSTAThread(() =>
        {
            try 
            {
                System.Windows.Forms.SendKeys.Flush();
                System.Windows.Forms.SendKeys.SendWait("^c");
                System.Windows.Forms.SendKeys.Flush();
            } 
            catch { }
        });

        // 2. Chờ dữ liệu vào Clipboard (max 20 lần = ~ 600ms)
        string selectedText = "";
        for (int i = 0; i < 20; i++) 
        {
            await Task.Delay(30);
            await RunOnSTAThread(() =>
            {
                if (Clipboard.ContainsText())
                    selectedText = Clipboard.GetText();
            });

            if (!string.IsNullOrEmpty(selectedText))
                break;
        }

        // 3. Khôi phục lại dữ liệu nếu có
        await RunOnSTAThread(() =>
        {
            if (previousText != null && previousText != selectedText)
                Clipboard.SetText(previousText);
            else if (previousText == null)
                Clipboard.Clear();
        });

        return selectedText;
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
