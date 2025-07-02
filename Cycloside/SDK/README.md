# Cycloside Plugin SDK

This folder contains the minimal interfaces needed to build external plugins.
Reference `Cycloside.dll` and implement `Cycloside.Plugins.IPlugin`.
The interface exposes metadata, lifecycle methods and an optional `Widget`
surface for dockable controls. For advanced hooks implement
`IPluginExtended`.

## Getting Started

1. Run `dotnet run -- --newplugin MyPlugin` from the repository root to generate
   a template plugin inside `Plugins/`.
2. Implement the methods in the generated class and compile it into its own DLL.
3. Place the compiled DLL back in the `Plugins/` folder and use **Settings →
   Plugin Manager** to enable or disable it. Dependencies should sit beside the
   DLL.

See `docs/plugin-dev.md` for more details on the plugin lifecycle, bus and
marketplace.

