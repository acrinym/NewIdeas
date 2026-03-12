# Cycloside Plugin SDK

This folder contains the public contracts external plugins are expected to compile against.

## Files

- `IPlugin.cs`: required base plugin contract
- `IPluginExtended.cs`: optional settings-saved and crash callbacks
- `IWorkspaceItem.cs`: optional workspace-tab contract

## Recommended development flow

1. Change into `Cycloside/`.
2. Run `dotnet run -- --newplugin MyPlugin`.
3. Build the generated project under `Cycloside/Plugins/MyPlugin/src`.
4. Copy the built DLL and any dependencies into the runtime `Plugins/` folder if you are testing outside the repo build output.

The generated sample is a working plugin window, not an empty shell.

## Docs

- `../docs/plugin-api.md`
- `../docs/plugin-lifecycle.md`
- `../docs/widget-interface.md`
- `../docs/skin-api.md`
