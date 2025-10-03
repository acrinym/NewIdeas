using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Cycloside.Converters
{
    /// <summary>
    /// Converts a boolean value to a string.
    /// </summary>
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter is string customValues)
                {
                    var values = customValues.Split(',');
                    if (values.Length >= 2)
                    {
                        return boolValue ? values[0] : values[1];
                    }
                }
                
                // Default values if no parameter is provided
                return boolValue ? "System" : "User";
            }
            
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}