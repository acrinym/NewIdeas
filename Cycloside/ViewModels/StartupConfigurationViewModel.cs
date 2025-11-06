using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cycloside.Models;
using Cycloside.Plugins;

namespace Cycloside.ViewModels;

public partial class StartupConfigurationViewModel : ObservableObject
{
    private readonly Plugins.PluginManager _pluginManager;
    private readonly Action<StartupConfiguration> _onComplete;

    [ObservableProperty]
    private ObservableCollection<PluginCategoryGroup> _pluginCategories = new();

    public StartupConfigurationViewModel(Plugins.PluginManager pluginManager, Action<StartupConfiguration> onComplete)
    {
        _pluginManager = pluginManager;
        _onComplete = onComplete;

        LoadPluginsByCategory();
    }

    private void LoadPluginsByCategory()
    {
        var monitors = GetAvailableMonitors();

        // Group plugins by category
        var grouped = _pluginManager.Plugins
            .GroupBy(p => p.Category)
            .OrderBy(g => GetCategoryOrder(g.Key));

        foreach (var group in grouped)
        {
            var categoryGroup = new PluginCategoryGroup
            {
                Category = group.Key,
                CategoryName = GetCategoryDisplayName(group.Key),
                Icon = GetCategoryIcon(group.Key),
                Description = GetCategoryDescription(group.Key),
                Plugins = new ObservableCollection<PluginConfigItem>(
                    group.Select(p => new PluginConfigItem
                    {
                        PluginName = p.Name,
                        Description = p.Description,
                        IsEnabled = p.EnabledByDefault, // Use plugin's default setting
                        PositionPreset = "Center",
                        MonitorIndex = 0,
                        AvailablePresets = GetAvailablePresets(),
                        AvailableMonitors = monitors
                    })
                )
            };

            // Update AllEnabled based on children
            categoryGroup.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(categoryGroup.AllEnabled))
                {
                    foreach (var plugin in categoryGroup.Plugins)
                    {
                        plugin.IsEnabled = categoryGroup.AllEnabled;
                    }
                }
            };

