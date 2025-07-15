using Avalonia.Data.Converters;
using Cycloside.Plugins;
using System;
using System.Globalization;

namespace Cycloside.Converters;

/// <summary>
/// Returns true if the bound value implements <see cref="IWorkspaceItem"/>.
/// Useful for showing UI elements only when a plugin supports the workspace.
/// </summary>
public class IsWorkspaceItemConverter : IValueConverter
{
    public static readonly IsWorkspaceItemConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is IWorkspaceItem;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

