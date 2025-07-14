using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Cycloside.Plugins;
using Cycloside.Plugins.BuiltIn;
using Cycloside.ViewModels;
using Cycloside.Services;
using Cycloside.Views;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace Cycloside;

public partial class App : Application
{
    private const string TrayIconBase64 = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAGElEQVR4nGNkaGAgCTCRpnxUw6iGoaQBALsfAKDg6Y6zAAAAAElFTkSuQmCC";
    private RemoteApiServer? _remoteServer;
    private PluginManager? _pluginManager;
    private TrayIcon? _trayIcon; // Keep a reference to the tray icon

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        var settings = SettingsManager.Settings;
        ThemeManager.LoadGlobalThemeFromSettings();

        if (settings.FirstRun)
        {
            var wiz = new WizardWindow();
            wiz.Closed += (_, _) =>
            {
                desktop.MainWindow = CreateMainWindow(SettingsManager.Settings);
                desktop.MainWindow.Show();
            };
            wiz.Show();
        }
        else
        {
            desktop.MainWindow = CreateMainWindow(settings);
            desktop.MainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private MainWindow CreateMainWindow(AppSettings settings)
    {
        _pluginManager = new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"), msg => Logger.Log(msg));
        
        // Subscribe to plugin reloads to update the UI when plugins are refreshed.
        _pluginManager.PluginsReloaded += OnPluginsReloaded;

        var volatileManager = new VolatilePluginManager();

        LoadAllPlugins(_pluginManager, settings);
        _pluginManager.StartWatching();

        var viewModel = new MainWindowViewModel(_pluginManager.Plugins);
        var mainWindow = new MainWindow(_pluginManager)
        {
            DataContext = viewModel
        };

        viewModel.ExitCommand = new RelayCommand(() => Shutdown());
        
        // Toggle plugin enablement from the main window.
        viewModel.StartPluginCommand = new RelayCommand(plugin =>
        {
            if (plugin is not IPlugin p || _pluginManager is null) return;
            
            bool shouldBeEnabled = !_pluginManager.IsEnabled(p);
            if (shouldBeEnabled)
            {
                _pluginManager.EnablePlugin(p);
            }
            else
            {
                _pluginManager.DisablePlugin(p);
            }

            SettingsManager.Settings.PluginEnabled[p.Name] = shouldBeEnabled;
            SettingsManager.Save();
            WorkspaceProfiles.UpdatePlugin(settings.ActiveProfile, p.Name, shouldBeEnabled);
        });

        _remoteServer = new RemoteApiServer(_pluginManager, settings.RemoteApiToken);
        _remoteServer.Start();
        WorkspaceProfiles.Apply(settings.ActiveProfile, _pluginManager);
        RegisterHotkeys(_pluginManager);
        
        _trayIcon = new TrayIcon
        {
            Icon = CreateTrayIcon(),
            ToolTipText = "Cycloside",
            Menu = BuildTrayMenu(_pluginManager, volatileManager, settings)
        };
        var icons = TrayIcon.GetIcons(this) ?? new TrayIcons();
        TrayIcon.SetIcons(this, icons);
        if (!icons.Contains(_trayIcon))
        {
            icons.Add(_trayIcon);
        }
        _trayIcon.IsVisible = true;
        
        return mainWindow;
    }
    
    // Handle the PluginsReloaded event to refresh menus and view models.
    private void OnPluginsReloaded()
    {
        if (_trayIcon is null || _pluginManager is null) return;

        // Rebuild the tray menu with the new plugin instances.
        var volatileManager = new VolatilePluginManager();
        _trayIcon.Menu = BuildTrayMenu(_pluginManager, volatileManager, SettingsManager.Settings);

        // Also update the main window's view model.
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow?.DataContext is MainWindowViewModel vm)
        {
            vm.AvailablePlugins.Clear();
            foreach (var plugin in _pluginManager.Plugins)
            {
                vm.AvailablePlugins.Add(plugin);
            }
        }
    }

