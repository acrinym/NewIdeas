using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Threading;

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

    // Application-wide semantic theme pack.
    public string GlobalTheme { get; set; } = "Dockside";

    // Theme variant (Light/Dark/Default)
    public string RequestedThemeVariant { get; set; } = "Dark";

    // Application-wide skin layered on top of the current theme.
    public string GlobalSkin { get; set; } = "Workbench";

    // Path to the QB64 executable for the QBasic IDE plugin.
    public string QB64Path { get; set; } = "qb64";

    // Path to the dotnet executable used by plugins or scripts.
    public string DotNetPath { get; set; } = "dotnet";

    // Optional local Tile World library root containing data/ and sets/.
    public string TileWorldLibraryPath { get; set; } = string.Empty;

    // Legacy component theme overrides. Prefer GlobalTheme plus PluginSkins.
    public Dictionary<string, string> ComponentThemes { get; set; } = new();

    // Theme used by game logic inside plugins such as Jezzball
    public Dictionary<string, string> PluginGameThemes { get; set; } = new();

    // Optional skin applied to plugin windows
    public Dictionary<string, string> PluginSkins { get; set; } = new();

    // Default animated backdrop applied to windows that opt into appearance management.
    public AnimatedBackgroundSettings GlobalAnimatedBackground { get; set; } = new();

    // Optional component-specific animated backdrop overrides.
    public Dictionary<string, AnimatedBackgroundSettings> ComponentAnimatedBackgrounds { get; set; } = new();

    // Optional plugin window animated backdrop overrides.
    public Dictionary<string, AnimatedBackgroundSettings> PluginAnimatedBackgrounds { get; set; } = new();

    // Sound effect files mapped by plugin name then event key
    public Dictionary<string, Dictionary<string, string>> PluginSoundEffects { get; set; } = new();

    public JezzballSettings Jezzball { get; set; } = new();

    public string Cursor { get; set; } = "Arrow";
    public Dictionary<string, string> ComponentCursors { get; set; } = new();
    public Dictionary<string, List<string>> WindowEffects { get; set; } = new();
    // Optional per-effect parameters (global). Keyed by effect name, then parameter name → value.
    // Example: { "MagicLampMinimize": { "DurationMs": "280", "SquashFactor": "0.8", "MinHeight": "36" } }
    public Dictionary<string, Dictionary<string, string>> WindowEffectParameters { get; set; } = new();
    public Dictionary<string, ThemeSnapshot> SavedThemes { get; set; } = new();
    /// <summary>
    /// Mapping of hotkey action names to gesture strings (e.g. "Ctrl+Alt+W").
    /// </summary>
    public Dictionary<string, string> Hotkeys { get; set; } = new()
    {
        { "WidgetHost", "Ctrl+Alt+W" },
        { "Terminal", "Ctrl+Alt+T" },
        { "FileExplorer", "Ctrl+Alt+E" },
        { "TextEditor", "Ctrl+Alt+N" },
        { "QuickLauncher", "Ctrl+Alt+Q" }

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
    private static Timer? _saveTimer;
    public static event Action? SettingsSaved;

    public static AppSettings Settings => _settings;

    static SettingsManager()
    {
        // Ensure a non-default API token is set on first run.
        if (string.IsNullOrWhiteSpace(_settings.RemoteApiToken) || _settings.RemoteApiToken == "secret")
        {
            _settings.RemoteApiToken = GenerateToken();
            Save();
        }
    }

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
        catch (Exception ex)
        {
            Services.Logger.Log($"Settings load error: {ex.Message}");
        }
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
            NotifySettingsSaved();
        }
        catch (Exception ex)
        {
            Services.Logger.Log($"Settings save error: {ex.Message}");
        }
    }

    /// <summary>
    /// Batches multiple Save requests into a single write after a short delay.
    /// Helps reduce startup I/O when many plugins update version info.
    /// </summary>
    public static void SaveSoon(int delayMs = 500)
    {
        try
        {
            _saveTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
        catch { }
        _saveTimer ??= new Timer(_ =>
        {
            try { Save(); }
            catch { }
        });
        _saveTimer.Change(delayMs, Timeout.Infinite);
    }

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

    private static void NotifySettingsSaved()
    {
        var handlers = SettingsSaved;
        if (handlers == null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action)handler)();
            }
            catch (Exception ex)
            {
                Services.Logger.Log($"SettingsSaved handler error: {ex.Message}");
            }
        }
    }
}
