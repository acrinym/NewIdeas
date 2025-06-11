# Cycloside

Cycloside lets you pin tiny, useful tools and visualizations right onto your desktop without the bloat of a full-blown shell replacement. Built with Avalonia, it's cross-platform by design and aims to stay fast and friendly for plugin developers.
Cycloside is a background tray application built with Avalonia. It supports a simple plugin system that loads `*.dll` files from the `Plugins` folder at runtime. The tray menu exposes built‑in modules and any external plugins you drop into that directory. Hot reload is provided via file watching so there is no need to restart the app when you update a plugin.

The tray icon image is embedded as a base64 string to keep the repository free of binary assets.

## ✅ Running

```bash
cd Cycloside
dotnet run
```

## 🔌 Plugins

Drop any assemblies implementing `Cycloside.Plugins.IPlugin` into the `Plugins` directory and they will be loaded automatically. The tray menu includes a **Plugins** submenu to toggle modules on or off.

Built-in examples:
- **Date/Time Overlay** – always-on-top window with current time
- **MP3 Player** – plays an MP3 from the `Music` folder
- **Macro Engine** – placeholder for keyboard macros
- **Text Editor** – small editor for notes or Markdown
- **Wallpaper Changer** – pick an image to use as your wallpaper
- **Widget Host** – surface plugins as dockable widgets

## 🧨 Volatile Scripts

The **Volatile** tray submenu lets you run Lua or C# scripts from memory. Choose **Run Lua Script...** or **Run C# Script...** and select a `.lua` or `.csx` file. Execution uses MoonSharp or Roslyn and logs results automatically.

## ⚙️ Settings and Auto-start

Stored in `settings.json`. Toggle **Launch at Startup** to register/unregister at boot:
- Uses registry (Windows)
- Adds `cycloside.desktop` to `~/.config/autostart` (Linux)

## 🪵 Logging

Logs rotate in the `logs/` folder after 1 MB. Plugin crashes are logged and trigger a tray notification.
When isolation is enabled, crashes won't take down the entire app and are simply logged.

## 🧰 Plugin Template Generator

Run `dotnet run -- --newplugin MyPlugin` to create a boilerplate class, or use **Settings → Generate New Plugin** from the tray menu.

## 🎨 Theming
See [docs/theming-skinning.md](../docs/theming-skinning.md) for details on applying themes, skins and custom cursors.

## 🧪 GUI Plugin Manager

Use **Settings → Plugin Manager** to:
- Toggle plugins
- Reload them
- Open the plugin folder

All plugin states are persistently stored.

## 🎨 Skins
Place Avalonia style files inside the `Skins` folder to theme the interface. The
current skin is loaded at startup based on `ActiveSkin` in `settings.json` and can be changed from **Settings → Runtime Settings**.

## 🌟 Why Cycloside?
Cycloside focuses on simplicity. Plugins are regular .NET classes, so you can tap into the entire ecosystem without learning a custom scripting language. Because it's built on Avalonia, the same setup runs on Windows and Linux alike.

## 🖼️ Widgets
See [docs/widget-interface.md](docs/widget-interface.md) for the current design of our dockable, skinnable widget system. Any plugin can expose a widget surface simply by returning one from the `Widget` property. The built-in `Widget Host` plugin demonstrates this with sample Clock, MP3 and Weather widgets. The goal is to surface plugin features directly on your desktop with minimal fuss.
See also [docs/plugin-lifecycle.md](docs/plugin-lifecycle.md) for lifecycle hooks and [docs/skin-api.md](docs/skin-api.md) for information on creating new skins.

See [docs/windowfx-design.md](docs/windowfx-design.md) for an overview of the planned compositor effects system.

See [docs/widget-interface.md](docs/widget-interface.md) for the current design of our dockable, skinnable widget system. The goal is to surface plugin features like the MP3 player or a future weather module directly on your desktop with minimal fuss.


## 🚧 Cycloside vs Rainmeter
Rainmeter is awesome for highly customized desktop skins, but it is Windows-only and relies heavily on its own scripting. Cycloside keeps things lightweight and cross-platform. If you already know C# or want to drop in compiled plugins, you'll feel right at home while still getting a friendly GUI to manage everything.
