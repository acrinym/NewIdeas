using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Cycloside;

public static class StateManager
{
    private static readonly string StatePath = Path.Combine(AppContext.BaseDirectory, "state.json");
    private static Dictionary<string, string> _state = Load();

    private static Dictionary<string, string> Load()
    {
        try
        {
            if (File.Exists(StatePath))
            {
                var json = File.ReadAllText(StatePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (data != null) return data;
            }
        }
        catch { }
        return new Dictionary<string, string>();
    }

    private static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StatePath, json);
        }
        catch { }
    }

    public static void Set(string key, string value)
    {
        _state[key] = value;
        Save();
    }

    public static string? Get(string key)
    {
        return _state.TryGetValue(key, out var v) ? v : null;
    }
}
