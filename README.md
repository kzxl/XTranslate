# XTranslate ✦

XTranslate is a modern, lightweight, and blazing-fast desktop translation tool for Windows, inspired by QTranslate. Designed with WPF and .NET 8, it offers an exceptionally smooth translation experience right at your fingertips.

## 🚀 Key Features

*   **⚡ Instantly Translate Anywhere**: Highlight text in any application and press your configured hotkey to instantly view translations. No more copying and pasting into browser tabs!
*   **🖱️ Smart Floating Popup**: Press `Ctrl + Q` to open a sleek, transparent translation popup near your cursor. It automatically disappears when you click outside or returns focus to your work.
*   **🖥️ Detailed Main Window**: Press `Ctrl + Enter` to open a broader window for editing lengthy translations, checking character counts, and browsing complete translation outputs.
*   **🧠 Intelligent Auto-Language Flipper**: Uses advanced context awareness! If your Target Language is `Vietnamese` and you highlight a `Vietnamese` sentence, XTranslate will automatically flip the Target Language to `English` and translate seamlessly.
*   **🔀 Quick Language Swapping**: Integrated UI buttons let you reverse the Source and Target languages with a single click in both the Main and Popup windows.
*   **🌙 Dark Mode First**: Features a modern, aesthetically pleasing Dark UI with glassmorphism effects, ensuring zero eye strain during late-night coding or reading sessions.
*   **⚙️ Multi-Engine Support**: Built to support multiple translation engines. Currently powers translations via *MyMemory*, with an extensible architecture ready for Google, Bing, and DeepL integrations.

## ⌨️ Default Hotkeys

- `Ctrl + Q`: Activate Floating Popup Translation.
- `Ctrl + Enter`: Open Detailed Main Translation Window.
- `Esc`: Close open windows instantly.

## 📦 Deployment & Binaries

XTranslate is highly optimized and distributed in two deployment formats. 

Run the included `build.ps1` script to generate outputs:
- **Lightweight Build (~5MB)**: Ultra-fast executable. Requires the `.NET 8 Desktop Runtime` installed on your target machine.
- **Standalone Build (~68MB)**: Fully self-contained. Runs on any Windows machine immediately, without requiring users to download or install any framework.

Outputs are generated in `PublishOutput/Lightweight/` and `PublishOutput/Standalone/` respectively.

## 🛠️ Internal Architecture
Built with best practices using the MVVM pattern in C# and WPF Windows Presentation Foundation:
*   `Windows API`: Leverages Global Keyboard Hooks (`SetWindowsHookEx`) to listen for hotkeys globally across applications.
*   `SendKeys API`: Automates system clipboard interaction (`^c`) for blazing-fast 10ms text capturing from other active windows without blocking the UI thread.
*   `Dependency Injection`: Strictly follows SOLID principles with Autofac for service encapsulation.
