using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Cycloside.Widgets.Animations;
using Cycloside.Widgets.Themes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Cycloside.Widgets;

/// <summary>
/// A calculator widget for basic mathematical operations
/// </summary>
public class CalculatorWidget : BaseWidget
{
    private TextBox? _displayTextBox;
    private Border? _container;
    private double _currentValue = 0;
    private double _previousValue = 0;
    private string _operation = "";
    private bool _isNewCalculation = true;
    
    public override string Name => "Calculator";
    public override string Description => "A basic calculator for mathematical operations";
    public override string Category => "Productivity";
    public override string Icon => "calculator";
    public override (double Width, double Height) DefaultSize => (250, 320);
    public override (double Width, double Height) MinimumSize => (200, 280);
    public override bool IsResizable => true;
    
    public override WidgetConfigurationSchema ConfigurationSchema => new()
    {
        Properties = GetConfigurationProperties(),
        DefaultValues = GetDefaultConfiguration()
    };
    
    protected override List<WidgetConfigurationProperty> GetConfigurationProperties()
    {
        return new List<WidgetConfigurationProperty>
        {
            new()
            {
                Name = "decimalPlaces",
                DisplayName = "Decimal Places",
                Description = "Number of decimal places to display",
                Type = WidgetPropertyType.Integer,
                DefaultValue = 2,
                IsRequired = false
            },
            new()
            {
                Name = "animateButtons",
                DisplayName = "Animate Buttons",
                Description = "Enable button press animations",
                Type = WidgetPropertyType.Boolean,
                DefaultValue = true,
                IsRequired = false
            },
            new()
            {
                Name = "soundEnabled",
                DisplayName = "Sound Effects",
                Description = "Enable button click sounds",
                Type = WidgetPropertyType.Boolean,
                DefaultValue = false,
                IsRequired = false
            }
        };
    }
    
    protected override Dictionary<string, object> GetDefaultConfiguration()
    {
        return new Dictionary<string, object>
        {
            ["decimalPlaces"] = 2,
            ["animateButtons"] = true,
            ["soundEnabled"] = false
        };
    }
    
    public override Control BuildView(WidgetContext context)
    {
        var theme = context.ThemeManager?.GetCurrentTheme() ?? new WidgetTheme();
        
        // Create main container
        _container = new Border
        {
            Background = theme.BackgroundBrush,
            BorderBrush = theme.BorderBrush,
            BorderThickness = new Avalonia.Thickness(theme.BorderThickness),
            CornerRadius = new Avalonia.CornerRadius(theme.CornerRadius),
            Padding = new Avalonia.Thickness(theme.Padding)
        };
        
        // Create main panel
        var mainPanel = new StackPanel
        {
            Spacing = 8
        };
        
        // Title
        var title = new TextBlock
        {
            Text = "Calculator",
            FontSize = theme.FontSize + 1,
            FontWeight = FontWeight.Bold,
            Foreground = theme.ForegroundBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };
        mainPanel.Children.Add(title);
        
        // Display
        _displayTextBox = new TextBox
        {
            Text = "0",
            FontSize = theme.FontSize + 4,
            FontWeight = FontWeight.Bold,
            IsReadOnly = true,
            TextAlignment = TextAlignment.Right,
            Background = theme.InputBackgroundBrush,
            Foreground = theme.InputForegroundBrush,
            BorderBrush = theme.InputBorderBrush,
            Height = 50,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };
        mainPanel.Children.Add(_displayTextBox);
        
        // Button grid
        var buttonGrid = CreateButtonGrid(theme);
        mainPanel.Children.Add(buttonGrid);
        
        _container.Child = mainPanel;
        
        return _container;
    }
    
