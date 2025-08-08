using System;
using Cycloside.Plugins;

public class ExamplePlugin : IPlugin
{
    public string Name => "Example";
    public string Description => "Example plugin";
    public Version Version => new(1, 0, 0);
    public Cycloside.Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;
    public void Start() { }
    public void Stop() { }
}
