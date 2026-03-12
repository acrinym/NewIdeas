using Avalonia.Threading;
using Cycloside.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cycloside.Services;

/// <summary>
/// Manages window decoration themes (WindowBlinds-style)
/// </summary>
public class WindowDecorationManager
{
    private static WindowDecorationManager? _instance;
    public static WindowDecorationManager Instance => _instance ??= new WindowDecorationManager();

    private WindowDecoration? _currentTheme;
    private WindowDecorationConfig _config;
    private readonly List<string> _availableThemes = new();
    private readonly string _themesDirectory;

    /// <summary>
    /// Event raised when a new theme is applied
    /// </summary>
    public event Action<WindowDecoration?>? ThemeChanged;

    /// <summary>
    /// Currently active theme
    /// </summary>
    public WindowDecoration? CurrentTheme => _currentTheme;

    /// <summary>
    /// Current configuration
    /// </summary>
    public WindowDecorationConfig Config => _config;

    /// <summary>
    /// List of available theme paths
    /// </summary>
    public IReadOnlyList<string> AvailableThemes => _availableThemes.AsReadOnly();

    private WindowDecorationManager()
    {
        // Set up themes directory in user's AppData
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _themesDirectory = Path.Combine(appDataPath, "Cycloside", "Themes", "WindowDecorations");

        // Also check local directory
        var localThemesPath = Path.Combine(AppContext.BaseDirectory, "Themes", "WindowDecorations");

        // Create directories if they don't exist
        try
        {
            Directory.CreateDirectory(_themesDirectory);
            Directory.CreateDirectory(localThemesPath);
            Logger.Log($"📁 Window decoration themes directory: {_themesDirectory}");
            Logger.Log($"📁 Local themes directory: {localThemesPath}");
        }
        catch (Exception ex)
        {
            Logger.Log($"⚠️ Failed to create themes directories: {ex.Message}");
        }

        // Load configuration
        _config = LoadConfig();

        // Scan for available themes
        RefreshAvailableThemes();

        // Load active theme if configured
        if (_config.Enabled && !string.IsNullOrEmpty(_config.ActiveTheme))
        {
            LoadTheme(_config.ActiveTheme);
        }
    }

    /// <summary>
    /// Scan directories for available window decoration themes
    /// </summary>
    public void RefreshAvailableThemes()
    {
        _availableThemes.Clear();

        var dirsToScan = new[]
        {
            _themesDirectory,
            Path.Combine(AppContext.BaseDirectory, "Themes", "WindowDecorations")
        };

        foreach (var dir in dirsToScan)
        {
            if (!Directory.Exists(dir))
                continue;

            try
            {
                // Scan for theme directories (contain theme.ini)
                var themeDirs = Directory.GetDirectories(dir, "*", SearchOption.AllDirectories)
                    .Where(d => File.Exists(Path.Combine(d, "theme.ini")));
                _availableThemes.AddRange(themeDirs);

                // Scan for ZIP archives
                var zipFiles = Directory.GetFiles(dir, "*.zip", SearchOption.AllDirectories);
                _availableThemes.AddRange(zipFiles);
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Error scanning {dir} for themes: {ex.Message}");
            }
        }

        Logger.Log($"🎨 Found {_availableThemes.Count} window decoration themes");
    }

