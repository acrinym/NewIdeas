# Example Theme

This example shows a simple Avalonia theme pack. Create a directory (e.g. `Cycloside/Themes/MyTheme/`) and add a `theme.json` manifest plus style files. Select from **Settings → Theme Settings**.

## theme.json

```json
{
  "name": "My Theme",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "Example theme pack",
  "styles": ["Styles.axaml"]
}
```

## Styles.axaml

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

Place `Styles.axaml` in the theme directory. Themes with `Tokens.axaml` are discovered by `ThemeManager.GetAvailableThemes()`. See [theme-manifest-schema.md](../theme-manifest-schema.md) for the full manifest schema.
