using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Cycloside.Plugins;
using Cycloside.Services;
using Cycloside.ViewModels;
using System.Diagnostics;

namespace Cycloside;

public partial class PluginSettingsWindow : Window
{
    private readonly PluginManager _manager;
    private readonly PluginSettingsViewModel _viewModel;

    public PluginSettingsWindow()
    {
        InitializeComponent();
        _manager = null!;
        _viewModel = null!;
    }

    public PluginSettingsWindow(PluginManager manager)
    {
        _manager = manager;
        _viewModel = new PluginSettingsViewModel(_manager);
        DataContext = _viewModel;
        InitializeComponent();

        CursorManager.ApplyFromSettings(this, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(PluginSettingsWindow));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ReloadButton_Click(object? sender, RoutedEventArgs e)
    {
        _manager.ReloadPlugins();
        // The list will update automatically thanks to data binding.
        // We just need to re-create the ViewModel.
        DataContext = new PluginSettingsViewModel(_manager);
    }

    private void OpenButton_Click(object? sender, RoutedEventArgs e)
    {
        var path = _manager.PluginDirectory;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch { }
    }
}
