# Theming and Skinning

Cycloside separates **themes** and **skins**:

- **Themes** apply a cohesive style across the entire application. They are XAML resource files located in `Cycloside/Themes/` and can be swapped at runtime.
- **Skins** target individual windows or controls. A skin may replace visuals or backgrounds for a specific component without affecting the rest of the UI. Skin files live in `Cycloside/Skins/`.

## Switching Themes

Use the tray icon menu **Settings → Appearance Editor** to choose the global theme. Theme selections are stored in `settings.json` under `GlobalTheme`.

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

## Appearance Editor

Open **Settings → Appearance Editor** from the tray icon to manage themes and skins.
Select the global theme from the dropdown, assign component skins, or directly
edit theme files. Preview renders your XAML at runtime so you immediately see
any errors or layout issues. The cursor list lets you experiment with different
`StandardCursorType` values while designing.
