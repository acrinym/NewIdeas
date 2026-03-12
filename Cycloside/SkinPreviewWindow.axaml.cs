using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using Cycloside.Controls;

namespace Cycloside;

public partial class SkinPreviewWindow : Window
{
    private ContentControl? _host;
    private TextBlock? _statusBlock;
    private StyledElement? _sampleRoot;
    private readonly List<IStyle> _appliedStyles = new();

    public SkinPreviewWindow()
    {
        InitializeComponent();
        _host = this.FindControl<ContentControl>("PreviewHost");
        _statusBlock = this.FindControl<TextBlock>("PreviewStatus");
        ResetSampleContent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void LoadPreview(string xaml)
    {
        try
        {
            var parsed = AvaloniaRuntimeXamlLoader.Parse(xaml, typeof(App).Assembly);

            if (parsed is IStyle style)
            {
                ResetSampleContent();
                ApplyStylePreview(style);
                return;
            }

            if (parsed is Control control)
            {
                ClearAppliedStyles();
                if (_host != null)
                {
                    _host.Content = control;
                }

                if (_statusBlock != null)
                {
                    _statusBlock.Text = "Control preview";
                }

                return;
            }

            ShowError("Preview file did not produce a style or control.");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ApplyStylePreview(IStyle style)
    {
        if (_sampleRoot == null)
        {
            ResetSampleContent();
        }

        if (_sampleRoot == null)
        {
            ShowError("Sample preview surface is unavailable.");
            return;
        }

        ClearAppliedStyles();
        _sampleRoot.Styles.Add(style);
        _appliedStyles.Add(style);

        if (_statusBlock != null)
        {
            _statusBlock.Text = "Style preview";
        }
    }

    private void ResetSampleContent()
    {
        ClearAppliedStyles();

        var sample = new Border
        {
            Padding = new Thickness(12),
            Classes = { "preview-surface" },
            Child = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Cycloside preview surface",
                        FontSize = 18,
                        FontWeight = FontWeight.Bold
                    },
                    new TextBlock
                    {
                        Text = "Buttons, inputs, tabs, and lists should all reflect the active theme or skin.",
                        TextWrapping = TextWrapping.Wrap
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        Children =
                        {
                            new Button { Content = "Primary" },
                            new Button { Content = "Danger", Classes = { "danger" } }
                        }
                    },
                    new TextBox
                    {
                        Watermark = "Sample input",
                        Text = "Preview text"
                    },
                    new ComboBox
                    {
                        ItemsSource = new[] { "Widget Host", "Netwatch", "Jezzball", "Tile World" },
                        SelectedIndex = 0
                    },
                    new StackPanel
                    {
                        Spacing = 8,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Progress surfaces",
                                FontWeight = FontWeight.Bold
                            },
                            new MagicalProgressBar
                            {
                                Width = 220,
                                Height = 24,
                                Progress = 68
                            },
                            new ProgressBar
                            {
                                Width = 220,
                                Height = 14,
                                Minimum = 0,
                                Maximum = 100,
                                Value = 52,
                                Classes = { "magical" }
                            }
                        }
                    },
                    new TabControl
                    {
                        Items =
                        {
                            new TabItem
                            {
                                Header = "Shell",
                                Content = new TextBlock { Text = "Main shell surface" }
                            },
                            new TabItem
                            {
                                Header = "Retro",
                                Content = new TextBlock { Text = "Retro module surface" }
                            }
                        }
                    },
                    new ListBox
                    {
                        ItemsSource = new[] { "Desktop widget", "Media visualizer", "Wallpaper scene" },
                        Height = 90
                    }
                }
            }
        };

        _sampleRoot = sample;

        if (_host != null)
        {
            _host.Content = sample;
        }

        if (_statusBlock != null)
        {
            _statusBlock.Text = "Preview ready.";
        }
    }

    private void ClearAppliedStyles()
    {
        if (_sampleRoot == null)
        {
            _appliedStyles.Clear();
            return;
        }

        foreach (var style in _appliedStyles)
        {
            _sampleRoot.Styles.Remove(style);
        }

        _appliedStyles.Clear();
    }

    private void ShowError(string message)
    {
        ClearAppliedStyles();

        if (_host != null)
        {
            _host.Content = new TextBlock
            {
                Text = message,
                Foreground = Brushes.Red,
                TextWrapping = TextWrapping.Wrap
            };
        }

        if (_statusBlock != null)
        {
            _statusBlock.Text = "Preview error";
        }
    }
}
