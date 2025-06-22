using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace Cycloside;

public partial class VolatileRunnerWindow : Window
{
    private readonly VolatilePluginManager _manager;

    // Parameterless constructor is required for the Avalonia XAML loader.
    // It creates a standalone manager for design-time usage.
    public VolatileRunnerWindow() : this(new VolatilePluginManager()) { }

    public VolatileRunnerWindow(VolatilePluginManager manager)
    {
        // Manager is now guaranteed to be valid by the constructor.
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));

        InitializeComponent();

        var langBox = this.FindControl<ComboBox>("LangBox");
        if (langBox != null)
            langBox.SelectedIndex = 0;

        // Assuming WindowEffectsManager is a valid part of your project
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(VolatileRunnerWindow));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnRun(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // --- Optimized Logic ---
        // 1. Find controls first to ensure they exist.
        var langBox = this.FindControl<ComboBox>("LangBox");
        var codeBox = this.FindControl<TextBox>("CodeBox");

        // 2. Guard Clause: If either essential control is missing, do nothing.
        //    This prevents the app from crashing.
        if (langBox == null || codeBox == null)
        {
            Console.WriteLine("Error: UI controls 'LangBox' or 'CodeBox' could not be found.");
            return;
        }

        // 3. Safely get the text, defaulting to an empty string if it's null.
        var code = codeBox.Text ?? string.Empty;

        // 4. Execute the appropriate code based on the selected language.
        if (langBox.SelectedIndex == 0)
        {
            _manager.RunLua(code);
        }
        else
        {
            _manager.RunCSharp(code);
        }
    }
}