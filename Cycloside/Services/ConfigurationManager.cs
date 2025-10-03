using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Cycloside;

namespace Cycloside.Services
{
    /// <summary>
    /// CONFIGURATION MANAGER - Persistent settings for Cycloside
    /// Manages welcome screen, plugin state, layouts, themes, and preferences
    /// </summary>
    public static class ConfigurationManager
    {
        private static readonly string ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cycloside", "config.json");
        private static CyclosideConfig? _currentConfig;
        private static bool _isLoaded;

        public static CyclosideConfig CurrentConfig => _currentConfig ??= new CyclosideConfig();

        public static event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        public static bool IsLoaded => _isLoaded;
        public static bool IsWelcomeEnabled => CurrentConfig.ShowWelcomeOnStartup;

        /// <summary>
        /// Initialize Configuration Manager and load settings
        /// </summary>
        public static async Task InitializeAsync()
        {
            Logger.Log("🔧 Initializing Configuration Manager...");

            try
            {
                await LoadConfigurationAsync();
                Logger.Log("✅ Configuration Manager initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Configuration Manager initialization failed: {ex.Message}");
                _currentConfig = new CyclosideConfig(); // Use defaults
            }
        }

        /// <summary>
        /// Load configuration from persistent storage
        /// </summary>
        public static async Task LoadConfigurationAsync()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    Logger.Log("🔄 No existing configuration found - using defaults");
                    _currentConfig = new CyclosideConfig();
                    await SaveConfigurationAsync(); // Create initial config
                    return;
                }

