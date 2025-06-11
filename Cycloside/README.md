# Cycloside

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

## 🧨 Volatile Scripts

The **Volatile** tray submenu lets you run Lua or C# scripts from memory. Choose **Run Lua Script...** or **Run C# Script...** and select a `.lua` or `.csx` file. Execution uses MoonSharp or Roslyn and logs results automatically.

## ⚙️ Settings and Auto-start

Stored in `settings.json`. Toggle **Launch at Startup** to register/unregister at boot:
- Uses registry (Windows)
- Adds `cycloside.desktop` to `~/.config/autostart` (Linux)

## 🪵 Logging

Logs rotate in the `logs/` folder after 1 MB. Plugin crashes are logged and trigger a tray notification.

## 🧰 Plugin Template Generator

Run `dotnet run -- --newplugin MyPlugin` to create a boilerplate class, or use **Settings → Generate New Plugin** from the tray menu.

## 🎨 Theming

See [docs/theming-skinning.md](../docs/theming-skinning.md) for details on applying themes or skins to different parts of Cycloside.

## 🧪 GUI Plugin Manager

Use **Settings → Plugin Manager** to:
- Toggle plugins
- Reload them
- Open the plugin folder

All plugin states are persistently stored.
