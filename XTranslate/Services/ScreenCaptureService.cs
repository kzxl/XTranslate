using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XTranslate.Services;

/// <summary>
/// Service for capturing a screen region selected by the user.
/// Shows a fullscreen overlay where user draws a rectangle.
/// </summary>
public class ScreenCaptureService
{
    /// <summary>
    /// Capture a region of the screen selected by the user.
    /// Returns null if the user cancels.
    /// </summary>
    public BitmapSource? CaptureRegion()
    {
        // First, capture the entire screen
        var screenBitmap = CaptureFullScreen();
        if (screenBitmap == null) return null;

        // Show overlay for user to select region
        var overlay = new Views.ScreenCaptureOverlay(screenBitmap);
        var result = overlay.ShowDialog();

        if (result != true || overlay.SelectedRegion == Rect.Empty)
            return null;

        // Crop the selected region
        var region = overlay.SelectedRegion;
        var dpiX = screenBitmap.DpiX;
        var dpiY = screenBitmap.DpiY;

        // Convert from device-independent to pixel coordinates
        var pixelX = (int)(region.X * dpiX / 96.0);
        var pixelY = (int)(region.Y * dpiY / 96.0);
        var pixelWidth = (int)(region.Width * dpiX / 96.0);
        var pixelHeight = (int)(region.Height * dpiY / 96.0);

        // Clamp
        pixelX = Math.Max(0, pixelX);
        pixelY = Math.Max(0, pixelY);
        pixelWidth = Math.Min(pixelWidth, screenBitmap.PixelWidth - pixelX);
        pixelHeight = Math.Min(pixelHeight, screenBitmap.PixelHeight - pixelY);

        if (pixelWidth <= 0 || pixelHeight <= 0)
            return null;

        return new CroppedBitmap(screenBitmap, new System.Windows.Int32Rect(pixelX, pixelY, pixelWidth, pixelHeight));
    }

    private static BitmapSource? CaptureFullScreen()
    {
        try
        {
            var screenWidth = (int)SystemParameters.VirtualScreenWidth;
            var screenHeight = (int)SystemParameters.VirtualScreenHeight;
            var screenLeft = (int)SystemParameters.VirtualScreenLeft;
            var screenTop = (int)SystemParameters.VirtualScreenTop;

            using var bmp = new System.Drawing.Bitmap(screenWidth, screenHeight);
            using var g = System.Drawing.Graphics.FromImage(bmp);
            g.CopyFromScreen(screenLeft, screenTop, 0, 0, new System.Drawing.Size(screenWidth, screenHeight));

            // Convert System.Drawing.Bitmap to WPF BitmapSource
            var handle = bmp.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    handle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                Native.NativeMethods.DeleteObject(handle);
            }
        }
        catch
        {
            return null;
        }
    }
}
