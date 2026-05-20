# xcrosshair

A minimalist, high-performance crosshair overlay for Windows built with C# and WPF.

## Project Overview

*   **Type:** C# WPF Application
*   **Target Framework:** .NET 8.0 (`net8.0-windows`)
*   **Purpose:** Provides a persistent, customizable crosshair overlay that remains on top of other windows and supports click-through functionality.
*   **Key Technologies:**
    *   **WPF:** Main UI and overlay rendering.
    *   **Win32 API (user32.dll):** Used for global hotkeys (`Home` key), window transparency (`WS_EX_LAYERED`, `WS_EX_TRANSPARENT`), and ensuring the window stays topmost and click-through.
    *   **System.Windows.Forms:** Used for screen/monitor detection and positioning.
    *   **System.Text.Json:** For settings persistence.

## Architecture

*   **`MainWindow.xaml / MainWindow.xaml.cs`:** The core of the application. Handles the overlay window, the settings UI (menu), global hotkey registration, and Win32 interop for window styling.
*   **`CrosshairSettings.cs`:** Manages user preferences. Now supports **multiple profiles**. Settings are persisted to `%AppData%\xcrosshair\settings.json`.
*   **`ValorantProfile.cs`:** Logic for parsing and applying crosshair settings from Valorant profile codes. Features an enhanced v2 parser with improved token mapping and RGBA-to-ARGB color conversion for accurate rendering.

## Features: Profiles

*   **Multiple Profiles:** Users can create, save, and switch between different crosshair configurations.
*   **Auto-Save:** Settings for the current profile are saved automatically when changed or when switching profiles.
*   **Clone/Save As:** Easily create a new profile based on current settings.

## Building and Running

*   **Build:** `dotnet build`
*   **Run:** `dotnet run`
*   **Publish (Standalone):** `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true`

## Development Conventions

*   **Win32 Interop:** Heavy reliance on `user32.dll` for window manipulation. P/Invoke declarations are located in `MainWindow.xaml.cs`.
*   **Settings:** Always use `CrosshairSettings.Load()` and `Save()` for persistence. Use `_settings.CurrentProfile` to access active crosshair settings.
*   **Error Handling:** Use `LogError` in `MainWindow.xaml.cs` which writes to `crash.log` in the application directory.
*   **UI:** The settings menu is part of the `MainWindow` and is toggled via the `Home` hotkey.

## Key Files

*   `MainWindow.xaml.cs`: Contains the Win32 logic for transparency and click-through, and profile management logic.
*   `CrosshairSettings.cs`: Defines the configuration schema (including `CrosshairProfile`) and persistence logic.
*   `ValorantProfile.cs`: Handles complex crosshair pattern parsing.
