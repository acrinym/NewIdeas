using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Cycloside.Widgets;
using System;
using System.Linq;

namespace Cycloside.Plugins.BuiltIn;

public class TextEditorPlugin : IPlugin
{
    private Window? _window;

    public string Name => "Text Editor";
    public string Description => "Simple Markdown/text editor";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        var box = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            FontFamily = new FontFamily("monospace"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
        };

        var openButton = new Button { Content = "Open" };
        openButton.Click += async (_, _) =>
        {
            if (_window == null) return;
            var files = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions());
            var file = files.FirstOrDefault()?.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(file))
            {
                box.Text = await System.IO.File.ReadAllTextAsync(file);
            }
        };
        var saveButton = new Button { Content = "Save" };
        saveButton.Click += async (_, _) =>
        {
            if (_window == null) return;
            var result = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions());
            var path = result?.TryGetLocalPath();
            if (!string.IsNullOrWhiteSpace(path))
            {
                await System.IO.File.WriteAllTextAsync(path, box.Text ?? string.Empty);
            }
        };
        var panel = new StackPanel();
        var top = new StackPanel { Orientation = Orientation.Horizontal };
        top.Children.Add(openButton);
        top.Children.Add(saveButton);
        panel.Children.Add(top);
        panel.Children.Add(box);

        _window = new Window
        {
            Title = "Cycloside Editor",
            Width = 500,
            Height = 400,
            Content = panel
        };
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(TextEditorPlugin));
        _window.Show();
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
    }
}
