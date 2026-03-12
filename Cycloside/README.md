# Cycloside

Cycloside is a personal OS shell for people who want their desktop to feel custom, alive, and a little weird in the best way. It mixes tray control, workspace tabs, widgets, theming, cursors, window effects, media tools, utilities, and retro-capable modules without trying to replace the operating system underneath.

Built with Avalonia, Cycloside stays cross-platform and plugin-first. You can pin useful tools and visualizations onto the desktop, script the environment, hot-reload modules, and shape the machine into a gadget bench, coding cockpit, Netwatch surface, or retro playroom.

The tray icon image is embedded as a base64 string to keep the repository free of binary assets.

## ✅ Running

```bash
cd Cycloside
dotnet run
```

> **📚 New to Cycloside development?** Check out [WhatIlearned.md](WhatIlearned.md) for our lessons learned, best practices, and common pitfalls guide - essential reading for each development session!

## 🔌 Plugins

Drop any assemblies implementing `Cycloside.Plugins.IPlugin` into the `Plugins` directory and they will be loaded automatically. The tray menu includes a **Plugins** submenu to toggle modules on or off.
See [docs/plugin-api.md](docs/plugin-api.md) for the current plugin contracts and [docs/plugin-lifecycle.md](docs/plugin-lifecycle.md) for host behavior.

Built-in examples:
- **Date/Time Overlay** – always-on-top clock overlay
- **MP3 Player** – choose songs and control playback with a widget
- **Managed Visual Host** – audio-reactive visuals rendered in pure C#
- **Macro Engine** – record and replay simple keyboard macros
- **Netwatch / Network Tools** – keep an eye on interfaces, traffic, and connectivity
- **Text Editor** – small editor for notes or Markdown
- **File Explorer** – browse directories with basic operations
- **Quick Launcher** – one-click bar to open built-in tools
- **Wallpaper Changer** – set wallpapers on Windows, Linux or macOS
- **ModPlug Tracker** – play `.mod`, `.it`, `.s3m` or `.xm` music modules
- **Notification Center** – view messages from plugins and the core app
- **Jezzball** – arcade game with powerups and Original Mode
- **Gweled** – native jewel-swap puzzle board with Normal, Timed, and Endless modes
- **Tile World** – native Chip's Challenge style puzzle boards plus a local Tile World DAT/DAC pack browser for compatible community levels
- **QBasic Retro IDE** – old-school coding corner for QB-style experiments
- **ScreenSaver Host** – run vintage 3D text and flower box screensavers
- **Terminal** - console with ANSI colour and scrollback
- **Widget Host** – surface plugins as dockable widgets
- **Winamp Visual Host** – run classic Winamp visualizer DLLs with MP3 integration

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

Run `dotnet run -- --newplugin MyPlugin` from the `Cycloside/` directory to generate a working sample plugin under `Plugins/MyPlugin/src`. Add `--with-tests` to also create `Plugins/MyPlugin/tests`.

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
the application is unfocused. Press **Ctrl+Alt+W** to summon the
widget host, **Ctrl+Alt+T** to pop open a terminal, **Ctrl+Alt+E**
to bring up the file explorer, **Ctrl+Alt+N** for a fresh text editor,
or **Ctrl+Alt+Q** to toggle the Quick Launcher. Profiles and other

features can be wired up to custom hotkeys.
Use **Settings → Hotkey Settings** to remap shortcuts from the GUI.
The helper source lives in `Hotkeys/HotkeyMonitor.swift` and should be built as
`libHotkeyMonitor.dylib` placed next to the application binary.

## 🎨 Theming
See [docs/theming-skinning.md](../docs/theming-skinning.md) for the current appearance model. In short: themes are app-wide palette packs, skins are the window and control treatment layered on top, and animated backdrops can now drive windows from media files or managed visualizers. The built-in packs now include `Dockside`, `AmberCRT`, `OrchardPaper`, `SynthwaveDream`, `Cyberpunk`, `Magical`, `Workbench`, `Classic`, `GlassDeck`, `Win98`, `AfterDark`, and `ProgramManager31`. Cycloside also now ships a native `MagicalProgressBar` control plus `ProgressBar.magical` styling for ritual/arcane progress surfaces. The editor window can browse real theme and skin assets, and the preview window can now render style sheets against a sample shell surface.

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
Place manifest-based skin packs under `Skins/<SkinName>/` or legacy flat `.axaml` files directly under `Skins/`.
Use `GlobalSkin` for the shell-wide skin and `PluginSkins` for per-plugin window overrides in `settings.json`.

## 🌀 Window Effects
Try out wobbly windows, drop shadows and more via **Settings → Runtime Settings**.
Effects are plugin friendly so you can write your own animations.

## 🔄 Auto-update
An optional helper lets Cycloside download and swap in updates when provided
with a download URL and expected checksum.

## 🌟 Why Cycloside?
Cycloside focuses on making the desktop feel personal. Plugins are regular .NET classes, so you can tap into the .NET ecosystem without learning a custom DSL, while themes, skins, cursors, widgets, and effects let the machine look and behave like your space instead of a stock workstation.

## 🖼️ Widgets
See [docs/widget-interface.md](docs/widget-interface.md) for the current widget split. The stable plugin-facing path is `IPlugin.Widget` plus `IWidget`; a richer `IWidgetV2` stack also exists under `Cycloside/Widgets/`, but it is not the default plugin widget host path yet. See also [docs/plugin-api.md](docs/plugin-api.md), [docs/plugin-lifecycle.md](docs/plugin-lifecycle.md), [docs/skin-api.md](docs/skin-api.md), and [docs/windowfx-design.md](docs/windowfx-design.md).


## 🚧 Cycloside vs Rainmeter
Rainmeter is awesome for highly customized desktop skins, but it is Windows-only and relies heavily on its own scripting. Cycloside keeps things lightweight and cross-platform. If you already know C# or want to drop in compiled plugins, you'll feel right at home while still getting a friendly GUI to manage everything.
