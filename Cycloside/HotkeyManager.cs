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
    private static readonly SharpGlobalHotkeyManager? _sharpManager;
    private static readonly Dictionary<KeyGesture, Action> _callbacks = new();

    static HotkeyManager()
    {
        if (OperatingSystem.IsMacOS())
        {
            _macManager = new MacGlobalHotkeyManager();
            _macManager.HotKeyPressed += gesture =>
            {
                if (_callbacks.TryGetValue(gesture, out var cb))
                {
                    try { cb(); } catch (Exception ex) { Logger.Log($"Hotkey error: {ex.Message}"); }
                }
            };
        }
        else if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
        {
            _sharpManager = new SharpGlobalHotkeyManager();
            _sharpManager.HotKeyPressed += gesture =>
            {
                if (_callbacks.TryGetValue(gesture, out var cb))
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
        _callbacks[gesture] = callback;
        if (OperatingSystem.IsMacOS())
        {
            _macManager?.Register(gesture);
        }
        else if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
        {
            _sharpManager?.Register(gesture);
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
        }
        else if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
        {
            _sharpManager?.UnregisterAll();
        }
        _callbacks.Clear();
    }
}
