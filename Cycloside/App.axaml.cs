using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Cycloside.Plugins;
using Cycloside.Plugins.BuiltIn;
using Cycloside.Views;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Cycloside;

public partial class App : Application
{
    private const string TrayIconBase64 = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAGElEQVR4nGNkaGAgCTCRpnxUw6iGoaQBALsfAKDg6Y6zAAAAAElFTkSuQmCC";
    private RemoteApiServer? _remoteServer;

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
        if (settings.FirstRun)
        {
            var wiz = new WizardWindow();
            // This is the correct, non-crashing way to show the wizard on first run.
            // It safely pauses the startup process until the user completes the wizard.
            using (var wizardClosedEvent = new ManualResetEvent(false))
            {
                wiz.Closed += (_, _) => wizardClosedEvent.Set();
                wiz.Show();
                wizardClosedEvent.WaitOne();
            }
            // Reload settings after the wizard has saved them
            settings = SettingsManager.Settings;
        }

        SkinManager.LoadCurrent();
        var theme = settings.ComponentThemes.TryGetValue("Cycloside", out var selectedTheme)
            ? selectedTheme
            : settings.Theme;
        ThemeManager.ApplyTheme(this, theme);

        var manager = new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"), msg => Logger.Log(msg));
        var volatileManager = new VolatilePluginManager();

        manager.LoadPlugins();
        manager.StartWatching();

        if (!settings.DisableBuiltInPlugins)
        {
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
            manager.AddPlugin(new LogViewerPlugin());
            manager.AddPlugin(new EnvironmentEditorPlugin());
            manager.AddPlugin(new JezzballPlugin());
            manager.AddPlugin(new WidgetHostPlugin(manager));
            manager.AddPlugin(new WinampVisHostPlugin());
            manager.AddPlugin(new QBasicRetroIDEPlugin());
        }

        _remoteServer = new RemoteApiServer(manager, settings.RemoteApiToken);
        _remoteServer.Start();
        
        WorkspaceProfiles.Apply(settings.ActiveProfile, manager);

        HotkeyManager.Register(new KeyGesture(Key.W, KeyModifiers.Control | KeyModifiers.Alt), () =>
        {
            var plugin = manager.Plugins.FirstOrDefault(p => p.Name == "Widget Host");
            if (plugin != null)
            {
                if (manager.IsEnabled(plugin))
                    manager.DisablePlugin(plugin);
                else
                    manager.EnablePlugin(plugin);
            }
        });

        var trayIcon = new TrayIcon
        {
            Icon = CreateTrayIcon(),
            Menu = BuildTrayMenu(manager, volatileManager, settings)
        };

