using System;

namespace Cycloside.Plugins;

public interface IPlugin
{
    string Name { get; }
    string Description { get; }
    Version Version { get; }
    Cycloside.Widgets.IWidget? Widget { get; }
    void Start();
    void Stop();
}
