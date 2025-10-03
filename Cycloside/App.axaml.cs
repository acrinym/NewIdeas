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
using CommunityToolkit.Mvvm.Input;
using ServicesRelayCommand = Cycloside.Services.RelayCommand;

namespace Cycloside;

public partial class App : Application
{
    private const string TrayIconBase64 = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAGElEQVR4nGNkaGAgCTCRpnxUw6iGoaQBALsfAKDg6Y6zAAAAAElFTkSuQmCC";
    private RemoteApiServer? _remoteServer;
    private PluginManager? _pluginManager;
    private TrayIcon? _trayIcon; // Keep a reference to the tray icon
    private MainWindow? _mainWindow;
    private MainWindowViewModel? _mainViewModel;

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
        ThemeManager.InitializeFromSettings();

        if (settings.FirstRun)
        {
            var wiz = new WizardWindow();
            wiz.Closed += (_, _) =>
            {
                _mainWindow = CreateMainWindow(SettingsManager.Settings);
                desktop.MainWindow = _mainWindow;
                _mainWindow.Show();
            };
            wiz.Show();
        }
        else
        {
            _mainWindow = CreateMainWindow(settings);
            desktop.MainWindow = _mainWindow;
            _mainWindow.Show();
        }

        desktop.Exit += (_, _) =>
        {
            SaveSessionState();
            // Ensure logs are flushed at shutdown
            Services.Logger.Shutdown();
        };

