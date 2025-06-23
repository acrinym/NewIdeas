using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using Cycloside.Services;

namespace Cycloside.Visuals;

public partial class VisHostWindow : Window
{
    public VisHostWindow()
    {
        InitializeComponent();
        CursorManager.ApplyFromSettings(this, "Plugins");
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
