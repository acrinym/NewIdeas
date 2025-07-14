using Avalonia.Controls;

namespace Cycloside.ViewModels;

/// <summary>
/// Represents an item hosted in the unified workspace.
/// </summary>
public class WorkspaceItemViewModel
{
    public WorkspaceItemViewModel(string header, Control view, Plugins.IPlugin plugin)
    {
        Header = header;
        View = view;
        Plugin = plugin;
    }

    public string Header { get; }
    public Control View { get; }
    public Plugins.IPlugin Plugin { get; }
}
