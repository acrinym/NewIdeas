using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cycloside.Plugins;
using Cycloside.Plugins.BuiltIn;
using System;
using System.IO;

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
            TrayIcon? trayIcon = null;
            var manager = new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"), msg => Logger.Log(msg));
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
            var settingsItem = new NativeMenuItem("Settings");
            settingsItem.Click += (_, _) =>
            {
                var win = new MainWindow();
                win.Show();
            };

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

            var openPluginFolderItem = new NativeMenuItem("Open Plugins Folder");
            openPluginFolderItem.Click += (_, _) =>
            {
                var path = manager.PluginDirectory;
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true }); } catch { }
            };

            var exitItem = new NativeMenuItem("Exit");
            exitItem.Click += (_, _) =>
            {
                manager.StopAll();
                desktop.Shutdown();
            };

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
