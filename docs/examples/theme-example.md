# Example Theme

This example shows a simple Avalonia theme file that covers the common resources used by Cycloside. Save the file in `Cycloside/Themes/` with a `.axaml` extension and select it from **Settings → Theme Settings**.

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
