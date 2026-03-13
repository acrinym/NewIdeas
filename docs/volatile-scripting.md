# Volatile Scripting

Cycloside can run small Lua and C# snippets directly from memory. Use this feature to prototype ideas or test behaviours without building a full plugin.

## Running Scripts

1. Right-click the tray icon and open the **Volatile** submenu.
2. Choose **Run Lua Script...** or **Run C# Script...** to select a file.
3. Use **Volatile → Inline Runner** to type code on the fly.
4. Output from scripts is written to the log window.

## Lua Examples

```lua
-- print the title of the first window
local main = Avalonia.Application.Current.Windows[1]
Logger.Log(main.Title)

-- change the wallpaper via the wallpaper plugin
PluginBus.Publish("wallpaper:set", "/path/to/image.jpg")
```

## C# Examples

Scripts must define a `Script` class with a static `Run` method:

```csharp
using Cycloside;
using Avalonia.Controls;

public static class Script
{
    public static void Run()
    {
        Logger.Log("Hello from C# script");
    }
}
```

Open a simple window:

```csharp
using Cycloside;
using Avalonia.Controls;

public static class Script
{
    public static void Run()
    {
        var w = new Window { Width = 200, Height = 100, Content = "Hi" };
        w.Show();
    }
}
```

## Theme Lua vs Volatile Lua

| Feature | Theme Lua | Volatile Lua |
|---------|-----------|--------------|
| When runs | On theme apply | On demand (user runs) |
| Sandbox | Yes (theme dir only) | No (full access) |
| API | `theme.*`, `system.log` | `PluginBus`, `Logger`, `ThemeManager`, etc. |
| Location | In theme pack, listed in theme.json | User-selected file |

Theme Lua scripts are defined in `theme.json` and run when a theme is applied. See [theme-lua-api.md](theme-lua-api.md). Volatile Lua has full access for prototyping and testing.

## Tips

- Scripts share the global `PluginBus` for communication.
- Call `ThemeManager.LoadGlobalTheme("MyTheme")` to switch themes at runtime.
- Keep snippets short; they are not saved after the app closes.

For details on the plugin system see [plugin-dev.md](plugin-dev.md).
