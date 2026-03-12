using System;

namespace Cycloside;

public static class AnimatedBackgroundModes
{
    public const string None = "None";
    public const string Media = "Media";
    public const string Visualizer = "Visualizer";
}

public class AnimatedBackgroundSettings
{
    public string Mode { get; set; } = AnimatedBackgroundModes.None;

    public string Source { get; set; } = string.Empty;

    public string Visualizer { get; set; } = "Starfield";

    public double Opacity { get; set; } = 0.55;

    public bool Loop { get; set; } = true;

    public bool MuteVideo { get; set; } = true;

    public AnimatedBackgroundSettings Clone()
    {
        return new AnimatedBackgroundSettings
        {
            Mode = Mode,
            Source = Source,
            Visualizer = Visualizer,
            Opacity = Opacity,
            Loop = Loop,
            MuteVideo = MuteVideo
        };
    }

    public void Normalize()
    {
        Mode = NormalizeMode(Mode);
        Opacity = Math.Clamp(Opacity, 0.05, 1.0);
        Source ??= string.Empty;
        Visualizer ??= string.Empty;
    }

    public bool IsDisabled()
    {
        return string.Equals(NormalizeMode(Mode), AnimatedBackgroundModes.None, StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeMode(string? mode)
    {
        if (string.Equals(mode, AnimatedBackgroundModes.Media, StringComparison.OrdinalIgnoreCase))
        {
            return AnimatedBackgroundModes.Media;
        }

        if (string.Equals(mode, AnimatedBackgroundModes.Visualizer, StringComparison.OrdinalIgnoreCase))
        {
            return AnimatedBackgroundModes.Visualizer;
        }

        return AnimatedBackgroundModes.None;
    }
}
