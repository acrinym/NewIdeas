# Cycloside

Cycloside is a background tray application built with Avalonia. It supports a
simple plugin system that loads `*.dll` files from the `Plugins` folder at
runtime. The tray menu exposes built‑in modules and any external plugins you
drop into that directory. Hot reload is provided via file watching so there is
no need to restart the app when you update a plugin.

The tray icon image is embedded as a base64 string to keep the repository free
of binary assets.

## Running
```bash
cd Cycloside
 dotnet run
```

## Plugins
Drop any assemblies implementing `Cycloside.Plugins.IPlugin` into the
`Plugins` directory and they will be loaded automatically. The tray menu has a
**Plugins** sub menu that allows you to enable or disable individual modules.
Built in examples include:

- **Date/Time Overlay** – shows a small always‑on‑top window with the current
  time.
- **MP3 Player** – plays an MP3 from the `Music` folder.
- **Macro Engine** – placeholder for keyboard macro recording and playback.

## Volatile Scripts
The **Volatile** tray submenu lets you run ad hoc Lua or C# scripts directly
from memory. Choose **Run Lua Script...** or **Run C# Script...** and select a
`.lua` or `.csx` file. Scripts execute immediately using MoonSharp or Roslyn and
their results are logged.

## Settings and Auto-start
Plugin enable states and the auto‑start preference are stored in
`settings.json`. Toggle **Launch at Startup** from the tray menu to register or
remove the application from system start‑up (registry on Windows or a
`cycloside.desktop` file in `~/.config/autostart` on Linux).

## Logging
Logs are written to the `logs` directory with simple rotation when the file
exceeds 1 MB. Plugin crashes are logged and a notification is displayed from the
tray icon.

## Plugin Template
Run `dotnet run -- --newplugin MyPlugin` to generate a boilerplate plugin class
under `Plugins/MyPlugin`. You can also select **Settings → Generate New
Plugin** from the tray menu to create a plugin interactively.

## GUI Plugin Manager
Open **Settings → Plugin Manager** from the tray to toggle plugins on or off,
reload them, or open the plugin folder. Your choices are saved in
`settings.json`.
