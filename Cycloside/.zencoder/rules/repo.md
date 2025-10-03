---
description: Repository Information Overview
alwaysApply: true
---

# Cycloside Information

## Summary
Cycloside is a cross-platform desktop application that allows users to pin small tools and visualizations to their desktop. Built with Avalonia, it features a plugin system that loads DLLs from the Plugins folder at runtime. The application runs in the system tray and provides various built-in modules like Date/Time Overlay, MP3 Player, Text Editor, and more.

## Structure
- **Plugins/**: Contains built-in plugins and external plugin DLLs
- **SDK/**: Plugin development interfaces and examples
- **Themes/**: Global and component-specific themes
- **Skins/**: Avalonia style files for UI customization
- **Widgets/**: Widget system implementation and built-in widgets
- **Services/**: Core application services (Audio, Notifications, etc.)
- **Visuals/**: Visualization components and managed visualizers
- **Effects/**: Window effects like shadows and wobbly windows
- **Hotkeys/**: Global hotkey implementation for different platforms

## Language & Runtime
**Language**: C#
**Version**: .NET 8.0
**Build System**: MSBuild (via dotnet CLI)
**Package Manager**: NuGet
**UI Framework**: Avalonia 11.3.1

## Dependencies
**Main Dependencies**:
- Avalonia 11.3.1 (UI framework)
- ReactiveUI 19.6.1 (MVVM framework)
- NAudio 2.2.1 (Audio processing)
- MoonSharp 2.0.0 (Lua scripting)
- Microsoft.CodeAnalysis.CSharp 4.9.2 (C# scripting)
- SharpHook 5.0.0 (Global hotkeys)
- System.CommandLine 2.0.0-beta4 (CLI parsing)

**Development Dependencies**:
- Avalonia.Diagnostics 11.3.1 (Debug tools)

## Build & Installation
```bash
# Build the project
dotnet build

# Run the application
dotnet run

# Generate a plugin template
dotnet run -- --newplugin MyPlugin

# Generate a plugin with tests
dotnet run -- --newplugin MyPlugin --with-tests
```

## Main Entry Points
**Main Application**: Program.cs contains the entry point with Avalonia initialization
**Plugin System**: PluginManager.cs handles loading and lifecycle of plugins
**Widget System**: WidgetManager.cs manages the widget hosting infrastructure
**Settings**: SettingsManager.cs handles application configuration

## Testing
**Framework**: xUnit (referenced in plugin template generator)
**Test Location**: Plugin tests are generated in a 'tests' subfolder
**Naming Convention**: *Tests.cs
**Run Command**:
```bash
dotnet test
```

## Plugin Development
**Interface**: IPlugin in SDK/IPlugin.cs
**Extended Interface**: IPluginExtended for advanced features
**Widget Interface**: IWidget for desktop widgets
**Template Generation**: Via CLI or GUI (Settings → Generate New Plugin)
**Examples**: SDK/Examples contains sample plugins in C#, Lua, and C# script

## Special Features
**Global Hotkeys**: System-wide shortcuts (Ctrl+Alt+W, Ctrl+Alt+T, etc.)
**Workspace Profiles**: Save and switch between plugin configurations
**Window Effects**: Wobbly windows, shadows, and transparency effects
**Remote API**: HTTP endpoint for external control (localhost:4123/trigger)
**Auto-update**: Optional update system with checksum verification
**Theming**: Customizable UI via Avalonia styles in Skins folder