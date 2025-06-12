namespace Cycloside.Hotkeys;

using System;
using Avalonia.Input;

/// <summary>
/// Provides global hotkey registration independent of platform.
/// </summary>
public interface IGlobalHotkeyManager
{
    /// <summary>
    /// Register a global hotkey with a callback.
    /// </summary>
    void Register(KeyGesture gesture, Action callback);

    /// <summary>
    /// Unregister all hotkeys and clean up.
    /// </summary>
    void UnregisterAll();
}
