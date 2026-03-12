# Plugin API

This document is the current source of truth for writing Cycloside plugins against the code in this repo.

## Core contract

Every plugin implements `Cycloside.Plugins.IPlugin` from `Cycloside/SDK/IPlugin.cs`.

```csharp
public interface IPlugin
{
    string Name { get; }
    string Description { get; }
    Version Version { get; }
    Cycloside.Widgets.IWidget? Widget { get; }
    bool ForceDefaultTheme { get; }
    void Start();
    void Stop();
}
```

### What each member means

- `Name`: display name used in menus, settings, and workspace surfaces
- `Description`: short summary shown in plugin management UIs
- `Version`: semantic version used by the host to mark plugins as new or updated
- `Widget`: optional widget surface exposed to the built-in widget host
- `ForceDefaultTheme`: blocks plugin-specific theme overrides and keeps the plugin on the global appearance path
- `Start()`: create windows, subscribe to bus topics, start timers, and allocate resources
- `Stop()`: unsubscribe, stop timers, close windows, and release resources

## Optional interfaces

### `IPluginMetadata`

`Cycloside/SDK/IPluginMetadata.cs`

- `PluginId` lets a plugin override the stable config key used by first-run and other persisted preferences
- if `PluginId` is not provided, Cycloside falls back to the implementation type name such as `JezzballPlugin`
- `Category` groups plugins in the tray menu and plugin manager
- `EnabledByDefault` controls first-run default loading
- `IsCore` marks shell-defining plugins for future onboarding and profile flows

### `IPluginExtended`

`Cycloside/SDK/IPluginExtended.cs`

- `OnSettingsSaved()` is called after `SettingsManager.Save()` completes successfully
- `OnCrash(Exception ex)` is called if `Start()` or `Stop()` throws inside the host

### `IWorkspaceItem`

`Cycloside/SDK/IWorkspaceItem.cs`

- `BuildWorkspaceView()` returns the control mounted into the main workspace tab surface
- `UseWorkspace` is set by the host so the plugin can avoid opening its own window when it is already docked in the shell

### `IThemablePlugin`

`Cycloside/Plugins/IThemablePlugin.cs`

- lets a plugin react to global theme and skin changes
- supplies window classes for skin targeting through `ThemeClasses`
- is honored by `PluginWindowBase`

## Recommended window pattern

If your plugin opens a window, prefer `PluginWindowBase`:

```csharp
public sealed class ExamplePlugin : IPlugin
{
    private PluginWindowBase? _window;

    public string Name => "Example";
    public string Description => "Example plugin.";
    public Version Version => new(1, 0, 0);
    public Cycloside.Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;

    public void Start()
    {
        if (_window != null)
        {
            _window.Activate();
            return;
        }

        _window = new PluginWindowBase
        {
            Title = Name,
            Width = 520,
            Height = 320,
            Content = new TextBlock { Text = "Example plugin is running." }
        };

        _window.Plugin = this;
        _window.ApplyPluginThemeAndSkin(this);
        _window.Closed += (_, _) => _window = null;
        _window.Show();
    }

    public void Stop()
    {
        if (_window == null)
        {
            return;
        }

        _window.Close();
        _window = null;
    }
}
```

## Widgets

There are two widget contracts in the repo:

- `IWidget` is the stable plugin-facing path used by `IPlugin.Widget` and the built-in `WidgetHostPlugin`
- `IWidgetV2` is a richer lifecycle/configuration contract used by the enhanced widget stack under `Cycloside/Widgets/`

If you want maximum compatibility with the current shell, expose a simple `IWidget`.
If you are building against the enhanced widget system on purpose, also read `docs/widget-interface.md`.

## Messaging and automation

Use `PluginBus` for loose coupling:

```csharp
PluginBus.Subscribe("example:event", payload => Handle(payload));
PluginBus.Publish("example:event", new { Value = 42 });
PluginBus.Unsubscribe("example:event", Handle);
```

For local external automation, `RemoteApiServer` exposes authenticated `POST` endpoints for `trigger`, `profile`, and `theme`.

## External plugin loading

- external plugin DLLs go in the runtime `Plugins/` directory beside the app
- when isolation is enabled, Cycloside loads them through `PluginLoadContext`
- the host watches the directory and can reload plugins without a full app restart

## Generator

Run this from `Cycloside/`:

```bash
dotnet run -- --newplugin MyPlugin
dotnet run -- --newplugin MyPlugin --with-tests
```

The generated sample is intended to compile and open a themed plugin window immediately, not just emit an empty class shell.
