using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Cycloside.Plugins;
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
            var manager = new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"));
            manager.LoadPlugins();
            manager.StartWatching();

            var iconData = Convert.FromBase64String(TrayIconBase64);
            var trayIcon = new TrayIcon
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
            var exitItem = new NativeMenuItem("Exit");
            exitItem.Click += (_, _) =>
            {
                manager.StopAll();
                desktop.Shutdown();
            };

            menu.Items.Add(settingsItem);
            menu.Items.Add(new NativeMenuItemSeparator());
            menu.Items.Add(exitItem);

            trayIcon.Menu = menu;
            trayIcon.IsVisible = true;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
