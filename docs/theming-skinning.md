# Dynamic Theming and Skinning

Cycloside uses a two-layer appearance model:

- **Theme**: the app-wide semantic palette, typography, spacing, and overall color system.
- **Skin**: the surface treatment layered on top of the current theme for windows, controls, cursors, and optional window replacements.

That split matches the original intent of the shell: themes define the whole desktop mood, skins reshape how the shell presents it.

Animated backdrops sit alongside that split: they are optional media or managed-visualizer layers shown behind windows that opt into the appearance pipeline.

## Merge Order

Cycloside applies appearance resources in this order:

1. `FluentTheme`
2. `Cycloside/Themes/Tokens.axaml`
3. Theme variant tokens from `Themes/LightTheme` or `Themes/DarkTheme`
4. Selected theme pack from `Themes/<ThemeName>/`
5. Selected global skin from `Skins/<SkinName>/` or `Skins/<SkinName>.axaml`
6. Optional per-plugin window skin from `settings.json`
7. Optional animated backdrop from `GlobalAnimatedBackground`, component overrides, or plugin overrides

## Built-In Theme Packs

Structured theme packs live in `Cycloside/Themes/<ThemeName>/`.

- `Dockside`: dark teal shell with cool accents
- `AmberCRT`: warm monochrome retro terminal palette
- `OrchardPaper`: softer paper-and-ink desktop palette
- `SynthwaveDream`: neon magenta-and-cyan late-night desktop palette
- `Cyberpunk`: sharper neon-city palette for pairing with darker skins
- `Magical`: moonlit violet-and-gold ritual desktop palette with Cycloside-native magical progress surfaces

Legacy flat themes in `Cycloside/Themes/Global/` still load for compatibility.

## Theme Variants

Cycloside currently supports:

- `Default`: follow the OS theme variant
- `Light`
- `Dark`

Runtime switching:

```csharp
await ThemeManager.ApplyThemeAsync("Dockside", ThemeVariant.Dark);
await ThemeManager.ApplySubthemeAsync("OrchardPaper");
await ThemeManager.ApplyVariantAsync(ThemeVariant.Light);
ThemeManager.InitializeFromSettings();
```

## Theme Pack Layout

Create a theme directory:

```text
Cycloside/Themes/MyTheme/
├── Tokens.axaml
└── Styles.axaml
```

`Tokens.axaml` should override semantic resources:

```xml
<Style xmlns="https://github.com/avaloniaui"
       xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Style.Resources>
    <Color x:Key="SystemAccent">#3D8A5F</Color>
    <Color x:Key="CanvasBackground">#F2EFE4</Color>
    <Color x:Key="CardBackground">#FCF8EC</Color>
    <Color x:Key="PrimaryText">#1F2A22</Color>
    <Color x:Key="BorderDefault">#9AA686</Color>
  </Style.Resources>
</Style>
```

`Styles.axaml` can add app-wide style selectors:

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

## Skin Packs

Manifest-based skins live in `Cycloside/Skins/<SkinName>/`.

- `Workbench`: squared tinkerer shell
- `Classic`: sharper Win9x-style shell treatment
- `GlassDeck`: softer floating glass treatment
- `Win98`: gray beveled shell treatment
- `AfterDark`: dark neon screensaver-style shell treatment
- `ProgramManager31`: square early-Windows shell treatment

Legacy flat skins in `Cycloside/Skins/*.axaml` still load.

## Skin Manifest

```json
{
  "name": "Workbench",
  "version": 1,
  "contract": "v1",
  "overlays": {
    "global": ["Styles/Global.axaml"],
    "bySelector": [
      {
        "type": "MainWindow",
        "classes": ["primary"],
        "styles": ["Styles/MainWindow.Primary.axaml"]
      },
      {
        "xName": "ApplyButton",
        "styles": ["Styles/ApplyButton.axaml"]
      }
    ]
  },
  "replaceWindows": {}
}
```

Notes:

- Global overlay styles affect the whole app when the skin is selected globally.
- Selector-targeted files are still ordinary Avalonia style sheets; the manifest keeps the pack organized.
- `replaceWindows` is optional. Use `UserControl` replacement files when you need full window-content swaps.

