using System;
using System.Collections.Generic;
using Cycloside.Plugins.BuiltIn;

namespace Cycloside.Plugins;

/// <summary>
/// Central registry for every built-in plugin so startup, wizards, and menus stay in sync.
/// </summary>
public static class BuiltInPluginCatalog
{
    /// <summary>
    /// Metadata describing how to create a built-in plugin and whether it should be enabled by default.
    /// </summary>
    public sealed record BuiltInPluginDescriptor(
        string Name,
        Func<PluginManager, IPlugin> Factory,
        bool EnabledByDefault = true,
        bool IsSafe = false);

    private static readonly IReadOnlyList<BuiltInPluginDescriptor> _descriptors = new List<BuiltInPluginDescriptor>
    {
        new("Date/Time Overlay", _ => new DateTimeOverlayPlugin()),
        new("MP3 Player", _ => new MP3PlayerPlugin()),
        new("Managed Visual Host", _ => new ManagedVisHostPlugin()),
        new("Macro Engine", _ => new MacroPlugin()),
        new("Text Editor", _ => new TextEditorPlugin()),
        new("File Explorer", _ => new FileExplorerPlugin()),
        new("Wallpaper Changer", _ => new WallpaperPlugin()),
        new("Clipboard Manager", _ => new ClipboardManagerPlugin()),
        new("Code Editor", _ => new CodeEditorPlugin()),
        new("Character Map", _ => new CharacterMapPlugin()),
        new("File Watcher", _ => new FileWatcherPlugin()),
        new("Process Monitor", _ => new ProcessMonitorPlugin()),
        new("Network Tools", _ => new NetworkToolsPlugin()),
        new("Task Scheduler", _ => new TaskSchedulerPlugin()),
        new("Disk Usage", _ => new DiskUsagePlugin()),
        new("Encryption", _ => new EncryptionPlugin()),
        new("Terminal", _ => new TerminalPlugin()),
        new("Log Viewer", _ => new LogViewerPlugin()),
        new("Notification Center", _ => new NotificationCenterPlugin()),
        new("Environment Editor", _ => new EnvironmentEditorPlugin()),
        new("ModPlug Tracker", _ => new ModTrackerPlugin(), EnabledByDefault: false),
        new("Jezzball", _ => new JezzballPlugin()),
        new("Quick Launcher", manager => new QuickLauncherPlugin(manager)),
        new("Widget Host", manager => new WidgetHostPlugin(manager)),
        new("QBasic Retro IDE", _ => new QBasicRetroIDEPlugin(), EnabledByDefault: false)
    };

    public static IReadOnlyList<BuiltInPluginDescriptor> Descriptors => _descriptors;
}
