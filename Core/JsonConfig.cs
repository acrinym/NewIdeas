using System.Text.Json;

namespace Cycloside.Core;

public static class JsonConfig
{
    public static T LoadOrDefault<T>(string path, T @default)
    {
        if (!File.Exists(path)) return @default;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? @default;
    }

    public static void Save<T>(string path, T value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }
}
