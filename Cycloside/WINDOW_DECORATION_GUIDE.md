# Window Decoration Integration Guide

This guide shows plugin developers how to add WindowBlinds-style themed window chrome to their plugin windows.

## Quick Start

The easiest way to create a themed window:

```csharp
using Cycloside.Helpers;

// Instead of:
var window = new Window { Title = "My Window", Content = myContent };

// Use:
var window = WindowDecorationHelper.CreateThemedWindow(
    title: "My Window",
    content: myContent,
    width: 800,
    height: 600
);
window.Show();
```

That's it! Your window now has custom title bar, borders, and themed buttons.

## Method 1: DecoratedWindow (Direct Use)

Use `DecoratedWindow` directly instead of `Window`:

```csharp
using Cycloside.Controls;

public void Start()
{
    var window = new DecoratedWindow
    {
        Title = "My Plugin Window",
        Width = 600,
        Height = 400,
        Content = new MyContentView(),
        DataContext = this
    };

    window.Show();
}
```

## Method 2: WindowDecorationHelper (Factory Methods)

Use helper methods for common scenarios:

### Basic Window
```csharp
var window = WindowDecorationHelper.CreateThemedWindow(
    title: "My Window",
    content: myView,
    width: 800,
    height: 600,
    canResize: true
);
```

### Window with ViewModel (MVVM)
```csharp
var window = WindowDecorationHelper.CreateThemedWindowWithViewModel(
    title: "Settings",
    content: new SettingsView(),
    dataContext: new SettingsViewModel(),
    width: 600,
    height: 500
);
```

### Plugin Window (with automatic positioning)
```csharp
var window = WindowDecorationHelper.CreatePluginWindow(
    pluginName: "MyPlugin",  // Used for startup positioning config
    title: "My Plugin",
    content: myView,
    width: 800,
    height: 600
);
window.Show();
```

### Centered Dialog
```csharp
var dialog = WindowDecorationHelper.CreateCenteredDialog(
    title: "Confirmation",
    content: dialogContent,
    width: 400,
    height: 200
);
dialog.ShowDialog(parentWindow);
```

### Standard Dialog with Buttons
```csharp
var dialog = WindowDecorationHelper.CreateDialog(
    title: "Delete File?",
    message: "Are you sure you want to delete this file?",
    onOk: (window) => {
        // Delete file
        Logger.Log("File deleted");
    },
    onCancel: (window) => {
        Logger.Log("Cancelled");
    }
);
dialog.Show();
```

### Info Message
```csharp
WindowDecorationHelper.ShowInfoDialog(
    title: "Success",
    message: "Operation completed successfully!"
);
```

## Method 3: Extension Methods (Fluent API)

Use extension methods for concise, chainable code:

```csharp
using Cycloside.Extensions;

var window = new DecoratedWindow { Title = "My Window" }
    .WithPaddedContent(myView, padding: 20)
    .WithStandardSettings("MyPlugin")
    .Configure(w => {
        w.Width = 800;
        w.Height = 600;
    })
    .ShowWithLogging("MyPlugin");
```

### Convert Existing Window
```csharp
// You have an existing Window
var oldWindow = new Window { Title = "Old", Content = myContent };

// Convert to DecoratedWindow
var decoratedWindow = oldWindow.ToDecoratedWindow();
decoratedWindow.Show();
```

### Replace Window In-Place
```csharp
// Replace and show in one step
_currentWindow = _currentWindow.ReplaceWithDecorated(
    onClosed: () => Logger.Log("Window closed")
);
```

## Method 4: Convert Existing Plugin

If you have an existing plugin with a window, here's how to add decorations:

### Before:
```csharp
public class MyPlugin : ObservableObject, IPlugin
{
    private Views.MyWindow? _window;

    public void Start()
    {
        _window = new Views.MyWindow { DataContext = this };
        _window.Show();
    }
}
```

### After (Option A - Change window type):
```csharp
using Cycloside.Controls;

public class MyPlugin : ObservableObject, IPlugin
{
    private Window? _window;  // Changed to base Window type

    public void Start()
    {
        // Use DecoratedWindow as base for your window
        _window = new DecoratedWindow
        {
            Title = "My Plugin",
            Content = new MyContentView(),
            DataContext = this,
            Width = 800,
            Height = 600
        };
        _window.Show();
    }
}
```

### After (Option B - Use helper):
```csharp
using Cycloside.Helpers;

public void Start()
{
    _window = WindowDecorationHelper.CreatePluginWindow(
        pluginName: "MyPlugin",
        title: "My Plugin",
        content: new MyContentView(),
        width: 800,
        height: 600
    );
    _window.DataContext = this;
    _window.Show();
}
```

## Real-World Example: MP3 Player Integration

Here's how the MP3PlayerPlugin could add themed decorations:

```csharp
public class MP3PlayerPlugin : ObservableObject, IPlugin
{
    private Window? _window;

    [RelayCommand]
    private void Start()
    {
        if (UseSkinned)
        {
            // Winamp skin - no decoration needed (custom chrome)
            _window = new Views.SkinnedMP3PlayerWindow { DataContext = this };
        }
        else
        {
            // Modern UI - use decorated window
            _window = WindowDecorationHelper.CreatePluginWindow(
                pluginName: "MP3Player",
                title: "Cycloside MP3 Player",
                content: new Views.MP3PlayerWindow { DataContext = this },
                width = 800,
                height = 600
            );
        }

        _window.Show();
    }
}
```

