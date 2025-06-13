# Plugin Development

Cycloside supports drop-in plugins written as DLLs or volatile scripts.

## DLL Plugins

Implement `Cycloside.Plugins.IPlugin` and place the compiled assembly in the `Plugins/` directory. Hot reload will load changes automatically.

You can generate a boilerplate plugin with:
```bash
 dotnet run -- --newplugin MyPlugin
```

## Volatile Scripts

Lua (`.lua`) and C# script (`.csx`) files can be executed in memory. Use the tray menu **Settings → Generate New Plugin** and choose a volatile type to create a starter file.

## Metadata

Each plugin should expose `Name`, `Description` and `Version`. The plugin manager uses these to display information in the GUI.

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

