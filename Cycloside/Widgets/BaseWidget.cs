using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Widgets;

/// <summary>
/// Base implementation of IWidgetV2 providing sensible defaults and common functionality
/// </summary>
public abstract class BaseWidget : IWidgetV2
{
    protected WidgetContext? _context;
    protected Dictionary<string, object> _configuration = new();
    protected string _currentTheme = "Default";

    #region IWidget Implementation
    public abstract string Name { get; }
    
    public virtual Control BuildView()
    {
        // Fallback for legacy IWidget compatibility
        return _context != null ? BuildView(_context) : new TextBlock { Text = "Widget not initialized" };
    }
    #endregion

    #region IWidgetV2 Implementation
    public virtual string Version => "1.0.0";
    public virtual string Description => $"A {Name} widget";
    public virtual string Category => "General";
    public virtual string Icon => "widget-icon";
    public virtual bool SupportsMultipleInstances => true;
    public virtual (double Width, double Height) DefaultSize => (200, 150);
    public virtual (double Width, double Height) MinimumSize => (100, 80);
    public virtual (double Width, double Height) MaximumSize => (double.PositiveInfinity, double.PositiveInfinity);
    public virtual bool IsResizable => true;

    public virtual WidgetConfigurationSchema ConfigurationSchema => new()
    {
        Properties = GetConfigurationProperties(),
        DefaultValues = GetDefaultConfiguration()
    };

    public virtual async Task OnInitializeAsync(WidgetContext context)
    {
        _context = context;
        _configuration = new Dictionary<string, object>(context.Configuration);
        _currentTheme = context.Theme?.Name ?? "Default";
        await OnInitializeInternalAsync();
    }

    public virtual async Task OnActivateAsync()
    {
        await OnActivateInternalAsync();
    }

    public virtual async Task OnDeactivateAsync()
    {
        await OnDeactivateInternalAsync();
    }

    public virtual async Task OnDestroyAsync()
    {
        await OnDestroyInternalAsync();
        _context = null;
    }

    public virtual async Task OnConfigurationChangedAsync(Dictionary<string, object> configuration)
    {
        _configuration = new Dictionary<string, object>(configuration);
        await OnConfigurationChangedInternalAsync(configuration);
    }

    public virtual async Task OnThemeChangedAsync(string themeName)
    {
        _currentTheme = themeName;
        await OnThemeChangedInternalAsync(themeName);
    }

    public abstract Control BuildView(WidgetContext context);

    public virtual Control? GetConfigurationView(WidgetContext context)
    {
        // Default implementation creates a simple form based on configuration schema
        return CreateDefaultConfigurationView(context);
    }

    public virtual WidgetValidationResult ValidateConfiguration(Dictionary<string, object> configuration)
    {
        var result = new WidgetValidationResult { IsValid = true };
        
        // Validate against schema
        foreach (var property in ConfigurationSchema.Properties)
        {
            if (property.IsRequired && !configuration.ContainsKey(property.Name))
            {
                result.Errors.Add($"Required property '{property.DisplayName}' is missing");
                result.IsValid = false;
            }
            
            if (configuration.TryGetValue(property.Name, out var value))
            {
                var validationResult = ValidatePropertyValue(property, value);
                if (!validationResult.IsValid)
                {
                    result.Errors.AddRange(validationResult.Errors);
                    result.IsValid = false;
                }
            }
        }
        
        return result;
    }

    public virtual Task<Dictionary<string, object>> ExportDataAsync()
    {
        return Task.FromResult(new Dictionary<string, object>
        {
            ["configuration"] = _configuration,
            ["theme"] = _currentTheme ?? string.Empty,
            ["version"] = Version,
            ["exportDate"] = DateTime.UtcNow
        });
    }

    public virtual async Task ImportDataAsync(Dictionary<string, object> data)
    {
        if (data.TryGetValue("configuration", out var configObj) && 
            configObj is Dictionary<string, object> config)
        {
            await OnConfigurationChangedAsync(config);
        }
        
        if (data.TryGetValue("theme", out var themeObj) && 
            themeObj is string theme)
        {
            await OnThemeChangedAsync(theme);
        }
    }

    public virtual async Task<object?> HandleCommandAsync(string command, Dictionary<string, object>? parameters = null)
    {
        return await HandleCommandInternalAsync(command, parameters ?? new Dictionary<string, object>());
    }
    #endregion

