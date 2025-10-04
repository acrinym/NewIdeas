# Cycloside

Cycloside is a comprehensive cross-platform desktop application built for tinkerers, developers, and cybersecurity professionals. It hosts plugins, custom window effects, live theming, and provides a complete development and hacking toolkit.

---

## Why Cycloside?

* **Complete Plugin Ecosystem**: Browse, install, and manage community plugins from a built-in marketplace
* **Professional Development Environment**: Advanced code editor with syntax highlighting, IntelliSense, and multi-language support
* **Comprehensive Cybersecurity Toolkit**: Network analysis, packet sniffing, port scanning, MAC/IP spoofing
* **Communication Bridges**: Serial, MQTT, OSC protocol support for IoT and automation
* **Input Device Integration**: MIDI and gamepad input routing for creative applications
* **Remote Management**: SSH client with command execution and file monitoring
* **Automation Engine**: Event-driven rule processing for workflow automation
* **Windows Utilities**: Screenshot annotation, sticky notes, color picker, pixel ruler
* **Cross-Platform**: Works on Windows, Linux, and macOS using the Avalonia UI framework

---

## Multi-Project Architecture

Cycloside now uses a modular multi-project structure:

```
CyclosideNextFeatures/
├── Core/           # EventBus, JSON config, message system
├── Bridge/         # Serial/MQTT/OSC communication protocols
├── Input/          # MIDI & Gamepad input routing
├── SSH/            # SSH client management with profiles
├── Rules/          # Event-driven automation engine
├── Utils/          # Windows utilities (screenshot, notes, etc.)
├── SampleHost/     # Console demo wiring everything together
└── Cycloside/      # Main Avalonia UI application
```

Each project can be developed, tested, and deployed independently while communicating through the shared EventBus system.

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
For details on the new fully managed visualization system (no native deps), see
[`docs/managed-visuals.md`](docs/managed-visuals.md).
Examples live under [`docs/examples/`](docs/examples/).
For volatile scripting see [`docs/volatile-scripting.md`](docs/volatile-scripting.md).

## Features

<details><summary>Core</summary>

* **Multi-Project Architecture**: Modular design with Core, Bridge, Input, SSH, Rules, Utils, and SampleHost projects
* **EventBus System**: In-process pub/sub messaging with wildcard topic support
* **JSON Configuration**: Persistent settings management with automatic serialization
* **Plugin Marketplace**: Browse, install, and manage community plugins
* **Advanced Code Editor**: Professional IDE with syntax highlighting, IntelliSense, and multi-language support
* **Dynamic Theming**: Live theme switching with custom skin support
* **Window Effects**: Custom window behaviors and visual enhancements
* Workspace profiles remember your wallpaper and plugin states for quick swaps.
* Run Lua or C# snippets as volatile scripts straight from the tray menu.
* Cross-platform auto-start and settings stored in `settings.json`.
* Rolling log files capture errors and plugin crashes with tray notifications.
* Generate new plugins via `dotnet run -- --newplugin` or from **Settings → Generate New Plugin**. Add `--with-tests` to scaffold a test project.
* Plugins communicate through a publish/subscribe bus and a remote HTTP API for
    triggering events, switching profiles or applying themes programmatically.
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

<details><summary>Communication Bridges</summary>

* **Serial Communication**: COM port bridging with real-time data forwarding
* **MQTT Protocol**: Full MQTT client with subscription and publishing capabilities
* **OSC Protocol**: Open Sound Control for multimedia and IoT communication
* **Cross-Platform**: Works on Windows, Linux, and macOS with proper protocol handling

</details>

<details><summary>Input Device Integration</summary>

* **MIDI Router**: MIDI device input routing and message forwarding to the event bus
* **Gamepad Router**: XInput gamepad state monitoring and event publishing
* **Real-time Input**: Live input device state monitoring and event generation
* **Cross-Platform**: Works on Windows with proper input device handling

</details>

<details><summary>SSH Management</summary>

* **SSH Client Manager**: SSH connection management with configurable profiles
* **Command Execution**: Remote command execution with timeout handling
* **File Tailing**: Real-time file monitoring over SSH connections
* **Profile Management**: Save and restore SSH connection configurations

</details>

<details><summary>Automation Engine</summary>

* **Rules Engine**: Event-driven automation with multiple trigger types
* **Trigger Types**: Bus topics, timers, file changes, process monitoring
* **Action Types**: Bus publishing, process execution, toast notifications
* **Configurable Rules**: JSON-based rule definitions with flexible matching

</details>

<details><summary>Windows Utilities</summary>

* **Screenshot Annotator**: Region selection and annotation overlay with save/copy functionality
* **Sticky Notes Manager**: Persistent JSON-based sticky notes with window management
* **Color Picker Tool**: Pixel color selection with hex output and event publishing
* **Pixel Ruler**: Screen measurement overlay tool for precise measurements
* **HTML/Markdown Host**: WebView2-based HTML/Markdown rendering with live preview
* **Python Runner**: IronPython execution with network import restrictions
* **QuickShare Server**: HTTP file sharing with QR code generation and upload form

</details>

<details><summary>Network Security</summary>

* **Packet Sniffer**: Real-time network packet capture and protocol analysis
* **Port Scanner**: Comprehensive port scanning for vulnerability assessment
* **HTTP Inspector**: Web traffic monitoring and request/response analysis
* **MAC Address Spoofing**: Network interface MAC address modification
* **IP Address Spoofing**: Network interface IP configuration spoofing
* **Network Traffic Monitor**: Real-time network traffic visualization and analysis

</details>

<details><summary>Built-in Plugins</summary>

| Plugin                   | Description                                                                              |
| ------------------------ | ---------------------------------------------------------------------------------------- |
| `ClipboardManagerPlugin` | Stores clipboard history in a window and broadcasts changes on `bus:clipboard`.          |
| `CodeEditorPlugin` | Multi-language code editor with syntax highlighting and run support. |
| `DateTimeOverlayPlugin`  | Small always-on-top window showing the current time.                                     |
| `DiskUsagePlugin`        | Visualises folder sizes in a tree view.                                                  |
| `EnvironmentEditorPlugin`| Edits environment variables at runtime (Process scope only on Linux/macOS).              |
| `FileWatcherPlugin`      | Watches a directory and logs file system events.                                         |
| `FileExplorerPlugin`     | Browse directories with tree and list views, context menu actions. |
| `NetworkToolsPlugin`     | Ping, traceroute and port scan utilities with export. |
| `EncryptionPlugin`       | Encrypt text or files using AES/RSA. Accessible from File Explorer. |
| `JezzballPlugin`         | Arcade game with powerups, visual effects and Original mode.|
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
| `TerminalPlugin`         | Console with ANSI colour and scrollback.|
| `WallpaperPlugin`        | Changes the desktop wallpaper periodically.                                              |
| `WidgetHostPlugin`       | Hosts small widgets inside dockable panels.                                              |
| `WinampVisHostPlugin`    | Winamp visualisations integrated with the MP3 player.|
| `ManagedVisHostPlugin`   | Fully managed C# visualizers (bars, oscilloscope, spectrogram, matrix rain, lava lamp, starfield, etc.).|

</details>

## Building

Ensure the .NET 8 SDK is installed (download from https://dotnet.microsoft.com/download or `sudo apt-get install dotnet-sdk-8.0`). Compile the main application with:

```bash
dotnet build Cycloside/Cycloside.csproj
```
