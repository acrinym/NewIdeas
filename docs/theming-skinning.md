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
