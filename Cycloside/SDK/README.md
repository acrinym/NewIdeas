# Cycloside Plugin SDK

This folder contains the interfaces and examples needed to build external plugins for Cycloside.

## Core Interface: IPlugin

The core of the plugin system is the `Cycloside.Plugins.IPlugin` interface. Every plugin must implement this interface.

```csharp
public interface IPlugin
{
    string Name { get; }
    string Description { get; }
    Version Version { get; }
    IWidget? Widget { get; }
    bool ForceDefaultTheme { get; }
    void Start();
    void Stop();
}
```

### Properties

-   **`Name`**: The name of your plugin. This is displayed in the Plugin Manager.
-   **`Description`**: A short description of what your plugin does.
-   **`Version`**: The version of your plugin.
-   **`Widget`**: An optional widget to be displayed in the Cycloside UI. See the "Widgets" section below.
-   **`ForceDefaultTheme`**: If `true`, your plugin's UI will ignore any custom themes or skins and use the default Avalonia theme.

### Methods

-   **`Start()`**: This method is called when your plugin is enabled. This is where you should create any windows, start any timers, or subscribe to any events.
-   **`Stop()`**: This method is called when your plugin is disabled. You should clean up any resources here, such as closing windows and stopping timers.

## Getting Started

1.  **Create a new .NET Class Library project.**
2.  **Add a reference to `Cycloside.dll`.** You can find this in the build output directory of the main Cycloside project.
3.  **Create a class that implements the `IPlugin` interface.**
4.  **Implement the properties and methods of the interface.**
5.  **Compile your project.**
6.  **Copy the resulting DLL to the `Plugins` directory** in the Cycloside application folder.

Alternatively, you can use the built-in command to scaffold a new plugin:
`dotnet run -- --newplugin MyPlugin`

## Creating a UI

To create a UI for your plugin, you can create an Avalonia `Window`. It's recommended to use the MVVM pattern for your UI logic.

1.  **Create a new `Window`** in your plugin project.
2.  **Create a ViewModel class** for your window.
3.  **In your plugin's `Start()` method, create an instance of your window and set its `DataContext` to your ViewModel.**
4.  **Call the `Show()` method** on your window to display it.

See the `KitchenSinkPlugin` for a detailed example of how to create a window with data binding.

## The PluginBus

The `PluginBus` is a simple event bus that allows plugins to communicate with each other.

-   **`PluginBus.Publish(string eventName, object? data = null)`**: Publishes an event with the given name and optional data.
-   **`PluginBus.Subscribe<T>(string eventName, Action<T?> action)`**: Subscribes to an event. The `action` will be called when the event is published.
-   **`PluginBus.Unsubscribe<T>(string eventName)`**: Unsubscribes from an event.

## Settings

The `SettingsManager` allows you to persist settings for your plugin.

-   **`SettingsManager.GetPluginSetting<T>(string pluginName, string key, T defaultValue)`**: Gets a setting for your plugin.
-   **`SettingsManager.SetPluginSetting<T>(string pluginName, string key, T value)`**: Sets a setting for your plugin. The settings are saved automatically.

## Widgets

A widget is a small `Control` that can be displayed in the Cycloside UI. To create a widget, create a class that implements the `IWidget` interface.

```csharp
public interface IWidget
{
    string Name { get; }
    Control Content { get; }
}
```

Then, return an instance of your widget class from the `Widget` property of your `IPlugin` implementation.

## Examples

-   **`ExamplePlugin`**: A minimal implementation of `IPlugin`.
-   **`KitchenSinkPlugin`**: A comprehensive example that demonstrates many of the SDK's features, including creating a UI, using the `PluginBus`, and persisting settings.

