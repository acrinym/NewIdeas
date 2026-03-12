using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Cycloside.Plugins;
using Cycloside.Plugins.BuiltIn.Controls;
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
        
        // Add keyboard shortcuts
        KeyDown += OnKeyDown;
    }

    // Public property to access the plugin manager
    public PluginManager PluginManager => _manager;

    private void OpenThemeSettings(object? sender, RoutedEventArgs e) =>
        new ThemeSettingsWindow(_manager).Show();

    private void OpenSkinEditor(object? sender, RoutedEventArgs e) =>
        new SkinThemeEditorWindow().Show();

    private void OpenControlPanel(object? sender, RoutedEventArgs e) =>
        new ControlPanelWindow(_manager).Show();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Handle Ctrl+F for Find and Ctrl+H for Find/Replace
        if (e.KeyModifiers == KeyModifiers.Control)
        {
            if (e.Key == Key.F || e.Key == Key.H)
            {
                // Find the active CodeEditor in the current tab
                var activeCodeEditor = FindActiveCodeEditor();
                if (activeCodeEditor != null)
                {
                    activeCodeEditor.ShowFindReplace();
                    e.Handled = true;
                }
            }
        }
    }

    private CodeEditor? FindActiveCodeEditor()
    {
        // Get the current tab content
        if (DataContext is ViewModels.MainWindowViewModel viewModel && 
            viewModel.SelectedWorkspaceItem?.View is UserControl userControl)
        {
            // Recursively search for CodeEditor in the visual tree
            return FindCodeEditorInVisualTree(userControl);
        }
        return null;
    }

    private CodeEditor? FindCodeEditorInVisualTree(Control control)
    {
        if (control is CodeEditor codeEditor)
            return codeEditor;

        // Search children
        if (control is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Control childControl)
                {
                    var result = FindCodeEditorInVisualTree(childControl);
                    if (result != null) return result;
                }
            }
        }
        else if (control is ContentControl contentControl && contentControl.Content is Control content)
        {
            return FindCodeEditorInVisualTree(content);
        }
        else if (control is Decorator decorator && decorator.Child is Control decoratorChild)
        {
            return FindCodeEditorInVisualTree(decoratorChild);
        }

        return null;
    }
}
