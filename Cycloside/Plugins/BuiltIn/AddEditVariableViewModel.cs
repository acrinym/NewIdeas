using Avalonia.Controls;
using ReactiveUI;
using System.Reactive;

namespace Cycloside.Plugins.BuiltIn;

public class AddEditVariableViewModel : ReactiveObject
{
    private string _key = string.Empty;
    private string _value = string.Empty;
    private bool _isEditMode;

    public string Key
    {
        get => _key;
        set => this.RaiseAndSetIfChanged(ref _key, value);
    }

    public string Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set => this.RaiseAndSetIfChanged(ref _isEditMode, value);
    }

    public string Title => IsEditMode ? "Edit Variable" : "Add New Variable";

    public ReactiveCommand<Window, Unit> OkCommand { get; }
    public ReactiveCommand<Window, Unit> CancelCommand { get; }

    public AddEditVariableViewModel()
    {
        var canExecuteOk = this.WhenAnyValue(x => x.Key, key => !string.IsNullOrWhiteSpace(key));
        OkCommand = ReactiveCommand.Create<Window>(w => w.Close(new EnvironmentEditorPlugin.EnvItem { Key = Key, Value = Value }), canExecuteOk);
        CancelCommand = ReactiveCommand.Create<Window>(w => w.Close(null));
    }

    public AddEditVariableViewModel(EnvironmentEditorPlugin.EnvItem item) : this()
    {
        IsEditMode = true;
        Key = item.Key;
        Value = item.Value;
    }
}
