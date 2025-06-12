using System;
using Avalonia.Input;

namespace Cycloside.Hotkeys;

/// <summary>
/// No-op fallback for unsupported platforms.
/// </summary>
internal sealed class StubHotkeyManager : IGlobalHotkeyManager
{
    public void Register(KeyGesture gesture, Action callback)
    {
        Logger.Log("Global hotkeys unsupported on this platform");
    }

    public void UnregisterAll() { }
}
