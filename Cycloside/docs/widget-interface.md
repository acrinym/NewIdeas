# Widget Interface

Cycloside currently has two widget layers in the repo.
They are related, but they are not the same host path.

## Stable plugin widget path

The stable path for plugins is `Cycloside.Widgets.IWidget`.

```csharp
public interface IWidget
{
    string Name { get; }
    string Description { get; }
    Control BuildView();
}
```

This is the contract exposed by `IPlugin.Widget`, and it is the path the built-in `WidgetHostPlugin` uses today.

### Current host behavior

- `WidgetHostPlugin` creates a `WidgetHostWindow`
- it loads built-in widgets through `WidgetManager.LoadBuiltIn()`
- it also asks each loaded plugin for `plugin.Widget`
- each widget view is placed on a canvas and can be dragged around

If you want a plugin widget that works with the default shell right now, target `IWidget`.

## Enhanced widget path

The repo also contains `IWidgetV2`, `BaseWidget`, `EnhancedWidgetHostViewModel`, and related classes under `Cycloside/Widgets/`.
That stack adds:

- lifecycle methods like `OnInitializeAsync()`, `OnActivateAsync()`, and `OnDestroyAsync()`
- configuration schema support
- theme notifications
- export and import hooks
- command routing
- richer sizing metadata

### Important limitation

The enhanced stack exists in code, but the built-in `WidgetHostPlugin` does not currently instantiate `EnhancedWidgetHostViewModel`.
That means `IWidgetV2` is available for advanced work and internal evolution, but it is not the default plugin widget host path yet.

## Recommendation

- For general plugin development, implement `IWidget`
- For experiments or future-facing widget work, derive from `BaseWidget` and implement `IWidgetV2`
- If you need both, keep `BuildView()` in the legacy path usable and let your richer host path call `BuildView(WidgetContext context)`

## Built-in examples

Simple `IWidget` examples exposed through plugins:

- MP3 player widget
- plugin marketplace widget
- any plugin returning a value from `IPlugin.Widget`

Enhanced `IWidgetV2` examples under `Cycloside/Widgets/`:

- `SystemMonitorWidget`
- `NetworkMonitorWidget`
- `QuickNotesWidget`
- `CalculatorWidget`

## Theme and skin considerations

- simple widgets should build visibly with no special skin assumptions
- enhanced widgets can use `WidgetContext` and the widget theme manager
- if you need full plugin-window theming hooks, use `PluginWindowBase` for the plugin itself and treat widgets as secondary surfaces
