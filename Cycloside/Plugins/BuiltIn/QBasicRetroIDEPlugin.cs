using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using CliWrap;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn;

public class QBasicRetroIDEPlugin : IPlugin
{
    private Window? _window;
    private TextEditor? _editor;
    private string _qb64Path = "qb64";

    public string Name => "QBasic Retro IDE";
    public string Description => "Edit and run .BAS files using QB64 Phoenix";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        _qb64Path = SettingsManager.Settings.ComponentThemes.TryGetValue("QB64Path", out var p) && !string.IsNullOrWhiteSpace(p)
            ? p : "qb64";

        _editor = new TextEditor
        {
            SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#"),
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0,0,128)),
            Foreground = Avalonia.Media.Brushes.Yellow,
            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
        };

        var open = new MenuItem { Header = "Open" };
        open.Click += async (_, _) => await OpenFile();
        var save = new MenuItem { Header = "Save" };
        save.Click += async (_, _) => await SaveFile();
        var run = new MenuItem { Header = "Run" };
        run.Click += async (_, _) => await CompileRun();
        var fileMenu = new MenuItem { Header = "File", Items = new[] { open, save, run } };
        var menu = new Menu { Items = new[] { fileMenu } };

        var panel = new DockPanel();
        DockPanel.SetDock(menu, Dock.Top);
        panel.Children.Add(menu);
        panel.Children.Add(_editor);

        _window = new Window
        {
            Title = "QBasic Retro IDE",
            Width = 800,
            Height = 600,
            Content = panel
        };
        ThemeManager.ApplyFromSettings(_window, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(QBasicRetroIDEPlugin));
        _window.Show();
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
        _editor = null;
    }

    private async Task OpenFile()
    {
        if (_window == null) return;
        var dlg = new OpenFileDialog();
        dlg.Filters.Add(new FileDialogFilter { Name = "BAS", Extensions = { "bas" } });
        var files = await dlg.ShowAsync(_window);
        if (files is { Length: > 0 })
            _editor!.Text = await File.ReadAllTextAsync(files[0]);
    }

    private async Task SaveFile()
    {
        if (_window == null) return;
        var dlg = new SaveFileDialog();
        dlg.Filters.Add(new FileDialogFilter { Name = "BAS", Extensions = { "bas" } });
        var path = await dlg.ShowAsync(_window);
        if (!string.IsNullOrWhiteSpace(path))
            await File.WriteAllTextAsync(path, _editor!.Text);
    }

    private async Task CompileRun()
    {
        if (_window == null) return;
        var tmp = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.bas");
        await File.WriteAllTextAsync(tmp, _editor!.Text);
        try
        {
            await Cli.Wrap(_qb64Path).WithArguments(tmp).WithValidation(CommandResultValidation.None).ExecuteAsync();
            var exe = Path.ChangeExtension(tmp, OperatingSystem.IsWindows() ? "exe" : "");
            if (File.Exists(exe))
                await Cli.Wrap(exe).ExecuteAsync();
        }
        catch (Exception ex)
        {
            new WindowNotificationManager(_window).Show(new Notification("QB64", ex.Message, NotificationType.Error));
        }
    }
}
