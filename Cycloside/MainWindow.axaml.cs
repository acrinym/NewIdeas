using Avalonia.Controls;
using Avalonia.Interactivity;
using Cycloside.Services;

namespace Cycloside;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ThemeManager.ApplyFromSettings(this, nameof(MainWindow));
        CursorManager.ApplyFromSettings(this, nameof(MainWindow));
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(MainWindow));
    }

    private void OpenThemeSettings(object? sender, RoutedEventArgs e) =>
        new ThemeSettingsWindow().Show();

    private void OpenSkinEditor(object? sender, RoutedEventArgs e) =>
        new SkinThemeEditorWindow().Show();
}