    #region Protected Virtual Methods for Subclasses
    protected virtual async Task OnInitializeInternalAsync() { await Task.CompletedTask; }
    protected virtual async Task OnActivateInternalAsync() { await Task.CompletedTask; }
    protected virtual async Task OnDeactivateInternalAsync() { await Task.CompletedTask; }
    protected virtual async Task OnDestroyInternalAsync() { await Task.CompletedTask; }
    protected virtual async Task OnConfigurationChangedInternalAsync(Dictionary<string, object> configuration) { await Task.CompletedTask; }
    protected virtual async Task OnThemeChangedInternalAsync(string themeName) { await Task.CompletedTask; }
    protected virtual async Task<object?> HandleCommandInternalAsync(string command, Dictionary<string, object> parameters) { await Task.CompletedTask; return null; }

    protected virtual List<WidgetConfigurationProperty> GetConfigurationProperties()
    {
        return new List<WidgetConfigurationProperty>();
    }

    protected virtual Dictionary<string, object> GetDefaultConfiguration()
    {
        return new Dictionary<string, object>();
    }
    #endregion

    #region Helper Methods
    protected T GetConfigurationValue<T>(string key, T defaultValue = default!)
    {
        if (_configuration.TryGetValue(key, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    protected void PublishEvent(string eventName, object? data = null)
    {
        _context?.EventPublisher(eventName, data);
    }

    protected async Task ExecuteCommandAsync(string command)
    {
        if (_context != null)
        {
            await _context.CommandExecutor(command);
        }
    }

    private Control CreateDefaultConfigurationView(WidgetContext context)
    {
        var panel = new StackPanel { Spacing = 10 };
        
        foreach (var property in ConfigurationSchema.Properties)
        {
            var label = new TextBlock 
            { 
                Text = property.DisplayName,
                FontWeight = Avalonia.Media.FontWeight.Bold
            };
            panel.Children.Add(label);
            
            if (!string.IsNullOrEmpty(property.Description))
            {
                var description = new TextBlock 
                { 
                    Text = property.Description,
                    FontSize = 11,
                    Foreground = Avalonia.Media.Brushes.Gray
                };
                panel.Children.Add(description);
            }
            
            var control = CreatePropertyControl(property, context);
            if (control != null)
            {
                panel.Children.Add(control);
            }
        }
        
        return new ScrollViewer { Content = panel };
    }

    private Control? CreatePropertyControl(WidgetConfigurationProperty property, WidgetContext context)
    {
        var currentValue = _configuration.TryGetValue(property.Name, out var val) ? val : property.DefaultValue;
        
        return property.Type switch
        {
            WidgetPropertyType.String => new TextBox { Text = currentValue?.ToString() ?? "" },
            WidgetPropertyType.Integer => new NumericUpDown { Value = Convert.ToDecimal(currentValue ?? 0) },
            WidgetPropertyType.Double => new NumericUpDown { Value = Convert.ToDecimal(currentValue ?? 0.0) },
            WidgetPropertyType.Boolean => new CheckBox { IsChecked = Convert.ToBoolean(currentValue ?? false) },
            WidgetPropertyType.Dropdown => CreateDropdownControl(property, currentValue),
            _ => new TextBlock { Text = $"Unsupported property type: {property.Type}" }
        };
    }

    private Control CreateDropdownControl(WidgetConfigurationProperty property, object? currentValue)
    {
        var comboBox = new ComboBox();
        
        if (property.Options != null)
        {
            foreach (var option in property.Options)
            {
                comboBox.Items.Add(new ComboBoxItem 
                { 
                    Content = option.DisplayText, 
                    Tag = option.Value 
                });
            }
            
            // Set selected item
            if (currentValue != null)
            {
                var selectedItem = comboBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(item => item.Tag?.ToString() == currentValue.ToString());
                if (selectedItem != null)
                {
                    comboBox.SelectedItem = selectedItem;
                }
            }
        }
        
        return comboBox;
    }

    private WidgetValidationResult ValidatePropertyValue(WidgetConfigurationProperty property, object value)
    {
        var result = new WidgetValidationResult { IsValid = true };
        
        // Type validation
        try
        {
            switch (property.Type)
            {
                case WidgetPropertyType.Integer:
                    Convert.ToInt32(value);
                    break;
                case WidgetPropertyType.Double:
                    Convert.ToDouble(value);
                    break;
                case WidgetPropertyType.Boolean:
                    Convert.ToBoolean(value);
                    break;
            }
        }
        catch
        {
            result.Errors.Add($"Invalid value type for property '{property.DisplayName}'");
            result.IsValid = false;
        }
        
        return result;
    }
    #endregion
}