# Using Avalonia in Cycloside

The Cycloside desktop utility leverages the [Avalonia](https://avaloniaui.net/) framework for its cross-platform user interface. The developer sometimes refers to "FMEO" ("For My Eyes Only") when discussing internal code, but the visible application is powered entirely by Avalonia.

## Cross-Platform Foundation

Avalonia handles the windowing and styling layers so that Cycloside behaves the same on Windows and Linux. The project README notes that it is "built with Avalonia" and "cross-platform by design" which allows plugin developers to work on either OS.

## Plugin Loading and Hot Reload

Cycloside watches the `Plugins` folder and loads any `*.dll` files it finds at runtime. Avalonia's reactive UI makes it easy to update elements when plugins change. Hot reload is triggered via file watching so plugins can be updated without restarting the app.

## Plugin Manager Interface

The tray menu includes a **Plugin Manager** implemented with Avalonia controls. From here you can toggle or reload plugins and open the plugin directory. All plugin states are stored so settings persist between sessions.

## Theming and Skins

Avalonia's styling system lets Cycloside apply XAML-based themes. Place style resources in the `Skins` folder and select an active skin from **Settings → Runtime Settings**. The chosen skin name is stored in `settings.json`.

## Why Avalonia?

By building on Avalonia, Cycloside remains lightweight while offering a modern UI toolkit. Plugins are regular .NET classes so you can tap into the wider ecosystem, and the same setup runs on both Windows and Linux.
