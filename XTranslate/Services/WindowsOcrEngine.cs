using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using XTranslate.Core.Interfaces;
using WinBitmapDecoder = Windows.Graphics.Imaging.BitmapDecoder;

namespace XTranslate.Services;

/// <summary>
/// OCR engine using Windows.Media.Ocr (built-in Windows 10+ API).
/// No external dependencies required.
/// </summary>
public class WindowsOcrEngine : IOcrEngine
{
    public bool IsAvailable => OcrEngine.AvailableRecognizerLanguages.Count > 0;

    public IReadOnlyList<string> AvailableLanguages =>
        OcrEngine.AvailableRecognizerLanguages
            .Select(l => l.LanguageTag)
            .ToList();

    public async Task<string> RecognizeAsync(BitmapSource image, string? languageTag = null)
    {
        try
        {
            OcrEngine? engine = null;
            if (!string.IsNullOrEmpty(languageTag))
            {
                var lang = new Windows.Globalization.Language(languageTag);
                if (OcrEngine.IsLanguageSupported(lang))
                    engine = OcrEngine.TryCreateFromLanguage(lang);
            }
            engine ??= OcrEngine.TryCreateFromUserProfileLanguages();

            if (engine == null)
            {
                Debug.WriteLine("[OCR] No OCR engine available.");
                return "";
            }

            var softwareBitmap = await ConvertToSoftwareBitmap(image);
            var result = await engine.RecognizeAsync(softwareBitmap);
            return result.Text;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[OCR] Error: {ex.Message}");
            return "";
        }
    }

    private static async Task<SoftwareBitmap> ConvertToSoftwareBitmap(BitmapSource source)
    {
        // Encode WPF BitmapSource to PNG in memory
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(source));

        using var memoryStream = new System.IO.MemoryStream();
        encoder.Save(memoryStream);
        var bytes = memoryStream.ToArray();

        // Create IRandomAccessStream from bytes
        var randomAccessStream = new InMemoryRandomAccessStream();
        await randomAccessStream.WriteAsync(bytes.AsBuffer());
        randomAccessStream.Seek(0);

        // Decode to SoftwareBitmap
        var decoder = await WinBitmapDecoder.CreateAsync(randomAccessStream);
        var bitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

        return bitmap;
    }
}
