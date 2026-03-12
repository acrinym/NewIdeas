# Example Skin Pack

Create a skin pack under `Cycloside/Skins/MySkin/`.

## `skin.json`

```json
{
  "name": "MySkin",
  "version": 1,
  "contract": "v1",
  "overlays": {
    "global": ["Styles/Global.axaml"],
    "bySelector": [
      {
        "type": "MainWindow",
        "classes": ["primary"],
        "styles": ["Styles/MainWindow.Primary.axaml"]
      }
    ]
  },
  "replaceWindows": {}
}
```

## `Styles/Global.axaml`

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Style Selector="Button">
    <Setter Property="Background" Value="#333333"/>
    <Setter Property="Foreground" Value="#F0F0F0"/>
    <Setter Property="BorderBrush" Value="#7A7A7A"/>
    <Setter Property="Cursor" Value="Hand"/>
  </Style>

  <Style Selector="TextBox">
    <Setter Property="Background" Value="#1E1E1E"/>
    <Setter Property="Foreground" Value="#F0F0F0"/>
    <Setter Property="BorderBrush" Value="#7A7A7A"/>
  </Style>
</Styles>
```

## `Styles/MainWindow.Primary.axaml`

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Style Selector="Window.primary TabControl">
    <Setter Property="Margin" Value="14"/>
  </Style>
</Styles>
```

Select the pack as the global shell skin or assign it to a specific plugin window through `PluginSkins` in `settings.json`.
