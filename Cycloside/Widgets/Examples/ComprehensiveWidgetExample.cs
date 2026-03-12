using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Cycloside.Widgets.Animations;
using Cycloside.Widgets.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Widgets.Examples;

/// <summary>
/// Comprehensive example widget that demonstrates all enhanced widget system features
/// </summary>
public class ComprehensiveWidgetExample : BaseWidget, IWidgetV2
{
    private Grid? _mainGrid;
    private TextBlock? _titleText;
    private TextBlock? _statusText;
    private Button? _actionButton;
    private ProgressBar? _progressBar;
    private ListBox? _dataList;
    private DispatcherTimer? _updateTimer;
    private int _counter;
    private readonly Random _random = new();

    public override string Name => "Comprehensive Example";
    public override string Description => "Demonstrates all enhanced widget system features";
    public override string Category => "Examples";
    public override string Icon => "🎯";
    public override bool SupportsMultipleInstances => true;
    public override (double Width, double Height) DefaultSize => (350, 400);
    public override (double Width, double Height) MinimumSize => (250, 300);

    public Dictionary<string, object> GetConfigurationSchema()
    {
        return new Dictionary<string, object>
        {
            ["updateInterval"] = new
            {
                type = "number",
                defaultValue = 1000,
                minimum = 100,
                maximum = 10000,
                description = "Update interval in milliseconds"
            },
            ["showProgress"] = new
            {
                type = "boolean",
                defaultValue = true,
                description = "Show progress bar"
            },
            ["showDataList"] = new
            {
                type = "boolean",
                defaultValue = true,
                description = "Show data list"
            },
            ["enableAnimations"] = new
            {
                type = "boolean",
                defaultValue = true,
                description = "Enable animations"
            },
            ["maxDataItems"] = new
            {
                type = "number",
                defaultValue = 10,
                minimum = 1,
                maximum = 50,
                description = "Maximum number of data items to display"
            },
            ["customTitle"] = new
            {
                type = "string",
                defaultValue = "Comprehensive Example",
                description = "Custom widget title"
            },
            ["accentColor"] = new
            {
                type = "color",
                defaultValue = "#007ACC",
                description = "Custom accent color"
            }
        };
    }

