using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Cycloside;

public class AppSettings
{
    public bool LaunchAtStartup { get; set; }
    public Dictionary<string, bool> PluginEnabled { get; set; } = new();
    public Dictionary<string, string> PluginVersions { get; set; } = new();
    public bool PluginIsolation { get; set; } = true;
    public bool PluginCrashLogging { get; set; } = true;
    public bool DisableBuiltInPlugins { get; set; } = false;
    public string ActiveSkin { get; set; } = "Default";
    public string Theme { get; set; } = "MintGreen";
    public Dictionary<string, string> ComponentThemes { get; set; } = new();
    public string Cursor { get; set; } = "Arrow";
    public Dictionary<string, string> ComponentCursors { get; set; } = new();
    public Dictionary<string, List<string>> WindowEffects { get; set; } = new();
    public Dictionary<string, ThemeSnapshot> SavedThemes { get; set; } = new();
    public string ActiveProfile { get; set; } = "default";
    public string RemoteApiToken { get; set; } = "secret";
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
