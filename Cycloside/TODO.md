# Avalonia Hacker's Paradise App — Master ToDo List

This is a development checklist for your Avalonia-powered hacker’s paradise app, plugin system, and future marketplace. Mark items as complete or in progress as you build.

---

## ✅ Legend
- [x] = Completed
- [ ] = Pending
- [~] = In Progress

---

## Core Plugin System
- [x] Implement `IPlugin` interface (`string Name`, `UserControl GetUI()`, `void Initialize()`)
- [x] Dynamic plugin loading from `/Plugins/*.dll` (reflection)
- [x] Register & display loaded plugins in main UI (tab or dockable panel)
- [x] Error handling: Safe fallback if a plugin fails to load

## Default Plugins
- [ ] **Code Editor Plugin** (AvaloniaEdit)
    - [ ] Syntax highlighting (C#, Python, JS, etc.)
    - [ ] Auto-complete support
    - [ ] “Run” button (Roslyn, IronPython, JS engine)
 - [x] **File Explorer Plugin**
    - [x] TreeView/ListView for file system
    - [x] Context menu (Open, Rename, Delete, etc.)
    - [x] “Open in Code Editor” action
- [x] **Terminal Emulator Plugin**
    - [x] Shell command execution
    - [x] Command history (arrow keys)
    - [x] Styled output (color text, scrollback)
- [x] Jezzball plugin powerups, visual effects & Original Mode
- [x] Managed visualizer host with MP3 player (Avalonia-based)
    - [ ] Reduce visualizer latency (UI marshalling/back-pressure)
    - [ ] Optional AVS preset support via transpiler (scoped subset)
    - [ ] Replace/augment NAudio backend (evaluate BASS/CSCore/FFmpeg)
- [x] **Network Tools Plugin**
    - [x] Ping, traceroute utilities
    - [x] Port scanner
    - [x] Export/save results to file
- [x] **Encryption Plugin**
    - [x] AES/RSA text and file encryption
    - [x] UI for entering data, key, selecting algorithm
    - [x] Integrate with File Explorer for file encryption

## Marketplace & Extensibility
- [ ] Design plugin marketplace UI (browse, install, update, remove plugins)
- [ ] Plugin versioning and compatibility checks
- [ ] Secure sandboxing for third-party plugins
- [ ] User-uploaded plugin directory (marketplace backend)

## Core App UX
- [x] Tabbed/dockable panel for plugins
- [x] Customizable themes (dark/light/hacker green, etc.)
- [x] Quick launcher bar for built-in tools
- [x] Settings dialog (plugin mgmt, color themes, keybindings)
- [x] Widget system (weather, notepad, calendar, etc.)
- [x] Welcome/Onboarding screen (first-run experience)
- [x] Save and restore last session state

## System Integration
- [ ] Hotkey support for quick access (e.g., Ctrl+T for Terminal)
- [ ] Cross-platform compatibility (Windows/Linux/Mac)
- [ ] Installer packaging (MSIX, AppImage, etc.)

## Advanced Features (Optional/Inspiration)
- [ ] Built-in scripting for automating workflows
- [ ] Overlay mode (always on top, side-dockable)
- [ ] Drag-and-drop between plugins (e.g., drop a file onto Terminal)
- [ ] Remote plugin install (GitHub/marketplace URL)
- [ ] Live reload for plugins (without app restart)

## DevOps
- [ ] Automated build pipeline (GitHub Actions/Azure Pipelines)
- [ ] Unit tests for plugin loader and plugins
- [ ] Documentation: API, plugin, and user guide
- [ ] Issue tracker/roadmap integration (e.g., GitHub Projects)

## Inspiration — Future Plugins & Ideas
- [ ] AI Assistant plugin (GPT-powered, in-app support/chatbot)
- [ ] Packet sniffer/traffic monitor
- [ ] WiFi analyzer
- [ ] Crypto wallet/address generator
- [ ] Clipboard manager
- [ ] Clipboard encryption/scrubber
- [ ] Screenshot tool / quick image editor
- [ ] Terminal cheat sheet plugin (Linux commands)
    - Source: https://cheatography.com/davechild/cheat-sheets/linux-command-line/
- [ ] Penetration testing toolkit (Nmap, wordlist generator)
- [ ] Rootkit detection and process monitor
- [ ] Exploit database search plugin
- [ ] VPN/Proxy switcher

---

*End of list. Add or edit as you go!*
