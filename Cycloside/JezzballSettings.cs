using System.Collections.Generic;

namespace Cycloside;

public class JezzballSettings
{
    public bool FastMode { get; set; }
    public bool SoundEnabled { get; set; } = true;
    public bool ShowGrid { get; set; }
    public bool ShowStatusBar { get; set; } = true;
    public bool ShowAreaPercentage { get; set; } = true;
    public bool OriginalMode { get; set; }
    public int StartingLives { get; set; } = 3;
    public int BaseLevelTimeSeconds { get; set; } = 20;
    public int TimePerLevelSeconds { get; set; } = 5;
    public int CaptureRequirementPercent { get; set; } = 75;
    public int PowerUpSpawnPercent { get; set; } = 30;
    public List<long> HighScores { get; set; } = new();
}
