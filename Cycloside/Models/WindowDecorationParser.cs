using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Cycloside.Models;

/// <summary>
/// Parser for window decoration theme files
/// Supports both directory-based and archive-based themes
/// </summary>
public class WindowDecorationParser
{
    private static readonly string[] KnownBitmapFiles = new[]
    {
        // Title bar
        "titlebar_active_left.png",
        "titlebar_active_center.png",
        "titlebar_active_right.png",
        "titlebar_inactive_left.png",
        "titlebar_inactive_center.png",
        "titlebar_inactive_right.png",

        // Borders
        "border_left.png",
        "border_right.png",
        "border_top.png",
        "border_bottom.png",

        // Corners
        "corner_topleft.png",
        "corner_topright.png",
        "corner_bottomleft.png",
        "corner_bottomright.png",

        // Close button
        "button_close_normal.png",
        "button_close_hover.png",
        "button_close_pressed.png",

        // Maximize button
        "button_maximize_normal.png",
        "button_maximize_hover.png",
        "button_maximize_pressed.png",

        // Minimize button
        "button_minimize_normal.png",
        "button_minimize_hover.png",
        "button_minimize_pressed.png",

        // Restore button
        "button_restore_normal.png",
        "button_restore_hover.png",
        "button_restore_pressed.png"
    };

