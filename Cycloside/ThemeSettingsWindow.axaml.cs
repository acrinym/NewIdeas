using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Cycloside.Plugins;
using Cycloside.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cycloside;

public partial class ThemeSettingsWindow : Window
{
    private const string BaseThemeLabel = "(Base Tokens Only)";
    private const string NoSkinLabel = "(No Skin)";
    private const string FollowSystemLabel = "Default (Follow System)";
    private const string BackgroundOffLabel = "Off";
    private const string BackgroundMediaLabel = "Media File";
    private const string BackgroundVisualizerLabel = "Managed Visualizer";

    private readonly PluginManager _manager;
    private readonly string[] _themeOptions;
    private readonly string[] _skinOptions;
    private readonly string[] _variantOptions;
    private readonly string[] _backgroundModeOptions = new[]
    {
        BackgroundOffLabel,
        BackgroundMediaLabel,
        BackgroundVisualizerLabel
    };
    private readonly string[] _visualizerOptions;

    private readonly Dictionary<string, ComboBox> _pluginSkinBoxes = new();

    private ComboBox? _globalThemeBox;
    private ComboBox? _globalSkinBox;
    private ComboBox? _variantBox;
    private ComboBox? _backgroundModeBox;
    private TextBox? _backgroundSourceBox;
    private Button? _backgroundBrowseButton;
    private ComboBox? _backgroundVisualizerBox;
    private Slider? _backgroundOpacitySlider;
    private TextBlock? _backgroundOpacityText;
    private CheckBox? _backgroundLoopBox;
    private CheckBox? _backgroundMuteBox;

    public ThemeSettingsWindow() : this(new PluginManager(Path.Combine(AppContext.BaseDirectory, "Plugins"), Services.NotificationCenter.Notify))
    {
    }

