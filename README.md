# Cycloside

Cycloside is a small cross‑platform tray application built for tinkerers. It hosts plugins,
custom window effects and live theming so you can shape the desktop to your liking.

## Why Cycloside?
- Collects small utilities in one place: from a clipboard history viewer to
  a simple IDE and wallpaper manager.
- Lets you experiment with window effects and custom cursors.
- Works on Windows, Linux and macOS using the Avalonia UI framework.

## Use Cases
- Organise shortcuts and tools in a single tray icon.
- Inspect disk usage or running processes with the built‑in plugins.
- Edit themes/skins and instantly preview them on a test window.
- Run volatile Lua or C# scripts for quick experiments.

## Customising
1. Right‑click the tray icon to access **Settings**.
2. Open **Skin/Theme Editor** to modify `.axaml` theme files or cursor choices.
3. Toggle window effects or enable/disable plugins as desired.
4. Themes and plugin settings are stored in `settings.json` next to the
   executable.

For plugin development details see [`docs/plugin-dev.md`](docs/plugin-dev.md).
Examples live under [`docs/examples/`](docs/examples/).

## FUTURE TODO
- Package the editor as a standalone tool for theme creators.
- Expand the plugin marketplace for one‑click installs.
- Portable build scripts for Windows, Linux and macOS.

## VIS_AVS_JS
`VIS_AVS_JS` contains an experimental WebAssembly/JavaScript port of the classic
Winamp **Advanced Visualization Studio** plugin. Building the module requires the
[Emscripten](https://emscripten.org/) toolchain.

1. Clone the `emsdk` repository and run `./emsdk install latest` then
   `./emsdk activate latest`.
2. Source `emsdk_env.sh` or run the batch file on Windows.
3. Compile the project with `VIS_AVS_JS/build.sh`.

## In‑Browser AHK Script
1. Install the [Tampermonkey](https://www.tampermonkey.net/) extension.
2. Open `In-Browser AHK (AutoHotkey-like Features)-0.2.6.user.js` in your browser.
3. Tampermonkey will prompt to install; confirm it.
4. Click the Tampermonkey icon to access the settings panel and configure hotkeys.
5. Export or import configurations as JSON/`.bahk` files as needed.

## Chrome API Emulator
`chrome-api-emulator.user.js` emulates several Chrome extension APIs for
Tampermonkey. It exposes `chrome.storage.sync`, `chrome.contextMenus` and basic
runtime messaging. A small drag-and-drop zone lets you load extra scripts on the
fly.
