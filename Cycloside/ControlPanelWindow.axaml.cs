using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cycloside.ViewModels;
using Cycloside.Services;
using Cycloside.Plugins;
using System;

namespace Cycloside;

public partial class ControlPanelWindow : Window
{
    public ControlPanelWindow(PluginManager manager)
    {
        InitializeComponent();
        DataContext = new ControlPanelViewModel(manager, Close);
        CursorManager.ApplyFromSettings(this, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(ControlPanelWindow));
    }

    public ControlPanelWindow() : this(new PluginManager(System.IO.Path.Combine(AppContext.BaseDirectory, "Plugins"), Services.NotificationCenter.Notify))
    {
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