    public ThemeSettingsWindow(PluginManager manager)
    {
        _manager = manager;
        _themeOptions = ThemeManager.GetAvailableThemes()
            .Prepend(BaseThemeLabel)
            .ToArray();
        _skinOptions = SkinManager.GetAvailableSkins()
            .Prepend(NoSkinLabel)
            .ToArray();
        _variantOptions = ThemeManager.GetAvailableVariants()
            .Select(VariantToLabel)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _visualizerOptions = AnimatedBackgroundManager.GetAvailableVisualizers().ToArray();

        InitializeComponent();
        BuildList();
        ThemeManager.ApplyFromSettings(this, nameof(ThemeSettingsWindow));
        CursorManager.ApplyFromSettings(this, nameof(ThemeSettingsWindow));
        WindowEffectsManager.Instance.ApplyConfiguredEffects(this, nameof(ThemeSettingsWindow));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void BuildList()
    {
        var panel = this.FindControl<StackPanel>("ThemePanel");
        if (panel == null)
        {
            return;
        }

        panel.Children.Clear();

        panel.Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.Parse("#11233A")),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Theme = app-wide palette and semantic tokens",
                        FontWeight = FontWeight.Bold
                    },
                    new TextBlock
                    {
                        Text = "Skin = window treatment, control styling, chrome, cursors, sounds, and other shell flavor layered on top.",
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = "Animated backdrop = optional media or managed visualizer content shown behind windows that participate in the appearance system.",
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        });

        panel.Children.Add(CreateSectionHeader("Shell Theme"));
        _globalThemeBox = CreateCombo(_themeOptions, GetSelectedThemeLabel());
        panel.Children.Add(CreateFieldRow("Theme Pack", _globalThemeBox));

        _variantBox = CreateCombo(_variantOptions, VariantToLabel(ThemeManager.CurrentVariant));
        panel.Children.Add(CreateFieldRow("Theme Variant", _variantBox));

        panel.Children.Add(CreateSectionHeader("Shell Skin"));
        _globalSkinBox = CreateCombo(_skinOptions, GetSelectedSkinLabel());
        panel.Children.Add(CreateFieldRow("Window Skin", _globalSkinBox));

        panel.Children.Add(CreateSectionHeader("Animated Backdrop"));
        BuildBackdropEditor(panel);

        panel.Children.Add(CreateSectionHeader("Plugin Window Skins"));
        var pluginNames = _manager.Plugins
            .Select(plugin => plugin.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var pluginName in pluginNames)
        {
            var currentSkin = SettingsManager.Settings.PluginSkins.TryGetValue(pluginName, out var savedSkin) &&
                              !string.IsNullOrWhiteSpace(savedSkin)
                ? savedSkin
                : NoSkinLabel;

            var combo = CreateCombo(_skinOptions, currentSkin);
            _pluginSkinBoxes[pluginName] = combo;
            panel.Children.Add(CreateFieldRow(pluginName, combo));
        }
    }

    private void BuildBackdropEditor(Panel panel)
    {
        var settings = SettingsManager.Settings.GlobalAnimatedBackground?.Clone() ?? new AnimatedBackgroundSettings();
        settings.Normalize();

        _backgroundModeBox = CreateCombo(_backgroundModeOptions, ModeToLabel(settings.Mode));
        _backgroundModeBox.SelectionChanged += (_, _) => UpdateBackdropEditorState();
        panel.Children.Add(CreateFieldRow("Backdrop Type", _backgroundModeBox));

        _backgroundSourceBox = new TextBox
        {
            Text = settings.Source,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _backgroundBrowseButton = new Button
        {
            Content = "Browse...",
            Width = 92
        };
        _backgroundBrowseButton.Click += BrowseBackdropSource_Click;

        var sourceRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 8
        };
        sourceRow.Children.Add(_backgroundSourceBox);
        Grid.SetColumn(_backgroundBrowseButton, 1);
        sourceRow.Children.Add(_backgroundBrowseButton);
        panel.Children.Add(CreateFieldRow("Media Source", sourceRow));

        var selectedVisualizer = NormalizeVisualizerSelection(settings.Visualizer);
        _backgroundVisualizerBox = CreateCombo(_visualizerOptions.Length == 0 ? new[] { string.Empty } : _visualizerOptions, selectedVisualizer);
        panel.Children.Add(CreateFieldRow("Visualizer", _backgroundVisualizerBox));

        _backgroundOpacitySlider = new Slider
        {
            Minimum = 0.05,
            Maximum = 1.0,
            Width = 220,
            Value = Math.Clamp(settings.Opacity, 0.05, 1.0)
        };
        _backgroundOpacitySlider.PropertyChanged += (_, e) =>
        {
            if (e.Property.Name == nameof(Slider.Value))
            {
                UpdateBackdropOpacityText();
            }
        };
        _backgroundOpacityText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        UpdateBackdropOpacityText();

        var opacityRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                _backgroundOpacitySlider,
                _backgroundOpacityText
            }
        };
        panel.Children.Add(CreateFieldRow("Backdrop Opacity", opacityRow));

        _backgroundLoopBox = new CheckBox
        {
            Content = "Loop media playback",
            IsChecked = settings.Loop
        };
        panel.Children.Add(CreateFieldRow("Media Looping", _backgroundLoopBox));

        _backgroundMuteBox = new CheckBox
        {
            Content = "Mute video audio",
            IsChecked = settings.MuteVideo
        };
        panel.Children.Add(CreateFieldRow("Video Audio", _backgroundMuteBox));

        UpdateBackdropEditorState();
    }

    private static TextBlock CreateSectionHeader(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontWeight = FontWeight.Bold,
            FontSize = 16,
            Margin = new Thickness(0, 8, 0, 6)
        };
    }

    private static Grid CreateFieldRow(string label, Control editor)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("190,*"),
            Margin = new Thickness(0, 0, 0, 8)
        };

        row.Children.Add(new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 12, 0)
        });

        Grid.SetColumn(editor, 1);
        row.Children.Add(editor);

        return row;
    }

    private static ComboBox CreateCombo(IEnumerable<string> items, string selectedItem)
    {
        return new ComboBox
        {
            ItemsSource = items.ToArray(),
            SelectedItem = selectedItem,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    private string GetSelectedThemeLabel()
    {
        var current = SettingsManager.Settings.GlobalTheme;
        if (string.IsNullOrWhiteSpace(current))
        {
            return BaseThemeLabel;
        }

        return _themeOptions.Contains(current, StringComparer.OrdinalIgnoreCase) ? current : BaseThemeLabel;
    }

    private string GetSelectedSkinLabel()
    {
        var current = SettingsManager.Settings.GlobalSkin;
        if (string.IsNullOrWhiteSpace(current))
        {
            return NoSkinLabel;
        }

        return _skinOptions.Contains(current, StringComparer.OrdinalIgnoreCase) ? current : NoSkinLabel;
    }

    private static string VariantToLabel(Avalonia.Styling.ThemeVariant variant)
    {
        if (variant == Avalonia.Styling.ThemeVariant.Light)
        {
            return "Light";
        }

        if (variant == Avalonia.Styling.ThemeVariant.Dark)
        {
            return "Dark";
        }

        return FollowSystemLabel;
    }

    private static Avalonia.Styling.ThemeVariant LabelToVariant(string? label)
    {
        if (string.Equals(label, "Light", StringComparison.OrdinalIgnoreCase))
        {
            return Avalonia.Styling.ThemeVariant.Light;
        }

        if (string.Equals(label, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            return Avalonia.Styling.ThemeVariant.Dark;
        }

        return Avalonia.Styling.ThemeVariant.Default;
    }

    private static string ModeToLabel(string? mode)
    {
        var normalized = AnimatedBackgroundSettings.NormalizeMode(mode);
        if (string.Equals(normalized, AnimatedBackgroundModes.Media, StringComparison.OrdinalIgnoreCase))
        {
            return BackgroundMediaLabel;
        }

        if (string.Equals(normalized, AnimatedBackgroundModes.Visualizer, StringComparison.OrdinalIgnoreCase))
        {
            return BackgroundVisualizerLabel;
        }

        return BackgroundOffLabel;
    }

    private static string LabelToMode(string? label)
    {
        if (string.Equals(label, BackgroundMediaLabel, StringComparison.OrdinalIgnoreCase))
        {
            return AnimatedBackgroundModes.Media;
        }

        if (string.Equals(label, BackgroundVisualizerLabel, StringComparison.OrdinalIgnoreCase))
        {
            return AnimatedBackgroundModes.Visualizer;
        }

        return AnimatedBackgroundModes.None;
    }

    private string NormalizeVisualizerSelection(string? selected)
    {
        if (_visualizerOptions.Length == 0)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(selected) &&
            _visualizerOptions.Contains(selected, StringComparer.OrdinalIgnoreCase))
        {
            return selected;
        }

        if (_visualizerOptions.Contains("Starfield", StringComparer.OrdinalIgnoreCase))
        {
            return _visualizerOptions.First(name => string.Equals(name, "Starfield", StringComparison.OrdinalIgnoreCase));
        }

        return _visualizerOptions[0];
    }

    private void UpdateBackdropOpacityText()
    {
        if (_backgroundOpacitySlider == null || _backgroundOpacityText == null)
        {
            return;
        }

        _backgroundOpacityText.Text = $"{Math.Round(_backgroundOpacitySlider.Value * 100)}%";
    }

    private void UpdateBackdropEditorState()
    {
        var mode = LabelToMode(_backgroundModeBox?.SelectedItem as string);
        var isMedia = string.Equals(mode, AnimatedBackgroundModes.Media, StringComparison.OrdinalIgnoreCase);
        var isVisualizer = string.Equals(mode, AnimatedBackgroundModes.Visualizer, StringComparison.OrdinalIgnoreCase);

        if (_backgroundSourceBox != null)
        {
            _backgroundSourceBox.IsEnabled = isMedia;
        }

        if (_backgroundBrowseButton != null)
        {
            _backgroundBrowseButton.IsEnabled = isMedia;
        }

        if (_backgroundVisualizerBox != null)
        {
            _backgroundVisualizerBox.IsEnabled = isVisualizer && _visualizerOptions.Length > 0;
        }

        if (_backgroundLoopBox != null)
        {
            _backgroundLoopBox.IsEnabled = isMedia;
        }

        if (_backgroundMuteBox != null)
        {
            _backgroundMuteBox.IsEnabled = isMedia;
        }
    }

    private AnimatedBackgroundSettings ReadBackdropSettings()
    {
        var settings = new AnimatedBackgroundSettings
        {
            Mode = LabelToMode(_backgroundModeBox?.SelectedItem as string),
            Source = _backgroundSourceBox?.Text?.Trim() ?? string.Empty,
            Visualizer = _backgroundVisualizerBox?.SelectedItem as string ?? string.Empty,
            Opacity = _backgroundOpacitySlider?.Value ?? 0.55,
            Loop = _backgroundLoopBox?.IsChecked == true,
            MuteVideo = _backgroundMuteBox?.IsChecked != false
        };
        settings.Normalize();
        return settings;
    }

    private bool ValidateBackdropSettings(AnimatedBackgroundSettings settings, out string message)
    {
        message = string.Empty;

        if (string.Equals(settings.Mode, AnimatedBackgroundModes.Media, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(settings.Source))
            {
                message = "Select a media file for the animated backdrop or switch the backdrop type to Off.";
                return false;
            }

            if (!File.Exists(settings.Source) && !File.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, settings.Source))))
            {
                message = $"Cycloside could not find the selected media file: {settings.Source}";
                return false;
            }
        }

        if (string.Equals(settings.Mode, AnimatedBackgroundModes.Visualizer, StringComparison.OrdinalIgnoreCase) &&
            _visualizerOptions.Length == 0)
        {
            message = "No managed visualizers are currently available for backdrop mode.";
            return false;
        }

        return true;
    }

    private async void BrowseBackdropSource_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!StorageProvider.CanOpen)
        {
            return;
        }

        var start = await DialogHelper.GetDefaultStartLocationAsync(StorageProvider);
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Animated Backdrop Media",
            AllowMultiple = false,
            SuggestedStartLocation = start,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Backdrop Media")
                {
                    Patterns = new[]
                    {
                        "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp", "*.tif", "*.tiff",
                        "*.gif",
                        "*.mp4", "*.m4v", "*.avi", "*.mov", "*.wmv", "*.mkv", "*.webm", "*.ogv", "*.ogg", "*.flv"
                    }
                },
                FilePickerFileTypes.All
            }
        });

        if (files.Count == 0)
        {
            return;
        }

        var path = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path) || _backgroundSourceBox == null)
        {
            return;
        }

        _backgroundSourceBox.Text = path;
        if (_backgroundModeBox != null)
        {
            _backgroundModeBox.SelectedItem = BackgroundMediaLabel;
        }

        UpdateBackdropEditorState();
    }

    private async void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var selectedTheme = _globalThemeBox?.SelectedItem as string;
        var selectedSkin = _globalSkinBox?.SelectedItem as string;
        var selectedVariant = _variantBox?.SelectedItem as string;
        var backdropSettings = ReadBackdropSettings();

        if (!ValidateBackdropSettings(backdropSettings, out var backdropError))
        {
            var errorWindow = new MessageWindow("Backdrop Configuration Error", backdropError);
            await errorWindow.ShowDialog(this);
            return;
        }

        var themeName = string.Equals(selectedTheme, BaseThemeLabel, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : selectedTheme ?? string.Empty;
        var skinName = string.Equals(selectedSkin, NoSkinLabel, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : selectedSkin ?? string.Empty;
        var variant = LabelToVariant(selectedVariant);

        var themeApplied = await ThemeManager.ApplyThemeAsync(themeName, variant, false);
        var skinApplied = await SkinManager.ApplySkinAsync(skinName);

        if (!themeApplied || !skinApplied)
        {
            var errorWindow = new MessageWindow(
                "Theme Apply Failed",
                "Cycloside could not apply the selected theme or skin. Check the log for the exact file that failed to load.");
            await errorWindow.ShowDialog(this);
            return;
        }

        SettingsManager.Settings.GlobalTheme = themeName;
        SettingsManager.Settings.RequestedThemeVariant = selectedVariant switch
        {
            "Light" => "Light",
            "Dark" => "Dark",
            _ => "Default"
        };
        SettingsManager.Settings.GlobalSkin = skinName;
        SettingsManager.Settings.GlobalAnimatedBackground = backdropSettings;

        var pluginSkins = SettingsManager.Settings.PluginSkins;
        pluginSkins.Clear();

        foreach (var entry in _pluginSkinBoxes)
        {
            var skinSelection = entry.Value.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(skinSelection) &&
                !string.Equals(skinSelection, NoSkinLabel, StringComparison.OrdinalIgnoreCase))
            {
                pluginSkins[entry.Key] = skinSelection;
            }
        }

        SettingsManager.Save();
        ReapplyOpenPluginSkins();
        AnimatedBackgroundManager.ReapplyAllWindows();

        var message = new MessageWindow(
            "Appearance Updated",
            "Cycloside applied the selected shell theme, skin, and animated backdrop settings.");
        await message.ShowDialog(this);
        Close();
    }

    private static void ReapplyOpenPluginSkins()
    {
        if (Application.Current?.ApplicationLifetime is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        foreach (var window in desktop.Windows.OfType<PluginWindowBase>())
        {
            if (window.Plugin != null)
            {
                ThemeManager.ApplyForPlugin(window, window.Plugin);
            }
        }
    }

    private sealed class MessageWindow : Window
    {
        public MessageWindow(string title, string message)
        {
            Title = title;
            Width = 400;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            };

            var closeButton = new Button
            {
                Content = "OK",
                IsDefault = true,
                HorizontalAlignment = HorizontalAlignment.Right,
                Width = 96
            };
            closeButton.Click += (_, _) => Close();
            content.Children.Add(closeButton);

            Content = content;
        }
    }
}