    private Grid CreateButtonGrid(WidgetTheme theme)
    {
        var grid = new Grid();
        
        // Define rows and columns
        for (int i = 0; i < 5; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        }
        for (int i = 0; i < 4; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }
        
        // Button definitions: [text, row, col, colspan, isOperation]
        var buttons = new[]
        {
            new { Text = "C", Row = 0, Col = 0, ColSpan = 2, IsOperation = true },
            new { Text = "±", Row = 0, Col = 2, ColSpan = 1, IsOperation = true },
            new { Text = "÷", Row = 0, Col = 3, ColSpan = 1, IsOperation = true },
            
            new { Text = "7", Row = 1, Col = 0, ColSpan = 1, IsOperation = false },
            new { Text = "8", Row = 1, Col = 1, ColSpan = 1, IsOperation = false },
            new { Text = "9", Row = 1, Col = 2, ColSpan = 1, IsOperation = false },
            new { Text = "×", Row = 1, Col = 3, ColSpan = 1, IsOperation = true },
            
            new { Text = "4", Row = 2, Col = 0, ColSpan = 1, IsOperation = false },
            new { Text = "5", Row = 2, Col = 1, ColSpan = 1, IsOperation = false },
            new { Text = "6", Row = 2, Col = 2, ColSpan = 1, IsOperation = false },
            new { Text = "-", Row = 2, Col = 3, ColSpan = 1, IsOperation = true },
            
            new { Text = "1", Row = 3, Col = 0, ColSpan = 1, IsOperation = false },
            new { Text = "2", Row = 3, Col = 1, ColSpan = 1, IsOperation = false },
            new { Text = "3", Row = 3, Col = 2, ColSpan = 1, IsOperation = false },
            new { Text = "+", Row = 3, Col = 3, ColSpan = 1, IsOperation = true },
            
            new { Text = "0", Row = 4, Col = 0, ColSpan = 2, IsOperation = false },
            new { Text = ".", Row = 4, Col = 2, ColSpan = 1, IsOperation = false },
            new { Text = "=", Row = 4, Col = 3, ColSpan = 1, IsOperation = true }
        };
        
        foreach (var buttonDef in buttons)
        {
            var button = new Button
            {
                Content = buttonDef.Text,
                FontSize = theme.FontSize + 2,
                FontWeight = FontWeight.Bold,
                Margin = new Avalonia.Thickness(2),
                Background = buttonDef.IsOperation ? theme.AccentBrush : theme.SecondaryBrush,
                Foreground = theme.ForegroundBrush
            };
            
            button.Click += async (s, e) => await OnButtonClick(buttonDef.Text);
            
            Grid.SetRow(button, buttonDef.Row);
            Grid.SetColumn(button, buttonDef.Col);
            Grid.SetColumnSpan(button, buttonDef.ColSpan);
            
            grid.Children.Add(button);
        }
        
        return grid;
    }
    
    protected override async Task OnActivateInternalAsync()
    {
        await base.OnActivateInternalAsync();
        
        // Reset calculator state
        Clear();
        
        // Animate widget appearance
        if (_container != null)
        {
            await WidgetAnimations.ScaleInAsync(_container);
        }
    }
    
    protected override async Task OnThemeChangedInternalAsync(string themeName)
    {
        await base.OnThemeChangedInternalAsync(themeName);
        
        var theme = _context?.ThemeManager?.GetCurrentTheme() ?? new WidgetTheme();
        
        // Update container appearance
        if (_container != null)
        {
            _container.Background = theme.BackgroundBrush;
            _container.BorderBrush = theme.BorderBrush;
            _container.BorderThickness = new Avalonia.Thickness(theme.BorderThickness);
            _container.CornerRadius = new Avalonia.CornerRadius(theme.CornerRadius);
            _container.Padding = new Avalonia.Thickness(theme.Padding);
        }
        
        // Update display appearance
        if (_displayTextBox != null)
        {
            _displayTextBox.Background = theme.InputBackgroundBrush;
            _displayTextBox.Foreground = theme.InputForegroundBrush;
            _displayTextBox.BorderBrush = theme.InputBorderBrush;
        }
    }
    
    private async Task OnButtonClick(string buttonText)
    {
        var animateButtons = GetConfigurationValue("animateButtons", true);
        
        // Find the clicked button for animation
        if (animateButtons && _container?.Child is StackPanel mainPanel)
        {
            var buttonGrid = mainPanel.Children[2] as Grid;
            var clickedButton = FindButtonByText(buttonGrid, buttonText);
            if (clickedButton != null)
            {
                await WidgetAnimations.PulseAsync(clickedButton, 150);
            }
        }
        
        // Handle button logic
        switch (buttonText)
        {
            case "C":
                Clear();
                break;
            case "±":
                ToggleSign();
                break;
            case "=":
                Calculate();
                break;
            case "+":
            case "-":
            case "×":
            case "÷":
                SetOperation(buttonText);
                break;
            case ".":
                AddDecimalPoint();
                break;
            default:
                if (int.TryParse(buttonText, out int digit))
                {
                    AddDigit(digit);
                }
                break;
        }
        
        UpdateDisplay();
    }
    
