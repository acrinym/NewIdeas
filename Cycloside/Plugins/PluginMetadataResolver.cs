using System;
using System.Collections.Generic;
using System.Linq;

namespace Cycloside.Plugins;

/// <summary>
/// Resolves plugin metadata without forcing the current built-in set to implement a new interface all at once.
/// </summary>
public static class PluginMetadataResolver
{
    private static readonly IReadOnlyDictionary<string, PluginMetadataInfo> BuiltInMetadata =
        new Dictionary<string, PluginMetadataInfo>(StringComparer.Ordinal)
        {
            ["DateTimeOverlayPlugin"] = new("DateTimeOverlayPlugin", PluginCategory.DesktopCustomization, true, true),
            ["WallpaperPlugin"] = new("WallpaperPlugin", PluginCategory.DesktopCustomization, true, true),
            ["QuickLauncherPlugin"] = new("QuickLauncherPlugin", PluginCategory.DesktopCustomization, true, true),
            ["WidgetHostPlugin"] = new("WidgetHostPlugin", PluginCategory.DesktopCustomization, true, true),

            ["JezzballPlugin"] = new("JezzballPlugin", PluginCategory.RetroComputing, true, true),
            ["GweledPlugin"] = new("GweledPlugin", PluginCategory.RetroComputing, true, true),
            ["TileWorldPlugin"] = new("TileWorldPlugin", PluginCategory.RetroComputing, true, true),
            ["QBasicRetroIDEPlugin"] = new("QBasicRetroIDEPlugin", PluginCategory.RetroComputing, true, true),

            ["MacroPlugin"] = new("MacroPlugin", PluginCategory.TinkererTools, true, false),
            ["HackerTerminalPlugin"] = new("HackerTerminalPlugin", PluginCategory.TinkererTools, true, false),
            ["PowerShellTerminalPlugin"] = new("PowerShellTerminalPlugin", PluginCategory.TinkererTools, true, false),
            ["FileWatcherPlugin"] = new("FileWatcherPlugin", PluginCategory.TinkererTools, true, false),
            ["TaskSchedulerPlugin"] = new("TaskSchedulerPlugin", PluginCategory.TinkererTools, true, false),
            ["EnvironmentEditorPlugin"] = new("EnvironmentEditorPlugin", PluginCategory.TinkererTools, true, false),

            ["TextEditorPlugin"] = new("TextEditorPlugin", PluginCategory.Utilities, true, false),
            ["ClipboardManagerPlugin"] = new("ClipboardManagerPlugin", PluginCategory.Utilities, true, false),
            ["PluginMarketplacePlugin"] = new("PluginMarketplacePlugin", PluginCategory.Utilities, false, false),
            ["NetworkToolsPlugin"] = new("NetworkToolsPlugin", PluginCategory.Utilities, true, false),
            ["HardwareMonitorPlugin"] = new("HardwareMonitorPlugin", PluginCategory.Utilities, true, false),
            ["CharacterMapPlugin"] = new("CharacterMapPlugin", PluginCategory.Utilities, true, false),
            ["DiskUsagePlugin"] = new("DiskUsagePlugin", PluginCategory.Utilities, true, false),
            ["LogViewerPlugin"] = new("LogViewerPlugin", PluginCategory.Utilities, true, false),
            ["NotificationCenterPlugin"] = new("NotificationCenterPlugin", PluginCategory.Utilities, true, false),
            ["FileExplorerPlugin"] = new("FileExplorerPlugin", PluginCategory.Utilities, true, false),

            ["AdvancedCodeEditorPlugin"] = new("AdvancedCodeEditorPlugin", PluginCategory.Development, false, false),
            ["DatabaseManagerPlugin"] = new("DatabaseManagerPlugin", PluginCategory.Development, false, false),
            ["ApiTestingPlugin"] = new("ApiTestingPlugin", PluginCategory.Development, false, false),
            ["TerminalPlugin"] = new("TerminalPlugin", PluginCategory.Development, false, false),

            ["HackersParadisePlugin"] = new("HackersParadisePlugin", PluginCategory.Security, false, false),
            ["VulnerabilityScannerPlugin"] = new("VulnerabilityScannerPlugin", PluginCategory.Security, false, false),
            ["ExploitDevToolsPlugin"] = new("ExploitDevToolsPlugin", PluginCategory.Security, false, false),
            ["ExploitDatabasePlugin"] = new("ExploitDatabasePlugin", PluginCategory.Security, false, false),
            ["DigitalForensicsPlugin"] = new("DigitalForensicsPlugin", PluginCategory.Security, false, false),

            ["MP3PlayerPlugin"] = new("MP3PlayerPlugin", PluginCategory.Entertainment, true, false),
            ["ManagedVisHostPlugin"] = new("ManagedVisHostPlugin", PluginCategory.Entertainment, true, false),
            ["ModTrackerPlugin"] = new("ModTrackerPlugin", PluginCategory.Entertainment, true, false),
            ["ScreenSaverPlugin"] = new("ScreenSaverPlugin", PluginCategory.Entertainment, true, false),

            ["AiAssistantPlugin"] = new("AiAssistantPlugin", PluginCategory.Experimental, false, false)
        };

    public static string GetPluginId(IPlugin plugin)
    {
        if (plugin is IPluginMetadata metadata && !string.IsNullOrWhiteSpace(metadata.PluginId))
        {
            return metadata.PluginId!.Trim();
        }

        return plugin.GetType().Name;
    }

    public static PluginMetadataInfo Resolve(IPlugin plugin)
    {
        if (plugin is IPluginMetadata metadata)
        {
            var pluginId = GetPluginId(plugin);
            return new PluginMetadataInfo(pluginId, metadata.Category, metadata.EnabledByDefault, metadata.IsCore);
        }

        return ResolveById(plugin.GetType().Name);
    }

    public static PluginMetadataInfo ResolveById(string pluginId)
    {
        if (BuiltInMetadata.TryGetValue(pluginId, out var info))
        {
            return info;
        }

        // Preserve legacy compatibility for unknown plugins by leaving them enabled by default.
        return new PluginMetadataInfo(pluginId, PluginCategory.Experimental, true, false);
    }

    public static int GetCategorySortOrder(PluginCategory category)
    {
        return category switch
        {
            PluginCategory.DesktopCustomization => 0,
            PluginCategory.RetroComputing => 1,
            PluginCategory.TinkererTools => 2,
            PluginCategory.Utilities => 3,
            PluginCategory.Entertainment => 4,
            PluginCategory.Development => 5,
            PluginCategory.Security => 6,
            _ => 7
        };
    }

    public static string GetCategoryDisplayName(PluginCategory category)
    {
        return category switch
        {
            PluginCategory.DesktopCustomization => "Desktop Customization",
            PluginCategory.RetroComputing => "Retro Computing",
            PluginCategory.TinkererTools => "Tinkerer Tools",
            PluginCategory.Utilities => "Utilities",
            PluginCategory.Development => "Development",
            PluginCategory.Security => "Security",
            PluginCategory.Entertainment => "Entertainment",
            _ => "Experimental"
        };
    }

    public static IEnumerable<IGrouping<PluginCategory, IPlugin>> GroupByCategory(IEnumerable<IPlugin> plugins)
    {
        return plugins
            .GroupBy(plugin => Resolve(plugin).Category)
            .OrderBy(group => GetCategorySortOrder(group.Key))
            .ThenBy(group => GetCategoryDisplayName(group.Key), StringComparer.Ordinal);
    }
}
