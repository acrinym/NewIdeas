# Theming and Skinning

Cycloside separates **themes** and **skins**:

- **Themes** apply a cohesive style across the entire application. They are XAML resource files located in `Cycloside/Themes/` and can be swapped at runtime.
- **Skins** target individual windows or controls. A skin may replace visuals or backgrounds for a specific component without affecting the rest of the UI. Skin files live in `Cycloside/Skins/`.

## Switching Themes

Use the tray icon menu **Settings → Theme Settings** to choose a theme for each part of Cycloside. The **Themes** submenu lets you change the global theme quickly.

Themes are reusable. You can mix and match them for different components – for example, `MintGreen` for the main UI and `ConsoleGreen` for the text editor. Theme selections are stored in `settings.json`.

## Using Skins

Call `SkinManager.ApplySkin(control, "SkinName")` to apply a skin to any `StyledElement`. Skins can define custom controls, wallpapers and other assets.

Both systems work together: a component can have a theme for colors and a skin that swaps individual visuals.

## Cursors

Themes and skins can also define the cursor to use for any window or control.
Store the desired cursor in `settings.json` under `ComponentCursors` and apply
it with `CursorManager.ApplyFromSettings`. Values correspond to
`StandardCursorType` names (e.g. `Arrow`, `Hand`, `Ibeam`). Windows and plugins
can supply their own cursor via skins or themes as needed.

See the [theme example](examples/theme-example.md) and [skin example](examples/skin-example.md) for sample resource files. The [custom cursor example](examples/custom-cursor-example.md) shows how to configure cursors.

## Skin/Theme Editor

Use **Settings → Skin/Theme Editor** from the tray icon to edit theme and skin files.
Select a theme or skin, edit the XAML directly and hit **Preview** to test it on
a sample window. The editor also exposes a list of available cursors so you can
quickly try them out while designing your look.
