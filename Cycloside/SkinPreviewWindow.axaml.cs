using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;

namespace Cycloside;

public partial class SkinPreviewWindow : Window
{
    private ContentPresenter? _host;

    public SkinPreviewWindow()
    {
        InitializeComponent();
        _host = this.FindControl<ContentPresenter>("PreviewHost");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Loads the provided XAML markup and replaces the preview content.
    /// </summary>
    public void LoadPreview(string xaml)
    {
        try
        {
            var control = AvaloniaRuntimeXamlLoader.Parse(xaml) as Control ?? new TextBlock { Text = "Invalid markup" };
            if (_host != null) _host.Content = control;
        }
        catch (Exception ex)
        {
            if (_host != null) _host.Content = new TextBlock { Text = ex.Message, Foreground = Brushes.Red };
        }
    }
}
