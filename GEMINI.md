# XTranslate — Project Summary

## Overview
XTranslate is a modern, lightweight desktop translation tool for Windows, inspired by QTranslate. Built with WPF (.NET 8) and MVVM architecture.

## Tech Stack
- **Framework**: WPF, .NET 8, C#
- **Architecture**: MVVM + Dependency Injection (Microsoft.Extensions.DependencyInjection)
- **Hotkey Engine**: Global Keyboard Hooks (`SetWindowsHookEx`)
- **Text Capture**: Clipboard automation via `SendKeys` (~10ms)
- **System Tray**: `Hardcodet.NotifyIcon.Wpf`

## Project Structure
```
XTranslate/
├── Models/           # Data models (Language, TranslationResult, AppSettings)
├── ViewModels/       # MVVM ViewModels (MainViewModel, PopupViewModel)
├── Views/            # XAML windows (PopupWindow, FloatingIcon)
├── Services/         # Core services
│   ├── TranslationEngine/  # Translation engine abstraction + MyMemory impl
│   ├── HotkeyService       # Global hotkey registration
│   ├── ClipboardService    # Text capture from other apps
│   ├── TextSelectionMonitor # Mouse selection detection
│   └── SettingsService     # User preferences persistence
├── Helpers/          # Utilities (LanguageDatabase, RelayCommand)
├── Assets/           # App icon
├── MainWindow.xaml   # Main translation window
└── App.xaml          # DI container setup, startup logic
```

## Key Features
- **Ctrl+Q**: Floating popup translation near cursor
- **Ctrl+Enter**: Full main translation window
- **Auto-Language Flip**: Detects source = target and auto-switches
- **Language Swap**: One-click Source ↔ Target swap
- **Dark Mode**: Glassmorphism UI, zero eye strain
- **Multi-Engine**: MyMemory (extensible for Google, Bing, DeepL)

## Build & Deployment
```powershell
# Run build script to generate both variants
.\build.ps1
```
- **Lightweight** (~5MB): Requires .NET 8 Desktop Runtime
- **Standalone** (~68MB): Fully self-contained, no dependencies

Output: `PublishOutput/Lightweight/` and `PublishOutput/Standalone/`

## GitHub Release
- **Tag**: v1.0.0
- **URL**: https://github.com/kzxl/XTranslate/releases/tag/v1.0.0
- **Assets**: Lightweight (.zip ~0.3MB) + Standalone (.zip ~63MB)