            PluginCategories.Add(categoryGroup);
        }
    }

    private int GetCategoryOrder(PluginCategory category)
    {
        return category switch
        {
            PluginCategory.DesktopCustomization => 1,
            PluginCategory.RetroComputing => 2,
            PluginCategory.Entertainment => 3,
            PluginCategory.TinkererTools => 4,
            PluginCategory.Utilities => 5,
            PluginCategory.Development => 6,
            PluginCategory.Security => 7,
            _ => 99
        };
    }

    private string GetCategoryDisplayName(PluginCategory category)
    {
        return category switch
        {
            PluginCategory.DesktopCustomization => "Desktop Customization (Core)",
            PluginCategory.RetroComputing => "Retro Computing (Core)",
            PluginCategory.Entertainment => "Entertainment",
            PluginCategory.TinkererTools => "Tinkerer Tools",
            PluginCategory.Utilities => "Utilities",
            PluginCategory.Development => "Development Tools (Advanced)",
            PluginCategory.Security => "Security Tools (Advanced)",
            _ => category.ToString()
        };
    }

    private string GetCategoryIcon(PluginCategory category)
    {
        return category switch
        {
            PluginCategory.DesktopCustomization => "🎨",
            PluginCategory.RetroComputing => "🎮",
            PluginCategory.Entertainment => "🎵",
            PluginCategory.TinkererTools => "🛠️",
            PluginCategory.Utilities => "📁",
            PluginCategory.Development => "💻",
            PluginCategory.Security => "🔒",
            _ => "📦"
        };
    }

    private string GetCategoryDescription(PluginCategory category)
    {
        return category switch
        {
            PluginCategory.DesktopCustomization => "Essential widgets and themes",
            PluginCategory.RetroComputing => "Classic games and computing",
            PluginCategory.Entertainment => "Media players and fun stuff",
            PluginCategory.TinkererTools => "Automation and power user features",
            PluginCategory.Utilities => "Everyday desktop tools",
            PluginCategory.Development => "Terminal, code editor, databases",
            PluginCategory.Security => "Network tools and analysis",
            _ => ""
        };
    }

    private ObservableCollection<string> GetAvailablePresets()
    {
        return new ObservableCollection<string>
        {
            "Center",
            "Top Left",
            "Top Right",
            "Bottom Left",
            "Bottom Right",
            "Left Edge",
            "Right Edge",
            "Top Edge",
            "Bottom Edge"
        };
    }

    private ObservableCollection<string> GetAvailableMonitors()
    {
        try
        {
            var screens = Application.Current?.PlatformSettings?.GetType()
                .GetProperty("Screens")?.GetValue(Application.Current.PlatformSettings);

            if (screens is IEnumerable<object> screenList)
            {
                var count = screenList.Count();
                var monitors = new ObservableCollection<string>();

                for (int i = 0; i < count; i++)
                {
                    monitors.Add(i == 0 ? "Primary Monitor" : $"Monitor {i + 1}");
                }

                return monitors.Count > 0 ? monitors : new ObservableCollection<string> { "Primary Monitor" };
            }
        }
        catch
        {
            // Fallback if screen enumeration fails
        }

        return new ObservableCollection<string> { "Primary Monitor" };
    }

    [RelayCommand]
    private void SelectAllCore()
    {
        // Enable all core categories (Desktop + Retro + Entertainment)
        foreach (var category in PluginCategories)
        {
            if (category.Category is PluginCategory.DesktopCustomization
                                  or PluginCategory.RetroComputing
                                  or PluginCategory.Entertainment)
            {
                category.AllEnabled = true;
            }
        }
    }

    [RelayCommand]
    private void UseDefaults()
    {
        // Set each plugin to its default enabled state
        foreach (var category in PluginCategories)
        {
            foreach (var plugin in category.Plugins)
            {
                var originalPlugin = _pluginManager.Plugins.FirstOrDefault(p => p.Name == plugin.PluginName);
                if (originalPlugin != null)
                {
                    plugin.IsEnabled = originalPlugin.EnabledByDefault;
                }
            }
        }
    }

    [RelayCommand]
    private void Continue()
    {
        var config = new StartupConfiguration
        {
            HasCompletedFirstLaunch = true
        };

        // Build configuration from UI state
        foreach (var category in PluginCategories)
        {
            foreach (var plugin in category.Plugins)
            {
                var pluginConfig = new PluginStartupConfig
                {
                    PluginName = plugin.PluginName,
                    EnableOnStartup = plugin.IsEnabled
                };

                if (plugin.IsEnabled)
                {
                    pluginConfig.Position = new WindowStartupPosition
                    {
                        MonitorIndex = plugin.MonitorIndex,
                        Preset = ParsePreset(plugin.PositionPreset)
                    };
                }

                config.PluginConfigs.Add(pluginConfig);
            }
        }

        _onComplete(config);
    }

    private WindowPositionPreset ParsePreset(string presetName)
    {
        return presetName switch
        {
            "Center" => WindowPositionPreset.Center,
            "Top Left" => WindowPositionPreset.TopLeft,
            "Top Right" => WindowPositionPreset.TopRight,
            "Bottom Left" => WindowPositionPreset.BottomLeft,
            "Bottom Right" => WindowPositionPreset.BottomRight,
            "Left Edge" => WindowPositionPreset.LeftEdge,
            "Right Edge" => WindowPositionPreset.RightEdge,
            "Top Edge" => WindowPositionPreset.TopEdge,
            "Bottom Edge" => WindowPositionPreset.BottomEdge,
            _ => WindowPositionPreset.Center
        };
    }
}

public partial class PluginCategoryGroup : ObservableObject
{
    public PluginCategory Category { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _allEnabled;

    public ObservableCollection<PluginConfigItem> Plugins { get; set; } = new();
}

public partial class PluginConfigItem : ObservableObject
{
    public string PluginName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _positionPreset = "Center";

    [ObservableProperty]
    private int _monitorIndex;

    public ObservableCollection<string> AvailablePresets { get; set; } = new();
    public ObservableCollection<string> AvailableMonitors { get; set; } = new();
}
