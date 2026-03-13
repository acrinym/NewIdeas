using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Cycloside.Services
{
    /// <summary>
    /// Caches theme assets (images, cursors, icons). Paths relative to theme directory.
    /// Rejects path traversal (..).
    /// </summary>
    public static class ThemeAssetCache
    {
        private static readonly Dictionary<string, (object Asset, DateTime Timestamp)> _cache = new();
        private static readonly object _lock = new object();

        public static Bitmap? GetImage(string themeId, string path)
        {
            var (themeDir, fullPath) = ResolvePath(themeId, path);
            if (fullPath == null || !File.Exists(fullPath)) return null;

            var key = $"{themeId}:img:{path}";
            var timestamp = File.GetLastWriteTimeUtc(fullPath);
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var entry) && timestamp <= entry.Timestamp)
                    return entry.Asset as Bitmap;

                try
                {
                    var ext = Path.GetExtension(path).ToLowerInvariant();
                    if ((ext == ".ico" || ext == ".cur") && !BinaryFormatValidator.ValidateIcoCurFile(fullPath))
                    {
                        Logger.Log($"ThemeAssetCache: invalid ICO/CUR format: {path}");
                        return null;
                    }
                    using var stream = File.OpenRead(fullPath);
                    var bmp = new Bitmap(stream);
                    _cache[key] = (bmp, timestamp);
                    return bmp;
                }
                catch (Exception ex)
                {
                    Logger.Log($"ThemeAssetCache: failed to load image {path}: {ex.Message}");
                    return null;
                }
            }
        }

        public static string? GetAssetPath(string themeId, string path)
        {
            var (_, fullPath) = ResolvePath(themeId, path);
            return fullPath != null && File.Exists(fullPath) ? fullPath : null;
        }

        private static (string themeDir, string? fullPath) ResolvePath(string themeId, string path)
        {
            if (string.IsNullOrWhiteSpace(themeId) || string.IsNullOrWhiteSpace(path))
                return ("", null);

            if (BinaryFormatValidator.IsDataUri(path))
            {
                Logger.Log("ThemeAssetCache: data URI rejected (CYC-2026-023)");
                return ("", null);
            }

            if (path.Contains("..", StringComparison.Ordinal))
            {
                Logger.Log($"ThemeAssetCache: path traversal rejected: {path}");
                return ("", null);
            }

            var themeDir = Path.Combine(AppContext.BaseDirectory, "Themes", themeId);
            if (!Directory.Exists(themeDir))
                return (themeDir, null);

            var fullPath = Path.GetFullPath(Path.Combine(themeDir, path));
            var themeDirFull = Path.GetFullPath(themeDir);
            if (!fullPath.StartsWith(themeDirFull, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"ThemeAssetCache: path outside theme dir: {path}");
                return (themeDir, null);
            }

            return (themeDir, fullPath);
        }

        public static void Clear()
        {
            lock (_lock)
            {
                foreach (var kv in _cache)
                {
                    if (kv.Value.Asset is IDisposable d)
                        d.Dispose();
                }
                _cache.Clear();
            }
        }
    }
}
