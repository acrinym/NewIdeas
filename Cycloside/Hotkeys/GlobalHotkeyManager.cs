using System;
using Avalonia.Input;

namespace Cycloside.Hotkeys;

/// <summary>
/// Provides static access to the platform-specific implementation.
/// </summary>
public static class GlobalHotkeyManager
{
    private static readonly IGlobalHotkeyManager _impl = GlobalHotkeyManagerFactory.Create();

    /// <summary>
    /// Register a global hotkey.
    /// </summary>
    public static void Register(KeyGesture gesture, Action callback) => _impl.Register(gesture, callback);

    /// <summary>
    /// Unregister all hotkeys.
    /// </summary>
    public static void UnregisterAll() => _impl.UnregisterAll();
}
