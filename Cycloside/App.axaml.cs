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
        var volatileManager = new VolatilePluginManager();

        LoadAllPlugins(_pluginManager, settings);
        _pluginManager.StartWatching();

        var viewModel = new MainWindowViewModel(_pluginManager.Plugins);
        var mainWindow = new MainWindow(_pluginManager)
        {
            DataContext = viewModel
        };

        viewModel.ExitCommand = new RelayCommand(() => Shutdown());
        viewModel.StartPluginCommand = new RelayCommand(plugin =>
        {
            if (plugin is IPlugin p) _pluginManager.EnablePlugin(p);
        });

        _remoteServer = new RemoteApiServer(_pluginManager, settings.RemoteApiToken);
        _remoteServer.Start();
        WorkspaceProfiles.Apply(settings.ActiveProfile, _pluginManager);
        RegisterHotkeys(_pluginManager);

        var trayIcon = new TrayIcon
        {
            Icon = CreateTrayIcon(),
            ToolTipText = "Cycloside",
            Menu = BuildTrayMenu(_pluginManager, volatileManager, settings)
        };
        var icons = TrayIcon.GetIcons(this) ?? new TrayIcons();
        TrayIcon.SetIcons(this, icons);
        if (!icons.Contains(trayIcon))
        {
            icons.Add(trayIcon);
        }
        trayIcon.IsVisible = true;

        return mainWindow;
    }

    private void LoadAllPlugins(PluginManager manager, AppSettings settings)
    {
        if (settings.DisableBuiltInPlugins) return;
        manager.AddPlugin(new DateTimeOverlayPlugin());
        manager.AddPlugin(new MP3PlayerPlugin());
        manager.AddPlugin(new MacroPlugin());
        manager.AddPlugin(new TextEditorPlugin());
        manager.AddPlugin(new WallpaperPlugin());
        manager.AddPlugin(new ClipboardManagerPlugin());
        manager.AddPlugin(new FileWatcherPlugin());
        manager.AddPlugin(new ProcessMonitorPlugin());
        manager.AddPlugin(new TaskSchedulerPlugin());
        manager.AddPlugin(new DiskUsagePlugin());
        manager.AddPlugin(new TerminalPlugin());
        manager.AddPlugin(new LogViewerPlugin());
        manager.AddPlugin(new EnvironmentEditorPlugin());
        manager.AddPlugin(new JezzballPlugin());
        manager.AddPlugin(new WidgetHostPlugin(manager));
        manager.AddPlugin(new WinampVisHostPlugin());
        manager.AddPlugin(new QBasicRetroIDEPlugin());
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

    // SHUTDOWN FIX: The shutdown sequence is now simplified to prevent incorrect state saving.
    private void Shutdown()
    {
        _pluginManager?.StopAll(); // This now correctly stops plugins without disabling them in settings.
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

        // MP3 PLAYER FIX: Add a direct menu item to launch the MP3 player as a workaround.
        var mp3Item = new NativeMenuItem("MP3 Player");
        mp3Item.Click += (s, e) =>
        {
            var mp3 = manager.Plugins.FirstOrDefault(p => p.Name == "MP3 Player");
            if (mp3 != null) manager.EnablePlugin(mp3);
        };

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
                mp3Item, // Added MP3 Player direct launch
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
            IsChecked = settings.PluginEnabled.TryGetValue(plugin.Name, out var isEnabled) && isEnabled
        };

        // This command now correctly toggles the plugin's state and saves it.
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

            var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new[] { filter }
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
