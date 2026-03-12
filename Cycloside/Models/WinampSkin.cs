using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Cycloside.Models;

/// <summary>
/// Represents a Winamp Classic skin (WSZ format)
/// WSZ files are ZIP archives containing bitmaps and configuration files
/// </summary>
public class WinampSkin
{
    /// <summary>Skin name (from folder or metadata)</summary>
    public string Name { get; set; } = "Default";

    /// <summary>Skin author</summary>
    public string? Author { get; set; }

    /// <summary>Skin version</summary>
    public string? Version { get; set; }

    /// <summary>Main window bitmap containing all UI elements</summary>
    public Bitmap? MainBitmap { get; set; }

    /// <summary>Playlist editor bitmap</summary>
    public Bitmap? PlaylistBitmap { get; set; }

    /// <summary>Equalizer bitmap</summary>
    public Bitmap? EqualizerBitmap { get; set; }

    /// <summary>Position bar bitmap (seek bar)</summary>
    public Bitmap? PositionBarBitmap { get; set; }

    /// <summary>Volume/Balance slider bitmaps</summary>
    public Bitmap? VolumeBitmap { get; set; }

    /// <summary>Numbers bitmap (time display)</summary>
    public Bitmap? NumbersBitmap { get; set; }

    /// <summary>PlayPause indicator bitmap</summary>
    public Bitmap? PlayPauseBitmap { get; set; }

    /// <summary>Mono/Stereo indicator bitmap</summary>
    public Bitmap? MonoStereoBitmap { get; set; }

    /// <summary>Titlebar bitmap</summary>
    public Bitmap? TitlebarBitmap { get; set; }

    /// <summary>Visualization colors (from VISCOLOR.TXT)</summary>
    public List<Color> VisualizationColors { get; set; } = new();

    /// <summary>Region data for window shape (from region.txt)</summary>
    public Dictionary<string, string> RegionData { get; set; } = new();

    /// <summary>Playlist editor configuration (from pledit.txt)</summary>
    public Dictionary<string, string> PlaylistConfig { get; set; } = new();

    /// <summary>All loaded bitmaps by filename (for custom elements)</summary>
    public Dictionary<string, Bitmap> CustomBitmaps { get; set; } = new();
}

/// <summary>
/// Parser for Winamp Classic skin files (WSZ/ZIP format)
/// </summary>
public class WinampSkinParser
{
    private static readonly string[] KnownBitmapFiles = new[]
    {
        "main.bmp",      // Main window
        "pledit.bmp",    // Playlist editor
        "eqmain.bmp",    // Equalizer
        "posbar.bmp",    // Position bar
        "volume.bmp",    // Volume slider
        "numbers.bmp",   // Time display numbers
        "playpaus.bmp",  // Play/Pause indicator
        "monoster.bmp",  // Mono/Stereo indicator
        "titlebar.bmp",  // Window titlebar
        "shufrep.bmp",   // Shuffle/Repeat buttons
        "text.bmp",      // Text font
        "cbuttons.bmp",  // Control buttons
        "balance.bmp",   // Balance slider
        "avs.bmp",       // AVS preset button
        "mb.bmp"         // Minibrowser
    };

