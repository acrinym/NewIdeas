# Example Theme Pack

Create a theme pack under `Cycloside/Themes/MyTheme/`.

## `Tokens.axaml`

```xml
<Style xmlns="https://github.com/avaloniaui"
       xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Style.Resources>
    <Color x:Key="SystemAccent">#3D8A5F</Color>
    <Color x:Key="SystemHighlight">#62A97C</Color>
    <Color x:Key="CanvasBackground">#F2EFE4</Color>
    <Color x:Key="CardBackground">#FCF8EC</Color>
    <Color x:Key="PanelBackground">#E6DFC9</Color>
    <Color x:Key="PrimaryText">#1F2A22</Color>
    <Color x:Key="BorderDefault">#9AA686</Color>
  </Style.Resources>
</Style>
```

## `Styles.axaml`

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Style Selector="Menu">
    <Setter Property="Background" Value="{DynamicResource CardBackgroundBrush}"/>
    <Setter Property="Foreground" Value="{DynamicResource PrimaryTextBrush}"/>
  </Style>

  <Style Selector="TabItem:selected">
    <Setter Property="BorderBrush" Value="{DynamicResource SystemAccentBrush}"/>
    <Setter Property="BorderThickness" Value="0,0,0,2"/>
  </Style>
</Styles>
```

Pick the pack from **Settings -> Theme Settings** as the global theme. Add `RequestedThemeVariant` if you want to force `Light` or `Dark`.
