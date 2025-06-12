using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Controls.HotKeys;
using Cycloside.Hotkeys;

namespace Cycloside;

/// <summary>
/// Cross-platform hotkey registration based on Avalonia.Controls.HotKeys.
/// </summary>
public static class HotkeyManager
{
    private static readonly GlobalHotKeyManager? _defaultManager;
    private static readonly MacGlobalHotkeyManager? _macManager;

    private static readonly Dictionary<GlobalHotKey, Action> _callbacks = new();
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
            _defaultManager = new GlobalHotKeyManager();
            _defaultManager.HotKeyPressed += (_, e) =>
            {
                if (_callbacks.TryGetValue(e.HotKey, out var cb))
                {
                    try { cb(); } catch (Exception ex) { Logger.Log($"Hotkey error: {ex.Message}"); }
                }
            };
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
            var hotkey = new GlobalHotKey(gesture);
            _callbacks[hotkey] = callback;
            _defaultManager?.Register(hotkey);
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
        else if (_defaultManager != null)
        {
            foreach (var hk in _callbacks.Keys)
                _defaultManager.Unregister(hk);
            _callbacks.Clear();
        }
    }
}
