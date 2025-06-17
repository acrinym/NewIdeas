using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace Cycloside.Widgets;

public partial class WidgetHostWindow : Window
{
    public WidgetHostWindow()
    {
        InitializeComponent();
        Topmost = true;
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(WidgetHostWindow));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public Canvas Root => this.FindControl<Canvas>("RootCanvas")!;

    public IntPtr GetHandle() => this.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
}
