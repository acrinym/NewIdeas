# Dynamic Theming and Skinning

Cycloside now features a **dynamic theming and skinning system** built on Avalonia's FluentTheme with semantic tokens and selector-driven skins.

## Architecture Overview

The system operates on a hierarchical merge order (bottom to top):

1. **FluentTheme** (base Avalonia theme)
2. **App Base Tokens** (semantic resources from `Tokens.axaml`)
3. **Theme Variant Overrides** (Light/Dark/HighContrast specific tokens)
4. **Subtheme Pack** (theme-specific styling)
5. **Skin Global Styles** (application-wide skin overrides)
6. **Skin Per-Component/Window Overrides** (selector-based skin application)

## Semantic Tokens

All styling uses semantic tokens defined in `Cycloside/Themes/Tokens.axaml`:

### Color Tokens
```xml
<Color x:Key="SystemBackground">#FFFFFF</Color>
<Color x:Key="SystemForeground">#000000</Color>
<Color x:Key="SystemAccent">#0078D4</Color>
<Color x:Key="CanvasBackground">#FFFFFF</Color>
<Color x:Key="CardBackground">#F8F9FA</Color>
<Color x:Key="PrimaryText">#000000</Color>
<Color x:Key="SecondaryText">#666666</Color>
```

### Brush Tokens
```xml
<SolidColorBrush x:Key="SystemBackgroundBrush" Color="{DynamicResource SystemBackground}"/>
<SolidColorBrush x:Key="PrimaryTextBrush" Color="{DynamicResource PrimaryText}"/>
<SolidColorBrush x:Key="InteractiveHoverBrush" Color="{DynamicResource InteractiveHover}"/>
```

### Typography Tokens
```xml
<FontFamily x:Key="FontFamilyPrimary">Segoe UI</FontFamily>
<FontSize x:Key="FontSizeBody">13</FontSize>
<FontSize x:Key="FontSizeHeader">20</FontSize>
```

### Spacing Tokens
```xml
<Thickness x:Key="SpacingXS">2</Thickness>
<Thickness x:Key="SpacingM">8</Thickness>
<Thickness x:Key="SpacingXL">16</Thickness>
```

## Theme System

### Theme Variants

The system supports three built-in theme variants with automatic token switching:

- **Light Theme** (`Cycloside/Themes/LightTheme/Tokens.axaml`)
- **Dark Theme** (`Cycloside/Themes/DarkTheme/Tokens.axaml`)  
- **High Contrast Theme** (`Cycloside/Themes/HighContrastTheme/Tokens.axaml`)

### Dynamic Theme Switching

```csharp
// Apply theme variant
await ThemeManager.ApplyVariantAsync(ThemeVariant.Dark);

// Apply subtheme pack
await ThemeManager.ApplySubthemeAsync("MyCustomTheme");

// Apply both simultaneously
await ThemeManager.ApplyThemeAsync("MyCustomTheme", ThemeVariant.Light);

// Initialize from settings
ThemeManager.InitializeFromSettings();
```

### Available Themes
```csharp
var themes = ThemeManager.GetAvailableThemes();
var variants = ThemeManager.GetAvailableVariants();

// Listen for theme changes
ThemeManager.ThemeChanged += (sender, args) => {
    Console.WriteLine($"Theme changed to {args.ThemeName} ({args.Variant})");
};
```

## Skin System

### Skin Manifest Format

Skins are defined by JSON manifests (e.g., `Cycloside/Skins/Classic/skin.json`):

```json
{
  "name": "Classic",
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
      },
      {
        "type": "Button",
        "classes": ["danger"],
        "styles": ["Styles/DangerButton.axaml"]
      }
    ]
  },
  "replaceWindows": {
    "MainWindow": "Components/MainWindow.axaml"
  }
}
```

### Skin Selectors

Skins support multiple selector types:

1. **By Type**: `"type": "Button"` targets all Button controls
2. **By Classes**: `"classes": ["primary"]` targets elements with specific classes
3. **By Name**: `"xName": "ApplyButton"` targets specific named elements
4. **Combined**: Mix types, classes, and names for precise targeting

### Dynamic Skin Application