    /// <summary>
    /// Load a window decoration theme from a directory
    /// </summary>
    public static WindowDecoration? LoadFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Logger.Log($"Theme directory not found: {directoryPath}");
            return null;
        }

        try
        {
            var theme = new WindowDecoration
            {
                Name = new DirectoryInfo(directoryPath).Name
            };

            // Load theme.ini configuration file
            var configFile = Path.Combine(directoryPath, "theme.ini");
            if (File.Exists(configFile))
            {
                LoadConfiguration(theme, configFile);
            }

            // Load all PNG bitmap files
            foreach (var file in Directory.GetFiles(directoryPath, "*.png"))
            {
                var fileName = Path.GetFileName(file).ToLowerInvariant();
                var bitmap = new Bitmap(file);

                MapBitmapToProperty(theme, fileName, bitmap);
            }

            Logger.Log($"✅ Loaded window decoration theme: {theme.Name}");
            return theme;
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Failed to load theme from directory {directoryPath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load a window decoration theme from a ZIP archive
    /// </summary>
    public static WindowDecoration? LoadFromArchive(string archivePath)
    {
        if (!File.Exists(archivePath))
        {
            Logger.Log($"Theme archive not found: {archivePath}");
            return null;
        }

        try
        {
            var theme = new WindowDecoration
            {
                Name = Path.GetFileNameWithoutExtension(archivePath)
            };

            using var archive = ZipFile.OpenRead(archivePath);

            // Load theme.ini first
            var configEntry = archive.Entries.FirstOrDefault(e =>
                e.Name.Equals("theme.ini", StringComparison.OrdinalIgnoreCase));

            if (configEntry != null)
            {
                using var stream = configEntry.Open();
                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();

                // Write to temp file and load
                var tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, content);
                LoadConfiguration(theme, tempFile);
                File.Delete(tempFile);
            }

            // Load all PNG bitmaps
            foreach (var entry in archive.Entries)
            {
                var fileName = entry.Name.ToLowerInvariant();

                if (fileName.EndsWith(".png"))
                {
                    using var stream = entry.Open();
                    using var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    memoryStream.Position = 0;

                    var bitmap = new Bitmap(memoryStream);
                    MapBitmapToProperty(theme, fileName, bitmap);
                }
            }

            Logger.Log($"✅ Loaded window decoration theme from archive: {theme.Name}");
            return theme;
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Failed to load theme from archive {archivePath}: {ex.Message}");
            return null;
        }
    }

    private static void MapBitmapToProperty(WindowDecoration theme, string fileName, Bitmap bitmap)
    {
        switch (fileName)
        {
            // Title bar
            case "titlebar_active_left.png":
                theme.TitleBarActiveLeft = bitmap;
                break;
            case "titlebar_active_center.png":
                theme.TitleBarActiveCenter = bitmap;
                break;
            case "titlebar_active_right.png":
                theme.TitleBarActiveRight = bitmap;
                break;
            case "titlebar_inactive_left.png":
                theme.TitleBarInactiveLeft = bitmap;
                break;
            case "titlebar_inactive_center.png":
                theme.TitleBarInactiveCenter = bitmap;
                break;
            case "titlebar_inactive_right.png":
                theme.TitleBarInactiveRight = bitmap;
                break;

            // Borders
            case "border_left.png":
                theme.BorderLeft = bitmap;
                break;
            case "border_right.png":
                theme.BorderRight = bitmap;
                break;
            case "border_top.png":
                theme.BorderTop = bitmap;
                break;
            case "border_bottom.png":
                theme.BorderBottom = bitmap;
                break;

            // Corners
            case "corner_topleft.png":
                theme.CornerTopLeft = bitmap;
                break;
            case "corner_topright.png":
                theme.CornerTopRight = bitmap;
                break;
            case "corner_bottomleft.png":
                theme.CornerBottomLeft = bitmap;
                break;
            case "corner_bottomright.png":
                theme.CornerBottomRight = bitmap;
                break;

            // Close button
            case "button_close_normal.png":
                theme.CloseButtonNormal = bitmap;
                break;
            case "button_close_hover.png":
                theme.CloseButtonHover = bitmap;
                break;
            case "button_close_pressed.png":
                theme.CloseButtonPressed = bitmap;
                break;

            // Maximize button
            case "button_maximize_normal.png":
                theme.MaximizeButtonNormal = bitmap;
                break;
            case "button_maximize_hover.png":
                theme.MaximizeButtonHover = bitmap;
                break;
            case "button_maximize_pressed.png":
                theme.MaximizeButtonPressed = bitmap;
                break;

            // Minimize button
            case "button_minimize_normal.png":
                theme.MinimizeButtonNormal = bitmap;
                break;
            case "button_minimize_hover.png":
                theme.MinimizeButtonHover = bitmap;
                break;
            case "button_minimize_pressed.png":
                theme.MinimizeButtonPressed = bitmap;
                break;

            // Restore button
            case "button_restore_normal.png":
                theme.RestoreButtonNormal = bitmap;
                break;
            case "button_restore_hover.png":
                theme.RestoreButtonHover = bitmap;
                break;
            case "button_restore_pressed.png":
                theme.RestoreButtonPressed = bitmap;
                break;

            default:
                // Store unknown bitmaps
                theme.CustomBitmaps[fileName] = bitmap;
                break;
        }
    }

    private static void LoadConfiguration(WindowDecoration theme, string configFile)
    {
        try
        {
            var lines = File.ReadAllLines(configFile);
            string? currentSection = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;

                // Section headers
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                    continue;
                }

                // Key=Value pairs
                var parts = trimmed.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                    continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                ApplyConfiguration(theme, key, value);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"⚠️ Error loading theme configuration: {ex.Message}");
        }
    }

    private static void ApplyConfiguration(WindowDecoration theme, string key, string value)
    {
        switch (key.ToLowerInvariant())
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
            case "titlebarheight":
                if (int.TryParse(value, out var tbHeight))
                    theme.TitleBarHeight = tbHeight;
                break;
            case "borderwidth":
                if (int.TryParse(value, out var bWidth))
                    theme.BorderWidth = bWidth;
                break;
            case "buttonwidth":
                if (int.TryParse(value, out var btnWidth))
                    theme.ButtonWidth = btnWidth;
                break;
            case "buttonheight":
                if (int.TryParse(value, out var btnHeight))
                    theme.ButtonHeight = btnHeight;
                break;
            case "buttonspacing":
                if (int.TryParse(value, out var spacing))
                    theme.ButtonSpacing = spacing;
                break;
            case "titlecoloractive":
                theme.TitleColorActive = ParseColor(value);
                break;
            case "titlecolorinactive":
                theme.TitleColorInactive = ParseColor(value);
                break;
            case "titlefontfamily":
                theme.TitleFontFamily = value;
                break;
            case "titlefontsize":
                if (double.TryParse(value, out var fontSize))
                    theme.TitleFontSize = fontSize;
                break;
            case "titlefontweight":
                theme.TitleFontWeight = value;
                break;
            case "titlemarginleft":
                if (int.TryParse(value, out var marginLeft))
                    theme.TitleMarginLeft = marginLeft;
                break;
            case "titlemarginright":
                if (int.TryParse(value, out var marginRight))
                    theme.TitleMarginRight = marginRight;
                break;
            case "titletextshadow":
                if (bool.TryParse(value, out var shadow))
                    theme.TitleTextShadow = shadow;
                break;
            case "enableglow":
                if (bool.TryParse(value, out var glow))
                    theme.EnableGlow = glow;
                break;
            case "glowcolor":
                theme.GlowColor = ParseColor(value);
                break;
            case "roundedcorners":
                if (bool.TryParse(value, out var rounded))
                    theme.RoundedCorners = rounded;
                break;
            case "cornerradius":
                if (int.TryParse(value, out var radius))
                    theme.CornerRadius = radius;
                break;
            default:
                // Store unknown properties
                theme.Properties[key] = value;
                break;
        }
    }

    private static Color ParseColor(string colorString)
    {
        // Support formats: #RRGGBB, #AARRGGBB, R,G,B, A,R,G,B
        try
        {
            if (colorString.StartsWith("#"))
            {
                return Color.Parse(colorString);
            }
            else
            {
                var parts = colorString.Split(',');
                if (parts.Length == 3)
                {
                    return Color.FromRgb(
                        byte.Parse(parts[0].Trim()),
                        byte.Parse(parts[1].Trim()),
                        byte.Parse(parts[2].Trim())
                    );
                }
                else if (parts.Length == 4)
                {
                    return Color.FromArgb(
                        byte.Parse(parts[0].Trim()),
                        byte.Parse(parts[1].Trim()),
                        byte.Parse(parts[2].Trim()),
                        byte.Parse(parts[3].Trim())
                    );
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"⚠️ Failed to parse color '{colorString}': {ex.Message}");
        }

        return Colors.White;
    }
}
