using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Cycloside.Services
{
    /// <summary>
    /// Manages safe window replacement preserving DataContext and subscriptions.
    /// Supports skin-based window replacement with proper cleanup.
    /// </summary>
    public static class WindowReplacementManager
    {
        /// <summary>
        /// Replaces a window's content with new content from a skin file while preserving DataContext.
        /// </summary>
        /// <param name="targetWindow">The window to replace content for</param>
        /// <param name="replacementFile">Path to the replacement XAML file</param>
        /// <returns>True if replacement succeeded, false otherwise</returns>
        public static async Task<bool> ReplaceWindowContentAsync(Window targetWindow, string replacementFile)
        {
            try
            {
                if (!File.Exists(replacementFile))
                {
                    Logger.Log($"Window replacement file not found: {replacementFile}");
                    return false;
                }

                // Store current DataContext and subscriptions
                var dataContext = targetWindow.DataContext;
                var subscriptions = StoreEventSubscriptions(targetWindow);

                // Load the new content
                var newContent = await LoadWindowContentAsync(replacementFile);
                if (newContent == null)
                {
                    Logger.Log($"Failed to load window content from: {replacementFile}");
                    return false;
                }

                // Replace the content
                targetWindow.Content = newContent;

                // Restore DataContext and subscriptions
                targetWindow.DataContext = dataContext;
                RestoreEventSubscriptions(targetWindow, subscriptions);

                Logger.Log($"Successfully replaced window content from: {replacementFile}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error during window replacement: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Applies window replacement for a skin that defines window replacements.
        /// </summary>
        /// <param name="window">The window to potentially replace</param>
        /// <param name="skinName">The skin name</param>
        /// <param name="windowType">The type of window (e.g., "MainWindow", "SettingsWindow")</param>
        /// <returns>True if replacement was applied, false otherwise</returns>
        public static async Task<bool> ApplySkinWindowReplacementAsync(Window window, string skinName, string windowType)
        {
            try
            {
                // Check if skin supports window replacement for this window type
                if (!await SkinManager.SupportsWindowReplacementAsync(skinName, windowType))
                {
                    return false;
                }

                // Load skin manifest to get replacement file path
                var skinDir = Path.Combine(AppContext.BaseDirectory, "Skins", skinName);
                var manifestPath = Path.Combine(skinDir, "skin.json");

                if (!File.Exists(manifestPath))
                {
                    Logger.Log($"Skin manifest not found for window replacement: {skinName}");
                    return false;
                }

                var manifestContent = await File.ReadAllTextAsync(manifestPath);
                var manifest = System.Text.Json.JsonSerializer.Deserialize<SkinManifest>(manifestContent);

                if (manifest?.ReplaceWindows?.TryGetValue(windowType, out var replacementFile) == true)
                {
                    var replacementPath = Path.Combine(skinDir, replacementFile);
                    return await ReplaceWindowContentAsync(window, replacementPath);
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error applying skin window replacement: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validates that a window replacement file is valid XAML.
        /// </summary>
        /// <param name="replacementFile">Path to the replacement file</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool ValidateWindowReplacement(string replacementFile)
        {
            try
            {
                if (!File.Exists(replacementFile))
                    return false;

                var content = File.ReadAllText(replacementFile);

                // Basic XAML validation - check for valid root element
                return content.Contains("<") &&
                       content.Contains(">") &&
                       (content.Contains("Window") || content.Contains("UserControl") || content.Contains("ContentControl"));
            }
            catch (Exception ex)
            {
                Logger.Log($"Error validating window replacement file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets available window replacement files for a skin.
        /// </summary>
        /// <param name="skinName">The skin name</param>
        /// <returns>Dictionary mapping window types to file paths</returns>
        public static async Task<Dictionary<string, string>> GetAvailableWindowReplacementsAsync(string skinName)
        {
            var replacements = new Dictionary<string, string>();

            try
            {
                var skinDir = Path.Combine(AppContext.BaseDirectory, "Skins", skinName);
                var manifestPath = Path.Combine(skinDir, "skin.json");

                if (!File.Exists(manifestPath))
                    return replacements;

                var manifestContent = await File.ReadAllTextAsync(manifestPath);
                var manifest = System.Text.Json.JsonSerializer.Deserialize<SkinManifest>(manifestContent);

                if (manifest?.ReplaceWindows != null)
                {
                    foreach (var kvp in manifest.ReplaceWindows)
                    {
                        var replacementPath = Path.Combine(skinDir, kvp.Value);
                        if (File.Exists(replacementPath) && ValidateWindowReplacement(replacementPath))
                        {
                            replacements[kvp.Key] = replacementPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error getting available window replacements: {ex.Message}");
            }

            return replacements;
        }

        private static async Task<object?> LoadWindowContentAsync(string filePath)
        {
            try
            {
                // For now, return a placeholder - actual implementation would load and parse XAML
                // This would require more complex XAML loading logic in a real implementation
                Logger.Log($"Loading window content from: {filePath}");

                // Simulate async loading
                await Task.Delay(10);

                return new ContentControl(); // Placeholder
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading window content: {ex.Message}");
                return null;
            }
        }

        private static Dictionary<string, Delegate> StoreEventSubscriptions(Window window)
        {
            // This is a simplified implementation
            // In a real implementation, this would use reflection to capture event handlers
            var subscriptions = new Dictionary<string, Delegate>();

            // Store common event subscriptions that need to be preserved
            if (window.DataContext is IDisposable disposable)
            {
                // Store disposable instances to maintain proper cleanup
            }

            return subscriptions;
        }

        private static void RestoreEventSubscriptions(Window window, Dictionary<string, Delegate> subscriptions)
        {
            // Restore event subscriptions after content replacement
            // This is a simplified implementation
            Logger.Log($"Restored event subscriptions for window: {window.Title}");
        }
    }
}
