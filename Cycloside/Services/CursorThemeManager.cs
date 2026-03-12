using Cycloside.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cycloside.Services
{
    /// <summary>
    /// Manages cursor themes for the application.
    /// Provides theme loading, scanning, and application services.
    /// </summary>
    public class CursorThemeManager
    {
        private static CursorThemeManager? _instance;
        public static CursorThemeManager Instance => _instance ??= new CursorThemeManager();

        private CursorTheme? _currentTheme;
        private readonly string _themesDirectory;
        private readonly List<string> _availableThemes = new();

        /// <summary>
        /// Event fired when the cursor theme changes
        /// </summary>
        public event Action<CursorTheme?>? ThemeChanged;

        /// <summary>
        /// Current active cursor theme
        /// </summary>
        public CursorTheme? CurrentTheme => _currentTheme;

        /// <summary>
        /// List of available theme paths
        /// </summary>
        public IReadOnlyList<string> AvailableThemes => _availableThemes.AsReadOnly();

        private CursorThemeManager()
        {
            // Determine themes directory based on platform
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _themesDirectory = Path.Combine(appDataPath, "Cycloside", "Themes", "Cursors");

            // Ensure directory exists
            Directory.CreateDirectory(_themesDirectory);

            // Scan for available themes
            RefreshAvailableThemes();

            Logger.Log($"📁 Cursor themes directory: {_themesDirectory}");
        }

        /// <summary>
        /// Refresh the list of available themes by scanning the themes directory
        /// </summary>
        public void RefreshAvailableThemes()
        {
            _availableThemes.Clear();

            try
            {
                // Scan for theme directories
                var directories = Directory.GetDirectories(_themesDirectory);
                foreach (var dir in directories)
                {
                    // Check if directory contains theme.ini or cursor files
                    if (File.Exists(Path.Combine(dir, "theme.ini")) ||
                        Directory.GetFiles(dir, "*.png").Any() ||
                        Directory.GetFiles(dir, "*.cur").Any())
                    {
                        _availableThemes.Add(dir);
                    }
                }

                // Scan for theme ZIP archives
                var archives = Directory.GetFiles(_themesDirectory, "*.zip");
                _availableThemes.AddRange(archives);

                Logger.Log($"✅ Found {_availableThemes.Count} cursor theme(s)");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Error scanning for cursor themes: {ex.Message}");
            }
        }

        /// <summary>
        /// Load a cursor theme from a directory or ZIP archive
        /// </summary>
        /// <param name="path">Path to theme directory or ZIP file</param>
        /// <returns>True if theme loaded successfully</returns>
        public bool LoadTheme(string path)
        {
            try
            {
                CursorTheme? theme = null;

                if (Directory.Exists(path))
                {
                    theme = CursorThemeParser.LoadFromDirectory(path);
                }
                else if (File.Exists(path) && path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    theme = CursorThemeParser.LoadFromArchive(path);
                }
                else
                {
                    Logger.Log($"❌ Cursor theme not found: {path}");
                    return false;
                }

                if (theme == null)
                {
                    Logger.Log($"❌ Failed to load cursor theme from: {path}");
                    return false;
                }

                _currentTheme = theme;
                Logger.Log($"✅ Loaded cursor theme: {theme.Name}");

                // Notify subscribers that theme changed
                ThemeChanged?.Invoke(theme);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Error loading cursor theme: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Import a cursor theme from an external location
        /// Copies the theme to the user's themes directory and loads it
        /// </summary>
        /// <param name="sourcePath">Path to theme directory or ZIP to import</param>
        /// <returns>True if import and load succeeded</returns>
        public bool ImportTheme(string sourcePath)
        {
            try
            {
                if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
                {
                    Logger.Log($"❌ Source path not found: {sourcePath}");
                    return false;
                }

                string destinationPath;

                if (File.Exists(sourcePath) && sourcePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    // Copy ZIP file
                    var fileName = Path.GetFileName(sourcePath);
                    destinationPath = Path.Combine(_themesDirectory, fileName);
                    File.Copy(sourcePath, destinationPath, overwrite: true);
                    Logger.Log($"✅ Imported cursor theme ZIP: {fileName}");
                }
                else if (Directory.Exists(sourcePath))
                {
                    // Copy directory
                    var dirName = new DirectoryInfo(sourcePath).Name;
                    destinationPath = Path.Combine(_themesDirectory, dirName);

                    // Create destination directory
                    Directory.CreateDirectory(destinationPath);

                    // Copy all files
                    foreach (var file in Directory.GetFiles(sourcePath))
                    {
                        var destFile = Path.Combine(destinationPath, Path.GetFileName(file));
                        File.Copy(file, destFile, overwrite: true);
                    }

                    Logger.Log($"✅ Imported cursor theme directory: {dirName}");
                }
                else
                {
                    Logger.Log($"❌ Invalid source path: {sourcePath}");
                    return false;
                }

                // Refresh available themes list
                RefreshAvailableThemes();

                // Load the imported theme
                return LoadTheme(destinationPath);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Error importing cursor theme: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unload the current theme (revert to system cursors)
        /// </summary>
        public void UnloadTheme()
        {
            _currentTheme = null;
            Logger.Log("✅ Unloaded cursor theme (reverted to system cursors)");
            ThemeChanged?.Invoke(null);
        }

        /// <summary>
        /// Get a cursor definition for a specific cursor type from the current theme
        /// </summary>
        /// <param name="type">The cursor type to retrieve</param>
        /// <returns>Cursor definition if available, null otherwise</returns>
        public CursorDefinition? GetCursor(CursorType type)
        {
            if (_currentTheme == null) return null;

            return type switch
            {
                CursorType.Arrow => _currentTheme.Arrow,
                CursorType.Hand => _currentTheme.Hand,
                CursorType.IBeam => _currentTheme.IBeam,
                CursorType.Wait => _currentTheme.Wait,
                CursorType.AppStarting => _currentTheme.AppStarting,
                CursorType.Cross => _currentTheme.Cross,
                CursorType.SizeNS => _currentTheme.SizeNS,
                CursorType.SizeEW => _currentTheme.SizeEW,
                CursorType.SizeNESW => _currentTheme.SizeNESW,
                CursorType.SizeNWSE => _currentTheme.SizeNWSE,
                CursorType.SizeAll => _currentTheme.SizeAll,
                CursorType.No => _currentTheme.No,
                CursorType.Help => _currentTheme.Help,
                CursorType.UpArrow => _currentTheme.UpArrow,
                _ => null
            };
        }

        /// <summary>
        /// Get information about an available theme without fully loading it
        /// </summary>
        /// <param name="path">Path to theme</param>
        /// <returns>Theme name and metadata, or null if unavailable</returns>
        public (string Name, string Author, string Description)? GetThemeInfo(string path)
        {
            try
            {
                CursorTheme? theme = null;

                if (Directory.Exists(path))
                {
                    theme = CursorThemeParser.LoadFromDirectory(path);
                }
                else if (File.Exists(path) && path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    theme = CursorThemeParser.LoadFromArchive(path);
                }

                if (theme != null)
                {
                    return (theme.Name, theme.Author, theme.Description);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Error reading theme info: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Export current theme configuration for saving/sharing
        /// </summary>
        /// <returns>INI format string with theme configuration</returns>
        public string? ExportCurrentThemeConfig()
        {
            if (_currentTheme == null) return null;

            var config = "[Theme]\n";
            config += $"Name={_currentTheme.Name}\n";
            config += $"Author={_currentTheme.Author}\n";
            config += $"Version={_currentTheme.Version}\n";
            config += $"Description={_currentTheme.Description}\n\n";

            config += "[Settings]\n";
            config += $"UseSystemCursorsAsFallback={_currentTheme.UseSystemCursorsAsFallback}\n";
            config += $"AnimationFrameRate={_currentTheme.AnimationFrameRate}\n\n";

            config += "[Cursors]\n";
            var cursors = _currentTheme.GetAllCursors();
            foreach (var (type, def) in cursors)
            {
                config += $"{CursorTypeNames.ToString(type)}={def.Frames.Count} frame(s)\n";
            }

            return config;
        }
    }
}
