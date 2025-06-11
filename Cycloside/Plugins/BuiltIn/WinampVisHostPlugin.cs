using System;
using System.IO;
using Cycloside.Visuals;

namespace Cycloside.Plugins.BuiltIn;

public class WinampVisHostPlugin : IPlugin
{
    private VisPluginManager? _manager;

    public string Name => "Winamp Visual Host";
    public string Description => "Hosts Winamp visualization plugins";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Plugins", "Winamp");
        _manager = new VisPluginManager();
        _manager.Load(dir);
        _manager.StartFirst();
    }

    public void Stop()
    {
        _manager?.Dispose();
        _manager = null;
    }
}