        base.OnFrameworkInitializationCompleted();
    }

    private MainWindow CreateMainWindow(AppSettings settings)
    {
        _pluginManager = new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"), Services.NotificationCenter.Notify);

        // Subscribe to plugin reloads to update the UI when plugins are refreshed.
        _pluginManager.PluginsReloaded += OnPluginsReloaded;

        var volatileManager = new VolatilePluginManager();

        LoadAllPlugins(_pluginManager, settings);
        _pluginManager.StartWatching();

        var viewModel = new MainWindowViewModel(_pluginManager.Plugins);
        _mainViewModel = viewModel;
        var mainWindow = new MainWindow(_pluginManager)
        {
            DataContext = viewModel
        };
        _mainWindow = mainWindow;


        viewModel.ExitCommand = new ServicesRelayCommand(() => Shutdown());

        // Toggle plugin enablement from the main window.
        viewModel.StartPluginCommand = new ServicesRelayCommand(pluginObj =>
        {
            if (pluginObj is not IPlugin plugin || _pluginManager is null) return;

            if (plugin is IWorkspaceItem workspace)
            {
                var existing = viewModel.WorkspaceItems.FirstOrDefault(w => w.Plugin == plugin);
                bool enable = existing is null;
                if (enable)
                {
                    workspace.UseWorkspace = true;
                    _pluginManager.EnablePlugin(plugin);
                    var view = workspace.BuildWorkspaceView();
                    var vm = new WorkspaceItemViewModel(plugin.Name, view, plugin, DetachWorkspaceItem);
                    viewModel.WorkspaceItems.Add(vm);
                    viewModel.SelectedWorkspaceItem = vm;
                }
                else
                {
                    workspace.UseWorkspace = false;
                    _pluginManager.DisablePlugin(plugin);
                    viewModel.WorkspaceItems.Remove(existing!);
                }
                SettingsManager.Settings.PluginEnabled[plugin.Name] = enable;
                SettingsManager.Save();
                WorkspaceProfiles.UpdatePlugin(settings.ActiveProfile, plugin.Name, enable);
            }
            else
            {
                bool shouldBeEnabled = !_pluginManager.IsEnabled(plugin);
                if (shouldBeEnabled)
                {
                    _pluginManager.EnablePlugin(plugin);
                }
                else
                {
                    _pluginManager.DisablePlugin(plugin);
                }

                SettingsManager.Settings.PluginEnabled[plugin.Name] = shouldBeEnabled;
                SettingsManager.Save();
                WorkspaceProfiles.UpdatePlugin(settings.ActiveProfile, plugin.Name, shouldBeEnabled);
            }
        });

        // Start or stop a plugin in its own window, even if it supports the workspace.
        viewModel.StartPluginWindowCommand = new ServicesRelayCommand(pluginObj =>
        {
            if (pluginObj is not IPlugin plugin || _pluginManager is null) return;

            var existing = viewModel.WorkspaceItems.FirstOrDefault(w => w.Plugin == plugin);
            bool shouldBeEnabled = !_pluginManager.IsEnabled(plugin);

            if (shouldBeEnabled)
            {
                if (plugin is IWorkspaceItem ws) ws.UseWorkspace = false;
                if (existing != null) viewModel.WorkspaceItems.Remove(existing);
                _pluginManager.EnablePlugin(plugin);
            }
            else
            {
                if (plugin is IWorkspaceItem ws) ws.UseWorkspace = false;
                _pluginManager.DisablePlugin(plugin);
                if (existing != null) viewModel.WorkspaceItems.Remove(existing);
            }

            SettingsManager.Settings.PluginEnabled[plugin.Name] = shouldBeEnabled;
            SettingsManager.Save();
            WorkspaceProfiles.UpdatePlugin(settings.ActiveProfile, plugin.Name, shouldBeEnabled);
        });

        _remoteServer = new RemoteApiServer(_pluginManager, settings.RemoteApiToken);
        _remoteServer.Start();
        WorkspaceProfiles.Apply(settings.ActiveProfile, _pluginManager);
        RestoreSessionState(viewModel);
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

        // Post-startup: try upgrading the tray icon from a fast placeholder to a system icon on Windows
        if (OperatingSystem.IsWindows())
        {
            try
            {
                Task.Run(() =>
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
                            var winIcon = new WindowIcon(stream);
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                if (_trayIcon != null)
                                    _trayIcon.Icon = winIcon;
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Tray icon upgrade failed: {ex.Message}");
                    }
                });
            }
            catch (Exception ex) { Logger.Log($"Tray icon upgrade scheduling failed: {ex.Message}"); }
        }

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
        TryAdd(() => new ManagedVisHostPlugin());
        TryAdd(() => new MacroPlugin());
        TryAdd(() => new TextEditorPlugin());
        TryAdd(() => new WallpaperPlugin());
        TryAdd(() => new ClipboardManagerPlugin());
        TryAdd(() => new HackersParadisePlugin());
        TryAdd(() => new HackerTerminalPlugin());
        TryAdd(() => new CharacterMapPlugin());
        TryAdd(() => new FileWatcherPlugin());
        TryAdd(() => new TaskSchedulerPlugin());
        TryAdd(() => new DiskUsagePlugin());
        TryAdd(() => new TerminalPlugin());
        TryAdd(() => new LogViewerPlugin());
        TryAdd(() => new NotificationCenterPlugin());
        TryAdd(() => new EnvironmentEditorPlugin());
        TryAdd(() => new JezzballPlugin());
        TryAdd(() => new QuickLauncherPlugin(manager));
        TryAdd(() => new WidgetHostPlugin(manager));
        // Switched from legacy Winamp-based visual host to the fully managed visualizer host.
        // The managed host renders with Avalonia, avoids native DLLs, and integrates directly
        // with our AudioData bus. This removes the dependency on vis_avs.dll and related C++ shims.
        TryAdd(() => new ManagedVisHostPlugin());
        TryAdd(() => new QBasicRetroIDEPlugin());
        // TryAdd(() => new ScreenSaverPlugin()); // Disabled for stability
    }

    private void RegisterHotkeys(PluginManager manager)
    {
        foreach (var kv in SettingsManager.Settings.Hotkeys)
        {
            KeyGesture gesture;
            try { gesture = KeyGesture.Parse(kv.Value); }
            catch { continue; }

            // Look for a plugin whose name matches the hotkey key (ignoring spaces)
            var plugin = manager.Plugins.FirstOrDefault(p =>
                string.Equals(p.Name.Replace(" ", string.Empty), kv.Key,
                    StringComparison.OrdinalIgnoreCase));

            if (plugin != null)
            {
                HotkeyManager.Register(gesture, () =>
                {
                    if (manager.IsEnabled(plugin)) manager.DisablePlugin(plugin);
                    else manager.EnablePlugin(plugin);
                });
            }
            // Additional actions can be handled here in the future
        }
    }

    private void Shutdown()
    {
        SaveSessionState();
        _pluginManager?.StopAll();
        _remoteServer?.Stop();
        HotkeyManager.UnregisterAll();
        Logger.Shutdown();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime appLifetime)
        {
            appLifetime.Shutdown();
        }
    }

    private void DetachWorkspaceItem(WorkspaceItemViewModel item)
    {
        if (_mainViewModel == null) return;
        if (item.Plugin is IWorkspaceItem workspace)
        {
            workspace.UseWorkspace = false;
            item.Plugin.Start();
        }
        _mainViewModel.WorkspaceItems.Remove(item);
    }

    private void SaveSessionState()
    {
        if (_mainViewModel == null) return;
        var names = string.Join(';', _mainViewModel.WorkspaceItems.Select(w => w.Plugin.Name));
        StateManager.Set("LastWorkspace", names);
    }

    private void RestoreSessionState(MainWindowViewModel vm)
    {
        if (_pluginManager == null) return;
        var data = StateManager.Get("LastWorkspace");
        if (string.IsNullOrWhiteSpace(data)) return;
        foreach (var name in data.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var plugin = _pluginManager.Plugins.FirstOrDefault(p => p.Name == name);
            if (plugin is IWorkspaceItem ws)
            {
                ws.UseWorkspace = true;
                _pluginManager.EnablePlugin(plugin);
                var view = ws.BuildWorkspaceView();
                var vmItem = new WorkspaceItemViewModel(plugin.Name, view, plugin, DetachWorkspaceItem);
                vm.WorkspaceItems.Add(vmItem);
            }
            else if (plugin != null)
            {
                _pluginManager.EnablePlugin(plugin);
            }
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
                    new NativeMenuItem("Control Panel...") { Command = new ServicesRelayCommand(() => new ControlPanelWindow(manager).Show()) },
                    new NativeMenuItem("Plugin Manager...") { Command = new ServicesRelayCommand(() => new PluginSettingsWindow(manager).Show()) },
                    new NativeMenuItem("Theme Settings...") { Command = new ServicesRelayCommand(() => new ThemeSettingsWindow(manager).Show()) },
                }}},
                new NativeMenuItemSeparator(),
                logsMenu, // **Add the new Logs menu here**
                new NativeMenuItemSeparator(),
                BuildProfilesMenu(manager),
                new NativeMenuItemSeparator(),
                pluginsMenu,
                volatileMenu,
                new NativeMenuItem("Open Plugins Folder") { Command = new ServicesRelayCommand(() => {
                    try { Process.Start(new ProcessStartInfo { FileName = manager.PluginDirectory, UseShellExecute = true }); }
                    catch (Exception ex) { Logger.Log($"Failed to open plugin folder: {ex.Message}"); }
                })},
                new NativeMenuItemSeparator(),
                new NativeMenuItem("Exit") { Command = new ServicesRelayCommand(() => Shutdown()) }
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

        menuItem.Command = new ServicesRelayCommand(o =>
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

    private NativeMenuItem BuildProfilesMenu(PluginManager manager)
    {
        var menu = new NativeMenuItem("Profiles") { Menu = new NativeMenu() };
        foreach (var name in WorkspaceProfiles.ProfileNames)
        {
            var item = new NativeMenuItem(name);
            item.Click += (_, _) => WorkspaceProfiles.Apply(name, manager);
            menu.Menu!.Items.Add(item);
        }
        return menu;
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