    public override Control BuildView(WidgetContext context)
    {
        var theme = context.CurrentTheme;

        _mainGrid = new Grid
        {
            Background = theme.GetBrush("WidgetBackground"),
            Margin = new Thickness(theme.GetDouble("WidgetPadding"))
        };

        _mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Title
        _mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Status
        _mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Progress
        _mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Button
        _mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star)); // Data list

        // Title
        var customTitle = context.GetConfigValue("customTitle", "Comprehensive Example");
        _titleText = new TextBlock
        {
            Text = customTitle,
            FontSize = theme.GetDouble("HeaderFontSize"),
            FontWeight = FontWeight.Bold,
            Foreground = theme.GetBrush("HeaderForeground"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(_titleText, 0);
        _mainGrid.Children.Add(_titleText);

        // Status
        _statusText = new TextBlock
        {
            Text = "Initializing...",
            FontSize = theme.GetDouble("BodyFontSize"),
            Foreground = theme.GetBrush("BodyForeground"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(_statusText, 1);
        _mainGrid.Children.Add(_statusText);

        // Progress bar
        if (context.GetConfigValue("showProgress", true))
        {
            _progressBar = new ProgressBar
            {
                Height = 20,
                Margin = new Thickness(0, 0, 0, 10),
                Background = theme.GetBrush("ControlBackground"),
                Foreground = theme.GetBrush("AccentColor")
            };
            Grid.SetRow(_progressBar, 2);
            _mainGrid.Children.Add(_progressBar);
        }

        // Action button
        _actionButton = new Button
        {
            Content = "Perform Action",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10),
            Background = theme.GetBrush("AccentColor"),
            Foreground = theme.GetBrush("AccentForeground"),
            Padding = new Thickness(theme.GetDouble("ButtonPadding")),
            CornerRadius = new CornerRadius(theme.GetDouble("CornerRadius"))
        };
        _actionButton.Click += OnActionButtonClick;
        Grid.SetRow(_actionButton, 3);
        _mainGrid.Children.Add(_actionButton);

        // Data list
        if (context.GetConfigValue("showDataList", true))
        {
            _dataList = new ListBox
            {
                Background = theme.GetBrush("ControlBackground"),
                Foreground = theme.GetBrush("BodyForeground"),
                BorderBrush = theme.GetBrush("BorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(theme.GetDouble("CornerRadius")),
                Margin = new Thickness(0, 5, 0, 0)
            };
            Grid.SetRow(_dataList, 4);
            _mainGrid.Children.Add(_dataList);
        }

        return _mainGrid;
    }

    public override Control? GetConfigurationView(WidgetContext context)
    {
        var theme = context.CurrentTheme;
        var configGrid = new Grid
        {
            Background = theme.GetBrush("WidgetBackground"),
            Margin = new Thickness(10)
        };

        configGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        configGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        configGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        configGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        configGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        // Update interval
        var intervalPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5) };
        intervalPanel.Children.Add(new TextBlock { Text = "Update Interval (ms):", Width = 150, VerticalAlignment = VerticalAlignment.Center });
        var intervalSlider = new Slider
        {
            Minimum = 100,
            Maximum = 10000,
            Value = context.GetConfigValue("updateInterval", 1000),
            Width = 200
        };
        intervalPanel.Children.Add(intervalSlider);
        Grid.SetRow(intervalPanel, 0);
        configGrid.Children.Add(intervalPanel);

        // Show progress checkbox
        var progressCheck = new CheckBox
        {
            Content = "Show Progress Bar",
            IsChecked = context.GetConfigValue("showProgress", true),
            Margin = new Thickness(0, 5)
        };
        Grid.SetRow(progressCheck, 1);
        configGrid.Children.Add(progressCheck);

        // Show data list checkbox
        var dataListCheck = new CheckBox
        {
            Content = "Show Data List",
            IsChecked = context.GetConfigValue("showDataList", true),
            Margin = new Thickness(0, 5)
        };
        Grid.SetRow(dataListCheck, 2);
        configGrid.Children.Add(dataListCheck);

        // Enable animations checkbox
        var animationsCheck = new CheckBox
        {
            Content = "Enable Animations",
            IsChecked = context.GetConfigValue("enableAnimations", true),
            Margin = new Thickness(0, 5)
        };
        Grid.SetRow(animationsCheck, 3);
        configGrid.Children.Add(animationsCheck);

        // Custom title
        var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5) };
        titlePanel.Children.Add(new TextBlock { Text = "Custom Title:", Width = 150, VerticalAlignment = VerticalAlignment.Center });
        var titleTextBox = new TextBox
        {
            Text = context.GetConfigValue("customTitle", "Comprehensive Example"),
            Width = 200
        };
        titlePanel.Children.Add(titleTextBox);
        Grid.SetRow(titlePanel, 4);
        configGrid.Children.Add(titlePanel);

        return configGrid;
    }

    public override async Task OnInitializeAsync(WidgetContext context)
    {
        Logger.Log($"Initializing {Name} widget (Instance: {context.InstanceId})");
        
        // Initialize data
        _counter = 0;
        
        // Set default configuration if not present
        if (!context.HasConfigValue("updateInterval"))
        {
            context.SetConfigValue("updateInterval", 1000);
        }
        
        await Task.Delay(100); // Simulate initialization work
        Logger.Log($"{Name} widget initialized successfully");
    }

    public async Task OnActivateAsync(WidgetContext context)
    {
        Logger.Log($"Activating {Name} widget");
        
        // Start update timer
        var interval = context.GetConfigValue("updateInterval", 1000);
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(interval)
        };
        _updateTimer.Tick += (s, e) => UpdateData(context);
        _updateTimer.Start();

        // Apply entrance animation if enabled
        if (context.GetConfigValue("enableAnimations", true) && _mainGrid != null)
        {
            await WidgetAnimations.FadeInAsync(_mainGrid, TimeSpan.FromMilliseconds(500));
            await WidgetAnimations.ScaleInAsync(_mainGrid, TimeSpan.FromMilliseconds(300));
        }

        UpdateStatus("Active");
        Logger.Log($"{Name} widget activated");
    }

    public override async Task OnDeactivateAsync()
    {
        Logger.Log($"Deactivating {Name} widget");
        
        // Stop timer
        _updateTimer?.Stop();
        _updateTimer = null;

        // Apply exit animation if enabled
        if (_context?.GetConfigValue("enableAnimations", true) == true && _mainGrid != null)
        {
            await WidgetAnimations.FadeOutAsync(_mainGrid, TimeSpan.FromMilliseconds(300));
        }

        UpdateStatus("Inactive");
        Logger.Log($"{Name} widget deactivated");
    }

    public override async Task OnDestroyAsync()
    {
        Logger.Log($"Destroying {Name} widget");
        
        // Cleanup resources
        _updateTimer?.Stop();
        _updateTimer = null;
        
        await Task.Delay(50); // Simulate cleanup work
        Logger.Log($"{Name} widget destroyed");
    }    public async Task OnConfigurationChangedAsync(WidgetContext context)
    {
        Logger.Log($"Configuration changed for {Name} widget");
        
        // Update timer interval
        if (_updateTimer != null)
        {
            var newInterval = context.GetConfigValue("updateInterval", 1000);
            _updateTimer.Interval = TimeSpan.FromMilliseconds(newInterval);
        }

        // Update title if changed
        if (_titleText != null)
        {
            var customTitle = context.GetConfigValue("customTitle", "Comprehensive Example");
            _titleText.Text = customTitle;
        }

        // Show/hide progress bar
        if (_progressBar != null)
        {
            _progressBar.IsVisible = context.GetConfigValue("showProgress", true);
        }

        // Show/hide data list
        if (_dataList != null)
        {
            _dataList.IsVisible = context.GetConfigValue("showDataList", true);
        }

        // Apply pulse animation to indicate configuration change
        if (context.GetConfigValue("enableAnimations", true) && _mainGrid != null)
        {
            await WidgetAnimationsExtended.PulseAsync(_mainGrid, 500);
        }

        Logger.Log($"Configuration updated for {Name} widget");
    }

    protected override async Task OnThemeChangedInternalAsync(string themeName)
    {
        await base.OnThemeChangedInternalAsync(themeName);
        
        var newTheme = _context?.ThemeManager?.GetCurrentTheme() ?? new WidgetTheme();
        
        Logger.Log($"Theme changed for {Name} widget to: {newTheme.Name}");
        
        // Update colors and styles
        if (_mainGrid != null)
        {
            _mainGrid.Background = newTheme.GetBrush("WidgetBackground");
        }

        if (_titleText != null)
        {
            _titleText.Foreground = newTheme.GetBrush("HeaderForeground");
            _titleText.FontSize = newTheme.GetDouble("HeaderFontSize");
        }

        if (_statusText != null)
        {
            _statusText.Foreground = newTheme.GetBrush("BodyForeground");
            _statusText.FontSize = newTheme.GetDouble("BodyFontSize");
        }

        if (_actionButton != null)
        {
            _actionButton.Background = newTheme.GetBrush("AccentColor");
            _actionButton.Foreground = newTheme.GetBrush("AccentForeground");
            _actionButton.CornerRadius = new CornerRadius(newTheme.GetDouble("CornerRadius"));
        }

        if (_progressBar != null)
        {
            _progressBar.Background = newTheme.GetBrush("ControlBackground");
            _progressBar.Foreground = newTheme.GetBrush("AccentColor");
        }

        if (_dataList != null)
        {
            _dataList.Background = newTheme.GetBrush("ControlBackground");
            _dataList.Foreground = newTheme.GetBrush("BodyForeground");
            _dataList.BorderBrush = newTheme.GetBrush("BorderBrush");
        }

        // Apply theme change animation
        if (_context?.GetConfigValue("enableAnimations", true) == true && _mainGrid != null)
        {
            await WidgetAnimationsExtended.PulseAsync(_mainGrid, 400);
        }

        Logger.Log($"Theme applied to {Name} widget");
    }

    public override async Task<Dictionary<string, object>> ExportDataAsync()
    {
        var data = new Dictionary<string, object>
        {
            ["counter"] = _counter,
            ["lastUpdate"] = DateTime.Now,
            ["dataItems"] = _dataList?.Items?.Cast<object>().ToList() ?? new List<object>()
        };

        Logger.Log($"Exported data from {Name} widget: {data.Count} items");
        await Task.CompletedTask;
        return data;
    }

    public override async Task ImportDataAsync(Dictionary<string, object> data)
    {
        try
        {
            if (data.TryGetValue("counter", out var counterValue) && counterValue is int counter)
            {
                _counter = counter;
            }

            if (data.TryGetValue("dataItems", out var itemsValue) && itemsValue is List<object> items && _dataList != null)
            {
                _dataList.Items.Clear();
                foreach (var item in items)
                {
                    _dataList.Items.Add(item);
                }
            }

            UpdateStatus($"Imported {data.Count} data items");
            Logger.Log($"Imported data to {Name} widget: {data.Count} items");
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to import data to {Name} widget: {ex.Message}");
        }

        await Task.CompletedTask;
    }    private async void OnActionButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            if (_actionButton != null)
            {
                _actionButton.IsEnabled = false;
                _actionButton.Content = "Working...";

                // Simulate work with animation
                if (_mainGrid != null)
                {
                    await WidgetAnimationsExtended.PulseAsync(_actionButton, 300);
                }

                // Add random data item
                var dataItem = $"Item {_counter++}: {DateTime.Now:HH:mm:ss}";
                _dataList?.Items.Add(dataItem);

                // Limit items
                var maxItems = 10; // Could be configurable
                while (_dataList?.Items.Count > maxItems)
                {
                    _dataList.Items.RemoveAt(0);
                }

                // Update progress
                if (_progressBar != null)
                {
                    _progressBar.Value = (_random.NextDouble() * 100);
                }

                UpdateStatus($"Action performed at {DateTime.Now:HH:mm:ss}");

                await Task.Delay(1000); // Simulate work

                _actionButton.Content = "Perform Action";
                _actionButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error in action button click: {ex.Message}");
        }
    }

    private void UpdateData(WidgetContext context)
    {
        try
        {
            // Update progress bar
            if (_progressBar != null)
            {
                _progressBar.Value = (_random.NextDouble() * 100);
            }

            // Update counter
            _counter++;

            // Add periodic data if enabled
            if (context.GetConfigValue("showDataList", true) && _counter % 5 == 0)
            {
                var dataItem = $"Auto-generated {_counter}: {DateTime.Now:HH:mm:ss}";
                _dataList?.Items.Add(dataItem);

                // Limit items
                var maxItems = context.GetConfigValue("maxDataItems", 10);
                while (_dataList?.Items.Count > maxItems)
                {
                    _dataList.Items.RemoveAt(0);
                }
            }

            UpdateStatus($"Updated: {DateTime.Now:HH:mm:ss} (Count: {_counter})");
        }
        catch (Exception ex)
        {
            Logger.Log($"Error updating data: {ex.Message}");
        }
    }

    private void UpdateStatus(string status)
    {
        if (_statusText != null)
        {
            _statusText.Text = status;
        }
    }
}