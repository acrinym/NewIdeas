# Theming and Skinning

Cycloside separates **themes** and **skins**:

- **Themes** apply a cohesive style across the entire application. They are XAML resource files located in `Cycloside/Themes/` and can be swapped at runtime.
- **Skins** target individual windows or controls. A skin may replace visuals or backgrounds for a specific component without affecting the rest of the UI. Skin files live in `Cycloside/Skins/`.

## Switching Themes

Use the tray icon menu **Settings → Theme Settings** to choose the global theme. Theme selections are stored in `settings.json` under `GlobalTheme`.

## Using Skins

Call `SkinManager.ApplySkin(control, "SkinName")` to apply a skin to any `StyledElement`. Skins can define custom controls, wallpapers and other assets.

Both systems work together: a component can have a theme for colors and a skin that swaps individual visuals.

## Cursors

Themes and skins can also define the cursor to use for any window or control.
Store the desired cursor in `settings.json` under `ComponentCursors` and apply
it with `CursorManager.ApplyFromSettings`. Values correspond to
`StandardCursorType` names (e.g. `Arrow`, `Hand`, `Ibeam`). Windows and plugins
can supply their own cursor via skins or themes as needed.

### Blank Theme/Skin

The repository includes `Blank.axaml` files in both `Cycloside/Themes/Global/`
and `Cycloside/Skins/`. Copy these as a starting point for your own design.
Each file is a minimal `<Styles>` block where you can define colors, brushes and
styles. Example:

```xml
<Styles xmlns="https://github.com/avaloniaui">
  <Styles.Resources>
    <!-- add ThemeBackgroundBrush etc -->
  </Styles.Resources>
</Styles>
```

After saving your theme or skin, select it from **Settings → Theme Settings** or
apply it in code via `SkinManager.ApplySkin(control, "MySkin")`.

See the [theme example](examples/theme-example.md) and [skin example](examples/skin-example.md) for sample resource files. The [custom cursor example](examples/custom-cursor-example.md) shows how to configure cursors.

## Skin/Theme Editor

Use **Settings → Skin/Theme Editor** from the tray icon to edit theme and skin files.
Select a theme or skin, edit the XAML directly and hit **Preview** to test it.
The preview window now loads your markup at runtime so the layout matches the
XAML exactly. If parsing fails, the error message is displayed inside the
preview. The editor also exposes a list of available cursors so you can quickly
try them out while designing your look.
