using System;

namespace Cycloside.Plugins.BuiltIn;

public class MacroPlugin : IPlugin
{
    public string Name => "Macro Engine";
    public string Description => "Records and plays simple keyboard macros.";
    public Version Version => new(1,0,0);

    public void Start()
    {
        Console.WriteLine("Macro engine started (placeholder)");
    }

    public void Stop()
    {
        Console.WriteLine("Macro engine stopped");
    }
}