    private void LoadAllPlugins(PluginManager manager, AppSettings settings)
    {
        void TryAdd(Func<IPlugin> factory)
        {
            var plugin = factory();
            if (!settings.DisableBuiltInPlugins || settings.SafeBuiltInPlugins.GetValueOrDefault(plugin.Name, false))
                manager.AddBuiltInPlugin(factory);
        }

        TryAdd(() => new DateTimeOverlayPlugin());
        TryAdd(() => new MP3PlayerPlugin());
        TryAdd(() => new MacroPlugin());
        TryAdd(() => new TextEditorPlugin());
        TryAdd(() => new WallpaperPlugin());
        TryAdd(() => new ClipboardManagerPlugin());
        TryAdd(() => new FileWatcherPlugin());
        TryAdd(() => new ProcessMonitorPlugin());
        TryAdd(() => new TaskSchedulerPlugin());
        TryAdd(() => new DiskUsagePlugin());
        TryAdd(() => new TerminalPlugin());
        TryAdd(() => new LogViewerPlugin());
        TryAdd(() => new EnvironmentEditorPlugin());
        TryAdd(() => new JezzballPlugin());
        TryAdd(() => new WidgetHostPlugin(manager));
        TryAdd(() => new WinampVisHostPlugin());
        TryAdd(() => new QBasicRetroIDEPlugin());
        TryAdd(() => new ScreenSaverPlugin());
    }

    private void RegisterHotkeys(PluginManager manager)
    {
        HotkeyManager.Register(new KeyGesture(Key.W, KeyModifiers.Control | KeyModifiers.Alt), () =>
        {
            var plugin = manager.Plugins.FirstOrDefault(p => p.Name == "Widget Host");
            if (plugin != null)
            {
                if (manager.IsEnabled(plugin)) manager.DisablePlugin(plugin);
                else manager.EnablePlugin(plugin);
            }
        });
    }