## Skin Style Files

Skin style sheets must use a `<Styles>` root when they contain multiple selectors:

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Style Selector="Button">
    <Setter Property="Padding" Value="12,6"/>
    <Setter Property="Background" Value="{DynamicResource CardBackgroundBrush}"/>
    <Setter Property="BorderBrush" Value="{DynamicResource BorderStrongBrush}"/>
    <Setter Property="Cursor" Value="Hand"/>
  </Style>

  <Style Selector="Button.danger">
    <Setter Property="Background" Value="{DynamicResource SystemErrorBrush}"/>
    <Setter Property="Foreground" Value="White"/>
  </Style>
</Styles>
```

## Animated Backdrops

Cycloside can render animated window backdrops in three modes:

- `None`: no animated backdrop
- `Media`: still images, animated GIFs, or video files such as `mp4`, `m4v`, `avi`, `mov`, `wmv`, `mkv`, `webm`, `ogv`, `ogg`, and `flv`
- `Visualizer`: any built-in managed visualizer rendered behind the window content

The theme settings window exposes a global backdrop selector. The underlying settings model also supports component and plugin overrides through `ComponentAnimatedBackgrounds` and `PluginAnimatedBackgrounds`.

Appearance settings are stored in `Cycloside/settings.json`:

```json
{
  "GlobalTheme": "Dockside",
  "RequestedThemeVariant": "Dark",
  "GlobalSkin": "Workbench",
  "GlobalAnimatedBackground": {
    "Mode": "Visualizer",
    "Visualizer": "Starfield",
    "Opacity": 0.55,
    "Loop": true,
    "MuteVideo": true
  },
  "ComponentAnimatedBackgrounds": {
    "MainWindow": {
      "Mode": "Media",
      "Source": "D:/Media/after-dark-loop.mp4",
      "Opacity": 0.48,
      "Loop": true,
      "MuteVideo": true
    }
  },
  "PluginSkins": {
    "Text Editor": "GlassDeck",
    "Notification Center": "Classic"
  }
}
```

## Runtime APIs

```csharp
var themes = ThemeManager.GetAvailableThemes();
var variants = ThemeManager.GetAvailableVariants();
var skins = SkinManager.GetAvailableSkins();
var visualizers = AnimatedBackgroundManager.GetAvailableVisualizers();

await ThemeManager.ApplyThemeAsync("Dockside", ThemeVariant.Dark);
await SkinManager.ApplySkinAsync("Workbench");
SkinManager.ApplySkinTo(myPluginWindow, "GlassDeck");
AnimatedBackgroundManager.ReapplyAllWindows();
```

## Code-Built UI

Older plugins in Cycloside still build parts of their UI in C# instead of XAML. Those controls should participate in themes and skins through semantic classes, not local hard-coded brushes.

Use `Cycloside.Services.AppearanceHelper` for code-built UI:

```csharp
AppearanceHelper.ApplyCardSurface(containerBorder);
AppearanceHelper.ApplyStatusChip(statusBorder);
AppearanceHelper.ApplyButtonRole(runButton, SemanticButtonRole.Accent);
AppearanceHelper.ApplyButtonRole(stopButton, SemanticButtonRole.Danger);
AppearanceHelper.ApplyCodeEditor(editor);
```

Shared semantic classes currently include:

- `surface-card`
- `status-chip`
- `warning-panel`
- `accent-strip`
- `accent`
- `success`
- `danger`
- `warning`
- `neutral`
- `secondary`
- `inverse`
- `code-editor`

Cycloside also ships a native `Cycloside.Controls.MagicalProgressBar` control, and standard `ProgressBar` controls can opt into the same look with the `magical` class.

## Authoring Guidance

- Use semantic tokens with `DynamicResource`; do not hard-code production colors into plugin XAML.
- Use `AppearanceHelper` and semantic classes for code-built UI; do not set production brushes directly in plugin C#.
- Keep theme packs responsible for palette and broad shell identity.
- Keep skins responsible for treatment, chrome, cursor feel, and per-window flavor.
- Animated backdrops work best when the relevant window surface is partially translucent instead of fully opaque.
- Preserve legacy flat files only for compatibility. Prefer structured theme and skin packs for new work.
