using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Cycloside.Models
{
    /// <summary>
    /// Parser for cursor theme packages (.zip or directories)
    /// Supports custom theme.ini format for cursor themes
    /// </summary>
    public class CursorThemeParser
    {
        /// <summary>
        /// Load cursor theme from a directory
        /// </summary>
        public static CursorTheme? LoadFromDirectory(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Logger.Log($"❌ Cursor theme directory not found: {directoryPath}");
                    return null;
                }

                var theme = new CursorTheme();

                // Load theme.ini configuration
                var configFile = Path.Combine(directoryPath, "theme.ini");
                if (File.Exists(configFile))
                {
                    LoadConfiguration(theme, configFile);
                }
                else
                {
                    Logger.Log($"⚠️ No theme.ini found in cursor theme directory");
                }

                // Load cursor files based on naming convention
                LoadCursorsFromDirectory(theme, directoryPath);

                Logger.Log($"✅ Loaded cursor theme: {theme.Name}");
                return theme;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Error loading cursor theme from directory: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load cursor theme from a ZIP archive
        /// </summary>
        public static CursorTheme? LoadFromArchive(string archivePath)
        {
            try
            {
                if (!File.Exists(archivePath))
                {
                    Logger.Log($"❌ Cursor theme archive not found: {archivePath}");
                    return null;
                }

                using var archive = ZipFile.OpenRead(archivePath);
                var theme = new CursorTheme();

                // Load theme.ini from archive
                var configEntry = archive.Entries.FirstOrDefault(e =>
                    e.Name.Equals("theme.ini", StringComparison.OrdinalIgnoreCase));

                if (configEntry != null)
                {
                    using var stream = configEntry.Open();
                    using var reader = new StreamReader(stream);
                    var configContent = reader.ReadToEnd();
                    LoadConfigurationFromString(theme, configContent);
                }

                // Load cursor files from archive
                LoadCursorsFromArchive(theme, archive);

                Logger.Log($"✅ Loaded cursor theme from archive: {theme.Name}");
                return theme;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Error loading cursor theme from archive: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load theme metadata and configuration from INI file
        /// </summary>
        private static void LoadConfiguration(CursorTheme theme, string configPath)
        {
            try
            {
                var content = File.ReadAllText(configPath);
                LoadConfigurationFromString(theme, content);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Error loading cursor theme config: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse theme configuration from INI string
        /// </summary>
        private static void LoadConfigurationFromString(CursorTheme theme, string content)
        {
            var lines = content.Split('\n');
            string currentSection = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip comments and empty lines
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                    continue;

                // Section header
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2).ToLower();
                    continue;
                }

                // Key=Value pair
                var parts = trimmed.Split('=', 2);
                if (parts.Length != 2) continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                // Parse based on section
                if (currentSection == "theme")
                {
                    switch (key.ToLower())
                    {
                        case "name":
                            theme.Name = value;
                            break;
                        case "author":
                            theme.Author = value;
                            break;
                        case "version":
                            theme.Version = value;
                            break;
                        case "description":
                            theme.Description = value;
                            break;
                    }
                }
                else if (currentSection == "settings")
                {
                    switch (key.ToLower())
                    {
                        case "usesystemcursorsasfallback":
                            theme.UseSystemCursorsAsFallback = ParseBool(value);
                            break;
                        case "animationframerate":
                            theme.AnimationFrameRate = ParseInt(value, 30);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Load cursor files from directory based on naming convention
        /// </summary>
        private static void LoadCursorsFromDirectory(CursorTheme theme, string directoryPath)
        {
            // Look for cursor files with standard naming
            // arrow.png, hand.png, ibeam.png, etc.
            // or animated: arrow_001.png, arrow_002.png, ...

            var cursorFiles = Directory.GetFiles(directoryPath, "*.png")
                .Concat(Directory.GetFiles(directoryPath, "*.cur"))
                .ToList();

            // Group files by cursor type (handle animations)
            var cursorGroups = new Dictionary<CursorType, List<string>>();

            foreach (var file in cursorFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                // Check for animation frame suffix (e.g., "arrow_001", "wait_05")
                var baseName = fileName;
                var match = System.Text.RegularExpressions.Regex.Match(fileName, @"^(.+?)_(\d+)$");
                if (match.Success)
                {
                    baseName = match.Groups[1].Value;
                }

                // Try to map filename to cursor type
                var cursorType = CursorTypeNames.FromString(baseName);
                if (cursorType.HasValue)
                {
                    if (!cursorGroups.ContainsKey(cursorType.Value))
                        cursorGroups[cursorType.Value] = new List<string>();

                    cursorGroups[cursorType.Value].Add(file);
                }
            }

            // Create cursor definitions for each type
            foreach (var (type, files) in cursorGroups)
            {
                var definition = new CursorDefinition();

                // Sort files to ensure correct animation frame order
                var sortedFiles = files.OrderBy(f => f).ToList();

                foreach (var file in sortedFiles)
                {
                    var frame = new CursorFrame
                    {
                        ImagePath = file,
                        ImageData = File.ReadAllBytes(file)
                    };
                    definition.Frames.Add(frame);
                }

                // Set cursor definition based on type
                SetCursorDefinition(theme, type, definition);
            }
        }

        /// <summary>
        /// Load cursor files from ZIP archive
        /// </summary>
        private static void LoadCursorsFromArchive(CursorTheme theme, ZipArchive archive)
        {
            var cursorEntries = archive.Entries
                .Where(e => e.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                           e.Name.EndsWith(".cur", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Group by cursor type
            var cursorGroups = new Dictionary<CursorType, List<ZipArchiveEntry>>();

            foreach (var entry in cursorEntries)
            {
                var fileName = Path.GetFileNameWithoutExtension(entry.Name);

                // Check for animation frame suffix
                var baseName = fileName;
                var match = System.Text.RegularExpressions.Regex.Match(fileName, @"^(.+?)_(\d+)$");
                if (match.Success)
                {
                    baseName = match.Groups[1].Value;
                }

                var cursorType = CursorTypeNames.FromString(baseName);
                if (cursorType.HasValue)
                {
                    if (!cursorGroups.ContainsKey(cursorType.Value))
                        cursorGroups[cursorType.Value] = new List<ZipArchiveEntry>();

                    cursorGroups[cursorType.Value].Add(entry);
                }
            }

            // Create cursor definitions
            foreach (var (type, entries) in cursorGroups)
            {
                var definition = new CursorDefinition();
                var sortedEntries = entries.OrderBy(e => e.Name).ToList();

                foreach (var entry in sortedEntries)
                {
                    using var stream = entry.Open();
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);

                    var frame = new CursorFrame
                    {
                        ImagePath = entry.Name,
                        ImageData = ms.ToArray()
                    };
                    definition.Frames.Add(frame);
                }

                SetCursorDefinition(theme, type, definition);
            }
        }

        /// <summary>
        /// Set cursor definition on theme based on cursor type
        /// </summary>
        private static void SetCursorDefinition(CursorTheme theme, CursorType type, CursorDefinition definition)
        {
            switch (type)
            {
                case CursorType.Arrow:
                    theme.Arrow = definition;
                    break;
                case CursorType.Hand:
                    theme.Hand = definition;
                    break;
                case CursorType.IBeam:
                    theme.IBeam = definition;
                    break;
                case CursorType.Wait:
                    theme.Wait = definition;
                    break;
                case CursorType.AppStarting:
                    theme.AppStarting = definition;
                    break;
                case CursorType.Cross:
                    theme.Cross = definition;
                    break;
                case CursorType.SizeNS:
                    theme.SizeNS = definition;
                    break;
                case CursorType.SizeEW:
                    theme.SizeEW = definition;
                    break;
                case CursorType.SizeNESW:
                    theme.SizeNESW = definition;
                    break;
                case CursorType.SizeNWSE:
                    theme.SizeNWSE = definition;
                    break;
                case CursorType.SizeAll:
                    theme.SizeAll = definition;
                    break;
                case CursorType.No:
                    theme.No = definition;
                    break;
                case CursorType.Help:
                    theme.Help = definition;
                    break;
                case CursorType.UpArrow:
                    theme.UpArrow = definition;
                    break;
            }
        }

        /// <summary>
        /// Helper to parse boolean from string
        /// </summary>
        private static bool ParseBool(string value)
        {
            return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("1", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Helper to parse integer with default fallback
        /// </summary>
        private static int ParseInt(string value, int defaultValue)
        {
            return int.TryParse(value, out var result) ? result : defaultValue;
        }
    }
}
