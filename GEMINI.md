# XTranslate — Project Summary

## Overview
XTranslate is a modern, lightweight desktop translation tool for Windows, inspired by QTranslate. Built with WPF (.NET 8) and MVVM architecture with DI container.

## Tech Stack
- **Framework**: WPF, .NET 8, C#
- **Architecture**: MVVM + DI (Microsoft.Extensions.DependencyInjection) + Universe Architecture
- **Hotkey Engine**: Global Keyboard Hooks (`SetWindowsHookEx`)
- **Text Capture**: Clipboard automation via `SendInput` (~100ms)
- **OCR**: Windows.Media.Ocr (built-in Windows 10+ API)
- **System Tray**: `Hardcodet.NotifyIcon.Wpf`

## Project Structure
```
XTranslate/
├── Core/                 # DI infrastructure, interfaces
│   ├── Interfaces/       # ISettingsService, IClipboardService, IHotkeyService, IOcrEngine
│   ├── ServiceCollectionExtensions.cs  # DI registration
│   └── AppOrchestrator.cs              # App lifecycle & hotkey management
├── Models/               # Data models (Language, TranslationResult, AppSettings)
├── ViewModels/           # MVVM ViewModels (MainViewModel, PopupViewModel)
├── Views/                # XAML windows (PopupWindow, FloatingIcon, ScreenCaptureOverlay)
├── Services/             # Core services
│   ├── Translation/      # TranslationEngineRegistry, GoogleTranslateEngine, MyMemory
│   ├── WindowsOcrEngine  # Built-in Windows OCR
│   ├── ScreenCaptureService  # Region capture for OCR
│   ├── HotkeyService     # Global hotkey registration
│   ├── ClipboardService  # Text capture from other apps
│   ├── TextSelectionMonitor # Mouse selection detection
│   └── SettingsService   # User preferences persistence
├── Helpers/              # Utilities (LanguageDatabase, RelayCommand)
├── Native/               # Win32 P/Invoke (NativeMethods)
├── Themes/               # DarkTheme.xaml ResourceDictionary
├── Assets/               # App icon
├── MainWindow.xaml       # Main translation window
└── App.xaml              # DI container setup, startup logic (68 lines)
```

## Key Features
- **Ctrl+Q**: Floating popup translation near cursor
- **Ctrl+Enter**: Full main translation window
- **Ctrl+Shift+Q**: OCR — capture screen region → recognize text → translate
- **Auto-Language Flip**: Detects source = target and auto-switches
- **Language Swap**: One-click Source ↔ Target swap
- **Dark Mode**: Glassmorphism UI, zero eye strain
- **Multi-Engine**: Google Translate, MyMemory (extensible)
- **OCR**: Windows.Media.Ocr built-in, no external dependencies

## Build & Deployment
```powershell
# Run build script to generate both variants
.\build.ps1
```
- **Lightweight** (~5MB): Requires .NET 8 Desktop Runtime
- **Standalone** (~68MB): Fully self-contained, no dependencies

Output: `PublishOutput/Lightweight/` and `PublishOutput/Standalone/`

## Architecture
Follows **Universe Architecture** pattern:
- **Core (Interfaces)**: `ITranslationEngine`, `IOcrEngine`, `ISettingsService`, `IClipboardService`, `IHotkeyService`
- **Infrastructure**: DI Container (`IServiceCollection`), AppOrchestrator, DarkTheme
- **Registry**: `TranslationEngineRegistry` — auto-register engines
- **Services**: Implementations behind interfaces, swappable

## GitHub Release
- **Tag**: v1.0.0
- **URL**: https://github.com/kzxl/XTranslate/releases/tag/v1.0.0
- **Assets**: Lightweight (.zip ~0.3MB) + Standalone (.zip ~63MB)