## Checking Theme Status

Check if a window should be themed based on configuration:

```csharp
using Cycloside.Extensions;

if (_window.ShouldBeThemed())
{
    Logger.Log("This window will use custom decorations");
}
```

Or check by title:

```csharp
if (WindowDecorationHelper.ShouldThemeWindow("My Window"))
{
    // Create decorated version
}
else
{
    // Use standard window
}
```

## Custom Content Panels

Create content with consistent spacing:

```csharp
// Simple padded content
var content = WindowDecorationHelper.CreateContentPanel(
    innerContent: myView,
    padding: 16
);

// Content with background color
var content = WindowDecorationHelper.CreateContentPanelWithBackground(
    innerContent: myView,
    backgroundColor: Colors.White,
    padding: 20
);
```

## Integration with WindowPositioningService

`CreatePluginWindow` automatically applies window positioning from startup config:

```csharp
// This window will use saved position if available
var window = WindowDecorationHelper.CreatePluginWindow(
    pluginName: "MyPlugin",  // Must match plugin name in config
    title: "My Window",
    content: myView
);
```

Or apply positioning manually:

```csharp
var window = new DecoratedWindow { /* ... */ };
WindowPositioningService.Instance.ApplyPosition(window, "MyPlugin");
```

Or use extension method:

```csharp
var window = new DecoratedWindow { /* ... */ }
    .WithStandardSettings("MyPlugin");  // Applies positioning + effects
```

## Theme Management

Themes are automatically managed by `WindowDecorationManager`. All `DecoratedWindow` instances automatically:
- Subscribe to theme changes
- Update when user changes theme
- Apply active/inactive states
- Handle button hover/pressed states

### Loading a Theme

Themes can be loaded programmatically:

```csharp
var manager = WindowDecorationManager.Instance;

// Load from directory
manager.LoadTheme("path/to/theme/directory");

// Load from ZIP
manager.LoadTheme("path/to/theme.zip");

// Import theme (copies to user directory and loads)
manager.ImportTheme("path/to/external/theme");
```

### Listening to Theme Changes

```csharp
WindowDecorationManager.Instance.ThemeChanged += (theme) =>
{
    Logger.Log($"Theme changed to: {theme?.Name ?? "None"}");
};
```

## Configuration Options

### Window-Specific Exclusions

Edit theme's `theme.ini` to exclude specific windows:

```ini
[Behavior]
ExcludedWindows=Terminal,Console,DebugWindow
```

### Window-Specific Inclusions

Only theme certain windows:

```ini
[Behavior]
ApplyToAllWindows=false
IncludedWindows=MyPlugin,AnotherPlugin,SettingsWindow
```

## Migration Checklist

Converting an existing plugin to use decorations:

- [ ] Change `Window` field type to `Window?` (to support both types)
- [ ] Update window creation to use `DecoratedWindow` or helper
- [ ] Test with different themes (Modern Dark, Classic XP, Aero)
- [ ] Verify positioning still works
- [ ] Test window resize, maximize, minimize
- [ ] Check active/inactive state appearance
- [ ] Test button hover and click interactions
- [ ] Verify dialog windows (if any) also use decorations

## Best Practices

1. **Use helpers over direct construction** - Helpers handle common setup automatically
2. **Apply positioning with `CreatePluginWindow`** - Integrates with startup config
3. **Use fluent API for complex setup** - Extension methods enable clean, readable code
4. **Consistent padding** - Use `CreateContentPanel` for standard 16px padding
5. **Log window operations** - Use `ShowWithLogging` extension for visibility
6. **Test with multiple themes** - Ensure your content works with light and dark themes
7. **Respect user preferences** - Honor exclusion/inclusion settings

## Troubleshooting

### Window doesn't show theme
- Check if window title is in `ExcludedWindows` in theme.ini
- Verify theme is loaded: `WindowDecorationManager.Instance.CurrentTheme`
- Ensure using `DecoratedWindow` not regular `Window`

### Buttons don't respond
- Buttons are handled by `DecoratedWindow` automatically
- Don't add custom title bar controls (they'll conflict)

### Content overlaps title bar
- Use `CreateContentPanel` for proper spacing
- Don't set negative margins on content
- DecoratedWindow automatically adjusts content area

### Theme changes don't apply
- DecoratedWindow subscribes to theme changes automatically
- Regular Windows don't support themes
- Convert to DecoratedWindow or recreate window

### Position not saving
- Ensure plugin name matches in `CreatePluginWindow`
- Check startup config has position saved
- Verify WindowPositioningService is initialized

## Examples in Codebase

See these plugins for reference implementations:

- **MP3PlayerPlugin** - Toggles between skinned and decorated modes
- **DesktopCustomizationPlugin** - Theme manager UI
- **Hardware Monitor** - Simple decorated window example

## Additional Resources

- **Theme Creation:** See `Themes/WindowDecorations/README.md`
- **Sample Themes:** `Themes/WindowDecorations/{ModernDark,ClassicXP,AeroGlass}`
- **API Reference:** `Cycloside/Services/WindowDecorationManager.cs`
- **Custom Control:** `Cycloside/Controls/DecoratedWindow.axaml.cs`
