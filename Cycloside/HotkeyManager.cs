using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Controls.HotKeys;

namespace Cycloside;

/// <summary>
/// Cross-platform hotkey registration based on Avalonia.Controls.HotKeys.
/// </summary>
public static class HotkeyManager
{
    private static readonly GlobalHotKeyManager _manager = new();
    private static readonly Dictionary<GlobalHotKey, Action> _callbacks = new();

    static HotkeyManager()
    {
        _manager.HotKeyPressed += (_, e) =>
        {
            if (_callbacks.TryGetValue(e.HotKey, out var cb))
            {
                try { cb(); } catch (Exception ex) { Logger.Log($"Hotkey error: {ex.Message}"); }
            }
        };
    }

    /// <summary>
    /// Registers a global hotkey.
    /// </summary>
    public static void Register(KeyGesture gesture, Action callback)
    {
        var hotkey = new GlobalHotKey(gesture);
        _callbacks[hotkey] = callback;
        _manager.Register(hotkey);
    }

    /// <summary>
    /// Unregisters all registered hotkeys.
    /// </summary>
    public static void UnregisterAll()
    {
        foreach (var hk in _callbacks.Keys)
            _manager.Unregister(hk);
        _callbacks.Clear();
    }
}
