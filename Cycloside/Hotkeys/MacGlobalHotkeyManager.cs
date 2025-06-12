using System;
using Avalonia.Input;

namespace Cycloside.Hotkeys;

/// <summary>
/// macOS global hotkey manager stub. TODO: implement using Cocoa APIs.
/// </summary>
internal sealed class MacGlobalHotkeyManager : IGlobalHotkeyManager
{
    public void Register(KeyGesture gesture, Action callback)
    {
        // TODO: Implement global hotkey registration on macOS
        Logger.Log("macOS global hotkeys not yet implemented");
    }

    public void UnregisterAll()
    {
        // TODO: Clean up registrations when implemented
    }
}
