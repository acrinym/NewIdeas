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
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("gsettings", $"set org.gnome.desktop.background picture-uri file://{path}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Wallpaper set failed: {ex.Message}");
        }
    }
}
