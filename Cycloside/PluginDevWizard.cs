using Avalonia;
using Avalonia.Controls;
using System;
using System.IO;
using Cycloside.Services;

namespace Cycloside;

public class PluginDevWizard : Window
{
    private TextBox? _nameBox;
    private ComboBox? _typeBox;

    public PluginDevWizard()
    {
        CursorManager.ApplyFromSettings(this, "Plugins");
        BuildUI();
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(PluginDevWizard));
    }

    private void BuildUI()
    {
        Width = 300;
        Height = 150;
        Title = "Generate Plugin";

        var panel = new StackPanel { Margin = new Thickness(10) };
        _nameBox = new TextBox { Watermark = "Plugin Name" };
        _typeBox = new ComboBox { SelectedIndex = 0 };
        _typeBox.Items.Clear();
        _typeBox.Items.Add("Basic DLL");
        _typeBox.Items.Add("Lua volatile");
        _typeBox.Items.Add("C# volatile");

        var create = new Button { Content = "Create", Margin = new Thickness(0,10,0,0) };
        create.Click += Create_Click;
        panel.Children.Add(_nameBox);
        panel.Children.Add(_typeBox);
        panel.Children.Add(create);
        Content = panel;
    }

    private void Create_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_nameBox == null || _typeBox == null) return;
        var name = _nameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        var dir = Path.Combine(AppContext.BaseDirectory, "Plugins");
        Directory.CreateDirectory(dir);

        switch (_typeBox.SelectedIndex)
        {
            case 0:
                Program.GeneratePluginTemplate(name);
                break;
            case 1:
                File.WriteAllText(Path.Combine(dir, $"{name}.lua"), "print(\"Lua says hi!\")\nreturn os.date()");
                break;
            default:
                File.WriteAllText(Path.Combine(dir, $"{name}.csx"), "namespace Script { public static class Main { public static string Run() => \"RAM-only C# says hi!\"; } }");
                break;
        }

        Close();
    }
}
