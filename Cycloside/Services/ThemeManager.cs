using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Services
{
    /// <summary>
    /// Manages dynamic theme switching including ThemeVariant and subtheme packs.
    /// Supports runtime switching of themes applied to all open windows.
    /// </summary>
    public static class ThemeManager
    {
        private static readonly Dictionary<string, StyleInclude> _themeCache = new();
        private static readonly Dictionary<string, StyleInclude> _variantCache = new();
        private static readonly Dictionary<string, DateTime> _fileTimestamps = new();
        private static readonly object _cacheLock = new object();

        /// <summary>
        /// Current active theme name (subtheme pack name)
        /// </summary>
        public static string CurrentTheme { get; private set; } = "LightTheme";

        /// <summary>
        /// Current ThemeVariant (Light/Dark/HighContrast)
        /// </summary>
        public static ThemeVariant CurrentVariant { get; private set; } = ThemeVariant.Default;

        /// <summary>
        /// Event fired when theme or variant changes
        /// </summary>
        public static event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        /// <summary>
        /// Preload theme resources for faster switching
        /// </summary>
        public static void PreloadThemeResources()
        {
            try
            {
                Logger.Log("🎨 Preloading theme resources...");

                // Preload common theme files
                var themeDir = Path.Combine(AppContext.BaseDirectory, "Themes");
                if (Directory.Exists(themeDir))
                {
                    var themeDirs = Directory.GetDirectories(themeDir);
                    foreach (var themePath in themeDirs)
                    {
                        var themeName = Path.GetFileName(themePath);
                        var tokensPath = Path.Combine(themePath, "Tokens.axaml");

                        if (File.Exists(tokensPath))
                        {
                            // Load and cache the theme file
                            LoadThemeTokensFile(tokensPath, themeName);
                        }
                    }
                }

                Logger.Log($"✅ Preloaded {themeDirs?.Length ?? 0} theme resources");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Theme resource preloading failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear theme cache to free memory
        /// </summary>
        public static void ClearThemeCache()
        {
            lock (_cacheLock)
            {
                _themeCache.Clear();
                _variantCache.Clear();
                _fileTimestamps.Clear();
                Logger.Log("🗑️ Theme cache cleared");
            }
        }

        /// <summary>
        /// Initialize theme system from settings
        /// </summary>
        public static void InitializeFromSettings()
        {
            var settings = SettingsManager.Settings;
            var themeName = settings.GlobalTheme ?? "LightTheme";
            var variant = ParseVariantFromSettings(settings.RequestedThemeVariant);

            ApplyThemeAsync(themeName, variant, false).Wait();
        }

        /// <summary>
        /// Apply a specific theme and variant combination
        /// </summary>
        public static async Task<bool> ApplyThemeAsync(string themeName, ThemeVariant variant, bool saveSettings = true)
        {
            if (Application.Current == null)
            {
                Logger.Log("ThemeManager: Application.Current is null, cannot apply theme");
                return false;
            }

            try
            {
                // Load theme variant tokens first
                if (!await LoadThemeVariantTokensAsync(variant))
                {
                    Logger.Log($"Failed to load theme variant tokens for {variant}");
                    return false;
                }

                // Load subtheme pack if specified
                if (!string.IsNullOrEmpty(themeName) && !await LoadSubthemeAsync(themeName))
                {
                    Logger.Log($"Failed to load subtheme pack: {themeName}");
                    return false;
                }

                // Update current theme state
                CurrentTheme = themeName ?? "LightTheme";
                CurrentVariant = variant;

                // Save settings if requested
                if (saveSettings)
                {
                    var settings = SettingsManager.Settings;
                    settings.GlobalTheme = themeName ?? "LightTheme";
                    settings.RequestedThemeVariant = variant.ToString();
                    SettingsManager.Save();
                }

                // Apply to all open windows
                await ApplyToAllWindowsAsync();

                // Fire theme changed event
                ThemeChanged?.Invoke(null, new ThemeChangedEventArgs(CurrentTheme, CurrentVariant));

                Logger.Log($"Successfully applied theme '{themeName}' with variant '{variant}'");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error applying theme '{themeName}' with variant '{variant}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply theme variant only (Light/Dark/HighContrast)
        /// </summary>
        public static async Task<bool> ApplyVariantAsync(ThemeVariant variant)
        {
            return await ApplyThemeAsync(CurrentTheme, variant);
        }

        /// <summary>
        /// Apply subtheme pack only
        /// </summary>
        public static async Task<bool> ApplySubthemeAsync(string themeName)
        {
            return await ApplyThemeAsync(themeName, CurrentVariant);
        }

        /// <summary>
        /// Get available theme names
        /// </summary>
        public static IEnumerable<string> GetAvailableThemes()
        {
            var themesDir = Path.Combine(AppContext.BaseDirectory, "Themes");
            if (!Directory.Exists(themesDir)) return Enumerable.Empty<string>();

            return Directory.GetDirectories(themesDir)
                .Select(dir => Path.GetFileName(dir)!)
                .Where(name => name != "Global" && File.Exists(Path.Combine(themesDir, name, "Tokens.axaml")))
                .OrderBy(name => name);
        }

        /// <summary>
        /// Get available theme variants
        /// </summary>
        public static ThemeVariant[] GetAvailableVariants()
        {
            return new[] { ThemeVariant.Light, ThemeVariant.Dark };
        }

        private static Task<bool> LoadThemeVariantTokensAsync(ThemeVariant variant)
        {
            try
            {
                // Clear any existing variant tokens
                ClearVariantTokens();

                string? variantTheme = null;
                if (variant == ThemeVariant.Light)
                {
                    variantTheme = Path.Combine(AppContext.BaseDirectory, "Themes", "LightTheme", "Tokens.axaml");
                }
                else if (variant == ThemeVariant.Dark)
                {
                    variantTheme = Path.Combine(AppContext.BaseDirectory, "Themes", "DarkTheme", "Tokens.axaml");
                }

                if (variantTheme != null && File.Exists(variantTheme))
                {
                    var cacheKey = $"variant_{variant}_{variantTheme}";

                    // Check cache first
                    if (IsCachedUpToDate(cacheKey, variantTheme))
                    {
                        var cachedStyle = GetCachedStyleInclude(cacheKey);
                        if (cachedStyle != null && Application.Current != null)
                        {
                            Application.Current.Styles.Add(cachedStyle);
                            Logger.Log($"Loaded cached variant theme: {variant}");
                            return Task.FromResult(true);
                        }
                    }

                    // Load and cache new variant
                    var uri = new Uri($"file:///{variantTheme.Replace('\\', '/')}");
                    var styleInclude = new StyleInclude(uri) { Source = uri };

                    CacheStyleInclude(cacheKey, styleInclude, variantTheme);
                    if (Application.Current != null)
                        Application.Current.Styles.Add(styleInclude);

                    Logger.Log($"Loaded and cached variant theme: {variant}");
                    return Task.FromResult(true);
                }
                else
                {
                    // Default variant - no special handling needed
                    return Task.FromResult(true);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load theme variant tokens for {variant}: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private static Task<bool> LoadSubthemeAsync(string themeName)
        {
            try
            {
                // Clear any existing subtheme
                ClearSubthemeStyles();

                var themeDir = Path.Combine(AppContext.BaseDirectory, "Themes", themeName);
                if (!Directory.Exists(themeDir))
                {
                    Logger.Log($"Subtheme directory not found: {themeDir}");
                    return Task.FromResult(false);
                }

                // Load subtheme styles files
                var styleFiles = Directory.GetFiles(themeDir, "*.axaml")
                    .Where(f => !Path.GetFileName(f).Equals("Tokens.axaml", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                foreach (var styleFile in styleFiles)
                {
                    try
                    {
                        var uri = new Uri($"file:///{styleFile.Replace('\\', '/')}");
                        var styleInclude = new StyleInclude(uri) { Source = uri };
                        if (Application.Current != null)
                            Application.Current.Styles.Add(styleInclude);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Failed to load subtheme style file '{styleFile}': {ex.Message}");
                    }
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load subtheme '{themeName}': {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private static void ClearVariantTokens()
        {
            if (Application.Current?.Styles == null) return;

            var variantTokens = Application.Current.Styles.OfType<StyleInclude>()
                .Where(s => s.Source?.OriginalString.Contains("/Themes/LightTheme/") == true ||
                           s.Source?.OriginalString.Contains("/Themes/DarkTheme/") == true ||
                           s.Source?.OriginalString.Contains("/Themes/HighContrastTheme/") == true)
                .ToList();

            foreach (var token in variantTokens)
            {
                Application.Current.Styles.Remove(token);
            }
        }

        private static void ClearSubthemeStyles()
        {
            if (Application.Current?.Styles == null) return;

            var subthemeStyles = Application.Current.Styles.OfType<StyleInclude>()
                .Where(s => s.Source?.OriginalString.Contains("/Themes/") == true &&
                           !s.Source.OriginalString.Contains("/Themes/LightTheme/") &&
                           !s.Source.OriginalString.Contains("/Themes/DarkTheme/") &&
                           !s.Source.OriginalString.Contains("/Themes/HighContrastTheme/") &&
                           !s.Source.OriginalString.Contains("/Themes/Tokens.axaml"))
                .ToList();

            foreach (var style in subthemeStyles)
            {
                Application.Current.Styles.Remove(style);
            }
        }

        private static async Task ApplyToAllWindowsAsync()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            var windows = desktop.Windows.ToArray(); // Create snapshot to avoid collection changes
            foreach (var window in windows)
            {
                try
                {
                    // Apply theme to window
                    await ApplyThemeToWindowAsync(window);

                    // Force visual refresh
                    window.InvalidateVisual();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to refresh window '{window.Title}': {ex.Message}");
                }
            }
        }

        private static Task ApplyThemeToWindowAsync(Window window)
        {
            try
            {
                // Apply global theme to window
                _ = ApplyThemeAsync(CurrentTheme, CurrentVariant, false).ConfigureAwait(false);

                // If this is a plugin window, apply plugin-specific theming  
                if (window is Plugins.PluginWindowBase pluginWindow && pluginWindow.Plugin != null)
                {
                    ApplyForPlugin(window, pluginWindow.Plugin);
                }

                // Trigger any window in events for theme change
                TriggerWindowThemeChangeEvents(window);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error applying theme to window '{window.Title}': {ex.Message}");
            }
            return Task.CompletedTask;
        }

        private static void TriggerWindowThemeChangeEvents(Window window)
        {
            // This allows windows to respond to theme changes if they implement theming interfaces
            // For now, we'll just invalidate the visual tree to force a re-render
            try
            {
                window.InvalidateMeasure();
                window.InvalidateArrange();

                // Trigger layout updates - simplified for now
                window.InvalidateVisual();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error triggering theme change events: {ex.Message}");
            }
        }

        private static ThemeVariant ParseVariantFromSettings(string? variantString)
        {
            return variantString switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }

        /// <summary>
        /// Legacy compatibility method - loads global theme (backward compatibility)
        /// </summary>
        public static bool LoadGlobalTheme(string themeName)
        {
            try
            {
                ApplyThemeAsync(themeName, ThemeVariant.Default, false).Wait();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Legacy compatibility method - applies theme for plugin windows
        /// </summary>
        public static void ApplyForPlugin(Window window, Cycloside.Plugins.IPlugin plugin)
        {
            if (plugin.ForceDefaultTheme)
                return;

            // Plugin-specific skins are now handled by SkinManager
            if (SettingsManager.Settings.PluginSkins.TryGetValue(plugin.Name, out var skin) && !string.IsNullOrEmpty(skin))
            {
                SkinManager.ApplySkinTo(window, skin);
            }
        }

        /// <summary>
        /// Legacy compatibility method - applies component theme
        /// </summary>
        public static void ApplyFromSettings(Window window, string componentName)
        {
            // Legacy method - no longer used in new system
            // Component themes are now handled via semantic tokens and skins
        }

        /// <summary>
        /// Legacy compatibility method - applies component theme to element
        /// </summary>
        public static void ApplyComponentTheme(StyledElement element, string componentName)
        {
            // Legacy method - component themes are now handled via semantic tokens and skins
        }

        /// <summary>
        /// Legacy compatibility method - removes component themes
        /// </summary>
        public static void RemoveComponentThemes(StyledElement element)
        {
            // Legacy method - no longer needed with new system
        }

        /// <summary>
        /// Validates theme file and performs safety checks
        /// </summary>
        private static bool ValidateThemeFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var content = File.ReadAllText(filePath);

                // Basic XAML validation
                return content.Contains("<Style") &&
                       content.Contains("</Style>") ||
                       content.Contains("<Styles") &&
                       content.Contains("</Styles>");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error validating theme file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if cached resource is up to date based on file timestamp
        /// </summary>
        private static bool IsCachedUpToDate(string cacheKey, string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            var currentTimestamp = File.GetLastWriteTime(filePath);

            lock (_cacheLock)
            {
                return _fileTimestamps.TryGetValue(cacheKey, out var cachedTimestamp) &&
                       currentTimestamp <= cachedTimestamp;
            }
        }

        /// <summary>
        /// Gets cached StyleInclude if available
        /// </summary>
        private static StyleInclude? GetCachedStyleInclude(string cacheKey)
        {
            lock (_cacheLock)
            {
                return _themeCache.TryGetValue(cacheKey, out var style) ?
                       CloneStyleInclude(style) : null;
            }
        }

        /// <summary>
        /// Caches StyleInclude with file timestamp
        /// </summary>
        private static void CacheStyleInclude(string cacheKey, StyleInclude styleInclude, string filePath)
        {
            lock (_cacheLock)
            {
                _themeCache[cacheKey] = CloneStyleInclude(styleInclude);
                _fileTimestamps[cacheKey] = File.GetLastWriteTime(filePath);
            }
        }

        /// <summary>
        /// Creates a clone of StyleInclude for caching
        /// </summary>
        private static StyleInclude CloneStyleInclude(StyleInclude original)
        {
            return new StyleInclude(original.Source!) { Source = original.Source };
        }

        /// <summary>
        /// Clears all cached themes to free memory
        /// </summary>
        public static void ClearThemeCache()
        {
            lock (_cacheLock)
            {
                _themeCache.Clear();
                _variantCache.Clear();
                _fileTimestamps.Clear();
                Logger.Log("Theme cache cleared");
            }
        }
    }

    /// <summary>
    /// Event args for theme changed events
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        public string ThemeName { get; }
        public ThemeVariant Variant { get; }

        public ThemeChangedEventArgs(string themeName, ThemeVariant variant)
        {
            ThemeName = themeName;
            Variant = variant;
        }
    }
}