using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using Cycloside.Services;

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
    /// Validates content before parse (CYC-2026-020, CYC-2026-019).
    /// </summary>
    public void LoadPreview(string xaml)
    {
        if (_host == null) return;
        if (string.IsNullOrWhiteSpace(xaml))
        {
            _host.Content = new TextBlock { Text = "No content to preview" };
            return;
        }
        if (!ThemeSecurityValidator.IsAxamlContentSafe(xaml))
        {
            _host.Content = new TextBlock { Text = "Content blocked: unsafe AXAML", Foreground = Brushes.Red, TextWrapping = TextWrapping.Wrap };
            return;
        }
        try
        {
            var control = AvaloniaRuntimeXamlLoader.Parse(xaml) as Control ?? new TextBlock { Text = "Invalid markup" };
            _host.Content = control;
        }
        catch (Exception ex)
        {
            _host.Content = new TextBlock { Text = ex.Message, Foreground = Brushes.Red };
        }
    }
}