    private Button? FindButtonByText(Grid? grid, string text)
    {
        if (grid == null) return null;
        
        foreach (var child in grid.Children)
        {
            if (child is Button button && button.Content?.ToString() == text)
            {
                return button;
            }
        }
        return null;
    }
    
    private void Clear()
    {
        _currentValue = 0;
        _previousValue = 0;
        _operation = "";
        _isNewCalculation = true;
    }
    
    private void ToggleSign()
    {
        _currentValue = -_currentValue;
    }
    
    private void SetOperation(string operation)
    {
        if (!string.IsNullOrEmpty(_operation) && !_isNewCalculation)
        {
            Calculate();
        }
        
        _previousValue = _currentValue;
        _operation = operation;
        _isNewCalculation = true;
    }
    
    private void Calculate()
    {
        if (string.IsNullOrEmpty(_operation)) return;
        
        try
        {
            switch (_operation)
            {
                case "+":
                    _currentValue = _previousValue + _currentValue;
                    break;
                case "-":
                    _currentValue = _previousValue - _currentValue;
                    break;
                case "×":
                    _currentValue = _previousValue * _currentValue;
                    break;
                case "÷":
                    if (_currentValue != 0)
                    {
                        _currentValue = _previousValue / _currentValue;
                    }
                    else
                    {
                        _currentValue = 0; // Division by zero
                    }
                    break;
            }
            
            _operation = "";
            _isNewCalculation = true;
        }
        catch (Exception)
        {
            Clear();
        }
    }
    
    private void AddDigit(int digit)
    {
        if (_isNewCalculation)
        {
            _currentValue = digit;
            _isNewCalculation = false;
        }
        else
        {
            _currentValue = _currentValue * 10 + digit;
        }
    }
    
    private void AddDecimalPoint()
    {
        if (_isNewCalculation)
        {
            _currentValue = 0;
            _isNewCalculation = false;
        }
        
        // Check if decimal point already exists
        var currentText = _displayTextBox?.Text ?? "0";
        if (!currentText.Contains("."))
        {
            // This is simplified - in a real calculator, you'd handle decimal input more carefully
        }
    }
    
    private void UpdateDisplay()
    {
        if (_displayTextBox != null)
        {
            var decimalPlaces = GetConfigurationValue("decimalPlaces", 2);
            var displayValue = Math.Round(_currentValue, decimalPlaces);
            
            // Format the number
            var formattedValue = displayValue.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture);
            
            // Remove trailing zeros after decimal point
            if (formattedValue.Contains("."))
            {
                formattedValue = formattedValue.TrimEnd('0').TrimEnd('.');
            }
            
            _displayTextBox.Text = formattedValue;
        }
    }
    
    public override async Task<Dictionary<string, object>> ExportDataAsync()
    {
        var data = await base.ExportDataAsync();
        data["currentValue"] = _currentValue;
        data["previousValue"] = _previousValue;
        data["operation"] = _operation;
        data["isNewCalculation"] = _isNewCalculation;
        return data;
    }
    
    public override async Task ImportDataAsync(Dictionary<string, object> data)
    {
        await base.ImportDataAsync(data);
        
        if (data.ContainsKey("currentValue"))
        {
            _currentValue = Convert.ToDouble(data["currentValue"]);
        }
        
        if (data.ContainsKey("previousValue"))
        {
            _previousValue = Convert.ToDouble(data["previousValue"]);
        }
        
        if (data.ContainsKey("operation"))
        {
            _operation = data["operation"]?.ToString() ?? "";
        }
        
        if (data.ContainsKey("isNewCalculation"))
        {
            _isNewCalculation = Convert.ToBoolean(data["isNewCalculation"]);
        }
        
        UpdateDisplay();
    }
}