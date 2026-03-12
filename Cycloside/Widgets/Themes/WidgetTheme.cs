using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace Cycloside.Widgets.Themes;

/// <summary>
/// Represents a widget theme with customizable appearance properties
/// </summary>
public class WidgetTheme
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    
    // Color properties
    public IBrush? BackgroundBrush { get; set; }
    public IBrush? ForegroundBrush { get; set; }
    public IBrush? BorderBrush { get; set; }
    public IBrush? AccentBrush { get; set; }
    public IBrush? SecondaryBrush { get; set; }
    public IBrush? SecondaryForegroundBrush { get; set; }
    public IBrush? ErrorBrush { get; set; }
    public IBrush? WarningBrush { get; set; }
    public IBrush? SuccessBrush { get; set; }
    public IBrush? InputBackgroundBrush { get; set; }
    public IBrush? InputForegroundBrush { get; set; }
    public IBrush? InputBorderBrush { get; set; }
    
    // Typography
    public FontFamily? FontFamily { get; set; }
    public double FontSize { get; set; } = 12;
    public FontWeight FontWeight { get; set; } = FontWeight.Normal;
    public FontStyle FontStyle { get; set; } = FontStyle.Normal;
    
    // Layout properties
    public double BorderThickness { get; set; } = 1;
    public double CornerRadius { get; set; } = 4;
    public double Padding { get; set; } = 8;
    public double Margin { get; set; } = 4;
    public double Spacing { get; set; } = 8;
    
    // Effects
    public double Opacity { get; set; } = 1.0;
    public bool HasShadow { get; set; } = false;
    public Color ShadowColor { get; set; } = Colors.Black;
    public double ShadowOpacity { get; set; } = 0.3;
    public double ShadowBlurRadius { get; set; } = 4;
    public double ShadowOffsetX { get; set; } = 2;
    public double ShadowOffsetY { get; set; } = 2;
    
    // Animation properties
    public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(200);
    public string AnimationEasing { get; set; } = "ease-out";
    
    // Custom properties for specific widgets
    public Dictionary<string, object> CustomProperties { get; set; } = new();
    
    /// <summary>
    /// Gets a custom property value with type conversion
    /// </summary>
    public T GetCustomProperty<T>(string key, T defaultValue = default!)
    {
        if (CustomProperties.TryGetValue(key, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Sets a custom property value
    /// </summary>
    public void SetCustomProperty(string key, object value)
    {
        CustomProperties[key] = value;
    }
    
    /// <summary>
    /// Gets a double value by property name
    /// </summary>
    public double GetDouble(string propertyName, double defaultValue = 0.0)
    {
        return propertyName switch
        {
            "FontSize" or "HeaderFontSize" => FontSize,
            "BodyFontSize" => FontSize * 0.9, // Slightly smaller for body text
            "BorderThickness" => BorderThickness,
            "CornerRadius" => CornerRadius,
            "Padding" or "WidgetPadding" => Padding,
            "ButtonPadding" => Padding * 0.75, // Smaller padding for buttons
            "Margin" => Margin,
            "Spacing" => Spacing,
            "Opacity" => Opacity,
            "ShadowOpacity" => ShadowOpacity,
            "ShadowBlurRadius" => ShadowBlurRadius,
            "ShadowOffsetX" => ShadowOffsetX,
            "ShadowOffsetY" => ShadowOffsetY,
            _ => GetCustomProperty(propertyName, defaultValue)
        };
    }
    
    /// <summary>
    /// Gets a brush by property name
    /// </summary>
    public IBrush? GetBrush(string propertyName)
    {
        return propertyName switch
        {
            "BackgroundBrush" or "WidgetBackground" => BackgroundBrush,
            "ForegroundBrush" or "HeaderForeground" or "BodyForeground" => ForegroundBrush,
            "BorderBrush" => BorderBrush,
            "AccentBrush" or "AccentColor" => AccentBrush,
            "AccentForeground" => new SolidColorBrush(Colors.White), // Default white text on accent
            "SecondaryBrush" => SecondaryBrush,
            "SecondaryForegroundBrush" => SecondaryForegroundBrush,
            "ErrorBrush" => ErrorBrush,
            "WarningBrush" => WarningBrush,
            "SuccessBrush" => SuccessBrush,
            "InputBackgroundBrush" => InputBackgroundBrush,
            "ControlBackground" => new SolidColorBrush(Colors.White), // Default control background
            _ => GetCustomProperty<IBrush?>(propertyName, null)
        };
    }
}