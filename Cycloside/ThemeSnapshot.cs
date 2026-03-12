using System.Collections.Generic;

namespace Cycloside;

public class ThemeSnapshot
{
    public string Theme { get; set; } = "Dockside";
    public string ThemeVariant { get; set; } = "Dark";
    public string Skin { get; set; } = "Workbench";
    public Dictionary<string, string> PluginSkins { get; set; } = new();
    public AnimatedBackgroundSettings GlobalAnimatedBackground { get; set; } = new();
    public Dictionary<string, AnimatedBackgroundSettings> ComponentAnimatedBackgrounds { get; set; } = new();
    public Dictionary<string, AnimatedBackgroundSettings> PluginAnimatedBackgrounds { get; set; } = new();
}
