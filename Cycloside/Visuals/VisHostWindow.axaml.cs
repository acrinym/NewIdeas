using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace Cycloside.Visuals;

public partial class VisHostWindow : Window
{
    public VisHostWindow()
    {
        InitializeComponent();
        ThemeManager.ApplyFromSettings(this, "Plugins");
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
