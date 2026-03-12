# Gweled Port Notes

## Source reference

The current Cycloside `Gweled` plugin was built against the locally provided source tree:

- `C:\Users\User\Downloads\TWorld\gweled-1.0-beta1`

That tree is the original GTK / Clutter game and includes the authoritative mode structure, score progression, and timed-mode decay behavior.

## Current Cycloside direction

Cycloside does not host the original GTK stack.
The plugin in `Cycloside/Plugins/BuiltIn/GweledPlugin.cs` is a native Avalonia implementation that keeps the important gameplay intent:

- `8x8` board
- `7` gem types
- `Normal`, `Timed`, and `Endless` modes
- level thresholds that double upward
- timed-mode backwards drift toward the prior threshold
- no-moves reshuffle for `Timed` and `Endless`
- no-moves game over for `Normal`
- auto-hints after idle time

## Audio

Cycloside now supports `.ogg` playback through `AudioService`, so the plugin can use the original `click.ogg` and `swap.ogg` files when the local source tree is present.
The current lookup path checks:

- `Cycloside/bin/.../Resources/Gweled/Sounds`
- `%USERPROFILE%\Downloads\TWorld\gweled-1.0-beta1\sounds`

## Working rule

Use the original Gweled source as a behavior reference.
Keep Cycloside's implementation native, theme-friendly, and shell-friendly instead of trying to transplant the GTK engine wholesale.
