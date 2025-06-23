using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Cycloside;

public class Macro
{
    public string Name { get; set; } = "";
    public List<string> Keys { get; set; } = new();
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
        catch { }
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
        catch { }
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
