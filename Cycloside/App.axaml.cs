using System.Threading.Tasks;
            var volatileManager = new VolatilePluginManager();

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cycloside.Plugins;
using Cycloside.Plugins.BuiltIn;
using System;
using System.IO;

namespace Cycloside
{
    public partial class App : Application
    {
        private const string TrayIconBase64 = "AAABAAEAEBACAAEAAQCwAAAAFgAAACgAAAAQAAAAIAAAAAEAAQAAAAAAQAAAAAAAAAAAAAAAAgAAAAIAAAAAAP8A////AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

            var settingsMenu = new NativeMenuItem("Settings") { Menu = new NativeMenu() };
            var pluginManagerItem = new NativeMenuItem("Plugin Manager...");
            pluginManagerItem.Click += (_, _) =>
                var win = new PluginSettingsWindow(manager);
            var generatePluginItem = new NativeMenuItem("Generate New Plugin...");
            generatePluginItem.Click += (_, _) =>
            {
                var win = new PluginDevWizard();
                win.Show();
            };
            settingsMenu.Menu!.Items.Add(pluginManagerItem);
            settingsMenu.Menu.Items.Add(generatePluginItem);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var settings = SettingsManager.Settings;
                TrayIcon? trayIcon = null;
                var manager = new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"), msg => Logger.Log(msg));

                // Load plugins
                manager.LoadPlugins();
                manager.StartWatching();
                manager.AddPlugin(new DateTimeOverlayPlugin());
                manager.AddPlugin(new MP3PlayerPlugin());
                manager.AddPlugin(new MacroPlugin());

                var iconData = Convert.FromBase64String(TrayIconBase64);
                trayIcon = new TrayIcon
                {
                    Icon = new WindowIcon(new MemoryStream(iconData))
                };

                var menu = new NativeMenu();

                // Settings Window
                var settingsItem = new NativeMenuItem("Settings");
                settingsItem.Click += (_, _) =>
                {
                    var win = new MainWindow();
                    win.Show();
                };

                // Launch at Startup
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
            var volatileMenu = new NativeMenuItem("Volatile") { Menu = new NativeMenu() };
            var luaItem = new NativeMenuItem("Run Lua Script...");
            luaItem.Click += async (_, _) =>
            {
                var dlg = new OpenFileDialog();
                dlg.Filters.Add(new FileDialogFilter { Name = "Lua", Extensions = { "lua" } });
                var files = await dlg.ShowAsync(new Window());
                if (files != null && files.Length > 0 && File.Exists(files[0]))
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
                if (files != null && files.Length > 0 && File.Exists(files[0]))
                {
                    var code = await File.ReadAllTextAsync(files[0]);
                    volatileManager.RunCSharp(code);
                }
            };
            volatileMenu.Menu!.Items.Add(luaItem);
            volatileMenu.Menu.Items.Add(csItem);

            menu.Items.Add(volatileMenu);
                    }
                    SettingsManager.Save();
                    autostartItem.IsChecked = settings.LaunchAtStartup;
                };

                // Plugins submenu
                var pluginsMenu = new NativeMenuItem("Plugins") { Menu = new NativeMenu() };
                foreach (var p in manager.Plugins)
                {
                    var item = new NativeMenuItem(p.Name)
                    {
                        ToggleType = NativeMenuItemToggleType.CheckBox,
                        IsChecked = settings.PluginEnabled.TryGetValue(p.Name, out var en) ? en : true
            menu.Items.Add(settingsMenu);

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

                    // Ensure enabled state matches settings
                    if (item.IsChecked && !manager.IsEnabled(p))
                        manager.EnablePlugin(p);
                    else if (!item.IsChecked && manager.IsEnabled(p))
                        manager.DisablePlugin(p);
                }

                // Open Plugin Folder
                var openPluginFolderItem = new NativeMenuItem("Open Plugins Folder");
                openPluginFolderItem.Click += (_, _) =>
                {
                    var path = manager.PluginDirectory;
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = path,
                            UseShellExecute = true
                        });
                    }
                    catch { }
                };

                // Exit
                var exitItem = new NativeMenuItem("Exit");
                exitItem.Click += (_, _) =>
                {
                    manager.StopAll();
                    desktop.Shutdown();
                };

                // Assemble the tray menu
                menu.Items.Add(settingsItem);
                menu.Items.Add(new NativeMenuItemSeparator());
                menu.Items.Add(autostartItem);
                menu.Items.Add(new NativeMenuItemSeparator());
                menu.Items.Add(pluginsMenu);
                menu.Items.Add(openPluginFolderItem);
                menu.Items.Add(new NativeMenuItemSeparator());
                menu.Items.Add(exitItem);

                trayIcon.Menu = menu;
                trayIcon.IsVisible = true;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
