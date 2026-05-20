# xcrosshair

A minimalist, high-performance crosshair overlay for Windows built with C# and WPF.

## Features

- **Global Hotkey**: Press `Home` to show/hide the settings menu.
- **Click-Through**: The crosshair doesn't interfere with mouse input.
- **Customizable**: Change color, size, and thickness on the fly.
- **Multi-Monitor**: Easily move the crosshair to any connected monitor.
- **Minimalist UI**: Dark-themed settings menu that can be docked to the top-right or centered.

## Requirements

- .NET 8.0 SDK or Runtime

## How to Run

1. Clone the repository:
   ```bash
   git clone https://github.com/XppaiCyberr/xcrosshair.git
   ```
2. Navigate to the project folder:
   ```bash
   cd xcrosshair
   ```
3. Run the application:
   ```bash
   dotnet run
   ```

## Changelog

### v1.3.0
- **Multi-Profile Management**: Create, save, and switch between multiple crosshair configurations.
- **Save As/Clone**: Easily duplicate existing profiles.
- **Documentation**: Added `GEMINI.md` for better context.

### v1.2.0
- **VALORANT Support**: Added ability to import and parse VALORANT crosshair profile codes.
- **Enhanced Rendering**: Support for dots, inner lines, and outer lines from VALORANT profiles.

### v1.1.0
- **UI Redesign**: New dark-themed minimalist settings menu.
- **Persistence**: Settings are now saved to `%AppData%`.
- **Custom Color Picker**: Added support for any custom hex color.
- **Stability**: Improved fullscreen compatibility and docking options.

### v1.0.1
- **DPI Scaling**: Fixed crosshair positioning on high-DPI displays.
- **Bug Fixes**: Improved stability when toggling the menu.

### v1.0.0
- **Initial Release**: Basic crosshair overlay with hotkey support and click-through.
- **Auto-Release**: Integrated GitHub Actions for automated builds.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
