using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Cycloside.Plugins;
using Cycloside.Services;

namespace Cycloside;

public class WorkspaceProfile
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, bool> Plugins { get; set; } = new();
    public string Wallpaper { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    // Names of plugins currently open as workspace tabs for this profile
    public List<string> WorkspaceTabs { get; set; } = new();
}

public static class WorkspaceProfiles
{
    private static readonly string ProfilePath = Path.Combine(AppContext.BaseDirectory, "profiles.json");
    private static Dictionary<string, WorkspaceProfile> _profiles = Load();

    public static IReadOnlyDictionary<string, WorkspaceProfile> Profiles => _profiles;

    public static IEnumerable<string> ProfileNames => _profiles.Keys;

    private static Dictionary<string, WorkspaceProfile> Load()
    {
        try
        {
            if (File.Exists(ProfilePath))
            {
                var json = File.ReadAllText(ProfilePath);
                var p = JsonSerializer.Deserialize<Dictionary<string, WorkspaceProfile>>(json);
                if (p != null) return p;
            }
        }
        catch (Exception ex)
        {
            Services.Logger.Log($"Profiles load error: {ex.Message}");
        }
        return new();
    }

    public static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ProfilePath, json);
        }
        catch (Exception ex)
        {
            Services.Logger.Log($"Profiles save error: {ex.Message}");
        }
    }

    public static void Apply(string name, PluginManager manager)
    {
        if (!_profiles.TryGetValue(name, out var profile))
            return;

        foreach (var plugin in manager.Plugins)
        {
            var enable = profile.Plugins.TryGetValue(plugin.Name, out var e) && e;
            if (enable && !manager.IsEnabled(plugin))
                manager.EnablePlugin(plugin);
            else if (!enable && manager.IsEnabled(plugin))
                manager.DisablePlugin(plugin);
        }

        if (!string.IsNullOrWhiteSpace(profile.Wallpaper))
            WallpaperHelper.SetWallpaper(profile.Wallpaper);

        if (!string.IsNullOrWhiteSpace(profile.Theme))
            ThemeManager.LoadGlobalTheme(profile.Theme);

        SettingsManager.Settings.ActiveProfile = name;
        SettingsManager.Save();
    }

    public static void AddOrUpdate(WorkspaceProfile profile)
    {
        _profiles[profile.Name] = profile;
        Save();
    }

    public static void Remove(string name)
    {
        if (_profiles.Remove(name))
            Save();
    }

    public static void UpdatePlugin(string profileName, string pluginName, bool enabled)
    {
        if (!_profiles.TryGetValue(profileName, out var profile))
            return;

        profile.Plugins[pluginName] = enabled;
        Save();
    }

    /// <summary>
    /// Updates the saved list of workspace tab plugin names for a profile.
    /// </summary>
    public static void UpdateWorkspaceTabs(string profileName, IEnumerable<string> pluginNames)
    {
        if (!_profiles.TryGetValue(profileName, out var profile))
            return;

        profile.WorkspaceTabs = new List<string>(pluginNames);
        Save();
    }

    /// <summary>
    /// Gets the saved workspace tab plugin names for a profile.
    /// Returns empty list if none saved.
    /// </summary>
    public static IReadOnlyList<string> GetWorkspaceTabs(string profileName)
    {
        if (_profiles.TryGetValue(profileName, out var profile) && profile.WorkspaceTabs != null)
        {
            return profile.WorkspaceTabs;
        }
        return Array.Empty<string>();
    }
}
