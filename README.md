# Cycloside

Cycloside is a small cross‑platform tray application built for tinkerers. It hosts plugins, custom window effects, and live theming so you can shape the desktop to your liking.

---

## Why Cycloside?

* Collects small utilities in one place: from a clipboard history viewer to a simple IDE and wallpaper manager.
* Lets you experiment with window effects and custom cursors.
* Works on Windows, Linux, and macOS using the Avalonia UI framework.

---

## Use Cases

* Organise shortcuts and tools in a single tray icon.
* Inspect disk usage or running processes with the built‑in plugins.
* Edit themes/skins and instantly preview them on a test window.
* Run volatile Lua or C# snippets for quick experiments.

---

## Customising

1.  **Right‑click the tray icon** to access **Settings**.
2.  Open **Skin/Theme Editor** to modify `.axaml` theme files or cursor choices.
3.  Toggle window effects or enable/disable plugins as desired.
4.  Themes and plugin settings are stored in `settings.json` next to the executable.
    Use the provided `Blank.axaml` theme and skin as starting points in
    `Cycloside/Themes/Global/` and `Cycloside/Skins/`.

Detailed instructions for creating your own look are in
[`docs/theming-skinning.md`](docs/theming-skinning.md).

For plugin development details see [`docs/plugin-dev.md`](docs/plugin-dev.md).
Examples live under [`docs/examples/`](docs/examples/).
For volatile scripting see [`docs/volatile-scripting.md`](docs/volatile-scripting.md).

## Features

<details><summary>Core</summary>

* Built-in plugin system with hot reload. Sample modules include a clock overlay,
    MP3 player, macro recorder (Windows only), text editor, wallpaper changer,
    widget host, Winamp visualizer host, a tracker module player and a simple
    command shell.
* Workspace profiles remember your wallpaper and plugin states for quick swaps.
* Run Lua or C# snippets as volatile scripts straight from the tray menu.
* Cross-platform auto-start and settings stored in `settings.json`.
* Rolling log files capture errors and plugin crashes with tray notifications.
* Generate new plugins via `dotnet run -- --newplugin` or from **Settings → Generate New Plugin**.
* Plugins communicate through a publish/subscribe bus and a remote HTTP API for
    triggering events.
* Global hotkeys work on Windows, Linux and macOS.
* Hotkeys can be edited from the new Hotkey Settings window.
* Built-in skin/theme engine with a live editor and custom cursors.
* Preview skins before applying them thanks to the Skin Preview window. A sample
    `SolarizedDark` skin is included.
* GUI plugin manager to toggle and reload plugins or open the plugin folder.
* Plugin marketplace downloads and verifies modules from remote feeds.
* Skinnable widgets surface plugin features directly on the desktop.
* Window effects like wobbly windows or drop shadows are plugin friendly.
* Optional auto-update helper swaps in new versions using a checksum.
* Dedicated logs menu surfaces recent errors from the tray.
* A unified workspace shows compatible plugins as tabs or docked panels.

</details>

<details><summary>Built-in Plugins</summary>

| Plugin                   | Description                                                                              |
| ------------------------ | ---------------------------------------------------------------------------------------- |
| `ClipboardManagerPlugin` | Stores clipboard history in a window and broadcasts changes on `bus:clipboard`.          |
| `DateTimeOverlayPlugin`  | Small always-on-top window showing the current time.                                     |
| `DiskUsagePlugin`        | Visualises folder sizes in a tree view.                                                  |
| `EnvironmentEditorPlugin`| Edits environment variables at runtime (Process scope only on Linux/macOS).              |
| `FileWatcherPlugin`      | Watches a directory and logs file system events.                                         |
| `JezzballPlugin`         | Simple recreation of the classic game.                                                   |
| `LogViewerPlugin`        | Tails a log file and filters lines on the fly.                                           |
| `NotificationCenterPlugin`| Aggregates messages broadcast via `NotificationCenter`.                                  |
| `MP3PlayerPlugin`        | Basic audio player built on NAudio.                                                      |
| `MacroPlugin`            | Records keyboard macros and saves them to disk. Playback is Windows-only.                |
| `ModTrackerPlugin`       | Plays and inspects tracker module files (MOD, IT, XM, etc.).                             |
| `ProcessMonitorPlugin`   | Lists running processes with CPU and memory usage.                                       |
| `QBasicRetroIDEPlugin`   | Minimal IDE for creating QBasic-style programs. Includes an option to launch QB64 for editing. |
| `ScreenSaverPlugin`      | Runs full-screen screensavers after a period of inactivity.                              |
| `TaskSchedulerPlugin`    | Schedules tasks with cron-style expressions.                                             |
| `TextEditorPlugin`       | Notepad-like editor supporting multiple files.                                           |
| `TerminalPlugin`         | Run shell commands in a simple console window.                                           |
| `WallpaperPlugin`        | Changes the desktop wallpaper periodically.                                              |
| `WidgetHostPlugin`       | Hosts small widgets inside dockable panels.                                              |
| `WinampVisHostPlugin`    | Runs Winamp AVS visualisation presets.                                                   |

</details>

## Building

Ensure the .NET 8 SDK is installed (download from https://dotnet.microsoft.com/download or `sudo apt-get install dotnet-sdk-8.0`). Compile the main application with:

```bash
dotnet build Cycloside/Cycloside.csproj