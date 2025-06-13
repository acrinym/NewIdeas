using System;
using System.Collections.Generic;
using Avalonia.Input;
using Cycloside.Hotkeys;

namespace Cycloside;

/// <summary>
/// Cross-platform hotkey registration based on Avalonia.Controls.HotKeys.
/// </summary>
public static class HotkeyManager
{
    private static readonly MacGlobalHotkeyManager? _macManager;
    private static readonly Dictionary<KeyGesture, Action> _macCallbacks = new();

    static HotkeyManager()
    {
        if (OperatingSystem.IsMacOS())
        {
            _macManager = new MacGlobalHotkeyManager();
            _macManager.HotKeyPressed += gesture =>
            {
                if (_macCallbacks.TryGetValue(gesture, out var cb))
                {
                    try { cb(); } catch (Exception ex) { Logger.Log($"Hotkey error: {ex.Message}"); }
                }
            };
        }
        else
        {
            // Hotkeys are not currently supported on other platforms
        }
    }

    /// <summary>
    /// Registers a global hotkey.
    /// </summary>
    public static void Register(KeyGesture gesture, Action callback)
    {
        if (OperatingSystem.IsMacOS())
        {
            _macCallbacks[gesture] = callback;
            _macManager?.Register(gesture);
        }
        else
        {
            // Hotkeys not supported on this platform
        }
    }

    /// <summary>
    /// Unregisters all registered hotkeys.
    /// </summary>
    public static void UnregisterAll()
    {
        if (OperatingSystem.IsMacOS())
        {
            _macManager?.UnregisterAll();
            _macCallbacks.Clear();
        }
        else
        {
            // Nothing to unregister on unsupported platforms
        }
    }
}