                var json = await File.ReadAllTextAsync(ConfigPath);
                _currentConfig = JsonSerializer.Deserialize<CyclosideConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                });

                _currentConfig ??= new CyclosideConfig(); // Fallback to defaults
                _isLoaded = true;

                Logger.Log($"📋 Configuration loaded: {_currentConfig?.Plugins?.Count ?? 0} plugins configured");
                OnConfigurationChanged("Load");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to load configuration: {ex.Message}");
                _currentConfig = new CyclosideConfig();
                _isLoaded = true;
            }
        }

        /// <summary>
        /// Save configuration to persistent storage
        /// </summary>
        public static async Task SaveConfigurationAsync()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_currentConfig, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(ConfigPath, json);
                Logger.Log($"💾 Configuration saved to: {ConfigPath}");
                OnConfigurationChanged("Save");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to save configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Update welcome screen preference
        /// </summary>
        public static async Task SetWelcomePreferenceAsync(bool showOnStartup)
        {
            if (_currentConfig?.ShowWelcomeOnStartup != showOnStartup)
            {
                CurrentConfig.ShowWelcomeOnStartup = showOnStartup;
                await SaveConfigurationAsync();
                Logger.Log($"🔔 Welcome preference updated: {(showOnStartup ? "Show" : "Hide")}");
            }
        }

        /// <summary>
        /// Update theme preference
        /// </summary>
        public static async Task SetThemePreferenceAsync(string theme, string variant)
        {
            var changed = false;
            
            if (CurrentConfig.SelectedTheme != theme)
            {
                CurrentConfig.SelectedTheme = theme;
                changed = true;
            }

            if (CurrentConfig.SelectedThemeVariant != variant)
            {
                CurrentConfig.SelectedThemeVariant = variant;
                changed = true;
            }

            if (changed)
            {
                await SaveConfigurationAsync();
                Logger.Log($"🎨 Theme preference updated: {theme}/{variant}");
                OnConfigurationChanged("Theme");
            }
        }

        /// <summary>
        /// Update plugin enabled state
        /// </summary>
        public static async Task SetPluginEnabledAsync(string pluginName, bool enabled)
        {
            CurrentConfig.Plugins ??= new Dictionary<string, PluginConfig>();

            var plugin = CurrentConfig.Plugins.GetValueOrDefault(pluginName, new PluginConfig
            {
                Name = pluginName,
                Enabled = true,
                LoadOnStartup = true
            });

            if (plugin.Enabled != enabled)
            {
                plugin.Enabled = enabled;
                plugin.LoadOnStartup = enabled; // If disabled, don't load on startup
                
                CurrentConfig.Plugins[pluginName] = plugin;
                await SaveConfigurationAsync();
                
                Logger.Log($"🔌 Plugin state updated: {pluginName} = {(enabled ? "Enabled" : "Disabled")}");
                OnConfigurationChanged("Plugin");
            }
        }

        /// <summary>
        /// Update plugin startup preference
        /// </summary>
        public static async Task SetPluginStartupPreferenceAsync(string pluginName, bool loadOnStartup)
        {
            CurrentConfig.Plugins ??= new Dictionary<string, PluginConfig>();

            var plugin = CurrentConfig.Plugins.GetValueOrDefault(pluginName, new PluginConfig
            {
                Name = pluginName,
                Enabled = true,
                LoadOnStartup = true
            });

            if (plugin.LoadOnStartup != loadOnStartup)
            {
                plugin.LoadOnStartup = loadOnStartup;
                
                CurrentConfig.Plugins[pluginName] = plugin;
                await SaveConfigurationAsync();
                
                Logger.Log($"🚀 Plugin startup preference updated: {pluginName} = {(loadOnStartup ? "Load" : "Don't Load")}");
                OnConfigurationChanged("Startup");
            }
        }

        /// <summary>
        /// Update terminal preference
        /// </summary>
        public static async Task SetTerminalPreferenceAsync(string terminalType)
        {
            if (CurrentConfig.PreferredTerminal != terminalType)
            {
                CurrentConfig.PreferredTerminal = terminalType;
                await SaveConfigurationAsync();
                Logger.Log($"💻 Terminal preference updated: {terminalType}");
                OnConfigurationChanged("Terminal");
            }
        }

        /// <summary>
        /// Update layout configuration
        /// </summary>
        public static async Task SetLayoutConfigurationAsync(LayoutConfig layout)
        {
            CurrentConfig.Layout = layout;
            await SaveConfigurationAsync();
            Logger.Log("🎯 Layout configuration updated");
            OnConfigurationChanged("Layout");
        }

        /// <summary>
        /// Get plugin configuration
        /// </summary>
        public static PluginConfig GetPluginConfig(string pluginName)
        {
            return CurrentConfig.Plugins?.GetValueOrDefault(pluginName, new PluginConfig
            {
                Name = pluginName,
                Enabled = true,
                LoadOnStartup = true,
                Position = new PluginPosition()
            }) ?? new PluginConfig();
        }

        /// <summary>
        /// Check if plugin should be loaded on startup
        /// </summary>
        public static bool ShouldLoadPluginOnStartup(string pluginName)
        {
            var config = GetPluginConfig(pluginName);
            return config.Enabled && config.LoadOnStartup;
        }

        /// <summary>
        /// Reset configuration to defaults
        /// </summary>
        public static async Task ResetToDefaultsAsync()
        {
            Logger.Log("🔄 Resetting configuration to defaults...");
            
            _currentConfig = new CyclosideConfig();
            await SaveConfigurationAsync();
            
            Logger.Log("✅ Configuration reset to defaults");
            OnConfigurationChanged("Reset");
        }

        private static void OnConfigurationChanged(string changeType)
        {
            ConfigurationChanged?.Invoke(null, new ConfigurationChangedEventArgs(changeType, DateTime.Now));
        }
    }

    /// <summary>
    /// Configuration classes for Cycloside settings
    /// </summary>
    public class CyclosideConfig
    {
        public bool ShowWelcomeOnStartup { get; set; } = true;
        public bool FirstTimeRun { get; set; } = true;
        public string SelectedTheme { get; set; } = "LightTheme";
        public string SelectedThemeVariant { get; set; } = "Light";
        public string PreferredTerminal { get; set; } = "PowerShell";
        public Dictionary<string, PluginConfig> Plugins { get; set; } = new Dictionary<string, PluginConfig>();
        public LayoutConfig Layout { get; set; } = new LayoutConfig();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string Version { get; set; } = "1.0.0";
    }

    public class PluginConfig
    {
        public string Name { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public bool LoadOnStartup { get; set; } = true;
        public PluginPosition Position { get; set; } = new PluginPosition();
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
    }

    public class PluginPosition
    {
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double Width { get; set; } = 300;
        public double Height { get; set; } = 200;
        public bool Maximized { get; set; } = false;
    }

    public class LayoutConfig
    {
        public string ScreenLayout { get; set; } = "Grid";
        public bool AutoArrangeWindows { get; set; } = true;
        public bool RememberWindowPositions { get; set; } = true;
        public Dictionary<string, WidgetLayout> WidgetLayouts { get; set; } = new Dictionary<string, WidgetLayout>();
    }

    public class WidgetLayout
    {
        public string WidgetName { get; set; } = "";
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double Width { get; set; } = 200;
        public double Height { get; set; } = 150;
        public bool Visible { get; set; } = true;
    }

    public class ConfigurationChangedEventArgs : EventArgs
    {
        public string ChangeType { get; }
        public DateTime Timestamp { get; }

        public ConfigurationChangedEventArgs(string changeType, DateTime timestamp)
        {
            ChangeType = changeType;
            Timestamp = timestamp;
        }
    }
}