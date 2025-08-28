using Avalonia.Controls;

namespace Cycloside.Visuals.Managed;

public interface IManagedVisualizerConfigurable
{
    string ConfigKey { get; }
    Control BuildOptionsView();
    void LoadOptions();
}

