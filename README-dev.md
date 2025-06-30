# Cycloside Developer Notes

This document collects implementation notes, plugin behaviour and
planned features for the Cycloside project.

## Core Components
- **SettingsManager** – loads and saves `AppSettings` from `settings.json`.
  Tracks plugin state, theme choices, cursors and window effects.
- **ThemeManager** – applies XAML theme files to `Application` or individual
  controls. Supports saving and restoring snapshots.
- **CursorManager** – assigns `StandardCursorType` values from settings to
  any `InputElement`.
- **WindowEffectsManager** – registers `IWindowEffect` implementations and
  attaches them to windows according to the configuration map.
- **PluginBus** – simple publish/subscribe message bus used by plugins to
  communicate.
- **PluginMarketplace** – downloads and verifies plugin packages from a
  remote JSON feed.
- **VolatilePluginManager** – executes Lua or C# snippets in memory for quick
  experiments.
- **PluginDevWizard** – small window that generates template plugin files or
  volatile scripts.

## Built-in Plugins
| Plugin | Description |
| ------ | ----------- |
| `ClipboardManagerPlugin` | Stores clipboard history in a window and
  broadcasts changes on `bus:clipboard`. |
| `DateTimeOverlayPlugin` | Small always-on-top window showing the current time. |
| `DiskUsagePlugin` | Visualises folder sizes in a tree view. |
| `EnvironmentEditorPlugin` | Edits environment variables at runtime (Process scope only on Linux/macOS). |
| `FileWatcherPlugin` | Watches a directory and logs file system events. |
| `JezzballPlugin` | Simple recreation of the classic game. |
| `LogViewerPlugin` | Tails a log file and filters lines on the fly. |
| `MP3PlayerPlugin` | Basic audio player built on NAudio. |
| `MacroPlugin` | Records keyboard macros and saves them to disk. Playback is Windows-only. |
| `ProcessMonitorPlugin` | Lists running processes with CPU and memory usage. |
| `QBasicRetroIDEPlugin` | Minimal IDE for creating QBasic-style programs. |
| `TaskSchedulerPlugin` | Schedules tasks with cron-style expressions. |
| `TextEditorPlugin` | Notepad-like editor supporting multiple files. |
| `WallpaperPlugin` | Changes the desktop wallpaper periodically. |
| `WidgetHostPlugin` | Hosts small widgets inside dockable panels. |
| `WinampVisHostPlugin` | Runs Winamp AVS visualisation presets. |

## Issues and TODO
- Crash logging is enabled by default but stack traces should also be written to
  an OS-specific log directory.
- Plugin isolation is limited; sandboxing volatile scripts is planned.
- Improve cross-platform file dialogs for Linux and macOS as suggested.
- Safe Mode currently disables built-in plugins globally; allow per-plugin safe
  toggles in the UI.

### Planned Features
- GUI for managing plugin marketplace feeds and downloads.
- Theme editor previews for window metrics and animated cursors.
- Per-profile settings to quickly swap between sets of plugins and themes.

