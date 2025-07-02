using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SharpHook.Native; // Required for KeyCode

namespace Cycloside;

/// <summary>
/// A public class to hold the data for a single recorded keyboard event.
/// This is now defined here to be shared with MacroPlugin.
/// </summary>
public class MacroEvent
{
    public bool IsPress { get; set; }
    public KeyCode Code { get; set; }
    public int Delay { get; set; } // Delay in milliseconds since the previous event
}

public class Macro
{
    public string Name { get; set; } = "";
    // FIXED: This now stores the detailed MacroEvent objects.
    public List<MacroEvent> Events { get; set; } = new();
}

public static class MacroManager
{
    private static readonly string MacroPath = Path.Combine(AppContext.BaseDirectory, "macros.json");
    private static List<Macro> _macros = Load();

    public static IReadOnlyList<Macro> Macros => _macros;

    private static List<Macro> Load()
    {
        try
        {
            if (File.Exists(MacroPath))
            {
                var json = File.ReadAllText(MacroPath);
                var list = JsonSerializer.Deserialize<List<Macro>>(json);
                if (list != null)
                    return list;
            }
        }
        catch { /* Ignore errors during load */ }
        return new List<Macro>();
    }

    public static void Reload()
    {
        _macros = Load();
    }

    public static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_macros, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(MacroPath, json);
        }
        catch { /* Ignore errors during save */ }
    }

    public static void Add(Macro macro)
    {
        _macros.Add(macro);
        Save();
    }

    public static void Remove(Macro macro)
    {
        _macros.Remove(macro);
        Save();
    }
}