    private void Shutdown()
    {
        _pluginManager?.StopAll();
        _remoteServer?.Stop();
        HotkeyManager.UnregisterAll();
        Logger.Shutdown();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime appLifetime)
        {
            appLifetime.Shutdown();
        }
    }

    #region Tray Menu and Icon Logic
    
    private NativeMenu BuildTrayMenu(PluginManager manager, VolatilePluginManager volatileManager, AppSettings settings)
    {
        var pluginsMenu = new NativeMenuItem("Plugins") { Menu = new NativeMenu() };
        var newPlugins = manager.Plugins.Where(p => manager.GetStatus(p) != PluginChangeStatus.None).ToList();
        var otherPlugins = manager.Plugins.Except(newPlugins).OrderBy(p => p.Name).ToList();

        if (newPlugins.Any())
        {
            var newMenu = new NativeMenuItem("New/Updated") { Menu = new NativeMenu() };
            foreach (var p in newPlugins) newMenu.Menu!.Items.Add(BuildPluginMenuItem(p, manager, settings));
            pluginsMenu.Menu!.Items.Add(newMenu);
            pluginsMenu.Menu!.Items.Add(new NativeMenuItemSeparator());
        }

        foreach (var p in otherPlugins) pluginsMenu.Menu!.Items.Add(BuildPluginMenuItem(p, manager, settings));

        var volatileMenu = new NativeMenuItem("Volatile") { Menu = new NativeMenu() };
        volatileMenu.Menu!.Items.Add(BuildVolatileScriptMenuItem("Run Lua Script...", new FilePickerFileType("Lua Script") { Patterns = new[] { "*.lua" } }, volatileManager.RunLua));
        volatileMenu.Menu!.Items.Add(BuildVolatileScriptMenuItem("Run C# Script...", new FilePickerFileType("C# Script") { Patterns = new[] { "*.csx" } }, volatileManager.RunCSharp));
        volatileMenu.Menu!.Items.Add(new NativeMenuItemSeparator());
        var inlineItem = new NativeMenuItem("Run Inline...");
        inlineItem.Click += (_, _) => new VolatileRunnerWindow(volatileManager).Show();
        volatileMenu.Menu!.Items.Add(inlineItem);
        
        // **NEW: A dedicated menu for log actions**
        var logsMenu = new NativeMenuItem("Logs") { Menu = new NativeMenu() };
        var viewErrorsItem = new NativeMenuItem("View Errors");
        viewErrorsItem.Click += (_, _) =>
        {
            var logViewerPlugin = manager.Plugins.FirstOrDefault(p => p.Name == "Log Viewer");
            if (logViewerPlugin is LogViewerPlugin viewer) // Cast to our specific type
            {
                viewer.InitialFilter = "[ERROR]"; // Set the filter before starting
                manager.EnablePlugin(viewer);
            }
        };
        logsMenu.Menu.Add(viewErrorsItem);

        return new NativeMenu
        {
            Items =
            {
                new NativeMenuItem("Settings") { Menu = new NativeMenu { Items = {
                    new NativeMenuItem("Control Panel...") { Command = new RelayCommand(() => new ControlPanelWindow(manager).Show()) },
                    new NativeMenuItem("Plugin Manager...") { Command = new RelayCommand(() => new PluginSettingsWindow(manager).Show()) },
                    new NativeMenuItem("Theme Settings...") { Command = new RelayCommand(() => new ThemeSettingsWindow(manager).Show()) },
                }}},
                new NativeMenuItemSeparator(),
                logsMenu, // **Add the new Logs menu here**
                new NativeMenuItemSeparator(),
                pluginsMenu,
                volatileMenu,
                new NativeMenuItem("Open Plugins Folder") { Command = new RelayCommand(() => {
                    try { Process.Start(new ProcessStartInfo { FileName = manager.PluginDirectory, UseShellExecute = true }); } 
                    catch (Exception ex) { Logger.Log($"Failed to open plugin folder: {ex.Message}"); }
                })},
                new NativeMenuItemSeparator(),
                new NativeMenuItem("Exit") { Command = new RelayCommand(() => Shutdown()) }
            }
        };
    }
    
    private NativeMenuItem BuildPluginMenuItem(IPlugin plugin, PluginManager manager, AppSettings settings)
    {
        var status = manager.GetStatus(plugin);
        string label = plugin.Name + status switch
        {
            PluginChangeStatus.New => " (NEW)",
            PluginChangeStatus.Updated => " (UPDATED)",
            _ => ""
        };

        var menuItem = new NativeMenuItem(label)
        {
            ToggleType = NativeMenuItemToggleType.CheckBox,
            IsChecked = manager.IsEnabled(plugin)
        };

        menuItem.Command = new RelayCommand(o =>
        {
            if (o is not NativeMenuItem item) return;
            
            bool shouldBeEnabled = !manager.IsEnabled(plugin);
            if (shouldBeEnabled)
            {
                manager.EnablePlugin(plugin);
            }
            else
            {
                manager.DisablePlugin(plugin);
            }

            item.IsChecked = manager.IsEnabled(plugin);
            settings.PluginEnabled[plugin.Name] = item.IsChecked;
            SettingsManager.Save();
            WorkspaceProfiles.UpdatePlugin(settings.ActiveProfile, plugin.Name, item.IsChecked);
        });
        menuItem.CommandParameter = menuItem;

        return menuItem;
    }

    private NativeMenuItem BuildVolatileScriptMenuItem(string title, FilePickerFileType filter, Action<string> scriptRunner)
    {
        var menuItem = new NativeMenuItem(title);
        menuItem.Click += async (_, _) =>
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null) return;

            var start = await DialogHelper.GetDefaultStartLocationAsync(desktop.MainWindow.StorageProvider);
            var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new[] { filter },
                SuggestedStartLocation = start
            });

            if (files.FirstOrDefault() is { } file)
            {
                try
                {
                    await using var stream = await file.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    var code = await reader.ReadToEndAsync();
                    scriptRunner(code);
                }
                catch (Exception ex) { Logger.Log($"Failed to run volatile script {file.Name}: {ex.Message}"); }
            }
        };
        return menuItem;
    }

    private static WindowIcon CreateTrayIcon()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                var icon = ExtractIconFromDll(Path.Combine(systemDir, "imageres.dll"), 25) ??
                             ExtractIconFromDll(Path.Combine(systemDir, "shell32.dll"), 20) ??
                             ExtractIconFromDll(Path.Combine(systemDir, "shell32.dll"), 8);
                if (icon != null)
                {
                    using var stream = new MemoryStream();
                    #pragma warning disable CA1416
                    icon.Save(stream);
                    #pragma warning restore CA1416
                    stream.Position = 0;
                    return new WindowIcon(stream);
                }
            }
            catch (Exception ex) { Logger.Log($"Failed to extract system icon: {ex.Message}"); }
        }
        var bytes = Convert.FromBase64String(TrayIconBase64);
        return new WindowIcon(new MemoryStream(bytes));
    }

    [SupportedOSPlatform("windows")]
    private static Icon? ExtractIconFromDll(string path, int index)
    {
        IntPtr hIcon = ExtractIcon(IntPtr.Zero, path, index);
        if (hIcon == IntPtr.Zero) return null;
        try
        {
            return (Icon)Icon.FromHandle(hIcon).Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);
    
    #endregion
}