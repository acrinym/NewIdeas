# Plugin Lifecycle

Plugins implement `IPlugin` from the `Cycloside.Plugins` namespace. Cycloside loads all DLLs from the `Plugins` folder at startup and calls `Start()` on those that are enabled. `Stop()` is invoked when a plugin is disabled or when Cycloside exits.

Additional optional hooks can be implemented by inheriting `IPluginExtended`:

```csharp
namespace Cycloside.Plugins;

public interface IPluginExtended : IPlugin
{
    void OnSettingsSaved();
    void OnCrash(Exception ex);
}
```

The plugin manager catches exceptions thrown during `Start` and `Stop` when isolation mode is enabled. Crashes are logged if crash logging is turned on and stack traces are written to an OS specific log directory. Plugins exposing a widget should return an implementation via the `Widget` property.

Plugins that implement `IWorkspaceItem` can render their UI inside the unified workspace rather than a separate window. The host sets `UseWorkspace` to `true` when the item is displayed as a tab or docked panel.

When **Plugin Isolation** is enabled in the Control Panel each plugin loads in its own context. This allows hot reloading without file locks and lets Cycloside unload crashing plugins cleanly.

## Plugin Bus

Plugins communicate by publishing messages to the global `PluginBus` and subscribing to topics of interest:

```csharp
PluginBus.Subscribe("my:event", data => Handle(data));
PluginBus.Publish("my:event", payload);
```

Unsubscribing removes the handler so plugins can clean up during `Stop()`.

## Remote API

`RemoteApiServer` exposes `http://localhost:4123/trigger`. Posting a topic name to this endpoint publishes that event on the bus. This allows scripts or other applications to control your plugins without direct references.
`RemoteApiServer` exposes `http://localhost:4123/trigger`. Include your pre‑shared token using the `X-Api-Token` header or a `token` query string when POSTing a topic name. Invalid or missing tokens result in `401 Unauthorized`. The token is configured in `settings.json` under `RemoteApiToken`.

Example:

```bash
curl -X POST -H "X-Api-Token: <token>" http://localhost:4123/trigger -d "my:event"
```

