using Avalonia;
using Avalonia.Input;
using Avalonia.Styling;
using System;

namespace Cycloside;

public static class CursorManager
{
    public static void ApplyCursor(InputElement element, string cursorName)
    {
        if (Enum.TryParse<StandardCursorType>(cursorName, true, out var type))
            element.Cursor = new Cursor(type);
    }

    public static void ApplyFromSettings(InputElement element, string component)
    {
        var map = SettingsManager.Settings.ComponentCursors;
        if (map != null && map.TryGetValue(component, out var cursorName))
            ApplyCursor(element, cursorName);
    }
}

