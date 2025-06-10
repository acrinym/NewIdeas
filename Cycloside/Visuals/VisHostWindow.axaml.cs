using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace Cycloside.Visuals;

public partial class VisHostWindow : Window
{
    public VisHostWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public IntPtr GetHandle()
    {
        return this.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
    }
}
