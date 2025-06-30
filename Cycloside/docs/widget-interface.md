# Cycloside Widget Interface

Cycloside aims to provide a lightweight and flexible widget system alongside its tray-based plugin model. This document explains the goals for the widget interface and how it differs from other solutions such as Microsoft Widgets or Rainmeter. The `WidgetHost` plugin in the main project demonstrates the current implementation.


## Purpose

Widgets allow small, skinnable user interface components to live on the desktop. They can display information or provide quick controls without opening full windows. In Cycloside, widgets are an extension of the existing plugin system and let you surface plugin features directly on your desktop.

## What It Offers

- **Dockable and Movable:** Widgets can float freely or snap to each other. Users can arrange them anywhere and create stacks of related widgets.
- **Resizable:** Each widget supports live resizing, letting you pick the perfect footprint on your desktop.
- **Skinning:** We plan to leverage Avalonia's styling engine so themes can be shared. Developers can bundle default styles or ship a library of skins.
- **Plugin Integration:** Built-in modules such as the MP3 player or future weather plugins can expose a widget, providing quick access without opening menus.

## How It's Different from Rainmeter

Rainmeter is a powerful, scriptable desktop customization tool focusing primarily on Windows. Cycloside shares the idea of modular widgets but takes a cross-platform approach using Avalonia. Instead of the Lua-based scripting found in Rainmeter, Cycloside relies on compiled plugins (or volatile C# and Lua scripts) to provide functionality. The goal is to remain lightweight while still allowing deeper integration with existing .NET libraries.

## Implementation Sketch

The widget system is built as a plugin host:

1. **WidgetHostWindow:** an always-on-top container that manages a set of widget controls.
2. **IWidget** interface for widget plugins to implement.
3. **WidgetManager:** responsible for loading widget assemblies from a `Widgets` folder, similar to `Plugins`.
4. **Docking Layout:** uses Avalonia's layout panels to allow snapping widgets next to each other.

This is currently a design document and serves as a guide for future development. Community contributions and feedback are welcome!

## Built-in Widgets

The repository includes a handful of sample widgets to show how the interface works:

- **ClockWidget** – simple digital clock
- **Mp3Widget** – pick MP3 files and control playback
- **WeatherWidget** – fetches temperature data from Open‑Meteo

Double‑click the Weather widget to set your city or latitude and longitude. The values
are stored in `settings.json` and used when requesting weather data.

