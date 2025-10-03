using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace Cycloside.Converters
{
    /// <summary>
    /// Converts a boolean value to a color.
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string colorParams)
            {
                var colors = colorParams.Split(',');
                if (colors.Length >= 2)
                {
                    var colorName = boolValue ? colors[0] : colors[1];

                    // Try to parse the color
                    if (Color.TryParse(colorName, out var color))
                    {
                        return new SolidColorBrush(color);
                    }

                    // If parsing fails, try to use a named color
                    return new SolidColorBrush(GetColorByName(colorName));
                }
            }

            // Default to black if conversion fails
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private Color GetColorByName(string colorName)
        {
            return colorName.Trim().ToLowerInvariant() switch
            {
                "red" => Colors.Red,
                "green" => Colors.Green,
                "blue" => Colors.Blue,
                "yellow" => Colors.Yellow,
                "orange" => Colors.Orange,
                "purple" => Colors.Purple,
                "gray" or "grey" => Colors.Gray,
                "white" => Colors.White,
                _ => Colors.Black
            };
        }
    }
}