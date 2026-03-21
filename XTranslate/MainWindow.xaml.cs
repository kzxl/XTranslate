using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using XTranslate.Core;
using XTranslate.Core.Interfaces;
using XTranslate.Services;
using XTranslate.ViewModels;
using XTranslate.Views;

namespace XTranslate;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Return && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            if (!string.IsNullOrWhiteSpace(ViewModel.SourceText) && !ViewModel.IsTranslating)
                await ViewModel.TranslateAsync();
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        var settings = App.Services.GetRequiredService<ISettingsService>();
        if (settings.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        var settings = App.Services.GetRequiredService<ISettingsService>();
        if (WindowState == WindowState.Minimized && settings.Settings.MinimizeToTray)
        {
            Hide();
            WindowState = WindowState.Normal;
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsService = App.Services.GetRequiredService<ISettingsService>();
        var registry = App.Services.GetRequiredService<TranslationEngineRegistry>();
        var ocrEngine = App.Services.GetRequiredService<IOcrEngine>();
        var settingsWindow = new SettingsWindow(settingsService, registry, ocrEngine) { Owner = this };
        if (settingsWindow.ShowDialog() == true)
        {
            var orchestrator = App.Services.GetRequiredService<AppOrchestrator>();
            orchestrator.ReRegisterHotkey();
        }
    }

    private async void OcrButton_Click(object sender, RoutedEventArgs e)
    {
        var ocrEngine = App.Services.GetRequiredService<IOcrEngine>();
        if (!ocrEngine.IsAvailable)
        {
            MessageBox.Show("OCR không khả dụng trên hệ thống này.",
                "XTranslate — OCR", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Ẩn main window để chụp màn hình sạch
        var wasVisible = IsVisible;
        if (wasVisible) Hide();

        // Delay nhỏ để window ẩn hoàn toàn
        await Task.Delay(200);

        try
        {
            var captureService = App.Services.GetRequiredService<ScreenCaptureService>();
            var bitmap = captureService.CaptureRegion();

            // Hiện lại main window trước
            if (wasVisible) Show();

            if (bitmap == null) return;

            Console.WriteLine($"[OCR-MainForm] Captured {bitmap.PixelWidth}x{bitmap.PixelHeight}");

            ViewModel.StatusText = "Đang nhận dạng văn bản (OCR)...";
            var text = await ocrEngine.RecognizeAsync(bitmap);
            Console.WriteLine($"[OCR-MainForm] Recognized: '{text}'");

            if (!string.IsNullOrWhiteSpace(text))
            {
                // Đổ text vào main form SourceText → translate 
                ViewModel.SourceText = text.Trim();
                await ViewModel.TranslateAsync();
            }
            else
            {
                ViewModel.StatusText = "OCR không nhận dạng được văn bản nào.";
            }
        }
        catch (Exception ex)
        {
            if (wasVisible && !IsVisible) Show();
            Console.WriteLine($"[OCR-MainForm] Error: {ex}");
            ViewModel.StatusText = $"OCR lỗi: {ex.Message}";
        }
    }
}