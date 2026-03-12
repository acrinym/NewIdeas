# Skinning API

Skins are the surface layer on top of the active theme. Use them to change window treatment, control styling, cursor feel, and optional window-specific presentation without redefining the whole app palette.

## Global Skin

```csharp
await SkinManager.ApplySkinAsync("Workbench");
await SkinManager.ApplySkinAsync("GlassDeck");
await SkinManager.ClearSkinAsync();
```

Global skin choice is stored in `settings.json` under `GlobalSkin`.

## Per-Window Skin

```csharp
SkinManager.ApplySkinTo(window, "Classic");
```

Per-plugin overrides are stored in `settings.json` under `PluginSkins`.

## Supported Skin Formats

- Legacy flat skin: `Cycloside/Skins/MySkin.axaml`
- Manifest-based skin pack:

```text
Cycloside/Skins/MySkin/
├── skin.json
└── Styles/
    ├── Global.axaml
    └── MainWindow.Primary.axaml
```

## Minimal Manifest Example

```json
{
  "name": "MySkin",
  "version": 1,
  "contract": "v1",
  "overlays": {
    "global": ["Styles/Global.axaml"],
    "bySelector": []
  },
  "replaceWindows": {}
}
```

## Minimal Style Example

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Style Selector="Button">
    <Setter Property="Background" Value="#333333"/>
    <Setter Property="Foreground" Value="#F0F0F0"/>
    <Setter Property="Cursor" Value="Hand"/>
  </Style>
</Styles>
```

Prefer structured packs for new work. Legacy flat `.axaml` skins still load for compatibility.
