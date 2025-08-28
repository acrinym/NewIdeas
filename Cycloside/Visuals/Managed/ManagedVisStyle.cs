using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace Cycloside.Visuals.Managed;

public static class ManagedVisStyle
{
    private static bool UseNative
    {
        get
        {
            var v = StateManager.Get("ManagedVis.NativeColors");
            return v != null && (v.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                                 v.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                 v.Equals("1"));
        }
    }

    public static string Theme
    {
        get
        {
            return StateManager.Get("ManagedVis.Theme") ?? "Neon";
        }
    }

    public static double Sensitivity
    {
        get
        {
            var s = 1.0;
            if (double.TryParse(StateManager.Get("ManagedVis.Sensitivity"), out var v)) s = v;
            if (s < 0.2) s = 0.2; if (s > 3.0) s = 3.0;
            return s;
        }
    }

    public static SolidColorBrush Background()
    {
        if (UseNative)
        {
            // Use application background as-is
            return new SolidColorBrush(GetAppBackgroundColor());
        }
        return Theme.ToLowerInvariant() switch
        {
            "classic" => new SolidColorBrush(Color.FromRgb(0, 0, 0)),
            "fire" => new SolidColorBrush(Color.FromRgb(12, 2, 2)),
            "ocean" => new SolidColorBrush(Color.FromRgb(2, 6, 12)),
            "matrix" => new SolidColorBrush(Color.FromRgb(0, 2, 0)),
            "sunset" => new SolidColorBrush(Color.FromRgb(10, 4, 6)),
            _ => new SolidColorBrush(Color.FromRgb(5, 5, 8))
        };
    }

    public static SolidColorBrush Accent()
    {
        if (UseNative)
        {
            return new SolidColorBrush(GetAppAccentColor());
        }
        return Theme.ToLowerInvariant() switch
        {
            "classic" => new SolidColorBrush(Color.FromRgb(180, 200, 255)),
            "fire" => new SolidColorBrush(Color.FromRgb(255, 110, 0)),
            "ocean" => new SolidColorBrush(Color.FromRgb(0, 200, 255)),
            "matrix" => new SolidColorBrush(Color.FromRgb(0, 255, 128)),
            "sunset" => new SolidColorBrush(Color.FromRgb(255, 60, 140)),
            _ => new SolidColorBrush(Color.FromRgb(50, 205, 255))
        };
    }

    public static SolidColorBrush Secondary()
    {
        if (UseNative)
        {
            var c = GetAppAccentColor();
            var s = ScaleColor(c, 0.7);
            return new SolidColorBrush(s);
        }
        return Theme.ToLowerInvariant() switch
        {
            "classic" => new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            "fire" => new SolidColorBrush(Color.FromArgb(180, 255, 80, 0)),
            "ocean" => new SolidColorBrush(Color.FromArgb(180, 0, 160, 255)),
            "matrix" => new SolidColorBrush(Color.FromArgb(180, 120, 255, 120)),
            "sunset" => new SolidColorBrush(Color.FromArgb(180, 255, 160, 40)),
            _ => new SolidColorBrush(Color.FromArgb(180, 200, 120, 255))
        };
    }

    public static SolidColorBrush Peak()
    {
        return UseNative ? new SolidColorBrush(Color.FromArgb(220, 255, 255, 255))
                         : new SolidColorBrush(Color.FromArgb(220, 255, 255, 255));
    }

    public static Pen Grid()
    {
        return new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)), 1, DashStyle.Dash);
    }

    private static Color GetAppAccentColor()
    {
        var app = Application.Current;
        var theme = app?.ActualThemeVariant ?? ThemeVariant.Light;
        if (app?.Resources.TryGetResource("SystemAccentColor", theme, out var value) == true && value is Color c)
            return c;
        // Fallback
        return Color.FromRgb(50, 205, 255);
    }

    private static Color GetAppBackgroundColor()
    {
        var app = Application.Current;
        var theme = app?.ActualThemeVariant ?? ThemeVariant.Light;
        if (app?.Resources.TryGetResource("ThemeBackgroundColor", theme, out var value) == true && value is Color c)
            return c;
        return Color.FromRgb(5, 5, 8);
    }

    private static Color ScaleColor(Color c, double factor)
    {
        byte sc(byte v) => (byte)Math.Clamp(v * factor, 0, 255);
        return Color.FromArgb(c.A, sc(c.R), sc(c.G), sc(c.B));
    }
}
