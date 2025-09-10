using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Cycloside.Plugins;

namespace Cycloside.Converters
{
    public class PluginStatusToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is PluginChangeStatus status)
            {
                return status switch
                {
                    PluginChangeStatus.New => "(NEW)",
                    PluginChangeStatus.Updated => "(UPDATED)",
                    _ => string.Empty,
                };
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
