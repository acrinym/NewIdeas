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
* Run volatile Lua or C# scripts for quick experiments.

---

## Customising

1. **Right‑click the tray icon** to access **Settings**.
2. Open **Skin/Theme Editor** to modify `.axaml` theme files or cursor choices.
3. Toggle window effects or enable/disable plugins as desired.
4. Themes and plugin settings are stored in `settings.json` next to the executable.
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
* Built-in skin/theme engine with a live editor and custom cursors.
* GUI plugin manager to toggle and reload plugins or open the plugin folder.
* Plugin marketplace downloads and verifies modules from remote feeds.
* Skinnable widgets surface plugin features directly on the desktop.
* Window effects like wobbly windows or drop shadows are plugin friendly.
* Optional auto-update helper swaps in new versions using a checksum.
* Dedicated logs menu surfaces recent errors from the tray.

</details>

<details><summary>Built-in Plugins</summary>

| Plugin | Description |
| ------ | ----------- |
| `ClipboardManagerPlugin` | Stores clipboard history in a window and broadcasts changes on `bus:clipboard`. |
| `DateTimeOverlayPlugin` | Small always-on-top window showing the current time. |
| `DiskUsagePlugin` | Visualises folder sizes in a tree view. |
| `EnvironmentEditorPlugin` | Edits environment variables at runtime (Process scope only on Linux/macOS). |
| `FileWatcherPlugin` | Watches a directory and logs file system events. |
| `JezzballPlugin` | Simple recreation of the classic game. |
| `LogViewerPlugin` | Tails a log file and filters lines on the fly. |
| `MP3PlayerPlugin` | Basic audio player built on NAudio. |
| `MacroPlugin` | Records keyboard macros and saves them to disk. Playback is Windows-only. |
| `ModTrackerPlugin` | Plays and inspects tracker module files (MOD, IT, XM, etc.). |
| `ProcessMonitorPlugin` | Lists running processes with CPU and memory usage. |
| `QBasicRetroIDEPlugin` | Minimal IDE for creating QBasic-style programs. Includes an option to launch QB64 for editing. |
| `ScreenSaverPlugin` | Runs full-screen screensavers after a period of inactivity. |
| `TaskSchedulerPlugin` | Schedules tasks with cron-style expressions. |
| `TextEditorPlugin` | Notepad-like editor supporting multiple files. |
| `TerminalPlugin` | Run shell commands in a simple console window. |
| `WallpaperPlugin` | Changes the desktop wallpaper periodically. |
| `ModTrackerPlugin` | Plays classic tracker music modules using libopenmpt. |
| `ScreenSaverPlugin` | Runs fullscreen screensavers after a period of inactivity. |
| `TerminalPlugin` | Simple command shell window. |
| `WidgetHostPlugin` | Hosts small widgets inside dockable panels. |
| `WinampVisHostPlugin` | Runs Winamp AVS visualisation presets. |

</details>

</details>

<details><summary>Built-in Plugins</summary>

| Plugin | Description |
| ------ | ----------- |
| `ClipboardManagerPlugin` | Stores clipboard history in a window and broadcasts changes on `bus:clipboard`. |
| `DateTimeOverlayPlugin` | Small always-on-top window showing the current time. |
| `DiskUsagePlugin` | Visualises folder sizes in a tree view. |
| `EnvironmentEditorPlugin` | Edits environment variables at runtime (Process scope only on Linux/macOS). |
| `FileWatcherPlugin` | Watches a directory and logs file system events. |
| `JezzballPlugin` | Simple recreation of the classic game. |
| `LogViewerPlugin` | Tails a log file and filters lines on the fly. |
| `MP3PlayerPlugin` | Basic audio player built on NAudio. |
| `MacroPlugin` | Records keyboard macros and saves them to disk. Playback is Windows-only. |
| `ProcessMonitorPlugin` | Lists running processes with CPU and memory usage. |
| `QBasicRetroIDEPlugin` | Minimal IDE for creating QBasic-style programs. Includes an option to launch QB64 for editing. |
| `TaskSchedulerPlugin` | Schedules tasks with cron-style expressions. |
| `TextEditorPlugin` | Notepad-like editor supporting multiple files. |
| `WallpaperPlugin` | Changes the desktop wallpaper periodically. |
| `WidgetHostPlugin` | Hosts small widgets inside dockable panels. |
| `WinampVisHostPlugin` | Runs Winamp AVS visualisation presets. |

</details>

## Building

Ensure the .NET 8 SDK is installed (download from https://dotnet.microsoft.com/download or `sudo apt-get install dotnet-sdk-8.0`). Compile the main application with:

```bash
dotnet build Cycloside/Cycloside.csproj
```

The build targets both `net8.0` and `net8.0-windows` and should finish with no warnings.

---

## FUTURE TODO

* Package the editor as a standalone tool for theme creators.
* Expand the plugin marketplace for one‑click installs.
* Portable build scripts for Windows, Linux, and macOS.

---

## Skin/Theme Editor

Cycloside now includes a lightweight **Skin/Theme Editor** accessible from the tray menu. Edit your `.axaml` files with live previews and experiment with different cursors before applying a new look.
Runtime settings allow enabling crash logging and toggling Safe Mode, which disables built-in plugins if they cause issues.

---

## VIS\_AVS\_JS

`VIS_AVS_JS` contains an experimental WebAssembly/JavaScript port of the classic Winamp **Advanced Visualization Studio** plugin. Building the module requires the [Emscripten](https://emscripten.org/) toolchain.

1. Clone the `emsdk` repository and run `./emsdk install latest` then `./emsdk activate latest`.
2. Source `emsdk_env.sh` or run the batch file on Windows.
3. Compile the project with `VIS_AVS_JS/build.sh`.

---

## In‑Browser AHK Script

1. Install the [Tampermonkey](https://www.tampermonkey.net/) extension.
2. Open `In-Browser AHK (AutoHotkey-like Features)-0.2.6.user.js` in your browser.
3. Tampermonkey will prompt to install; confirm it.
4. Click the Tampermonkey icon to access the settings panel and configure hotkeys.
5. Export or import configurations as JSON/`.bahk` files as needed.

---

## Chrome API Emulator

`chrome-api-emulator.user.js` emulates several Chrome extension APIs for Tampermonkey.
It exposes `chrome.storage.sync`, `chrome.contextMenus`, and basic runtime messaging.
The script also provides a drag-and-drop zone so you can drop additional JavaScript files onto any page and load them as plugins on the fly.

---

## Global Hotkeys

`HotkeyManager` now supports registering global hotkeys on Windows, Linux, and macOS.
macOS uses a small Swift helper while the other platforms rely on the cross-platform **SharpHook** library (`libuiohook`).
Hotkey registration may fail under Wayland or restricted environments on Linux. In those cases, no key events will be received.

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for commit guidelines.

---

### ✨ Cycloside is an evolving project—contributions and plugin ideas are always welcome!
