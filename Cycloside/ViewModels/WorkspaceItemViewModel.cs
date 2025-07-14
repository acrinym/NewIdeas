using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cycloside.ViewModels;

/// <summary>
/// Represents an item hosted in the unified workspace.
/// </summary>
public partial class WorkspaceItemViewModel : ObservableObject
{
    private readonly Action<WorkspaceItemViewModel> _detachAction;

    public WorkspaceItemViewModel(string header, Control view, Plugins.IPlugin plugin,
        Action<WorkspaceItemViewModel> detachAction)
    {
        Header = header;
        View = view;
        Plugin = plugin;
        _detachAction = detachAction;
    }

    public string Header { get; }
    public Control View { get; }
    public Plugins.IPlugin Plugin { get; }

    [RelayCommand]
    private void Detach() => _detachAction(this);
}