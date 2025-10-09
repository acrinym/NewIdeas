using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cycloside.Services
{
    /// <summary>
    /// PLUGIN REPOSITORY - Marketplace for discovering and managing plugins
    /// Provides API for browsing, downloading, and installing community plugins
    /// </summary>
    public static class PluginRepository
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string _repositoryUrl = "https://api.github.com/repos/cycloside/plugins/contents/plugins";
        private static readonly string _localPluginsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cycloside", "Plugins", "Community");

        public static event EventHandler<PluginDiscoveryEventArgs>? PluginDiscovered;
        public static event EventHandler<PluginInstallEventArgs>? PluginInstalled;
        public static event EventHandler<PluginInstallEventArgs>? PluginInstallFailed;

        public static bool IsRepositoryAvailable => !string.IsNullOrEmpty(_repositoryUrl);

        /// <summary>
        /// Initialize the plugin repository system
        /// </summary>
        public static async Task InitializeAsync()
        {
            Logger.Log("🔌 Initializing Plugin Repository...");

            try
            {
                // Ensure local plugins directory exists
                if (!Directory.Exists(_localPluginsPath))
                {
                    Directory.CreateDirectory(_localPluginsPath);
                }

                // Test repository connectivity
                await Task.Run(async () =>
                {
                    var isOnline = await TestRepositoryConnectivityAsync();

                    if (isOnline)
                    {
                        Logger.Log("✅ Plugin Repository initialized successfully");
                    }
                    else
                    {
                        Logger.Log("⚠️ Plugin Repository: Offline mode - Local plugins only");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Plugin Repository initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Test connectivity to the plugin repository
        /// </summary>
        private static async Task<bool> TestRepositoryConnectivityAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_repositoryUrl);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Discover available plugins from the repository
        /// </summary>
        public static async Task<List<PluginManifest>> DiscoverPluginsAsync()
        {
            var plugins = new List<PluginManifest>();

            try
            {
                Logger.Log("🔍 Discovering plugins from repository...");

                var response = await _httpClient.GetAsync(_repositoryUrl);

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Log($"⚠️ Repository discovery failed: {response.StatusCode}");
                    return plugins;
                }

                var json = await response.Content.ReadAsStringAsync();
                var repositoryContents = JsonSerializer.Deserialize<List<RepositoryItem>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (repositoryContents != null)
                {
                    foreach (var item in repositoryContents.Where(item => item.Type == "dir"))
                    {
                        var manifest = await GetPluginManifestAsync(item.Name);
                        if (manifest != null)
                        {
                            plugins.Add(manifest);
                            OnPluginDiscovered(manifest);
                        }
                    }
                }

                Logger.Log($"✅ Discovered {plugins.Count} plugins from repository");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Plugin discovery failed: {ex.Message}");
            }

            return plugins;
        }

        /// <summary>
        /// Get manifest for a specific plugin
        /// </summary>
        private static async Task<PluginManifest?> GetPluginManifestAsync(string pluginName)
        {
            try
            {
                var manifestUrl = $"{_repositoryUrl}/{pluginName}/manifest.json";
                var response = await _httpClient.GetAsync(manifestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var manifest = JsonSerializer.Deserialize<PluginManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return manifest;
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Failed to get manifest for {pluginName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Install a plugin from the repository
        /// </summary>
        public static async Task<bool> InstallPluginAsync(PluginManifest plugin)
        {
            try
            {
                Logger.Log($"📥 Installing plugin: {plugin.Name} v{plugin.Version}");

                // Create plugin directory
                var pluginPath = Path.Combine(_localPluginsPath, plugin.Name);
                if (!Directory.Exists(pluginPath))
                {
                    Directory.CreateDirectory(pluginPath);
                }

                // Download plugin files
                var success = await DownloadPluginFilesAsync(plugin, pluginPath);

                if (success)
                {
                    // Save manifest locally
                    var manifestPath = Path.Combine(pluginPath, "manifest.json");
                    var manifestJson = JsonSerializer.Serialize(plugin, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    await File.WriteAllTextAsync(manifestPath, manifestJson);

                    Logger.Log($"✅ Plugin installed successfully: {plugin.Name}");
                    OnPluginInstalled(plugin, true);
                    return true;
                }
                else
                {
                    Logger.Log($"❌ Plugin installation failed: {plugin.Name}");
                    OnPluginInstallFailed(plugin, "Download failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Plugin installation error: {ex.Message}");
                OnPluginInstallFailed(plugin, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Download all files for a plugin
        /// </summary>
        private static async Task<bool> DownloadPluginFilesAsync(PluginManifest plugin, string pluginPath)
        {
            try
            {
                foreach (var file in plugin.Files)
                {
                    var fileUrl = $"{_repositoryUrl}/{plugin.Name}/{file.Path}";
                    var response = await _httpClient.GetAsync(fileUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Log($"⚠️ Failed to download {file.Path}");
                        return false;
                    }

                    var content = await response.Content.ReadAsByteArrayAsync();
                    var filePath = Path.Combine(pluginPath, file.Path);

                    // Ensure directory exists
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    await File.WriteAllBytesAsync(filePath, content);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ File download error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get locally installed plugins
        /// </summary>
        public static List<PluginManifest> GetInstalledPlugins()
        {
            var plugins = new List<PluginManifest>();

            try
            {
                if (!Directory.Exists(_localPluginsPath))
                {
                    return plugins;
                }

                var pluginDirectories = Directory.GetDirectories(_localPluginsPath);

                foreach (var dir in pluginDirectories)
                {
                    var manifestPath = Path.Combine(dir, "manifest.json");

                    if (File.Exists(manifestPath))
                    {
                        try
                        {
                            var json = File.ReadAllText(manifestPath);
                            var manifest = JsonSerializer.Deserialize<PluginManifest>(json, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (manifest != null)
                            {
                                plugins.Add(manifest);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"⚠️ Failed to load manifest from {dir}: {ex.Message}");
                        }
                    }
                }

                Logger.Log($"📦 Found {plugins.Count} locally installed plugins");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Error getting installed plugins: {ex.Message}");
            }

            return plugins;
        }

        /// <summary>
        /// Uninstall a locally installed plugin
        /// </summary>
        public static Task<bool> UninstallPluginAsync(string pluginName)
        {
            try
            {
                var pluginPath = Path.Combine(_localPluginsPath, pluginName);

                if (!Directory.Exists(pluginPath))
                {
                    Logger.Log($"⚠️ Plugin not found: {pluginName}");
                    return Task.FromResult(false);
                }

                // Remove plugin directory
                Directory.Delete(pluginPath, true);

                Logger.Log($"✅ Plugin uninstalled: {pluginName}");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Plugin uninstall failed: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Update a locally installed plugin
        /// </summary>
        public static async Task<bool> UpdatePluginAsync(PluginManifest plugin)
        {
            try
            {
                // For now, just reinstall (in production, would check versions)
                return await InstallPluginAsync(plugin);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Plugin update failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Search for plugins by name or description
        /// </summary>
        public static async Task<List<PluginManifest>> SearchPluginsAsync(string searchTerm)
        {
            var allPlugins = await DiscoverPluginsAsync();
            return allPlugins.Where(p =>
                p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (p.Tags?.Any(t => t.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ?? false)
            ).ToList();
        }

        // Event handlers
        private static void OnPluginDiscovered(PluginManifest plugin)
        {
            PluginDiscovered?.Invoke(null, new PluginDiscoveryEventArgs(plugin));
        }

        private static void OnPluginInstalled(PluginManifest plugin, bool success)
        {
            PluginInstalled?.Invoke(null, new PluginInstallEventArgs(plugin, success));
        }

        private static void OnPluginInstallFailed(PluginManifest plugin, string error)
        {
            PluginInstallFailed?.Invoke(null, new PluginInstallEventArgs(plugin, false, error));
        }
    }

    /// <summary>
    /// Plugin manifest structure
    /// </summary>
    public class PluginManifest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public string Version { get; set; } = "1.0.0";
        public string License { get; set; } = "";
        public string Homepage { get; set; } = "";
        public List<string>? Tags { get; set; }
        public List<PluginFile> Files { get; set; } = new List<PluginFile>();
        public PluginDependencies? Dependencies { get; set; }
        public DateTime PublishedDate { get; set; }
        public int Downloads { get; set; }
        public double Rating { get; set; }
        public List<string>? Screenshots { get; set; }
        public bool IsInstalled { get; set; } = false;
    }

    public class PluginFile
    {
        public string Path { get; set; } = "";
        public string Checksum { get; set; } = "";
        public long Size { get; set; }
    }

    public class PluginDependencies
    {
        public List<string>? RequiredPlugins { get; set; }
        public string? MinimumCyclosideVersion { get; set; }
    }

    public class RepositoryItem
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
    }

    public class PluginDiscoveryEventArgs : EventArgs
    {
        public PluginManifest Plugin { get; }

        public PluginDiscoveryEventArgs(PluginManifest plugin)
        {
            Plugin = plugin;
        }
    }

    public class PluginInstallEventArgs : EventArgs
    {
        public PluginManifest Plugin { get; }
        public bool Success { get; }
        public string? ErrorMessage { get; }

        public PluginInstallEventArgs(PluginManifest plugin, bool success, string? errorMessage = null)
        {
            Plugin = plugin;
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
}
