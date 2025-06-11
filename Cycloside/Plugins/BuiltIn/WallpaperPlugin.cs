using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Cycloside.Plugins.BuiltIn;

public class WallpaperPlugin : IPlugin
{
    private Window? _window;

    public string Name => "Wallpaper Changer";
    public string Description => "Change desktop wallpaper";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        var button = new Button { Content = "Select Wallpaper" };
        button.Click += async (_, _) =>
        {
            var dlg = new OpenFileDialog();
            dlg.Filters.Add(new FileDialogFilter { Name = "Images", Extensions = { "jpg", "png", "bmp" } });
            var files = await dlg.ShowAsync(_window);
            if (files is { Length: > 0 })
                SetWallpaper(files[0]);
        };
        _window = new Window
        {
            Title = "Wallpaper",
            Width = 200,
            Height = 80,
            Content = button
        };
        _window.Show();
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
    }

    private void SetWallpaper(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                const int SPI_SETDESKWALLPAPER = 20;
                const int SPIF_UPDATEINIFILE = 1;
                const int SPIF_SENDWININICHANGE = 2;
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("gsettings", $"set org.gnome.desktop.background picture-uri file://{path}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Wallpaper change failed: {ex.Message}");
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
}
