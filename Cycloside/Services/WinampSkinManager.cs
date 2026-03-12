using Avalonia.Threading;
using Cycloside.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cycloside.Services;

/// <summary>
/// Manages Winamp skins (WSZ files) for the MP3 Player
/// </summary>
public class WinampSkinManager
{
    private static WinampSkinManager? _instance;
    public static WinampSkinManager Instance => _instance ??= new WinampSkinManager();

    private WinampSkin? _currentSkin;
    private readonly List<string> _availableSkins = new();
    private readonly string _skinsDirectory;

    /// <summary>
    /// Event raised when a new skin is applied
    /// </summary>
    public event Action<WinampSkin>? SkinChanged;

    /// <summary>
    /// Currently active skin
    /// </summary>
    public WinampSkin? CurrentSkin => _currentSkin;

    /// <summary>
    /// List of available skin file paths
    /// </summary>
    public IReadOnlyList<string> AvailableSkins => _availableSkins.AsReadOnly();

    private WinampSkinManager()
    {
        // Set up skins directory in user's Documents or AppData
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _skinsDirectory = Path.Combine(appDataPath, "Cycloside", "Skins", "Winamp");

        // Also check local directory
        var localSkinsPath = Path.Combine(AppContext.BaseDirectory, "Skins", "Winamp");

        // Create directories if they don't exist
        try
        {
            Directory.CreateDirectory(_skinsDirectory);
            Directory.CreateDirectory(localSkinsPath);
            Logger.Log($"📁 Winamp skins directory: {_skinsDirectory}");
            Logger.Log($"📁 Local skins directory: {localSkinsPath}");
        }
        catch (Exception ex)
        {
            Logger.Log($"⚠️ Failed to create skins directories: {ex.Message}");
        }

        // Scan for available skins
        RefreshAvailableSkins();
    }

    /// <summary>
    /// Scan directories for available WSZ skin files
    /// </summary>
    public void RefreshAvailableSkins()
    {
        _availableSkins.Clear();

        var dirsToScan = new[]
        {
            _skinsDirectory,
            Path.Combine(AppContext.BaseDirectory, "Skins", "Winamp")
        };

        foreach (var dir in dirsToScan)
        {
            if (!Directory.Exists(dir))
                continue;

            try
            {
                // Scan for WSZ files
                var wszFiles = Directory.GetFiles(dir, "*.wsz", SearchOption.AllDirectories);
                _availableSkins.AddRange(wszFiles);

                // Scan for extracted skin directories (contain main.bmp)
                var skinDirs = Directory.GetDirectories(dir, "*", SearchOption.AllDirectories)
                    .Where(d => File.Exists(Path.Combine(d, "main.bmp")));
                _availableSkins.AddRange(skinDirs);
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Error scanning {dir} for skins: {ex.Message}");
            }
        }

        Logger.Log($"🎨 Found {_availableSkins.Count} Winamp skins");
    }

    /// <summary>
    /// Load and apply a skin from file path or directory
    /// </summary>
    public bool LoadSkin(string path)
    {
        try
        {
            WinampSkin? skin = null;

            // Check if it's a WSZ file or directory
            if (File.Exists(path) && path.EndsWith(".wsz", StringComparison.OrdinalIgnoreCase))
            {
                skin = WinampSkinParser.LoadFromWszFile(path);
            }
            else if (Directory.Exists(path))
            {
                skin = WinampSkinParser.LoadFromDirectory(path);
            }
            else
            {
                Logger.Log($"❌ Skin path not found: {path}");
                return false;
            }

            if (skin == null)
            {
                Logger.Log($"❌ Failed to parse skin: {path}");
                return false;
            }

            // Validate skin has at least main.bmp
            if (skin.MainBitmap == null)
            {
                Logger.Log($"❌ Skin is missing main.bmp: {path}");
                return false;
            }

            _currentSkin = skin;

            // Notify subscribers on UI thread
            Dispatcher.UIThread.Post(() => SkinChanged?.Invoke(skin));

            Logger.Log($"✅ Applied Winamp skin: {skin.Name}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Error loading skin from {path}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Load skin by index from available skins list
    /// </summary>
    public bool LoadSkinByIndex(int index)
    {
        if (index < 0 || index >= _availableSkins.Count)
        {
            Logger.Log($"❌ Invalid skin index: {index}");
            return false;
        }

        return LoadSkin(_availableSkins[index]);
    }

    /// <summary>
    /// Get display name for a skin path
    /// </summary>
    public string GetSkinDisplayName(string path)
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
    /// Load the default/built-in skin
    /// </summary>
    public void LoadDefaultSkin()
    {
        // For now, create a simple default skin
        // In the future, could include a built-in classic Winamp skin
        _currentSkin = null;
        Dispatcher.UIThread.Post(() => SkinChanged?.Invoke(null!));
        Logger.Log("🎨 Using default MP3 Player skin");
    }

    /// <summary>
    /// Import a WSZ file from anywhere on the system
    /// </summary>
    public bool ImportSkin(string sourcePath)
    {
        try
        {
            if (!File.Exists(sourcePath))
            {
                Logger.Log($"❌ Source file not found: {sourcePath}");
                return false;
            }

            var fileName = Path.GetFileName(sourcePath);
            var destPath = Path.Combine(_skinsDirectory, fileName);

            // Copy to skins directory
            File.Copy(sourcePath, destPath, overwrite: true);

            Logger.Log($"✅ Imported skin: {fileName}");

            // Refresh and load the new skin
            RefreshAvailableSkins();
            return LoadSkin(destPath);
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Failed to import skin: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get the skins directory path for user to add skins
    /// </summary>
    public string GetSkinsDirectory() => _skinsDirectory;
}
