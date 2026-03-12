# Plugin Lifecycle

Cycloside loads external plugins from the app-local `Plugins/` directory and built-in plugins from code registered in `App.axaml.cs`.
The host contract lives in `Cycloside/SDK/`.

## Startup and discovery

- External plugins are discovered by `PluginManager.LoadPlugins()` by scanning `*.dll` files in the runtime `Plugins/` folder.
- Built-in plugins are registered through factories in `App.LoadAllPlugins()`.
- Every plugin must implement `Cycloside.Plugins.IPlugin`.
- Plugins are instantiated during discovery but are not started until the host enables them.

## Enable and disable

- `PluginManager.EnablePlugin()` calls `Start()`.
- `PluginManager.DisablePlugin()` calls `Stop()`.
- The tray menu, the main window, workspace restoration, and profile application all flow through the same manager.
- `SettingsManager.Settings.PluginEnabled` persists enablement state by plugin name.

## Reload and isolation

- `PluginManager.StartWatching()` uses `FileSystemWatcher` to watch external plugin DLLs.
- `PluginManager.ReloadPlugins()` stops enabled plugins, unloads collectible load contexts, reloads built-in and external plugins, reapplies workspace profiles, and then raises `PluginsReloaded`.
- When `Settings.PluginIsolation` is enabled, external DLLs load through `PluginLoadContext`, which allows unload and reduces file-lock pain during development.

## Crash and settings callbacks

- `IPluginExtended.OnCrash(Exception ex)` is now invoked when `Start()` or `Stop()` throws inside the host.
- `IPluginExtended.OnSettingsSaved()` is now invoked for enabled plugins after `SettingsManager.Save()` completes successfully.
- The host still logs exceptions and disables a plugin that fails during startup.

## Windows and workspace tabs

- A plain plugin can open its own window from `Start()`.
- A workspace-capable plugin additionally implements `IWorkspaceItem`.
- When a workspace plugin is launched from the main shell tab surface, the host sets `UseWorkspace = true`, enables the plugin, and mounts `BuildWorkspaceView()` into a workspace tab.
- When the same plugin is launched in its own window, the host sets `UseWorkspace = false`.
- `PluginWindowBase` is the recommended base window for plugin UIs because it hooks theme and skin refresh plus cleanup automatically.

## Widgets

- `IPlugin.Widget` is the stable widget entry point used by the built-in `WidgetHostPlugin`.
- That path currently consumes the legacy `IWidget` contract.
- A richer `IWidgetV2` stack also exists under `Cycloside/Widgets/`, but it is a separate enhanced host path and should be treated as advanced or in-progress rather than the default plugin widget contract.

## Plugin bus

- `PluginBus.Subscribe(topic, handler)` registers a callback.
- `PluginBus.Unsubscribe(topic, handler)` removes it.
- `PluginBus.Publish(topic, payload)` publishes synchronously.
- `PluginBus.PublishAsync(topic, payload)` publishes on the thread pool.
- Topic names are free-form strings; the bus does not enforce schemas.

## Remote API

`RemoteApiServer` starts with the app and exposes authenticated local HTTP endpoints:

- `POST /trigger` publishes the request body as a plugin bus topic
- `POST /profile` applies the request body as a workspace profile name
- `POST /theme` loads the request body as the global theme name

Authentication uses `X-Api-Token` or the `token` query value and is backed by `Settings.RemoteApiToken`.

## Template generator

Run the template generator from the `Cycloside/` project directory:

```bash
dotnet run -- --newplugin MyPlugin
dotnet run -- --newplugin MyPlugin --with-tests
```

The generator creates a working sample plugin under `Cycloside/Plugins/<Name>/src` and an optional test project under `Cycloside/Plugins/<Name>/tests`.
