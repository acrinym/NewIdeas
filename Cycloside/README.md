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
- **Date/Time Overlay** – always-on-top clock overlay
- **MP3 Player** – choose songs and control playback with a widget
- **Macro Engine** – record and replay simple keyboard macros
- **Text Editor** – small editor for notes or Markdown
- **Wallpaper Changer** – set wallpapers on Windows, Linux or macOS
- **ModPlug Tracker** – play `.mod`, `.it`, `.s3m` or `.xm` music modules
- **Notification Center** – view messages from plugins and the core app
- **ScreenSaver Host** – run vintage 3D text and flower box screensavers
- **Terminal** – simple shell window for running commands
- **Widget Host** – surface plugins as dockable widgets
- **Winamp Visual Host** – run classic Winamp visualizer DLLs

## 🗂️ Workspace Profiles

Save wallpaper choices and plugin states into named profiles. You can
switch between profiles from the tray menu or bind them to global
hotkeys for quick swaps when changing tasks.

## 🧨 Volatile Scripts

The **Volatile** tray submenu lets you run Lua or C# scripts from memory. Choose **Run Lua Script...** or **Run C# Script...** and select a `.lua` or `.csx` file. Execution uses MoonSharp or Roslyn and logs results automatically.

## ⚙️ Settings and Auto-start

Stored in `settings.json`. Toggle **Launch at Startup** to register/unregister at boot:
- Uses registry (Windows)
- Adds `cycloside.desktop` to `~/.config/autostart` (Linux)
- Writes a LaunchAgents plist for `launchctl` (macOS)

## 🪵 Logging

Logs rotate in the `logs/` folder after 1 MB. Plugin crashes are logged and trigger a tray notification.
When isolation is enabled, crashes won't take down the entire app and are simply logged.

## 🧰 Plugin Template Generator

Run `dotnet run -- --newplugin MyPlugin` to create a boilerplate class, or use **Settings → Generate New Plugin** from the tray menu.

## 📣 Plugin Bus and Remote API

Plugins can talk to each other through a simple publish/subscribe bus. You can
also POST events to `http://localhost:4123/trigger` to control plugins from
other tools or scripts. Include your pre‑shared token via the `X-Api-Token`
header or `?token=` query string or the request will be rejected with a 401.

### Enabling the Remote API

`RemoteApiServer` starts automatically when Cycloside runs. Set your token in
`settings.json` under `RemoteApiToken` to secure the endpoint. Then you can send
events over HTTP:

```bash
curl -X POST -H "X-Api-Token: <token>" http://localhost:4123/trigger -d "my:event"
```

## ⌨️ Global Hotkeys

Cycloside registers system-wide shortcuts using Avalonia's hotkey framework.
On macOS a small Swift helper hooks into `NSEvent` so hotkeys fire even when
the application is unfocused. Press **Ctrl+Alt+W** at any time to summon the
widget host. Profiles and other features can be wired up to custom hotkeys.
Use **Settings → Hotkey Settings** to remap shortcuts from the GUI.
The helper source lives in `Hotkeys/HotkeyMonitor.swift` and should be built as
`libHotkeyMonitor.dylib` placed next to the application binary.

## 🎨 Theming
See [docs/theming-skinning.md](../docs/theming-skinning.md) for details on applying themes, skins and custom cursors. A Skin Preview window lets you test styles before saving. Example files live in [docs/examples](../docs/examples).

## 🧪 GUI Plugin Manager

Use **Settings → Plugin Manager** to:
- Toggle plugins
- Reload them
- Open the plugin folder

All plugin states are persistently stored.

## ⚙️ Control Panel
Launch **Settings → Control Panel** for a single place to tweak common options.
It lets you toggle startup behavior, set the `dotnet` path and jump to other
settings windows.

## 📦 Plugin Marketplace
`PluginMarketplace` can fetch a list of available modules from a remote URL and
install them directly into your `Plugins/` directory. Each download is verified
with a SHA256 hash before it is placed on disk.

## 🎨 Skins
Place Avalonia style files inside the `Skins` folder to customize the interface.
Assign skins to specific plugins using the `ComponentSkins` section of `settings.json`.

## 🌀 Window Effects
Try out wobbly windows, drop shadows and more via **Settings → Runtime Settings**.
Effects are plugin friendly so you can write your own animations.

## 🔄 Auto-update
An optional helper lets Cycloside download and swap in updates when provided
with a download URL and expected checksum.

## 🌟 Why Cycloside?
Cycloside focuses on simplicity. Plugins are regular .NET classes, so you can tap into the entire ecosystem without learning a custom scripting language. Because it's built on Avalonia, the same setup runs on Windows and Linux alike.

## 🖼️ Widgets
See [docs/widget-interface.md](docs/widget-interface.md) for the current design of our dockable, skinnable widget system. Any plugin can expose a widget surface simply by returning one from the `Widget` property. The built-in `Widget Host` plugin demonstrates this with sample Clock, MP3 and Weather widgets. The goal is to surface plugin features directly on your desktop with minimal fuss. See also [docs/plugin-lifecycle.md](docs/plugin-lifecycle.md) for lifecycle hooks, [docs/skin-api.md](docs/skin-api.md) for information on creating new skins, and [docs/windowfx-design.md](docs/windowfx-design.md) for an overview of the planned compositor effects system.


## 🚧 Cycloside vs Rainmeter
Rainmeter is awesome for highly customized desktop skins, but it is Windows-only and relies heavily on its own scripting. Cycloside keeps things lightweight and cross-platform. If you already know C# or want to drop in compiled plugins, you'll feel right at home while still getting a friendly GUI to manage everything.
