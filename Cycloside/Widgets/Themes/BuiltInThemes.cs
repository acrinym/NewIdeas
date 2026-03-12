using Avalonia.Media;

namespace Cycloside.Widgets.Themes;

/// <summary>
/// Contains built-in theme definitions
/// </summary>
public static class BuiltInThemes
{
    public static WidgetTheme CreateDefaultTheme()
    {
        return new WidgetTheme
        {
            Name = "Default",
            DisplayName = "Default",
            Description = "The default widget theme",
            Author = "Cycloside",
            BackgroundBrush = new SolidColorBrush(Color.FromArgb(240, 255, 255, 255)),
            ForegroundBrush = new SolidColorBrush(Colors.Black),
            BorderBrush = new SolidColorBrush(Color.FromArgb(100, 128, 128, 128)),
            AccentBrush = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
            SecondaryBrush = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
            ErrorBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
            WarningBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
            SuccessBrush = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
            FontSize = 12,
            CornerRadius = 4,
            Padding = 8,
            HasShadow = true,
            ShadowOpacity = 0.1
        };
    }
    
    public static WidgetTheme CreateDarkTheme()
    {
        return new WidgetTheme
        {
            Name = "Dark",
            DisplayName = "Dark",
            Description = "A dark theme for widgets",
            Author = "Cycloside",
            BackgroundBrush = new SolidColorBrush(Color.FromArgb(240, 32, 32, 32)),
            ForegroundBrush = new SolidColorBrush(Colors.White),
            BorderBrush = new SolidColorBrush(Color.FromArgb(100, 128, 128, 128)),
            AccentBrush = new SolidColorBrush(Color.FromRgb(100, 181, 246)),
            SecondaryBrush = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
            ErrorBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
            WarningBrush = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
            SuccessBrush = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
            FontSize = 12,
            CornerRadius = 4,
            Padding = 8,
            HasShadow = true,
            ShadowOpacity = 0.3
        };
    }
    
    public static WidgetTheme CreateLightTheme()
    {
        return new WidgetTheme
        {
            Name = "Light",
            DisplayName = "Light",
            Description = "A bright light theme for widgets",
            Author = "Cycloside",
            BackgroundBrush = new SolidColorBrush(Color.FromArgb(250, 255, 255, 255)),
            ForegroundBrush = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(50, 128, 128, 128)),
            AccentBrush = new SolidColorBrush(Color.FromRgb(13, 110, 253)),
            SecondaryBrush = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
            ErrorBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
            WarningBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
            SuccessBrush = new SolidColorBrush(Color.FromRgb(25, 135, 84)),
            FontSize = 12,
            CornerRadius = 6,
            Padding = 12,
            HasShadow = false
        };
    }
    
    public static WidgetTheme CreateAccentTheme()
    {
        return new WidgetTheme
        {
            Name = "Accent",
            DisplayName = "Accent",
            Description = "A colorful accent theme",
            Author = "Cycloside",
            BackgroundBrush = new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(1, 1, Avalonia.RelativeUnit.Relative),
                GradientStops = new Avalonia.Media.GradientStops
                {
                    new Avalonia.Media.GradientStop(Color.FromArgb(240, 138, 43, 226), 0),
                    new Avalonia.Media.GradientStop(Color.FromArgb(240, 30, 144, 255), 1)
                }
            },
            ForegroundBrush = new SolidColorBrush(Colors.White),
            BorderBrush = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)),
            AccentBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
            SecondaryBrush = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
            ErrorBrush = new SolidColorBrush(Color.FromRgb(255, 99, 71)),
            WarningBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
            SuccessBrush = new SolidColorBrush(Color.FromRgb(50, 205, 50)),
            FontSize = 12,
            FontWeight = FontWeight.Medium,
            CornerRadius = 8,
            Padding = 10,
            HasShadow = true,
            ShadowOpacity = 0.4,
            ShadowBlurRadius = 8
        };
    }
}