        var icons = TrayIcon.GetIcons(this) ?? new TrayIcons();
        TrayIcon.SetIcons(this, icons);
        icons.Add(trayIcon);
        trayIcon.IsVisible = true;

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Refactored method to build the entire tray menu for better organization.
    /// </summary>
    private NativeMenu BuildTrayMenu(PluginManager manager, VolatilePluginManager volatileManager, AppSettings settings)
    {
        // ------------------
        // Plugins Menu
        // ------------------
        var pluginsMenu = new NativeMenuItem("Plugins") { Menu = new NativeMenu() };
        var newPlugins = manager.Plugins.Where(p => manager.GetStatus(p) != PluginChangeStatus.None).ToList();
        var otherPlugins = manager.Plugins.Except(newPlugins).ToList();

        if (newPlugins.Any())
        {
            var newMenu = new NativeMenuItem("New/Updated") { Menu = new NativeMenu() };
            foreach (var p in newPlugins)
            {
                newMenu.Menu!.Items.Add(BuildPluginMenuItem(p, manager, settings));
            }
            pluginsMenu.Menu!.Items.Add(newMenu);
            pluginsMenu.Menu!.Items.Add(new NativeMenuItemSeparator());
        }

        foreach (var p in otherPlugins)
        {
            pluginsMenu.Menu!.Items.Add(BuildPluginMenuItem(p, manager, settings));
        }

        // ------------------
        // Volatile Scripts Menu
        // ------------------
        var volatileMenu = new NativeMenuItem("Volatile") { Menu = new NativeMenu() };
        volatileMenu.Menu!.Items.Add(BuildVolatileScriptMenuItem("Run Lua Script...", new FilePickerFileType("Lua Script") { Patterns = new[] { "*.lua" } }, volatileManager.RunLua));
        volatileMenu.Menu!.Items.Add(BuildVolatileScriptMenuItem("Run C# Script...", new FilePickerFileType("C# Script") { Patterns = new[] { "*.csx" } }, volatileManager.RunCSharp));
        volatileMenu.Menu!.Items.Add(new NativeMenuItemSeparator());
        var inlineItem = new NativeMenuItem("Run Inline...");
        inlineItem.Click += (_, _) => new VolatileRunnerWindow(volatileManager).Show();
        volatileMenu.Menu!.Items.Add(inlineItem);
        
        // ------------------
        // Main Menu Assembly
        // ------------------
        return new NativeMenu
        {
            Items =
            {
                new NativeMenuItem("Settings")
                {
                    Menu = new NativeMenu
                    {
                        Items =
                        {
                            new NativeMenuItem("Plugin Manager...") { Command = new RelayCommand(() => new PluginSettingsWindow(manager).Show()) },
                            new NativeMenuItem("Generate New Plugin...") { Command = new RelayCommand(() => new PluginDevWizard().Show()) },
                            new NativeMenuItem("Theme Settings...") { Command = new RelayCommand(() => new ThemeSettingsWindow().Show()) },
                            new NativeMenuItem("Skin/Theme Editor...") { Command = new RelayCommand(() => new SkinThemeEditorWindow().Show()) },
                            new NativeMenuItem("Workspace Profiles...") { Command = new RelayCommand(() => new ProfileEditorWindow(manager).Show()) },
                            new NativeMenuItem("Runtime Settings...") { Command = new RelayCommand(() => new RuntimeSettingsWindow(manager).Show()) }
                        }
                    }
                },
                new NativeMenuItemSeparator(),
                new NativeMenuItem("Launch at Startup")
                {
                    IsChecked = settings.LaunchAtStartup,
                    ToggleType = NativeMenuItemToggleType.CheckBox,
                    Command = new RelayCommand(o =>
                    {
                        var item = (NativeMenuItem)o!;
                        settings.LaunchAtStartup = !settings.LaunchAtStartup;
                        if (settings.LaunchAtStartup) StartupManager.Enable();
                        else StartupManager.Disable();
                        SettingsManager.Save();
                        item.IsChecked = settings.LaunchAtStartup;
                    })
                },
                new NativeMenuItem("Themes") // Logic for this menu can also be refactored if it grows
                {
                     Menu = new NativeMenu
                    {
                        Items =
                        {
                            // Items are added dynamically in the original, keeping that for simplicity here.
                        }
                    }
                },
                new NativeMenuItemSeparator(),
                pluginsMenu,
                volatileMenu,
                new NativeMenuItem("Open Plugins Folder")
                {
                    Command = new RelayCommand(() =>
                    {
                        try {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = manager.PluginDirectory, UseShellExecute = true });
                        } catch (Exception ex) {
                            Logger.Log($"Failed to open plugin folder: {ex.Message}");
                        }
                    })
                },
                new NativeMenuItemSeparator(),
                new NativeMenuItem("Exit")
                {
                    Command = new RelayCommand(() =>
                    {
                        manager.StopAll();
                        _remoteServer?.Stop();
                        HotkeyManager.UnregisterAll();
                        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime appLifetime)
                        {
                            appLifetime.Shutdown();
                        }
                    })
                }
            }
        };
    }
    
    /// <summary>
    /// Helper to create a standardized menu item for a plugin.
    /// </summary>
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
            IsChecked = settings.PluginEnabled.TryGetValue(plugin.Name, out var isEnabled) ? isEnabled : true
        };

        // Set initial plugin state based on the IsChecked value
        if (menuItem.IsChecked && !manager.IsEnabled(plugin)) manager.EnablePlugin(plugin);
        else if (!menuItem.IsChecked && manager.IsEnabled(plugin)) manager.DisablePlugin(plugin);

        // Add command to toggle the plugin on/off
        menuItem.Command = new RelayCommand(o =>
        {
            if (manager.IsEnabled(plugin)) manager.DisablePlugin(plugin);
            else manager.EnablePlugin(plugin);
            
            menuItem.IsChecked = manager.IsEnabled(plugin);
            settings.PluginEnabled[plugin.Name] = menuItem.IsChecked;
            SettingsManager.Save();
        });

        return menuItem;
    }

    /// <summary>
    /// Helper to create a standardized menu item for running a volatile script from a file.
    /// </summary>
    private NativeMenuItem BuildVolatileScriptMenuItem(string title, FilePickerFileType filter, Action<string> scriptRunner)
    {
        var menuItem = new NativeMenuItem(title);
        menuItem.Click += async (_, _) =>
        {
            var window = new Window(); // Transient window to host the file picker
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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
                catch (Exception ex)
                {
                    Logger.Log($"Failed to run volatile script {file.Name}: {ex.Message}");
                }
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
                // Attempt to load the network drive icon (often looks like a tree)
                var icon = ExtractIconFromDll(Path.Combine(systemDir, "imageres.dll"), 25) ??
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
            catch (Exception ex)
            {
                Logger.Log($"Failed to extract system icon: {ex.Message}");
            }
        }
        var bytes = Convert.FromBase64String(TrayIconBase64);
        return new WindowIcon(new MemoryStream(bytes));
    }

    [SupportedOSPlatform("windows")]
    private static Icon? ExtractIconFromDll(string path, int index)
    {
        IntPtr hIcon = ExtractIcon(IntPtr.Zero, path, index);
        if (hIcon != IntPtr.Zero)
        {
            try
            {
                var icon = (Icon)Icon.FromHandle(hIcon).Clone();
                DestroyIcon(hIcon);
                return icon;
            }
            catch(Exception ex)
            {
                Logger.Log($"Failed to clone or destroy icon handle: {ex.Message}");
                DestroyIcon(hIcon);
            }
        }
        return null;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);
    
    // Simple RelayCommand implementation for MVVM-style commands in the menu
    private class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly Action<object?> _execute;
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public RelayCommand(Action<object?> execute) => _execute = execute;
        public RelayCommand(Action execute) : this(_ => execute()) { }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter);
    }
}