    /// <summary>
    /// Load a Winamp skin from a WSZ file (ZIP archive)
    /// </summary>
    public static WinampSkin? LoadFromWszFile(string wszPath)
    {
        if (!File.Exists(wszPath))
        {
            Logger.Log($"WSZ file not found: {wszPath}");
            return null;
        }

        try
        {
            var skin = new WinampSkin
            {
                Name = Path.GetFileNameWithoutExtension(wszPath)
            };

            using var archive = ZipFile.OpenRead(wszPath);

            // Load all bitmap files
            foreach (var entry in archive.Entries)
            {
                var fileName = entry.Name.ToLowerInvariant();

                if (fileName.EndsWith(".bmp"))
                {
                    using var stream = entry.Open();
                    using var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    memoryStream.Position = 0;

                    var bitmap = new Bitmap(memoryStream);

                    // Map known bitmap files to properties
                    switch (fileName)
                    {
                        case "main.bmp":
                            skin.MainBitmap = bitmap;
                            break;
                        case "pledit.bmp":
                            skin.PlaylistBitmap = bitmap;
                            break;
                        case "eqmain.bmp":
                            skin.EqualizerBitmap = bitmap;
                            break;
                        case "posbar.bmp":
                            skin.PositionBarBitmap = bitmap;
                            break;
                        case "volume.bmp":
                            skin.VolumeBitmap = bitmap;
                            break;
                        case "numbers.bmp":
                            skin.NumbersBitmap = bitmap;
                            break;
                        case "playpaus.bmp":
                            skin.PlayPauseBitmap = bitmap;
                            break;
                        case "monoster.bmp":
                            skin.MonoStereoBitmap = bitmap;
                            break;
                        case "titlebar.bmp":
                            skin.TitlebarBitmap = bitmap;
                            break;
                        default:
                            // Store custom/additional bitmaps
                            skin.CustomBitmaps[fileName] = bitmap;
                            break;
                    }
                }
                else if (fileName == "region.txt")
                {
                    skin.RegionData = LoadTextConfig(entry);
                }
                else if (fileName == "pledit.txt")
                {
                    skin.PlaylistConfig = LoadTextConfig(entry);
                }
                else if (fileName == "viscolor.txt")
                {
                    skin.VisualizationColors = LoadVisualizationColors(entry);
                }
                else if (fileName == "readme.txt" || fileName == "read_me.txt")
                {
                    // Could parse skin metadata from readme
                    var readme = LoadTextFile(entry);
                    ParseSkinMetadata(skin, readme);
                }
            }

            Logger.Log($"✅ Loaded Winamp skin: {skin.Name}");
            return skin;
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Failed to load WSZ skin from {wszPath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load a skin from an extracted directory
    /// </summary>
    public static WinampSkin? LoadFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Logger.Log($"Skin directory not found: {directoryPath}");
            return null;
        }

        try
        {
            var skin = new WinampSkin
            {
                Name = new DirectoryInfo(directoryPath).Name
            };

            // Load all bitmap files
            foreach (var file in Directory.GetFiles(directoryPath, "*.bmp"))
            {
                var fileName = Path.GetFileName(file).ToLowerInvariant();
                var bitmap = new Bitmap(file);

                switch (fileName)
                {
                    case "main.bmp":
                        skin.MainBitmap = bitmap;
                        break;
                    case "pledit.bmp":
                        skin.PlaylistBitmap = bitmap;
                        break;
                    case "eqmain.bmp":
                        skin.EqualizerBitmap = bitmap;
                        break;
                    case "posbar.bmp":
                        skin.PositionBarBitmap = bitmap;
                        break;
                    case "volume.bmp":
                        skin.VolumeBitmap = bitmap;
                        break;
                    case "numbers.bmp":
                        skin.NumbersBitmap = bitmap;
                        break;
                    case "playpaus.bmp":
                        skin.PlayPauseBitmap = bitmap;
                        break;
                    case "monoster.bmp":
                        skin.MonoStereoBitmap = bitmap;
                        break;
                    case "titlebar.bmp":
                        skin.TitlebarBitmap = bitmap;
                        break;
                    default:
                        skin.CustomBitmaps[fileName] = bitmap;
                        break;
                }
            }

            // Load config files
            var regionFile = Path.Combine(directoryPath, "region.txt");
            if (File.Exists(regionFile))
            {
                skin.RegionData = LoadTextConfigFromFile(regionFile);
            }

            var pleditFile = Path.Combine(directoryPath, "pledit.txt");
            if (File.Exists(pleditFile))
            {
                skin.PlaylistConfig = LoadTextConfigFromFile(pleditFile);
            }

            var viscolorFile = Path.Combine(directoryPath, "viscolor.txt");
            if (File.Exists(viscolorFile))
            {
                skin.VisualizationColors = LoadVisualizationColorsFromFile(viscolorFile);
            }

            Logger.Log($"✅ Loaded Winamp skin from directory: {skin.Name}");
            return skin;
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Failed to load skin from directory {directoryPath}: {ex.Message}");
            return null;
        }
    }

    private static Dictionary<string, string> LoadTextConfig(ZipArchiveEntry entry)
    {
        var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StreamReader(entry.Open());

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                continue;

            var parts = line.Split(new[] { '=' }, 2);
            if (parts.Length == 2)
            {
                config[parts[0].Trim()] = parts[1].Trim();
            }
        }

        return config;
    }

    private static Dictionary<string, string> LoadTextConfigFromFile(string filePath)
    {
        var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                continue;

            var parts = trimmed.Split(new[] { '=' }, 2);
            if (parts.Length == 2)
            {
                config[parts[0].Trim()] = parts[1].Trim();
            }
        }

        return config;
    }

    private static string LoadTextFile(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }

    private static List<Color> LoadVisualizationColors(ZipArchiveEntry entry)
    {
        var colors = new List<Color>();
        using var reader = new StreamReader(entry.Open());

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith(";"))
                continue;

            // Parse RGB format: "R,G,B"
            var parts = line.Split(',');
            if (parts.Length >= 3 &&
                byte.TryParse(parts[0], out var r) &&
                byte.TryParse(parts[1], out var g) &&
                byte.TryParse(parts[2], out var b))
            {
                colors.Add(Color.FromRgb(r, g, b));
            }
        }

        return colors;
    }

    private static List<Color> LoadVisualizationColorsFromFile(string filePath)
    {
        var colors = new List<Color>();

        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";"))
                continue;

            var parts = trimmed.Split(',');
            if (parts.Length >= 3 &&
                byte.TryParse(parts[0], out var r) &&
                byte.TryParse(parts[1], out var g) &&
                byte.TryParse(parts[2], out var b))
            {
                colors.Add(Color.FromRgb(r, g, b));
            }
        }

        return colors;
    }

    private static void ParseSkinMetadata(WinampSkin skin, string readme)
    {
        // Simple metadata extraction from readme
        var lines = readme.Split('\n');
        foreach (var line in lines)
        {
            var lower = line.ToLowerInvariant();
            if (lower.Contains("author:") || lower.Contains("by "))
            {
                skin.Author = line.Split(':', 2).LastOrDefault()?.Trim();
            }
            else if (lower.Contains("version:"))
            {
                skin.Version = line.Split(':', 2).LastOrDefault()?.Trim();
            }
        }
    }
}
