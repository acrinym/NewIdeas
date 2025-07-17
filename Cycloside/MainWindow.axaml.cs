using Avalonia.Controls;
using Avalonia.Interactivity;
using Cycloside.Plugins;
using Cycloside.Services;

namespace Cycloside;

public partial class MainWindow : Window
{
    private readonly PluginManager _manager;

    // Parameterless constructor for designer support
    public MainWindow()
    {
        InitializeComponent();
        _manager = null!;
        ThemeManager.ApplyFromSettings(this, nameof(MainWindow));
        CursorManager.ApplyFromSettings(this, nameof(MainWindow));
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(MainWindow));
    }

    public MainWindow(PluginManager manager)
    {
        _manager = manager;
        InitializeComponent();
        ThemeManager.ApplyFromSettings(this, nameof(MainWindow));
        CursorManager.ApplyFromSettings(this, nameof(MainWindow));
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(MainWindow));
    }

    // Public property to access the plugin manager
    public PluginManager PluginManager => _manager;

    private void OpenThemeSettings(object? sender, RoutedEventArgs e) =>
        new ThemeSettingsWindow(_manager).Show();

    private void OpenSkinEditor(object? sender, RoutedEventArgs e) =>
        new SkinThemeEditorWindow().Show();

    private void OpenControlPanel(object? sender, RoutedEventArgs e) =>
        new ControlPanelWindow(_manager).Show();
}
