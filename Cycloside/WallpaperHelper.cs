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
                    var psi = new ProcessStartInfo
                    {
                        FileName = "osascript"
                    };
                    psi.ArgumentList.Add("-e");
                    psi.ArgumentList.Add($"tell application \"System Events\" to set picture of every desktop to POSIX file \"{path}\"");
                    Process.Start(psi);
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
                        var encodedPath = Uri.EscapeDataString(path);
                        var script = $"var Desktops = desktops();for (i=0;i<Desktops.length;i++){{d=Desktops[i];d.wallpaperPlugin='org.kde.image';d.currentConfigGroup=['Wallpaper','org.kde.image','General'];d.writeConfig('Image','file://{encodedPath}');}}";
                        var psi = new ProcessStartInfo { FileName = "qdbus" };
                        psi.ArgumentList.Add("org.kde.plasmashell");
                        psi.ArgumentList.Add("/PlasmaShell");
                        psi.ArgumentList.Add("org.kde.PlasmaShell.evaluateScript");
                        psi.ArgumentList.Add(script);
                        Process.Start(psi);
                    }
                    else if (lowered.Contains("gnome") || lowered.Contains("unity") || lowered.Contains("cinnamon"))
                    {
                        var psi = new ProcessStartInfo { FileName = "gsettings" };
                        psi.ArgumentList.Add("set");
                        psi.ArgumentList.Add("org.gnome.desktop.background");
                        psi.ArgumentList.Add("picture-uri");
                        psi.ArgumentList.Add($"file://{path}");
                        Process.Start(psi);
                    }
                    else if (lowered.Contains("xfce"))
                    {
                        var psi = new ProcessStartInfo { FileName = "xfconf-query" };
                        psi.ArgumentList.Add("-c");
                        psi.ArgumentList.Add("xfce4-desktop");
                        psi.ArgumentList.Add("-p");
                        psi.ArgumentList.Add("/backdrop/screen0/monitor0/image-path");
                        psi.ArgumentList.Add("-s");
                        psi.ArgumentList.Add(path);
                        Process.Start(psi);
                    }
                    else if (lowered.Contains("lxde"))
                    {
                        var psi = new ProcessStartInfo { FileName = "pcmanfm" };
                        psi.ArgumentList.Add($"--set-wallpaper={path}");
                        Process.Start(psi);
                    }
                    else
                    {
                        recognized = false;
                        var psi = new ProcessStartInfo { FileName = "feh" };
                        psi.ArgumentList.Add("--bg-scale");
                        psi.ArgumentList.Add(path);
                        Process.Start(psi);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Linux wallpaper command failed: {ex.Message}");
                }
                if (!recognized)
                    Logger.Log($"Unsupported desktop '{desktop}', used feh fallback");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Wallpaper set failed: {ex.Message}");
        }
    }
}

