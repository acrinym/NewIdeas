using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Cycloside;

/// <summary>
/// Application-wide settings persisted to <c>settings.json</c>.
/// </summary>
public class AppSettings
{
    public bool LaunchAtStartup { get; set; }
    public Dictionary<string, bool> PluginEnabled { get; set; } = new();
    public Dictionary<string, string> PluginVersions { get; set; } = new();
    public bool PluginIsolation { get; set; } = true;
    public bool PluginCrashLogging { get; set; } = true;
    public bool DisableBuiltInPlugins { get; set; } = false;
    public Dictionary<string, bool> SafeBuiltInPlugins { get; set; } = new();

    // RENAMED: This is now the single, application-wide theme.
    public string GlobalTheme { get; set; } = "MintGreen";

    // Path to the QB64 executable for the QBasic IDE plugin.
    public string QB64Path { get; set; } = "qb64";

    // Path to the dotnet executable used by plugins or scripts.
    public string DotNetPath { get; set; } = "dotnet";

    // RENAMED: This maps components (like plugin names) to specific skins.
    public Dictionary<string, string> ComponentThemes { get; set; } = new();

    // Theme used by game logic inside plugins such as Jezzball
    public Dictionary<string, string> PluginGameThemes { get; set; } = new();

    // Optional skin applied to plugin windows
    public Dictionary<string, string> PluginSkins { get; set; } = new();

    public string Cursor { get; set; } = "Arrow";
    public Dictionary<string, string> ComponentCursors { get; set; } = new();
    public Dictionary<string, List<string>> WindowEffects { get; set; } = new();
    public Dictionary<string, ThemeSnapshot> SavedThemes { get; set; } = new();
    /// <summary>
    /// Mapping of hotkey action names to gesture strings (e.g. "Ctrl+Alt+W").
    /// </summary>
    public Dictionary<string, string> Hotkeys { get; set; } = new()
    {
        { "WidgetHost", "Ctrl+Alt+W" }
    };
    public double WeatherLatitude { get; set; } = 35;
    public double WeatherLongitude { get; set; } = 139;
    public string WeatherCity { get; set; } = "";
    public string ActiveProfile { get; set; } = "default";
    public string RemoteApiToken { get; set; } = "secret";
    public bool FirstRun { get; set; } = true;
}

/// <summary>
/// Helper for loading and saving <see cref="AppSettings"/>. Plugins can read
/// and update values via <see cref="Settings"/> then call <see cref="Save"/>.
/// </summary>
public static class SettingsManager
{
    private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
    private static AppSettings _settings = Load();

    public static AppSettings Settings => _settings;

    /// <summary>
    /// Loads settings from disk or returns defaults when the file is missing.
    /// </summary>
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

    /// <summary>
    /// Persists the current <see cref="Settings"/> to disk.
    /// </summary>
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
