using System;

namespace Cycloside.Hotkeys;

/// <summary>
/// Detects the runtime OS and creates the appropriate global hotkey manager.
/// </summary>
internal static class GlobalHotkeyManagerFactory
{
    public static IGlobalHotkeyManager Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsGlobalHotkeyManager();
        if (OperatingSystem.IsLinux())
            return new LinuxGlobalHotkeyManager();
        if (OperatingSystem.IsMacOS())
            return new MacGlobalHotkeyManager();

        return new StubHotkeyManager();
    }
}
