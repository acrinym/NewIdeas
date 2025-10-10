# Cycloside Control Panel

The Control Panel provides a single place to tweak common Cycloside options.
It is accessible from the tray icon via **Settings → Control Panel…** or from
the main window's **Settings** menu.

## Features

- Toggle **Launch at Startup** and set the path to the `dotnet` executable.
- Open the plugin manager, theme settings and skin editor windows.
- Access runtime settings for plugin isolation and crash logging.
- Enable Safe Mode per built-in plugin when needed.

### Window Effects Settings

The Appearance tab includes **Window Effects Settings…** to configure Compiz‑style effects.

- Target selection: choose **Global (*)** to apply effects to all windows, or a specific component like `MainWindow` or a plugin name.
- Effects list: check any available effect (e.g., `RollUp`, `Wobbly`, `Transparency`, `Shadow`, `ZoomOpen`, `ExplodeOnClose`).
- Save applies changes to `SettingsManager.Settings.WindowEffects` and persists to `settings.json`.

Example configuration in `settings.json`:

```
"WindowEffects": {
  "*": ["Transparency", "Shadow"],
  "MainWindow": ["Wobbly"],
  "PluginDevWizard": ["ZoomOpen"],
  "VolatileRunnerWindow": ["ExplodeOnClose"]
}
```

Changes are saved back to `settings.json` when you press **Save**.

Note: Managed Visual Host options (native colors, preset theme, sensitivity, and per‑visualizer settings)
are available directly in the visualizer window and persist automatically; they do not appear in the Control Panel.