```csharp
// Apply skin to application
await SkinManager.ApplySkinAsync("Classic");

// Apply skin to specific element
SkinManager.ApplySkinTo(window, "Classic");

// Check available skins
var skins = SkinManager.GetAvailableSkins();

// Check if skin supports window replacement
bool supportsReplacement = await SkinManager.SupportsWindowReplacementAsync("Classic", "MainWindow");

// Listen for skin changes
SkinManager.SkinChanged += (sender, args) => {
    Console.WriteLine($"Skin changed to {args.SkinName}");
};
```

### Skin Styles

Skin style files use semantic tokens and can override any control:

```xml
<!-- Styles/Global.axaml -->
<Style xmlns="https://github.com/avaloniaui">
  <Style Selector="Button">
    <Setter Property="Padding" Value="{DynamicResource SpacingM}"/>
    <Setter Property="CornerRadius" Value="{DynamicResource RadiusS}"/>
    <Setter Property="Background" Value="{DynamicResource CardBackgroundBrush}"/>
    <Setter Property="Foreground" Value="{DynamicResource PrimaryTextBrush}"/>
  </Style>
  
  <Style Selector="Button.danger">
    <Setter Property="Background" Value="{DynamicResource SystemErrorBrush}"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="FontWeight" Value="Bold"/>
  </Style>
</Style>
```

## Building Custom Themes and Skins

### Creating a Theme

1. **Create theme directory**: `Cycloside/Themes/MyTheme/`
2. **Add Tokens.axaml**: Override semantic tokens for your theme
3. **Add *.axaml files**: Define additional styles as needed
4. **Test**: Use `ThemeManager.ApplySubthemeAsync("MyTheme")`

### Creating a Skin

1. **Create skin directory**: `Cycloside/Skins/MySkin/`
2. **Create skin.json**: Define manifest with selectors and overlays
3. **Add Styles/**: Create overlay style files as referenced in manifest
4. **Add Components/**: Create full window replacement files (optional)
5. **Test**: Use `SkinManager.ApplySkinAsync("MySkin")`

### Element Targeting

Add classes and names to XAML for skin targeting:

```xml
<!-- MainWindow.axaml -->
<Window Classes="primary" Background="{DynamicResource CanvasBackgroundBrush}">
  <Button x:Name="ApplyButton" Classes="danger primary">Apply</Button>
  <Grid Classes="main-content">
    <!-- content -->
  </Grid>
</Window>
```

## Settings Integration

Theme and skin preferences are stored in `settings.json`:

```json
{
  "GlobalTheme": "MyCustomTheme",
  "RequestedThemeVariant": "Dark",
  "PluginSkins": {
    "ProcessMonitor": "Classic",
    "TextEditor": "Minimal"
  }
}
```

## Plugin Integration

Plugins can access theming through standardized interfaces:

```csharp
// Apply theme to plugin window
ThemeManager.ApplyForPlugin(myWindow, myPlugin);

// Apply skin specifically
SkinManager.ApplySkinTo(myWindow, "PluginSpecificSkin");
```

Plugins should use semantic tokens in their XAML rather than hard-coded colors:

```xml
<!-- Use semantic tokens -->
<Button Background="{DynamicResource SystemAccentBrush}"/>

<!-- Instead of hard-coded colors -->
<Button Background="#0078D4"/>
```

## Migration from Legacy System

### Legacy Theme Files

Old theme files in `Cycloside/Themes/Global/` are still supported but should migrate to the new token system:

```xml
<!-- Legacy (still works) -->
<Setter Property="Background" Value="{StaticResource ThemeBackgroundBrush}"/>

<!-- New (preferred) -->
<Setter Property="Background" Value="{DynamicResource SystemBackgroundBrush}"/>
```

### Legacy Skin Files

Old skin files in `Cycloside/Skins/` continue to work but should be migrated to manifest-based skins for better organization and targeting.

## Performance Notes

- Themes and skins are cached after first load
- Dynamically switching themes applies to all open windows without restart
- Resource dictionaries are properly cleaned up to prevent memory leaks
- Manifest-driven skins are validated on load with error handling

## Cross-Platform Support

The entire theming system is platform-agnostic and works identically on Windows, Linux, and macOS. All theme resources are applied via Avalonia's cross-platform styling system.