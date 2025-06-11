# Plugin Lifecycle

Plugins implement `IPlugin` from the `SiloCide.SDK` namespace. Cycloside loads all DLLs from the `Plugins` folder at startup and calls `Start()` on those that are enabled. `Stop()` is invoked when a plugin is disabled or when Cycloside exits.

Additional optional hooks can be implemented by inheriting `IPluginExtended`:

```csharp
namespace SiloCide.SDK;

public interface IPluginExtended : IPlugin
{
    void OnSettingsSaved();
    void OnCrash(Exception ex);
}
```

The plugin manager catches exceptions thrown during `Start` and `Stop` when isolation mode is enabled. Crashes are logged if crash logging is turned on. Plugins exposing a widget should return an implementation via the `Widget` property.
