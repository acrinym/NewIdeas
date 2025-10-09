// ============================================================================
// WIDGET CONVERTERS - Value converters for widget UI binding
// ============================================================================
// Purpose: Convert boolean values to icons and text for widget controls
// Features: Icon and text converters for lock/unlock states
// Dependencies: Avalonia (for IValueConverter)
// ============================================================================

using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Cycloside.Widgets;

public class BoolToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isLocked && parameter is string icons)
        {
            var iconParts = icons.Split(':');
            return isLocked ? iconParts[0] : iconParts[1];
        }
        return "❓";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isLocked && parameter is string texts)
        {
            var textParts = texts.Split(':');
            return isLocked ? textParts[1] : textParts[0]; // Note: Reversed for tooltip logic
        }
        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
