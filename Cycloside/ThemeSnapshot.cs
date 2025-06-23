using System.Collections.Generic;

namespace Cycloside;

public class ThemeSnapshot
{
    public string Theme { get; set; } = "MintGreen";
    public Dictionary<string, List<string>> ComponentSkins { get; set; } = new();
}
