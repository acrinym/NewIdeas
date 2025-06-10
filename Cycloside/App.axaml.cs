using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cycloside.Plugins;
using Cycloside.Plugins.BuiltIn;
using System;
using System.IO;
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
            var manager = new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"), msg => Logger.Log(msg));
            var volatileManager = new VolatilePluginManager();

            manager.LoadPlugins();
            manager.StartWatching();
            manager.AddPlugin(new DateTimeOverlayPlugin());
            manager.AddPlugin(new MP3PlayerPlugin());
            manager.AddPlugin(new MacroPlugin());

            var iconData = Convert.FromBase64String(TrayIconBase64);
            var trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(new MemoryStream(iconData))
            };

            var menu = new NativeMenu();

            // Settings submenu
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

            settingsMenu.Menu!.Items.Add(pluginManagerItem);
            settingsMenu.Menu.Items.Add(generatePluginItem);

            // Autostart toggle
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

            // Plugin toggle submenu
            var pluginsMenu = new NativeMenuItem("Plugins") { Menu = new NativeMenu() };
            foreach (var p in manager.Plugins)
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

            // Volatile script submenu
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

            // Open plugin folder
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

            // Exit app
            var exitItem = new NativeMenuItem("Exit");
            exitItem.Click += (_, _) =>
            {
                manager.StopAll();
                desktop.Shutdown();
            };

            // Build the tray menu
            menu.Items.Add(settingsMenu);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(autostartItem);
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
