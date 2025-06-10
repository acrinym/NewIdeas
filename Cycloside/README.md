# Cycloside


## Volatile Scripts
The **Volatile** tray submenu lets you run ad hoc Lua or C# scripts directly
from memory. Choose **Run Lua Script...** or **Run C# Script...** and select a
`.lua` or `.csx` file. Scripts execute immediately using MoonSharp or Roslyn and
their results are logged.

Cycloside is a background tray application built with Avalonia. It supports a
simple plugin system that loads `*.dll` files from the `Plugins` folder at
runtime. The tray menu exposes built‑in modules and any external plugins you
drop into that directory. Hot reload is provided via file watching so there is
no need to restart the app when you update a plugin.

The tray icon image is embedded as a base64 string to keep the repository free
of binary assets.
Cycloside is a simple background tray application built with Avalonia.
It demonstrates a plugin architecture that loads `*.dll` files from the
`Plugins` folder at runtime. The application starts minimized to the
system tray and shows a menu with **Settings** and **Exit** options.
Plugins dropped into the folder are loaded automatically and any
under `Plugins/MyPlugin`. You can also select **Settings → Generate New
Plugin** from the tray menu to create a plugin interactively.

## GUI Plugin Manager
Open **Settings → Plugin Manager** from the tray to toggle plugins on or off,
reload them, or open the plugin folder. Your choices are saved in
`settings.json`.


## Running
```bash
cd Cycloside
 dotnet run
```

## 🔌 Plugins

Drop any assemblies implementing `Cycloside.Plugins.IPlugin` into the `Plugins` directory and they will be loaded automatically when the application starts.

The tray menu includes a **Plugins** submenu that allows you to enable or disable individual modules.

Built-in examples include:

- **Date/Time Overlay** – shows a small always‑on‑top window with the current time.
- **MP3 Player** – plays an MP3 from the `Music` folder.
- **Macro Engine** – placeholder for keyboard macro recording and playback.

---

## ⚙️ Settings and Auto-start

Plugin enable states and the auto‑start preference are stored in `settings.json`.

Toggle **Launch at Startup** from the tray menu to register or remove the application from system startup:
- Uses the registry on **Windows**
- Adds a `cycloside.desktop` file to `~/.config/autostart` on **Linux**

---

## 🪵 Logging

Logs are written to the `logs/` directory with simple rotation once a file exceeds **1 MB**.

Plugin crashes are logged automatically, and the tray icon will display a notification if a plugin fails.

---

## 🧰 Plugin Template Generator

Run the following to generate a boilerplate plugin class under `Plugins/MyPlugin`:

```bash
dotnet run -- --newplugin MyPlugin

