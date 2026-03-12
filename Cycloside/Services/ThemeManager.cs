using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Services
{
    /// <summary>
    /// Manages the application-wide semantic theme pack and theme variant.
    /// Themes own the app-wide resource palette. Skins are layered separately.
    /// </summary>
    public static class ThemeManager
    {
        private static readonly object SyncLock = new();
        private static readonly List<IStyle> ApplicationThemeStyles = new();
        private static readonly Dictionary<StyledElement, List<IStyle>> ElementThemeStyles = new();

        private static string ThemesDirectory => Path.Combine(AppContext.BaseDirectory, "Themes");
        private static string LegacyThemesDirectory => Path.Combine(ThemesDirectory, "Global");

        public static string CurrentTheme { get; private set; } = "Dockside";

        public static ThemeVariant CurrentVariant { get; private set; } = ThemeVariant.Dark;

        public static event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        public static void PreloadThemeResources()
        {
            try
            {
                var themeCount = GetAvailableThemes().Count();
                var variantCount = GetAvailableVariants().Length;
                Logger.Log($"ThemeManager: detected {themeCount} theme packs and {variantCount} theme variants");
            }
            catch (Exception ex)
            {
                Logger.Log($"ThemeManager preload failed: {ex.Message}");
            }
        }

        public static void ClearThemeCache()
        {
            try
            {
                RemoveManagedApplicationThemeStyles();

                lock (SyncLock)
                {
                    var elementEntries = ElementThemeStyles.ToArray();
                    ElementThemeStyles.Clear();

                    foreach (var entry in elementEntries)
                    {
                        RemoveManagedElementThemeStyles(entry.Key, entry.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"ThemeManager cleanup failed: {ex.Message}");
            }
        }

        public static void InitializeFromSettings()
        {
            var settings = SettingsManager.Settings;
            var themeName = NormalizeThemeName(settings.GlobalTheme);
            var variant = ParseVariantFromSettings(settings.RequestedThemeVariant);

            ApplyThemeAsync(themeName, variant, false).GetAwaiter().GetResult();
        }

        public static async Task<bool> ApplyThemeAsync(string themeName, ThemeVariant variant, bool saveSettings = true)
        {
            if (Application.Current == null)
            {
                Logger.Log("ThemeManager: Application.Current is null");
                return false;
            }

            themeName = NormalizeThemeName(themeName);

            try
            {
                RemoveManagedApplicationThemeStyles();

                Application.Current.RequestedThemeVariant = variant;

                if (!LoadVariantResources(variant))
                {
                    Logger.Log($"ThemeManager: failed to load theme variant '{variant}'");
                    return false;
                }

                if (!LoadThemeResources(themeName))
                {
                    Logger.Log($"ThemeManager: failed to load theme pack '{themeName}'");
                    return false;
                }

                CurrentTheme = themeName;
                CurrentVariant = variant;

                if (saveSettings)
                {
                    SettingsManager.Settings.GlobalTheme = themeName;
                    SettingsManager.Settings.RequestedThemeVariant = VariantToSettingsString(variant);
                    SettingsManager.Save();
                }

                if (!string.IsNullOrWhiteSpace(SkinManager.CurrentSkin))
                {
                    await SkinManager.ReapplyCurrentSkinAsync(false);
                }

                await ApplyToAllWindowsAsync();

                ThemeChanged?.Invoke(null, new ThemeChangedEventArgs(CurrentTheme, CurrentVariant));
                Logger.Log($"ThemeManager: applied theme '{CurrentTheme}' with variant '{CurrentVariant}'");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"ThemeManager: error applying theme '{themeName}': {ex.Message}");
                return false;
            }
        }

        public static Task<bool> ApplyVariantAsync(ThemeVariant variant)
        {
            return ApplyThemeAsync(CurrentTheme, variant);
        }

        public static Task<bool> ApplySubthemeAsync(string themeName)
        {
            return ApplyThemeAsync(themeName, CurrentVariant);
        }

        public static IEnumerable<string> GetAvailableThemes()
        {
            var themeNames = new List<string>();

            if (Directory.Exists(ThemesDirectory))
            {
                foreach (var directory in Directory.GetDirectories(ThemesDirectory))
                {
                    var name = Path.GetFileName(directory);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (string.Equals(name, "Global", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "LightTheme", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "DarkTheme", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "HighContrastTheme", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (File.Exists(Path.Combine(directory, "Tokens.axaml")))
                    {
                        themeNames.Add(name);
                    }
                }
            }

            if (Directory.Exists(LegacyThemesDirectory))
            {
                foreach (var filePath in Directory.GetFiles(LegacyThemesDirectory, "*.axaml"))
                {
                    var name = Path.GetFileNameWithoutExtension(filePath);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        themeNames.Add(name);
                    }
                }
            }

            return themeNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
        }

        public static ThemeVariant[] GetAvailableVariants()
        {
            return new[]
            {
                ThemeVariant.Default,
                ThemeVariant.Light,
                ThemeVariant.Dark
            };
        }

        public static bool LoadGlobalTheme(string themeName)
        {
            try
            {
                return ApplyThemeAsync(themeName, CurrentVariant, true).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Log($"ThemeManager: LoadGlobalTheme failed: {ex.Message}");
                return false;
            }
        }

        public static void ApplyForPlugin(Window window, Cycloside.Plugins.IPlugin plugin)
        {
            if (plugin.ForceDefaultTheme)
            {
                RemoveComponentThemes(window);
                AnimatedBackgroundManager.ApplyForPlugin(window, plugin);
                return;
            }

            if (SettingsManager.Settings.ComponentThemes.TryGetValue(plugin.Name, out var componentTheme) &&
                !string.IsNullOrWhiteSpace(componentTheme))
            {
                ApplyComponentTheme(window, componentTheme);
            }

            if (SettingsManager.Settings.PluginSkins.TryGetValue(plugin.Name, out var skinName) &&
                !string.IsNullOrWhiteSpace(skinName))
            {
                SkinManager.ApplySkinTo(window, skinName);
            }

            AnimatedBackgroundManager.ApplyForPlugin(window, plugin);
        }

        public static void ApplyFromSettings(Window window, string componentName)
        {
            if (SettingsManager.Settings.ComponentThemes.TryGetValue(componentName, out var themeName) &&
                !string.IsNullOrWhiteSpace(themeName))
            {
                ApplyComponentTheme(window, themeName);
            }

            AnimatedBackgroundManager.ApplyFromSettings(window, componentName);
        }

        public static void ApplyComponentTheme(StyledElement element, string componentName)
        {
            try
            {
                RemoveComponentThemes(element);

                var filePaths = ResolveThemeFilePaths(componentName);
                if (filePaths.Count == 0)
                {
                    return;
                }

                var appliedStyles = new List<IStyle>();
                foreach (var filePath in filePaths)
                {
                    var style = LoadStyle(filePath);
                    if (style == null)
                    {
                        continue;
                    }

                    element.Styles.Add(style);
                    appliedStyles.Add(style);
                }

                if (appliedStyles.Count == 0)
                {
                    return;
                }

                lock (SyncLock)
                {
                    ElementThemeStyles[element] = appliedStyles;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"ThemeManager: failed to apply component theme '{componentName}': {ex.Message}");
            }
        }

        public static void RemoveComponentThemes(StyledElement element)
        {
            try
            {
                List<IStyle>? styles;

                lock (SyncLock)
                {
                    if (!ElementThemeStyles.TryGetValue(element, out styles))
                    {
                        return;
                    }

                    ElementThemeStyles.Remove(element);
                }

                RemoveManagedElementThemeStyles(element, styles);
            }
            catch (Exception ex)
            {
                Logger.Log($"ThemeManager: failed to remove component themes: {ex.Message}");
            }
        }

        private static bool LoadVariantResources(ThemeVariant variant)
        {
            var filePath = GetVariantFilePath(variant);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return true;
            }

            return AddThemeStylesToApplication(new[] { filePath });
        }

        private static bool LoadThemeResources(string themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
            {
                return true;
            }

            var filePaths = ResolveThemeFilePaths(themeName);
            if (filePaths.Count == 0)
            {
                return false;
            }

            return AddThemeStylesToApplication(filePaths);
        }

        private static bool AddThemeStylesToApplication(IEnumerable<string> filePaths)
        {
            if (Application.Current == null)
            {
                return false;
            }

            foreach (var filePath in filePaths)
            {
                var style = LoadStyle(filePath);
                if (style == null)
                {
                    return false;
                }

                Application.Current.Styles.Add(style);

                lock (SyncLock)
                {
                    ApplicationThemeStyles.Add(style);
                }
            }

            return true;
        }

        private static IStyle? LoadStyle(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Logger.Log($"ThemeManager: style file not found: {filePath}");
                    return null;
                }

                var xaml = File.ReadAllText(filePath);
                var parsed = AvaloniaRuntimeXamlLoader.Parse(xaml, typeof(App).Assembly);
                if (parsed is IStyle style)
                {
                    return style;
                }

                Logger.Log($"ThemeManager: '{filePath}' does not contain a style root");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"ThemeManager: failed to parse '{filePath}': {ex.Message}");
                return null;
            }
        }

        private static List<string> ResolveThemeFilePaths(string themeName)
        {
            var filePaths = new List<string>();

            if (string.IsNullOrWhiteSpace(themeName))
            {
                return filePaths;
            }

            var structuredDirectory = Path.Combine(ThemesDirectory, themeName);
            if (Directory.Exists(structuredDirectory))
            {
                var tokensPath = Path.Combine(structuredDirectory, "Tokens.axaml");
                if (File.Exists(tokensPath))
                {
                    filePaths.Add(tokensPath);
                }

                foreach (var filePath in Directory.GetFiles(structuredDirectory, "*.axaml")
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    if (string.Equals(filePath, tokensPath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    filePaths.Add(filePath);
                }

                if (filePaths.Count > 0)
                {
                    return filePaths;
                }
            }

            var legacyPath = Path.Combine(LegacyThemesDirectory, $"{themeName}.axaml");
            if (File.Exists(legacyPath))
            {
                filePaths.Add(legacyPath);
            }

            return filePaths;
        }

        private static string NormalizeThemeName(string? themeName)
        {
            if (string.IsNullOrWhiteSpace(themeName))
            {
                return string.Empty;
            }

            if (ResolveThemeFilePaths(themeName).Count > 0)
            {
                return themeName;
            }

            if (ResolveThemeFilePaths("Dockside").Count > 0)
            {
                return "Dockside";
            }

            if (ResolveThemeFilePaths("MintGreen").Count > 0)
            {
                return "MintGreen";
            }

            var firstTheme = GetAvailableThemes().FirstOrDefault();
            return firstTheme ?? string.Empty;
        }

        private static string? GetVariantFilePath(ThemeVariant variant)
        {
            if (variant == ThemeVariant.Light)
            {
                return Path.Combine(ThemesDirectory, "LightTheme", "Tokens.axaml");
            }

            if (variant == ThemeVariant.Dark)
            {
                return Path.Combine(ThemesDirectory, "DarkTheme", "Tokens.axaml");
            }

            return null;
        }

        private static ThemeVariant ParseVariantFromSettings(string? variantString)
        {
            if (string.Equals(variantString, "Light", StringComparison.OrdinalIgnoreCase))
            {
                return ThemeVariant.Light;
            }

            if (string.Equals(variantString, "Dark", StringComparison.OrdinalIgnoreCase))
            {
                return ThemeVariant.Dark;
            }

            return ThemeVariant.Default;
        }

        private static string VariantToSettingsString(ThemeVariant variant)
        {
            if (variant == ThemeVariant.Light)
            {
                return "Light";
            }

            if (variant == ThemeVariant.Dark)
            {
                return "Dark";
            }

            return "Default";
        }

        private static void RemoveManagedApplicationThemeStyles()
        {
            if (Application.Current == null)
            {
                return;
            }

            List<IStyle> stylesToRemove;

            lock (SyncLock)
            {
                stylesToRemove = ApplicationThemeStyles.ToList();
                ApplicationThemeStyles.Clear();
            }

            foreach (var style in stylesToRemove)
            {
                Application.Current.Styles.Remove(style);
            }
        }

        private static void RemoveManagedElementThemeStyles(StyledElement element, IEnumerable<IStyle> styles)
        {
            foreach (var style in styles)
            {
                element.Styles.Remove(style);
            }
        }

        private static Task ApplyToAllWindowsAsync()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return Task.CompletedTask;
            }

            foreach (var window in desktop.Windows.ToArray())
            {
                try
                {
                    window.RequestedThemeVariant = CurrentVariant;

                    if (window is Plugins.PluginWindowBase pluginWindow && pluginWindow.Plugin != null)
                    {
                        ApplyForPlugin(window, pluginWindow.Plugin);
                    }

                    window.InvalidateMeasure();
                    window.InvalidateArrange();
                    window.InvalidateVisual();
                }
                catch (Exception ex)
                {
                    Logger.Log($"ThemeManager: failed to refresh window '{window.Title}': {ex.Message}");
                }
            }

            return Task.CompletedTask;
        }
    }

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
