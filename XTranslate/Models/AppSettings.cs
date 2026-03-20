using System.Windows.Forms;

namespace XTranslate.Models;

/// <summary>
/// Application settings, persisted to JSON.
/// </summary>
public class AppSettings
{
    // --- Hotkeys ---
    public Keys TranslateHotkey { get; set; } = Keys.Control | Keys.Q;
    public Keys TranslateMainWindowHotkey { get; set; } = Keys.Control | Keys.Enter;
    
    // --- Languages ---
    public string DefaultSourceLanguage { get; set; } = "auto";
    public string DefaultTargetLanguage { get; set; } = "vi";

    // --- Behavior ---
    public bool StartMinimized { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool ShowPopupOnHotkey { get; set; } = true;
    public bool ShowFloatingIcon { get; set; } = true;
    public bool MouseModeRequiresCtrl { get; set; } = false;
    public int PopupAutoCloseSeconds { get; set; } = 5;

    // --- Engine ---
    public string ActiveEngineName { get; set; } = "Google Translate";

    // --- Window State ---
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public double WindowWidth { get; set; } = 900;
    public double WindowHeight { get; set; } = 560;
}
