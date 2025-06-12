using System;
using SiloCide.SDK;
using System;

public class ExamplePlugin : IPlugin
{
    public string Name => "Example";
    public string Description => "Example plugin";
    public Version Version => new(1,0,0);
    public Cycloside.Widgets.IWidget? Widget => null;
    public void Start() { }
    public void Stop() { }
}