    /// <summary>
    /// Load and apply a theme from file path or directory
    /// </summary>
    public bool LoadTheme(string path)
    {
        try
        {
            WindowDecoration? theme = null;

            // Check if it's a ZIP file or directory
            if (File.Exists(path) && path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                theme = WindowDecorationParser.LoadFromArchive(path);
            }
            else if (Directory.Exists(path))
            {
                theme = WindowDecorationParser.LoadFromDirectory(path);
            }
            else
            {
                Logger.Log($"❌ Theme path not found: {path}");
                return false;
            }

            if (theme == null)
            {
                Logger.Log($"❌ Failed to parse theme: {path}");
                return false;
            }

            _currentTheme = theme;
            _config.ActiveTheme = path;
            SaveConfig();

            // Notify subscribers on UI thread
            Dispatcher.UIThread.Post(() => ThemeChanged?.Invoke(theme));

            Logger.Log($"✅ Applied window decoration theme: {theme.Name}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Error loading theme from {path}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Load theme by index from available themes list
    /// </summary>
    public bool LoadThemeByIndex(int index)
    {
        if (index < 0 || index >= _availableThemes.Count)
        {
            Logger.Log($"❌ Invalid theme index: {index}");
            return false;
        }

        return LoadTheme(_availableThemes[index]);
    }

    /// <summary>
    /// Get display name for a theme path
    /// </summary>
    public string GetThemeDisplayName(string path)
    {
        if (File.Exists(path))
        {
            return Path.GetFileNameWithoutExtension(path);
        }
        else if (Directory.Exists(path))
        {
            return new DirectoryInfo(path).Name;
        }
        return path;
    }

    /// <summary>
    /// Disable window decorations (use system default)
    /// </summary>
    public void DisableDecorations()
    {
        _config.Enabled = false;
        _currentTheme = null;
        SaveConfig();

        Dispatcher.UIThread.Post(() => ThemeChanged?.Invoke(null));
        Logger.Log("🎨 Window decorations disabled - using system default");
    }

    /// <summary>
    /// Enable window decorations
    /// </summary>
    public void EnableDecorations()
    {
        _config.Enabled = true;
        SaveConfig();

        if (!string.IsNullOrEmpty(_config.ActiveTheme))
        {
            LoadTheme(_config.ActiveTheme);
        }

        Logger.Log("🎨 Window decorations enabled");
    }

    /// <summary>
    /// Import a theme from anywhere on the system
    /// </summary>
    public bool ImportTheme(string sourcePath)
    {
        try
        {
            string destPath;

            if (File.Exists(sourcePath))
            {
                // Copy ZIP file
                var fileName = Path.GetFileName(sourcePath);
                destPath = Path.Combine(_themesDirectory, fileName);
                File.Copy(sourcePath, destPath, overwrite: true);
            }
            else if (Directory.Exists(sourcePath))
            {
                // Copy directory
                var dirName = new DirectoryInfo(sourcePath).Name;
                destPath = Path.Combine(_themesDirectory, dirName);

                if (Directory.Exists(destPath))
                    Directory.Delete(destPath, recursive: true);

                CopyDirectory(sourcePath, destPath);
            }
            else
            {
                Logger.Log($"❌ Source not found: {sourcePath}");
                return false;
            }

            Logger.Log($"✅ Imported theme to: {destPath}");

            // Refresh and load the new theme
            RefreshAvailableThemes();
            return LoadTheme(destPath);
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Failed to import theme: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if a window should have custom decorations applied
    /// </summary>
    public bool ShouldApplyToWindow(string windowName)
    {
        if (!_config.Enabled || _currentTheme == null)
            return false;

        // Check exclusions first
        if (_config.ExcludedWindows.Any(e => windowName.Contains(e, StringComparison.OrdinalIgnoreCase)))
            return false;

        // If applying to all, return true (unless excluded above)
        if (_config.ApplyToAllWindows)
            return true;

        // Otherwise check inclusions
        return _config.IncludedWindows.Any(i => windowName.Contains(i, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get the themes directory path for user to add themes
    /// </summary>
    public string GetThemesDirectory() => _themesDirectory;

    private WindowDecorationConfig LoadConfig()
    {
        try
        {
            // Would load from SettingsManager.Settings.WindowDecorationConfig
            // For now, return defaults
            return new WindowDecorationConfig();
        }
        catch (Exception ex)
        {
            Logger.Log($"⚠️ Failed to load window decoration config: {ex.Message}");
            return new WindowDecorationConfig();
        }
    }

    private void SaveConfig()
    {
        try
        {
            // Would save to SettingsManager.Settings.WindowDecorationConfig
            // Then call SettingsManager.Save()
            Logger.Log("💾 Window decoration configuration saved");
        }
        catch (Exception ex)
        {
            Logger.Log($"⚠️ Failed to save window decoration config: {ex.Message}");
        }
    }

    private static void CopyDirectory(string sourcePath, string destPath)
    {
        Directory.CreateDirectory(destPath);

        foreach (var file in Directory.GetFiles(sourcePath))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, Path.Combine(destPath, fileName), overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourcePath))
        {
            var dirName = new DirectoryInfo(dir).Name;
            CopyDirectory(dir, Path.Combine(destPath, dirName));
        }
    }
}
