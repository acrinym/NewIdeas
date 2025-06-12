# Example Skin

Skins override visuals for a single window or control without touching the rest of the UI. Place `.axaml` skin files in `Cycloside/Skins/` and apply them with `SkinManager.ApplySkin(control, "MySkin")`.

```xml
<Styles xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Style Selector="Window">
    <Setter Property="Background" Value="#1b1b1b"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="Cursor" Value="Arrow"/>
  </Style>

  <Style Selector="Button">
    <Setter Property="Background" Value="#333"/>
    <Setter Property="Foreground" Value="#E0E0E0"/>
    <Setter Property="Cursor" Value="Hand"/>
  </Style>
</Styles>
```

Save this as `DarkSkin.axaml`. When combined with a theme, the skin's styles override only the controls it defines. Everything else keeps the theme resources.
