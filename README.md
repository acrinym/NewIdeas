# NewIdeas
Generally new ideas. Private.

For details on building plugins for the **Cycloside** tray application, see
`docs/plugin-dev.md`. Example themes, skins, cursors and a WindowFX plugin can
be found under `docs/examples/`.

Cycloside now includes a lightweight **Skin/Theme Editor** accessible from the
tray menu. Edit your `.axaml` files with live previews and experiment with
different cursors before applying a new look.
Runtime settings allow enabling crash logging and toggling Safe Mode which
disables built-in plugins if they cause issues.

## VIS_AVS_JS

`VIS_AVS_JS` contains an experimental WebAssembly/JavaScript port of the classic
Winamp **Advanced Visualization Studio** plugin. Building the module requires
the [Emscripten](https://emscripten.org/) toolchain. In short you should:

1. Clone the `emsdk` repository and run `./emsdk install latest` followed by
   `./emsdk activate latest`.
2. Source `emsdk_env.sh` to update your environment (or run the provided batch
   file on Windows).

After the toolchain is set up you can compile the project by executing
`VIS_AVS_JS/build.sh`.

## Installing In-Browser AHK Script
1. Install the [Tampermonkey](https://www.tampermonkey.net/) browser extension.
2. Open `In-Browser AHK (AutoHotkey-like Features)-0.2.6.user.js` in your browser.
3. Tampermonkey should prompt you to install the script. Confirm the installation.
4. After installation, click the Tampermonkey icon and you should see "In-Browser AHK Settings" in the menu.
5. Use this menu to configure hotkeys or open the settings panel.
6. You can now export or import configurations as JSON or `.bahk` files from the settings panel and reset everything to defaults if needed.

## Chrome API Emulator

`chrome-api-emulator.user.js` provides a lightweight emulation of several Chrome extension APIs for Tampermonkey. The script exposes `chrome.storage.sync`, `chrome.contextMenus`, and basic runtime messaging. It also includes a small drag-and-drop zone so you can drop additional JavaScript files onto any page to load them as plugins.
