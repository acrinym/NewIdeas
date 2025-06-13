using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cycloside;

public partial class VolatileRunnerWindow : Window
{
    private readonly VolatilePluginManager _manager;

    public VolatileRunnerWindow(VolatilePluginManager manager)
    {
        _manager = manager;
        InitializeComponent();
        this.FindControl<ComboBox>("LangBox").SelectedIndex = 0;
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(VolatileRunnerWindow));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnRun(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var langBox = this.FindControl<ComboBox>("LangBox");
        var code = this.FindControl<TextBox>("CodeBox").Text ?? string.Empty;
        if (langBox.SelectedIndex == 0)
            _manager.RunLua(code);
        else
            _manager.RunCSharp(code);
    }
}
