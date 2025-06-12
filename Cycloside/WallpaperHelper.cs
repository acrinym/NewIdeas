using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cycloside;

public static class WallpaperHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    public static void SetWallpaper(string path)
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
            else if (OperatingSystem.IsMacOS())
            {
                try
                {
                    Process.Start("osascript", $"-e 'tell application \"System Events\" to set picture of every desktop to \"{path}\"'");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Mac wallpaper command failed: {ex.Message}");
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                var desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ??
                               Environment.GetEnvironmentVariable("DESKTOP_SESSION") ?? string.Empty;
                var lowered = desktop.ToLowerInvariant();
                var recognized = true;
                try
                {
                    if (lowered.Contains("kde"))
                    {
                        var script = "var Desktops = desktops();for (i=0;i<Desktops.length;i++){d=Desktops[i];d.wallpaperPlugin='org.kde.image';d.currentConfigGroup=['Wallpaper','org.kde.image','General'];d.writeConfig('Image','file://" + path + "');}";
                        Process.Start("qdbus", $"org.kde.plasmashell /PlasmaShell org.kde.PlasmaShell.evaluateScript \"{script}\"");
                    }
                    else if (lowered.Contains("gnome") || lowered.Contains("unity") || lowered.Contains("cinnamon"))
                    {
                        Process.Start("gsettings", $"set org.gnome.desktop.background picture-uri file://{path}");
                    }
                    else if (lowered.Contains("xfce"))
                    {
                        Process.Start("xfconf-query", $"-c xfce4-desktop -p /backdrop/screen0/monitor0/image-path -s {path}");
                    }
                    else if (lowered.Contains("lxde"))
                    {
                        Process.Start("pcmanfm", $"--set-wallpaper={path}");
                    }
                    else
                    {
                        recognized = false;
                        Process.Start("feh", $"--bg-scale {path}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Linux wallpaper command failed: {ex.Message}");
                }
                if (!recognized)
                {
                    Logger.Log($"Unsupported desktop '{desktop}', used feh fallback");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Wallpaper set failed: {ex.Message}");
        }
    }
}
