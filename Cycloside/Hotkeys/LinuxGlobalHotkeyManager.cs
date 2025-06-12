using System;
using Avalonia.Input;

namespace Cycloside.Hotkeys;

/// <summary>
/// Linux global hotkey manager stub. TODO: implement using X11.
/// </summary>
internal sealed class LinuxGlobalHotkeyManager : IGlobalHotkeyManager
{
    public void Register(KeyGesture gesture, Action callback)
    {
        // TODO: Implement global hotkey registration on Linux
        Logger.Log("Linux global hotkeys not yet implemented");
    }

    public void UnregisterAll()
    {
        // TODO: Clean up registrations when implemented
    }
}
