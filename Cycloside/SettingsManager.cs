using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Cycloside;

public class AppSettings
{
    public bool LaunchAtStartup { get; set; }
    public string Theme { get; set; } = "MintGreen";
    public Dictionary<string, string> ComponentThemes { get; set; } = new();

}

public static class SettingsManager
{
    private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
    private static AppSettings _settings = Load();

    public static AppSettings Settings => _settings;

    private static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s != null) return s;
            }
        }
        catch { }
        return new AppSettings();
    }

    public static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
