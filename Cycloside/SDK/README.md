# Cycloside Plugin SDK

This folder contains the minimal interfaces needed to build external plugins.
Reference `Cycloside.dll` and implement `Cycloside.Plugins.IPlugin`.

## Getting Started

1. Run `dotnet run -- --newplugin MyPlugin` from the repository root to generate
a template plugin inside `Plugins/`.
2. Implement the methods in the generated class and compile it into its own DLL.
3. Place the compiled DLL back in the `Plugins/` folder and use **Settings →
Plugin Manager** to enable or disable it.

See `docs/plugin-dev.md` for more details on the plugin lifecycle, bus and
marketplace.

