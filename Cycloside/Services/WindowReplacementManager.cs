using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cycloside.Services
{
    /// <summary>
    /// Loads replacement window content from loose XAML files while preserving DataContext.
    /// </summary>
    public static class WindowReplacementManager
    {
        public static async Task<bool> ReplaceWindowContentAsync(Window targetWindow, string replacementFile)
        {
            try
            {
                if (!File.Exists(replacementFile))
                {
                    Logger.Log($"WindowReplacementManager: replacement file not found: {replacementFile}");
                    return false;
                }

                var dataContext = targetWindow.DataContext;
                var newContent = await LoadWindowContentAsync(replacementFile);
                if (newContent == null)
                {
                    Logger.Log($"WindowReplacementManager: failed to load content from '{replacementFile}'");
                    return false;
                }

                targetWindow.Content = newContent;
                targetWindow.DataContext = dataContext;

                Logger.Log($"WindowReplacementManager: replaced content for '{targetWindow.Title}'");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"WindowReplacementManager: replacement failed: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> ApplySkinWindowReplacementAsync(Window window, string skinName, string windowType)
        {
            try
            {
                if (!await SkinManager.SupportsWindowReplacementAsync(skinName, windowType))
                {
                    return false;
                }

                var skinDirectory = Path.Combine(AppContext.BaseDirectory, "Skins", skinName);
                var manifestPath = Path.Combine(skinDirectory, "skin.json");
                if (!File.Exists(manifestPath))
                {
                    Logger.Log($"WindowReplacementManager: missing manifest for skin '{skinName}'");
                    return false;
                }

                var manifestContent = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<SkinManifest>(manifestContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (manifest?.ReplaceWindows.TryGetValue(windowType, out var relativePath) != true ||
                    string.IsNullOrWhiteSpace(relativePath))
                {
                    return false;
                }

                var replacementPath = Path.Combine(skinDirectory, relativePath);
                return await ReplaceWindowContentAsync(window, replacementPath);
            }
            catch (Exception ex)
            {
                Logger.Log($"WindowReplacementManager: failed to apply replacement from skin '{skinName}': {ex.Message}");
                return false;
            }
        }

        public static bool ValidateWindowReplacement(string replacementFile)
        {
            try
            {
                return LoadWindowContentAsync(replacementFile).GetAwaiter().GetResult() != null;
            }
            catch (Exception ex)
            {
                Logger.Log($"WindowReplacementManager: validation failed for '{replacementFile}': {ex.Message}");
                return false;
            }
        }

        public static async Task<Dictionary<string, string>> GetAvailableWindowReplacementsAsync(string skinName)
        {
            var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var skinDirectory = Path.Combine(AppContext.BaseDirectory, "Skins", skinName);
                var manifestPath = Path.Combine(skinDirectory, "skin.json");
                if (!File.Exists(manifestPath))
                {
                    return replacements;
                }

                var manifestContent = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<SkinManifest>(manifestContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (manifest?.ReplaceWindows == null)
                {
                    return replacements;
                }

                foreach (var entry in manifest.ReplaceWindows)
                {
                    var replacementPath = Path.Combine(skinDirectory, entry.Value);
                    if (ValidateWindowReplacement(replacementPath))
                    {
                        replacements[entry.Key] = replacementPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"WindowReplacementManager: failed to inspect replacements for '{skinName}': {ex.Message}");
            }

            return replacements;
        }

        private static Task<object?> LoadWindowContentAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return Task.FromResult<object?>(null);
                }

                var xaml = File.ReadAllText(filePath);
                var parsed = AvaloniaRuntimeXamlLoader.Parse(xaml, typeof(App).Assembly);

                if (parsed is UserControl userControl)
                {
                    return Task.FromResult<object?>(userControl);
                }

                if (parsed is Window window)
                {
                    return Task.FromResult(window.Content);
                }

                if (parsed is Control control)
                {
                    return Task.FromResult<object?>(control);
                }

                if (parsed is ContentControl contentControl)
                {
                    return Task.FromResult<object?>(contentControl);
                }

                return Task.FromResult<object?>(null);
            }
            catch (Exception ex)
            {
                Logger.Log($"WindowReplacementManager: failed to parse '{filePath}': {ex.Message}");
                return Task.FromResult<object?>(null);
            }
        }

    }
}
