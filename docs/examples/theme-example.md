# Example Theme

This example shows a simple Avalonia theme file that covers the common resources used by Cycloside.

**Two ways to use themes:**

1. **Single file (global theme)** — Save one `.axaml` file in `Cycloside/Themes/Global/` (e.g. `DarkExample.axaml`). It will appear in **Settings → Theme Settings** under the global theme dropdown. No pack directory or `Tokens.axaml` required.
2. **Theme pack (directory)** — Create a directory `Cycloside/Themes/MyTheme/` with a **required** `Tokens.axaml` (used for discovery). Add `theme.json` and style files. The pack appears in Theme Settings only if `Tokens.axaml` exists in that directory.

## Theme Pack with Manifest

For a full theme pack (directory-based), add `Tokens.axaml` and `theme.json`:

**Required:** Create `Tokens.axaml` in the pack directory. Discovery in Theme Settings is gated on this file. Minimal content: a `<Styles>` root with Avalonia/XAML namespaces; see existing themes under `Themes/` for examples.

```json
{
  "name": "MyTheme",
  "version": "1.0.0",
  "author": "You",
  "description": "My custom theme",
  "styles": ["Styles.axaml"],
  "assets": {
    "images": ["images/logo.png"],
    "sounds": ["sounds/click.wav"]
  },
  "scripts": {
    "lua": ["scripts/init.lua"]
  }
}
```

Example `scripts/init.lua`:

```lua
function OnApply()
  theme.setSetting("accent", "#ff0000")
end
```

```xml
<Styles xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Styles.Resources>
    <!-- Global colors used throughout the UI -->
    <Color x:Key="ThemeBackgroundColor">#202020</Color>
    <Color x:Key="ThemeForegroundColor">#FFFFFF</Color>
    <Color x:Key="EditorRed">#FF5555</Color>
    <Color x:Key="EditorBlue">#55AAFF</Color>

    <SolidColorBrush x:Key="ThemeBackgroundBrush" Color="{StaticResource ThemeBackgroundColor}"/>
    <SolidColorBrush x:Key="ThemeForegroundBrush" Color="{StaticResource ThemeForegroundColor}"/>
    <SolidColorBrush x:Key="EditorRedBrush" Color="{StaticResource EditorRed}"/>
    <SolidColorBrush x:Key="EditorBlueBrush" Color="{StaticResource EditorBlue}"/>
  </Styles.Resources>

  <!-- Default window look and cursor -->
  <Style Selector="Window">
    <Setter Property="Background" Value="{StaticResource ThemeBackgroundBrush}"/>
    <Setter Property="Foreground" Value="{StaticResource ThemeForegroundBrush}"/>
    <Setter Property="Cursor" Value="Arrow"/>
  </Style>

  <!-- Basic control styling -->
  <Style Selector="Button">
    <Setter Property="Foreground" Value="Black"/>
    <Setter Property="Cursor" Value="Hand"/>
  </Style>

  <Style Selector="TextBox">
    <Setter Property="Background" Value="{StaticResource ThemeBackgroundBrush}"/>
    <Setter Property="Foreground" Value="Black"/>
    <Setter Property="Cursor" Value="Ibeam"/>
  </Style>
</Styles>
```

Save this file as `DarkExample.axaml` and pick it as the global theme. Specific plugins can apply their own skins via `ComponentSkins` in `settings.json`.
