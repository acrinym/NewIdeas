using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cycloside.Plugins;
using Cycloside.Plugins.BuiltIn;
using Avalonia.Input;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside;

public partial class App : Application
{
    private const string TrayIconBase64 = "AAABAAEAEBACAAEAAQCwAAAAFgAAACgAAAAQAAAAIAAAAAEAAQAAAAAAQAAAAAAAAAAAAAAAAgAAAAIAAAAAAP8A////AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settings = SettingsManager.Settings;
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

            var remoteServer = new RemoteApiServer(manager, settings.RemoteApiToken);
            remoteServer.Start();


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

            var iconData = Convert.FromBase64String(TrayIconBase64);
            var trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(new MemoryStream(iconData))
            };

            var menu = new NativeMenu();

            // ⚙ Settings Menu
            var settingsMenu = new NativeMenuItem("Settings") { Menu = new NativeMenu() };

            var pluginManagerItem = new NativeMenuItem("Plugin Manager...");
            pluginManagerItem.Click += (_, _) =>
            {
                var win = new PluginSettingsWindow(manager);
                win.Show();
            };

            var generatePluginItem = new NativeMenuItem("Generate New Plugin...");
            generatePluginItem.Click += (_, _) =>
            {
                var win = new PluginDevWizard();
                win.Show();
            };

            var themeSettingsItem = new NativeMenuItem("Theme Settings...");
            themeSettingsItem.Click += (_, _) =>
            {
                var win = new ThemeSettingsWindow();
                win.Show();
            };

            var themeEditorItem = new NativeMenuItem("Skin/Theme Editor...");
            themeEditorItem.Click += (_, _) =>
            {
                var win = new SkinThemeEditorWindow();
                win.Show();
            };

            settingsMenu.Menu!.Items.Add(pluginManagerItem);
            settingsMenu.Menu.Items.Add(generatePluginItem);
            settingsMenu.Menu.Items.Add(themeSettingsItem);
            settingsMenu.Menu.Items.Add(themeEditorItem);

            var profileItem = new NativeMenuItem("Workspace Profiles...");
            profileItem.Click += (_, _) =>
            {
                var win = new ProfileEditorWindow(manager);
                win.Show();
            };
            settingsMenu.Menu.Items.Add(profileItem);


            var runtimeItem = new NativeMenuItem("Runtime Settings...");
            runtimeItem.Click += (_, _) =>
            {
                var win = new RuntimeSettingsWindow(manager);
                win.Show();
            };
            settingsMenu.Menu.Items.Add(runtimeItem);

            // 🪄 Autostart Toggle
            var autostartItem = new NativeMenuItem("Launch at Startup")
            {
                ToggleType = NativeMenuItemToggleType.CheckBox,
                IsChecked = settings.LaunchAtStartup
            };
            autostartItem.Click += (_, _) =>
            {
                if (autostartItem.IsChecked)
                {
                    StartupManager.Disable();
                    settings.LaunchAtStartup = false;
                }
                else
                {
                    StartupManager.Enable();
                    settings.LaunchAtStartup = true;
                }
                SettingsManager.Save();
                autostartItem.IsChecked = settings.LaunchAtStartup;
            };

            // 🎨 Theme Menu
            var themeMenu = new NativeMenuItem("Themes") { Menu = new NativeMenu() };
            var themeNames = new[] { "MintGreen", "Matrix", "Orange", "ConsoleGreen", "MonochromeOrange", "DeepBlue" };
            foreach (var t in themeNames)
            {
                var themeItem = new NativeMenuItem(t)
                {
                    ToggleType = NativeMenuItemToggleType.Radio,
                    IsChecked = settings.Theme == t
                };
                themeItem.Click += (_, _) =>
                {
                    ThemeManager.ApplyTheme(this, t);
                    settings.Theme = t;
                    SettingsManager.Save();
                    foreach (var i in themeMenu.Menu!.Items.OfType<NativeMenuItem>())
                        i.IsChecked = i == themeItem;
                };
                themeMenu.Menu!.Items.Add(themeItem);
            }

            // 🔌 Plugins Menu
            var pluginsMenu = new NativeMenuItem("Plugins") { Menu = new NativeMenu() };

            var newMenu = new NativeMenuItem("New/Updated") { Menu = new NativeMenu() };
            foreach (var p in manager.Plugins.Where(p => manager.GetStatus(p) != Plugins.PluginChangeStatus.None))
            {
                var tag = manager.GetStatus(p) == Plugins.PluginChangeStatus.New ? " (NEW)" : " (UPDATED)";
                var item = new NativeMenuItem(p.Name + tag)
                {
                    ToggleType = NativeMenuItemToggleType.CheckBox,
                    IsChecked = settings.PluginEnabled.TryGetValue(p.Name, out var en) ? en : true
                };
                item.Click += (_, _) =>
                {
                    if (manager.IsEnabled(p))
                        manager.DisablePlugin(p);
                    else
                        manager.EnablePlugin(p);

                    item.IsChecked = manager.IsEnabled(p);
                    settings.PluginEnabled[p.Name] = item.IsChecked;
                    SettingsManager.Save();
                };
                newMenu.Menu!.Items.Add(item);

                if (item.IsChecked && !manager.IsEnabled(p))
                    manager.EnablePlugin(p);
                else if (!item.IsChecked && manager.IsEnabled(p))
                    manager.DisablePlugin(p);
            }

            if (newMenu.Menu!.Items.Count > 0)
            {
                pluginsMenu.Menu!.Items.Add(newMenu);
                pluginsMenu.Menu!.Items.Add(new NativeMenuItemSeparator());
            }

            foreach (var p in manager.Plugins.Where(p => manager.GetStatus(p) == Plugins.PluginChangeStatus.None))
            {
                var item = new NativeMenuItem(p.Name)
                {
                    ToggleType = NativeMenuItemToggleType.CheckBox,
                    IsChecked = settings.PluginEnabled.TryGetValue(p.Name, out var en) ? en : true
                };
                item.Click += (_, _) =>
                {
                    if (manager.IsEnabled(p))
                        manager.DisablePlugin(p);
                    else
                        manager.EnablePlugin(p);

                    item.IsChecked = manager.IsEnabled(p);
                    settings.PluginEnabled[p.Name] = item.IsChecked;
                    SettingsManager.Save();
                };
                pluginsMenu.Menu!.Items.Add(item);

                if (item.IsChecked && !manager.IsEnabled(p))
                    manager.EnablePlugin(p);
                else if (!item.IsChecked && manager.IsEnabled(p))
                    manager.DisablePlugin(p);
            }

            // 🧨 Volatile Scripts
            var volatileMenu = new NativeMenuItem("Volatile") { Menu = new NativeMenu() };

            var luaItem = new NativeMenuItem("Run Lua Script...");
            luaItem.Click += async (_, _) =>
            {
                var dlg = new OpenFileDialog();
                dlg.Filters.Add(new FileDialogFilter { Name = "Lua", Extensions = { "lua" } });
                var files = await dlg.ShowAsync(new Window());
                if (files is { Length: > 0 } && File.Exists(files[0]))
                {
                    var code = await File.ReadAllTextAsync(files[0]);
                    volatileManager.RunLua(code);
                }
            };

            var csItem = new NativeMenuItem("Run C# Script...");
            csItem.Click += async (_, _) =>
            {
                var dlg = new OpenFileDialog();
                dlg.Filters.Add(new FileDialogFilter { Name = "C#", Extensions = { "csx" } });
                var files = await dlg.ShowAsync(new Window());
                if (files is { Length: > 0 } && File.Exists(files[0]))
                {
                    var code = await File.ReadAllTextAsync(files[0]);
                    volatileManager.RunCSharp(code);
                }
            };

            volatileMenu.Menu!.Items.Add(luaItem);
            volatileMenu.Menu.Items.Add(csItem);

            var inlineItem = new NativeMenuItem("Run Inline...");
            inlineItem.Click += (_, _) =>
            {
                var win = new VolatileRunnerWindow(volatileManager);
                win.Show();
            };
            volatileMenu.Menu.Items.Add(inlineItem);

            // 📁 Open Plugins Folder
            var openPluginFolderItem = new NativeMenuItem("Open Plugins Folder");
            openPluginFolderItem.Click += (_, _) =>
            {
                try
                {
                    var path = manager.PluginDirectory;
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
                catch { }
            };

            // ❌ Exit
            var exitItem = new NativeMenuItem("Exit");
            exitItem.Click += (_, _) =>
            {
                manager.StopAll();
                remoteServer.Stop();
                HotkeyManager.UnregisterAll();
                desktop.Shutdown();
            };

            // 📋 Final Tray Menu Assembly
            menu.Items.Add(settingsMenu);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(autostartItem);
            menu.Items.Add(themeMenu);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(pluginsMenu);
            menu.Items.Add(volatileMenu);
            menu.Items.Add(openPluginFolderItem);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(exitItem);

            trayIcon.Menu = menu;
            trayIcon.IsVisible = true;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
