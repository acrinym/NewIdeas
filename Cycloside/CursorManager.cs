using Avalonia;
using Avalonia.Input;
using System;

namespace Cycloside.Services;

/// <summary>
/// Manages the application of custom cursors to UI elements.
/// </summary>
public static class CursorManager
{
    /// <summary>
    /// Applies a standard cursor to a UI element by its name.
    /// </summary>
    public static void ApplyCursor(InputElement element, string cursorName)
    {
        try
        {
            if (Enum.TryParse<StandardCursorType>(cursorName, true, out var type))
            {
                element.Cursor = new Cursor(type);
            }
            else
            {
                Logger.Log($"Warning: Cursor '{cursorName}' not found. Defaulting to Arrow.");
                element.Cursor = new Cursor(StandardCursorType.Arrow);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error applying cursor '{cursorName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Applies a cursor to a UI element based on the component's name from the global settings.
    /// </summary>
    public static void ApplyFromSettings(InputElement element, string component)
    {
        var map = SettingsManager.Settings.ComponentCursors;
        if (map != null && map.TryGetValue(component, out var cursorName) && !string.IsNullOrWhiteSpace(cursorName))
        {
            ApplyCursor(element, cursorName);
        }
    }
}
