# Cycloside

Cycloside is a simple background tray application built with Avalonia.
It demonstrates a plugin architecture that loads `*.dll` files from the
`Plugins` folder at runtime. The application starts minimized to the
system tray and shows a menu with **Settings** and **Exit** options.
Plugins dropped into the folder are loaded automatically and any
changes are detected at runtime without restarting the application.

The tray icon image is embedded directly in the code as a base64 string
to keep the repository free of binary assets.

## Running
```bash
cd Cycloside
 dotnet run
```

## Plugins
Drop any assemblies implementing `Cycloside.Plugins.IPlugin` into the
`Plugins` directory and they will be loaded when the application starts.
