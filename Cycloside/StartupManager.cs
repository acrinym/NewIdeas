using System;
using System.IO;
using Microsoft.Win32;

namespace Cycloside;

public static class StartupManager
{
    private static string ExecutablePath => System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
    private static string LinuxAutostartDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "autostart");
    private static string MacPlistDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/LaunchAgents");
    private static string MacPlistPath => Path.Combine(MacPlistDir, "com.cycloside.app.plist");

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
        else if (OperatingSystem.IsMacOS())
        {
            return File.Exists(MacPlistPath);
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
        else if (OperatingSystem.IsMacOS())
        {
            Directory.CreateDirectory(MacPlistDir);
            var plist = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                        "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
                        "<plist version=\"1.0\"><dict><key>Label</key><string>com.cycloside.app</string><key>ProgramArguments</key><array><string>" + ExecutablePath + "</string></array><key>RunAtLoad</key><true/></dict></plist>";
            File.WriteAllText(MacPlistPath, plist);
            try
            {
                System.Diagnostics.Process.Start("launchctl", $"load -w {MacPlistPath}");
            }
            catch { }
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
        else if (OperatingSystem.IsMacOS())
        {
            try
            {
                System.Diagnostics.Process.Start("launchctl", $"unload -w {MacPlistPath}");
            }
            catch { }
            if (File.Exists(MacPlistPath))
                File.Delete(MacPlistPath);
        }
    }
}
