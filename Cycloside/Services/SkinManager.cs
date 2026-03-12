using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cycloside.Services
{
    public class SkinManifest
    {
        public string Name { get; set; } = string.Empty;

        public int Version { get; set; } = 1;

        public string Contract { get; set; } = "v1";

        public SkinOverlays Overlays { get; set; } = new();

        public Dictionary<string, string> ReplaceWindows { get; set; } = new();
    }

    public class SkinOverlays
    {
        public List<string> Global { get; set; } = new();

        public List<SkinSelector> BySelector { get; set; } = new();
    }

    public class SkinSelector
    {
        public string? Type { get; set; }

        public List<string> Classes { get; set; } = new();

        public string? XName { get; set; }

        public List<string> Styles { get; set; } = new();
    }

    /// <summary>
    /// Manages app-wide skins and per-window skin overlays layered on top of themes.
    /// </summary>
    public static class SkinManager
    {
        private static readonly object SyncLock = new();
        private static readonly Dictionary<string, SkinManifest> ManifestCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<IStyle> ApplicationSkinStyles = new();
        private static readonly Dictionary<StyledElement, List<IStyle>> ElementSkinStyles = new();

        private static string SkinDirectory => Path.Combine(AppContext.BaseDirectory, "Skins");

        public static string CurrentSkin { get; private set; } = string.Empty;

        public static event EventHandler<SkinChangedEventArgs>? SkinChanged;

        public static void InitializeFromSettings()
        {
            var skinName = SettingsManager.Settings.GlobalSkin;
            if (string.IsNullOrWhiteSpace(skinName))
            {
                CurrentSkin = string.Empty;
                return;
            }

            ApplyGlobalSkinAsync(skinName, false, false).GetAwaiter().GetResult();
        }

        public static Task<bool> ApplySkinAsync(string skinName, StyledElement? element = null)
        {
            if (element == null)
            {
                return ApplyGlobalSkinAsync(skinName, true, true);
            }

            return ApplyElementSkinAsync(skinName, element);
        }

        public static void ApplySkinTo(StyledElement element, string skinName)
        {
            _ = ApplyElementSkinAsync(skinName, element);
        }

        public static Task ClearSkinAsync(StyledElement? element = null)
        {
            if (element == null)
            {
                return ApplyGlobalSkinAsync(string.Empty, true, true);
            }

            RemoveAllSkinsFrom(element);
            return Task.CompletedTask;
        }

        public static IEnumerable<string> GetAvailableSkins()
        {
            var skinNames = new List<string>();

            if (Directory.Exists(SkinDirectory))
            {
                foreach (var directory in Directory.GetDirectories(SkinDirectory))
                {
                    var name = Path.GetFileName(directory);
                    if (!string.IsNullOrWhiteSpace(name) &&
                        File.Exists(Path.Combine(directory, "skin.json")))
                    {
                        skinNames.Add(name);
                    }
                }

                foreach (var filePath in Directory.GetFiles(SkinDirectory, "*.axaml"))
                {
                    var name = Path.GetFileNameWithoutExtension(filePath);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        skinNames.Add(name);
                    }
                }
            }

            return skinNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
        }

        public static async Task<bool> SupportsWindowReplacementAsync(string skinName, string windowType)
        {
            var manifest = await LoadSkinManifestAsync(skinName);
            return manifest?.ReplaceWindows.ContainsKey(windowType) == true;
        }

        public static async Task<bool> ReapplyCurrentSkinAsync(bool notifyListeners = false)
        {
            if (string.IsNullOrWhiteSpace(CurrentSkin))
            {
                return true;
            }

            return await ApplyGlobalSkinAsync(CurrentSkin, false, notifyListeners);
        }

        private static async Task<bool> ApplyGlobalSkinAsync(string skinName, bool saveSettings, bool notifyListeners)
        {
            try
            {
                ClearApplicationSkins();

                if (string.IsNullOrWhiteSpace(skinName))
                {
                    CurrentSkin = string.Empty;

                    if (saveSettings)
                    {
                        SettingsManager.Settings.GlobalSkin = string.Empty;
                        SettingsManager.Save();
                    }

                    if (notifyListeners)
                    {
                        SkinChanged?.Invoke(null, new SkinChangedEventArgs(CurrentSkin));
                    }

                    return true;
                }

                if (!await ApplySkinDefinitionAsync(skinName, null))
                {
                    return false;
                }

                CurrentSkin = skinName;

                if (saveSettings)
                {
                    SettingsManager.Settings.GlobalSkin = skinName;
                    SettingsManager.Save();
                }

                if (notifyListeners)
                {
                    SkinChanged?.Invoke(null, new SkinChangedEventArgs(CurrentSkin));
                }

                AnimatedBackgroundManager.ReapplyAllWindows();
                Logger.Log($"SkinManager: applied global skin '{skinName}'");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"SkinManager: failed to apply global skin '{skinName}': {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> ApplyElementSkinAsync(string skinName, StyledElement element)
        {
            try
            {
                RemoveAllSkinsFrom(element);

                if (string.IsNullOrWhiteSpace(skinName))
                {
                    return true;
                }

                var applied = await ApplySkinDefinitionAsync(skinName, element);
                if (applied)
                {
                    AnimatedBackgroundManager.ReapplyAllWindows();
                    Logger.Log($"SkinManager: applied skin '{skinName}' to '{element.GetType().Name}'");
                }

                return applied;
            }
            catch (Exception ex)
            {
                Logger.Log($"SkinManager: failed to apply skin '{skinName}' to '{element.GetType().Name}': {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> ApplySkinDefinitionAsync(string skinName, StyledElement? element)
        {
            var manifest = await LoadSkinManifestAsync(skinName);
            if (manifest != null)
            {
                return await ApplyManifestSkinAsync(skinName, manifest, element);
            }

            var legacyPath = Path.Combine(SkinDirectory, $"{skinName}.axaml");
            if (File.Exists(legacyPath))
            {
                return AddStyleToTarget(legacyPath, element);
            }

            Logger.Log($"SkinManager: skin '{skinName}' was not found");
            return false;
        }

        private static async Task<bool> ApplyManifestSkinAsync(string skinName, SkinManifest manifest, StyledElement? element)
        {
            var skinRoot = Path.Combine(SkinDirectory, skinName);

            foreach (var relativePath in manifest.Overlays.Global)
            {
                var stylePath = Path.Combine(skinRoot, relativePath);
                if (!AddStyleToTarget(stylePath, element))
                {
                    return false;
                }
            }

            foreach (var selector in manifest.Overlays.BySelector)
            {
                foreach (var relativePath in selector.Styles)
                {
                    var stylePath = Path.Combine(skinRoot, relativePath);
                    if (!AddStyleToTarget(stylePath, element))
                    {
                        return false;
                    }
                }
            }

            if (manifest.ReplaceWindows.Count > 0)
            {
                if (element is Window singleWindow)
                {
                    await ApplyWindowReplacementAsync(singleWindow, skinRoot, manifest.ReplaceWindows);
                }
                else
                {
                    await ApplyWindowReplacementsAsync(skinRoot, manifest.ReplaceWindows);
                }
            }

            return true;
        }

        private static bool AddStyleToTarget(string stylePath, StyledElement? element)
        {
            try
            {
                if (!File.Exists(stylePath))
                {
                    Logger.Log($"SkinManager: style file not found: {stylePath}");
                    return false;
                }

                var xaml = File.ReadAllText(stylePath);
                var parsed = AvaloniaRuntimeXamlLoader.Parse(xaml, typeof(App).Assembly);
                if (parsed is not IStyle style)
                {
                    Logger.Log($"SkinManager: '{stylePath}' does not contain a style root");
                    return false;
                }

                if (element == null)
                {
                    if (Application.Current == null)
                    {
                        return false;
                    }

                    Application.Current.Styles.Add(style);

                    lock (SyncLock)
                    {
                        ApplicationSkinStyles.Add(style);
                    }
                }
                else
                {
                    element.Styles.Add(style);

                    lock (SyncLock)
                    {
                        if (!ElementSkinStyles.TryGetValue(element, out var styles))
                        {
                            styles = new List<IStyle>();
                            ElementSkinStyles[element] = styles;
                        }

                        styles.Add(style);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"SkinManager: failed to parse '{stylePath}': {ex.Message}");
                return false;
            }
        }

        private static async Task<SkinManifest?> LoadSkinManifestAsync(string skinName)
        {
            if (ManifestCache.TryGetValue(skinName, out var cached))
            {
                return cached;
            }

            var manifestPath = Path.Combine(SkinDirectory, skinName, "skin.json");
            if (!File.Exists(manifestPath))
            {
                return null;
            }

            try
            {
                var json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<SkinManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (manifest != null)
                {
                    ManifestCache[skinName] = manifest;
                }

                return manifest;
            }
            catch (Exception ex)
            {
                Logger.Log($"SkinManager: failed to load manifest '{manifestPath}': {ex.Message}");
                return null;
            }
        }

        private static async Task ApplyWindowReplacementsAsync(string skinRoot, Dictionary<string, string> replacements)
        {
            if (Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            foreach (var window in desktop.Windows.ToArray())
            {
                await ApplyWindowReplacementAsync(window, skinRoot, replacements);
            }
        }

        private static Task ApplyWindowReplacementAsync(Window window, string skinRoot, Dictionary<string, string> replacements)
        {
            if (!replacements.TryGetValue(window.GetType().Name, out var relativePath))
            {
                return Task.CompletedTask;
            }

            var replacementPath = Path.Combine(skinRoot, relativePath);
            return WindowReplacementManager.ReplaceWindowContentAsync(window, replacementPath);
        }

        private static void ClearApplicationSkins()
        {
            if (Application.Current == null)
            {
                return;
            }

            List<IStyle> stylesToRemove;

            lock (SyncLock)
            {
                stylesToRemove = ApplicationSkinStyles.ToList();
                ApplicationSkinStyles.Clear();
            }

            foreach (var style in stylesToRemove)
            {
                Application.Current.Styles.Remove(style);
            }
        }

        public static void RemoveAllSkinsFrom(StyledElement element)
        {
            try
            {
                List<IStyle>? styles;

                lock (SyncLock)
                {
                    if (!ElementSkinStyles.TryGetValue(element, out styles))
                    {
                        return;
                    }

                    ElementSkinStyles.Remove(element);
                }

                foreach (var style in styles)
                {
                    element.Styles.Remove(style);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"SkinManager: failed to clear element skins: {ex.Message}");
            }
        }

        public static void ApplySkinsTo(StyledElement element, IEnumerable<string> skinNames)
        {
            foreach (var skinName in skinNames)
            {
                ApplySkinTo(element, skinName);
            }
        }
    }

    public class SkinChangedEventArgs : EventArgs
    {
        public string SkinName { get; }

        public SkinChangedEventArgs(string skinName)
        {
            SkinName = skinName;
        }
    }
}
