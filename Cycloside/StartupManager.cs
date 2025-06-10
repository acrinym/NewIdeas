using System;
using System.IO;
using Microsoft.Win32;

namespace Cycloside;

public static class StartupManager
{
    private static string ExecutablePath => System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
    private static string LinuxAutostartDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "autostart");

    public static bool IsEnabled()
    {
        if (OperatingSystem.IsWindows())
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("Cycloside") != null;
        }
        else if (OperatingSystem.IsLinux())
        {
            return File.Exists(Path.Combine(LinuxAutostartDir, "cycloside.desktop"));
        }
        return false;
    }

    public static void Enable()
    {
        if (OperatingSystem.IsWindows())
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.SetValue("Cycloside", $"\"{ExecutablePath}\"");
        }
        else if (OperatingSystem.IsLinux())
        {
            Directory.CreateDirectory(LinuxAutostartDir);
            var path = Path.Combine(LinuxAutostartDir, "cycloside.desktop");
            File.WriteAllText(path, "[Desktop Entry]\nType=Application\nExec=\"" + ExecutablePath + "\"\nHidden=false\nNoDisplay=false\nX-GNOME-Autostart-enabled=true\nName=Cycloside\n");
        }
    }

    public static void Disable()
    {
        if (OperatingSystem.IsWindows())
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.DeleteValue("Cycloside", false);
        }
        else if (OperatingSystem.IsLinux())
        {
            var path = Path.Combine(LinuxAutostartDir, "cycloside.desktop");
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
