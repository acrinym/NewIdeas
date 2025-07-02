# Plugin Development

Cycloside supports drop‑in plugins written as DLLs or as in‑memory scripts.

Plugins implement the `IPlugin` interface which exposes the following
properties and methods:

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

Additional hooks like `OnSettingsSaved()` and `OnCrash(Exception ex)` can be
implemented by inheriting `IPluginExtended`.

## Exposed Utilities

Plugins can call into several helper classes shipped with the main application:

- `PluginBus` – simple publish/subscribe message bus for cross-plugin
  communication.
- `SkinManager` and `ThemeManager` – apply XAML skins and themes at runtime.
- `CursorManager` – assign `StandardCursorType` values from settings.
- `WindowEffectsManager` – enable compositor/physics effects on a window.
- `PluginMarketplace` – fetch and install plugin packages from remote feeds.

These classes live in the `Cycloside` namespace and are available when you
reference `Cycloside.dll`.

### Theming from Plugins

Use `ThemeManager.ApplyComponentTheme(element, name)` to override the theme for
a specific window or control. Combine it with
`SkinManager.ApplySkinTo(element, skin)` to layer a skin on top. To respect
user-selected cursors call `CursorManager.ApplyFromSettings(element, name)`.

## DLL Plugins

Implement `Cycloside.Plugins.IPlugin` and place the compiled assembly in the `Plugins/` directory. Hot reload will load changes automatically.
When compiled, each plugin resides in its own folder under `Plugins/`.
Dependencies should be copied alongside the main DLL.

When the plugin folder contents change, `PluginManager` reloads all plugins and
triggers the `PluginsReloaded` event.  Subscribe to this event if your code
needs to refresh UI elements after new plugins are detected.

You can generate a boilerplate plugin with:
```bash
 dotnet run -- --newplugin MyPlugin
```

## Volatile Scripts

Lua (`.lua`) and C# script (`.csx`) files can be executed in memory. Use the tray menu **Settings → Generate New Plugin** and choose a volatile type to create a starter file.
See [volatile-scripting.md](volatile-scripting.md) for common examples and tips.

## Metadata

Each plugin should expose `Name`, `Description` and `Version`. The plugin manager uses these to display information in the GUI.
If your plugin exposes a dockable widget, return an `IWidget` implementation via
the `Widget` property. Set `ForceDefaultTheme` to `true` if your UI should always
use the global theme and ignore component skins.

## Communication

Plugins can talk to one another using the global `PluginBus`:

```csharp
PluginBus.Subscribe("my:event", data => Handle(data));
PluginBus.Publish("my:event", payload);
```

The optional `RemoteApiServer` publishes bus events over HTTP. POST a topic name
to `http://localhost:4123/trigger` with `X-Api-Token` or a `token` query
parameter. See `docs/plugin-lifecycle.md` for details.

## Marketplace

`PluginMarketplace` can retrieve a list of available plugins from a remote JSON feed and
install them automatically. Each entry includes a download `Url` and the expected
SHA256 `Hash`.

```csharp
var plugins = await PluginMarketplace.FetchAsync("https://example.com/plugins.json");
foreach (var p in plugins)
    await PluginMarketplace.InstallAsync(p, Path.Combine(AppContext.BaseDirectory, "Plugins"));
```

## Examples

- [WindowFX Plugin Example](examples/windowfx-plugin-example.md)
- [Theme Example](examples/theme-example.md)
- [Skin Example](examples/skin-example.md)
- [Custom Cursor Example](examples/custom-cursor-example.md)

## Notes

`EnvironmentEditorPlugin` lists environment variables from the selected scope.
On Linux and macOS only the `Process` scope is available.

