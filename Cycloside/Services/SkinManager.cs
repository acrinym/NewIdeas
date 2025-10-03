using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cycloside.Services
{
    /// <summary>
    /// SkinData model for JSON deserialization
    /// </summary>
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
    /// Manages skin application with selector-based overlays and window replacement.
    /// Supports the new skin manifest format with JSON-based configuration.
    /// </summary>
    public static class SkinManager
    {
        private static readonly Dictionary<string, SkinManifest> _manifestCache = new();
        private static readonly Dictionary<string, Dictionary<string, StyleInclude>> _skinCache = new();

        /// <summary>
        /// Current active skin name
        /// </summary>
        public static string CurrentSkin { get; private set; } = string.Empty;

        /// <summary>
        /// Event fired when skin changes
        /// </summary>
        public static event EventHandler<SkinChangedEventArgs>? SkinChanged;

        private static string SkinDir => Path.Combine(AppContext.BaseDirectory, "Skins");

        /// <summary>
        /// Apply a skin with selector-based overlays and optional window replacement
        /// </summary>
        public static async Task<bool> ApplySkinAsync(string skinName, StyledElement? element = null)
        {
            if (string.IsNullOrEmpty(skinName))
            {
                await ClearSkinAsync(element);
                return true;
            }

            try
            {
                var manifest = await LoadSkinManifestAsync(skinName);
                if (manifest == null)
                {
                    Logger.Log($"Skin manifest not found: {skinName}");
                    return false;
                }

                // Clear existing skins first
                if (element != null)
                {
                    RemoveAllSkinsFrom(element);
                }
                else
                {
                    ClearApplicationSkins();
                }

                // Apply global overlays first
                if (manifest.Overlays.Global.Any())
                {
                    await ApplyGlobalOverlaysAsync(manifest.Overlays.Global, element);
                }

                // Apply selector-based overlays
                if (manifest.Overlays.BySelector.Any())
                {
                    await ApplySelectorOverlaysAsync(manifest.Overlays.BySelector, element);
                }

                // Handle window replacements
                if (manifest.ReplaceWindows.Any())
                {
                    await ApplyWindowReplacementsAsync(manifest.ReplaceWindows);
                }

                CurrentSkin = skinName;
                SkinChanged?.Invoke(null, new SkinChangedEventArgs(skinName));

                Logger.Log($"Successfully applied skin: {skinName}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error applying skin '{skinName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply skin to specific element (legacy compatibility)
        /// </summary>
        public static void ApplySkinTo(StyledElement element, string skinName)
        {
            _ = ApplySkinAsync(skinName, element);
        }

        /// <summary>
        /// Clear all skins from element or application
        /// </summary>
        public static Task ClearSkinAsync(StyledElement? element = null)
        {
            if (element != null)
            {
                RemoveAllSkinsFrom(element);
            }
            else
            {
                ClearApplicationSkins();
                CurrentSkin = string.Empty;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get available skin names
        /// </summary>
        public static IEnumerable<string> GetAvailableSkins()
        {
            if (!Directory.Exists(SkinDir)) return Enumerable.Empty<string>();

            return Directory.GetDirectories(SkinDir)
                .Select(dir => Path.GetFileName(dir)!)
                .Where(name => File.Exists(Path.Combine(SkinDir, name, "skin.json")))
                .OrderBy(name => name);
        }

        /// <summary>
        /// Check if skin supports window replacement
        /// </summary>
        public static async Task<bool> SupportsWindowReplacementAsync(string skinName, string windowType)
        {
            var manifest = await LoadSkinManifestAsync(skinName);
            return manifest?.ReplaceWindows.ContainsKey(windowType) == true;
        }

        private static async Task<SkinManifest?> LoadSkinManifestAsync(string skinName)
        {
            if (_manifestCache.TryGetValue(skinName, out var cached))
                return cached;

            try
            {
                var manifestPath = Path.Combine(SkinDir, skinName, "skin.json");
                if (!File.Exists(manifestPath))
                {
                    Logger.Log($"Skin manifest not found: {manifestPath}");
                    return null;
                }

                var json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<SkinManifest>(json);

                if (manifest != null)
                {
                    _manifestCache[skinName] = manifest;
                }

                return manifest;
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load skin manifest '{skinName}': {ex.Message}");
                return null;
            }
        }

        private static async Task ApplyGlobalOverlaysAsync(List<string> globalStyles, StyledElement? element)
        {
            var skinDir = Path.Combine(SkinDir, CurrentSkin);

            foreach (var styleFile in globalStyles)
            {
                var stylePath = Path.Combine(skinDir, styleFile);
                if (!File.Exists(stylePath))
                {
                    Logger.Log($"Global style file not found: {stylePath}");
                    continue;
                }

                await ApplyStyleFileAsync(stylePath, element);
            }
        }

        private static async Task ApplySelectorOverlaysAsync(List<SkinSelector> selectors, StyledElement? element)
        {
            var skinDir = Path.Combine(SkinDir, CurrentSkin);

            foreach (var selector in selectors)
            {
                foreach (var styleFile in selector.Styles)
                {
                    var stylePath = Path.Combine(skinDir, styleFile);
                    if (!File.Exists(stylePath))
                    {
                        Logger.Log($"Selector style file not found: {stylePath}");
                        continue;
                    }

                    await ApplyStyleFileAsync(stylePath, element);
                }
            }
        }

        private static Task ApplyStyleFileAsync(string stylePath, StyledElement? element)
        {
            try
            {
                var uri = new Uri($"file:///{stylePath.Replace('\\', '/')}");
                var styleInclude = new StyleInclude(uri) { Source = uri };

                if (element != null)
                {
                    element.Styles.Add(styleInclude);
                }
                else if (Application.Current != null)
                {
                    Application.Current.Styles.Add(styleInclude);
                }

                Logger.Log($"Applied style file: {stylePath}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to apply style file '{stylePath}': {ex.Message}");
            }
            return Task.CompletedTask;
        }

        private static async Task ApplyWindowReplacementsAsync(Dictionary<string, string> replacements)
        {
            foreach (var kvp in replacements)
            {
                var windowType = kvp.Key;
                var replacementFile = kvp.Value;

                Logger.Log($"Window replacement configuration: {windowType} -> {replacementFile}");

                // Apply window replacements to any open windows of the specified type
                if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    foreach (var window in desktop.Windows)
                    {
                        if (window.GetType().Name == windowType)
                        {
                            var skinDir = Path.Combine(AppContext.BaseDirectory, "Skins", CurrentSkin);
                            var replacementPath = Path.Combine(skinDir, replacementFile);

                            await WindowReplacementManager.ReplaceWindowContentAsync(window, replacementPath);
                        }
                    }
                }
            }
        }

        private static void ClearApplicationSkins()
        {
            if (Application.Current?.Styles == null) return;

            var skinStyles = Application.Current.Styles.OfType<StyleInclude>()
                .Where(s => s.Source?.OriginalString.Contains("/Skins/") == true)
                .ToList();

            foreach (var style in skinStyles)
            {
                Application.Current.Styles.Remove(style);
            }
        }

        public static void RemoveAllSkinsFrom(StyledElement element)
        {
            if (element?.Styles == null) return;

            var skinStyles = element.Styles.OfType<StyleInclude>()
                .Where(s => s.Source?.OriginalString.Contains("/Skins/") == true)
                .ToList();

            foreach (var skin in skinStyles)
            {
                element.Styles.Remove(skin);
            }

            if (skinStyles.Count > 0)
            {
                Logger.Log($"Removed {skinStyles.Count} skins from element");
            }
        }

        /// <summary>
        /// Legacy compatibility methods
        /// </summary>
        public static void ApplySkinsTo(StyledElement element, IEnumerable<string> skinNames)
        {
            // Legacy method - convert to new async system
            foreach (var skinName in skinNames)
            {
                ApplySkinTo(element, skinName);
            }
        }

        private static bool IsFileATheme(string path)
        {
            try
            {
                var content = File.ReadAllText(path);
                return content.Contains("ApplicationBackgroundBrush") || content.Contains("ThemeForegroundColor");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error checking if file is a theme: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Event args for skin changed events
    /// </summary>
    public class SkinChangedEventArgs : EventArgs
    {
        public string SkinName { get; }

        public SkinChangedEventArgs(string skinName)
        {
            SkinName = skinName;
        }
    }
}