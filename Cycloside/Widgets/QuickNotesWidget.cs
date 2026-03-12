using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Cycloside.Widgets.Animations;
using Cycloside.Widgets.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Cycloside.Widgets;

/// <summary>
/// A quick notes widget for jotting down thoughts and reminders
/// </summary>
public class QuickNotesWidget : BaseWidget
{
    private TextBox? _notesTextBox;
    private Border? _container;
    private Button? _saveButton;
    private Button? _clearButton;
    private TextBlock? _statusText;
    private string _notesFilePath = "";
    private DateTime _lastAutoSave = DateTime.MinValue;
    
    public override string Name => "Quick Notes";
    public override string Description => "A simple notepad for quick thoughts and reminders";
    public override string Category => "Productivity";
    public override string Icon => "note";
    public override bool SupportsMultipleInstances => true;
    public override (double Width, double Height) DefaultSize => (300, 200);
    public override (double Width, double Height) MinimumSize => (200, 150);
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
                Name = "autoSave",
                DisplayName = "Auto Save",
                Description = "Automatically save notes as you type",
                Type = WidgetPropertyType.Boolean,
                DefaultValue = true,
                IsRequired = false
            },
            new()
            {
                Name = "autoSaveInterval",
                DisplayName = "Auto Save Interval (seconds)",
                Description = "How often to auto-save notes",
                Type = WidgetPropertyType.Integer,
                DefaultValue = 5,
                IsRequired = false
            },
            new()
            {
                Name = "fontSize",
                DisplayName = "Font Size",
                Description = "Font size for the notes text",
                Type = WidgetPropertyType.Integer,
                DefaultValue = 12,
                IsRequired = false
            },
            new()
            {
                Name = "wordWrap",
                DisplayName = "Word Wrap",
                Description = "Enable word wrapping for long lines",
                Type = WidgetPropertyType.Boolean,
                DefaultValue = true,
                IsRequired = false
            },
            new()
            {
                Name = "notesFile",
                DisplayName = "Notes File",
                Description = "File path to save notes (leave empty for default)",
                Type = WidgetPropertyType.String,
                DefaultValue = "",
                IsRequired = false
            }
        };
    }
    
    protected override Dictionary<string, object> GetDefaultConfiguration()
    {
        return new Dictionary<string, object>
        {
            ["autoSave"] = true,
            ["autoSaveInterval"] = 5,
            ["fontSize"] = 12,
            ["wordWrap"] = true,
            ["notesFile"] = ""
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
        var mainPanel = new DockPanel();
        
        // Create header panel
        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        };
        DockPanel.SetDock(headerPanel, Dock.Top);
        
        // Title
        var title = new TextBlock
        {
            Text = "Quick Notes",
            FontSize = theme.FontSize + 1,
            FontWeight = FontWeight.Bold,
            Foreground = theme.ForegroundBrush,
            VerticalAlignment = VerticalAlignment.Center
        };
        headerPanel.Children.Add(title);
        
        // Spacer
        var spacer = new Border { Width = 1 };
        headerPanel.Children.Add(spacer);
        
        // Save button
        _saveButton = new Button
        {
            Content = "Save",
            FontSize = theme.FontSize - 1,
            Padding = new Avalonia.Thickness(8, 2),
            Background = theme.AccentBrush,
            Foreground = theme.ForegroundBrush
        };
        _saveButton.Click += async (s, e) => await SaveNotes();
        headerPanel.Children.Add(_saveButton);
        
        // Clear button
        _clearButton = new Button
        {
            Content = "Clear",
            FontSize = theme.FontSize - 1,
            Padding = new Avalonia.Thickness(8, 2),
            Background = theme.SecondaryBrush,
            Foreground = theme.SecondaryForegroundBrush
        };
        _clearButton.Click += async (s, e) => await ClearNotes();
        headerPanel.Children.Add(_clearButton);
        
        mainPanel.Children.Add(headerPanel);
        
        // Create notes text box
        _notesTextBox = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = GetConfigurationValue("wordWrap", true) ? TextWrapping.Wrap : TextWrapping.NoWrap,
            FontSize = GetConfigurationValue("fontSize", 12),
            Background = theme.InputBackgroundBrush,
            Foreground = theme.InputForegroundBrush,
            BorderBrush = theme.InputBorderBrush,
            Watermark = "Start typing your notes here..."
        };
        
        // Auto-save on text changed
        if (GetConfigurationValue("autoSave", true))
        {
            _notesTextBox.TextChanged += OnTextChanged;
        }
        
        DockPanel.SetDock(_notesTextBox, Dock.Top);
        mainPanel.Children.Add(_notesTextBox);
        
        // Status text
        _statusText = new TextBlock
        {
            Text = "Ready",
            FontSize = theme.FontSize - 2,
            Foreground = theme.SecondaryBrush,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(0, 4, 0, 0)
        };
        DockPanel.SetDock(_statusText, Dock.Bottom);
        mainPanel.Children.Add(_statusText);
        
        _container.Child = mainPanel;
        
        return _container;
    }
    
    public override async Task OnInitializeAsync(WidgetContext context)
    {
        await base.OnInitializeAsync(context);
        
        // Set up notes file path
        var customPath = GetConfigurationValue("notesFile", "");
        if (!string.IsNullOrEmpty(customPath))
        {
            _notesFilePath = customPath;
        }
        else
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var widgetDataPath = Path.Combine(appDataPath, "Cycloside", "Widgets", "QuickNotes");
            Directory.CreateDirectory(widgetDataPath);
            _notesFilePath = Path.Combine(widgetDataPath, $"notes_{context.InstanceId}.txt");
        }
    }
    
    protected override async Task OnActivateInternalAsync()
    {
        await base.OnActivateInternalAsync();
        
        // Load existing notes
        await LoadNotes();
        
        // Animate widget appearance
        if (_container != null)
        {
            await WidgetAnimations.FadeInAsync(_container);
        }
    }
    
    protected override async Task OnDeactivateInternalAsync()
    {
        await base.OnDeactivateInternalAsync();
        
        // Save notes before deactivating
        await SaveNotes();
    }
    
    protected override async Task OnConfigurationChangedInternalAsync(Dictionary<string, object> newConfiguration)
    {
        await base.OnConfigurationChangedInternalAsync(newConfiguration);
        
        // Update text box properties
        if (_notesTextBox != null)
        {
            _notesTextBox.FontSize = GetConfigurationValue("fontSize", 12);
            _notesTextBox.TextWrapping = GetConfigurationValue("wordWrap", true) ? TextWrapping.Wrap : TextWrapping.NoWrap;
            
            // Update auto-save behavior
            _notesTextBox.TextChanged -= OnTextChanged;
            if (GetConfigurationValue("autoSave", true))
            {
                _notesTextBox.TextChanged += OnTextChanged;
            }
        }
        
        // Update notes file path if changed
        var newNotesFile = GetConfigurationValue("notesFile", "");
        if (newNotesFile != GetConfigurationValue("notesFile", "") && !string.IsNullOrEmpty(newNotesFile))
        {
            await SaveNotes(); // Save to old location first
            _notesFilePath = newNotesFile;
            await LoadNotes(); // Load from new location
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
        
        // Update text box appearance
        if (_notesTextBox != null)
        {
            _notesTextBox.Background = theme.InputBackgroundBrush;
            _notesTextBox.Foreground = theme.InputForegroundBrush;
            _notesTextBox.BorderBrush = theme.InputBorderBrush;
        }
        
        // Update button appearances
        if (_saveButton != null)
        {
            _saveButton.Background = theme.AccentBrush;
            _saveButton.Foreground = theme.ForegroundBrush;
        }
        
        if (_clearButton != null)
        {
            _clearButton.Background = theme.SecondaryBrush;
            _clearButton.Foreground = theme.ForegroundBrush;
        }
        
        if (_statusText != null)
        {
            _statusText.Foreground = theme.SecondaryBrush;
        }
    }
    
    private async void OnTextChanged(object? sender, EventArgs e)
    {
        var autoSaveInterval = GetConfigurationValue("autoSaveInterval", 5);
        var now = DateTime.Now;
        
        if ((now - _lastAutoSave).TotalSeconds >= autoSaveInterval)
        {
            await SaveNotes(isAutoSave: true);
            _lastAutoSave = now;
        }
    }
    
    private async Task LoadNotes()
    {
        try
        {
            if (File.Exists(_notesFilePath) && _notesTextBox != null)
            {
                var content = await File.ReadAllTextAsync(_notesFilePath);
                _notesTextBox.Text = content;
                UpdateStatus("Notes loaded");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error loading notes: {ex.Message}");
        }
    }
    
    private async Task SaveNotes(bool isAutoSave = false)
    {
        try
        {
            if (_notesTextBox != null && !string.IsNullOrEmpty(_notesFilePath))
            {
                var directory = Path.GetDirectoryName(_notesFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                await File.WriteAllTextAsync(_notesFilePath, _notesTextBox.Text ?? "");
                
                var statusMessage = isAutoSave ? "Auto-saved" : "Notes saved";
                UpdateStatus(statusMessage);
                
                // Animate save button
                if (_saveButton != null && !isAutoSave)
                {
                    await WidgetAnimations.PulseAsync(_saveButton, 300);
                }
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error saving notes: {ex.Message}");
        }
    }
    
    private async Task ClearNotes()
    {
        if (_notesTextBox != null)
        {
            // Animate before clearing
            await WidgetAnimations.FadeOutAsync(_notesTextBox, TimeSpan.FromMilliseconds(200));
            
            _notesTextBox.Text = "";
            UpdateStatus("Notes cleared");
            
            // Animate back in
            await WidgetAnimations.FadeInAsync(_notesTextBox, TimeSpan.FromMilliseconds(200));
        }
    }
    
    private void UpdateStatus(string message)
    {
        if (_statusText != null)
        {
            _statusText.Text = $"{message} - {DateTime.Now:HH:mm:ss}";
        }
    }
    
    public override async Task<Dictionary<string, object>> ExportDataAsync()
    {
        var data = await base.ExportDataAsync();
        data["notesContent"] = _notesTextBox?.Text ?? "";
        data["notesFilePath"] = _notesFilePath;
        return data;
    }
    
    public override async Task ImportDataAsync(Dictionary<string, object> data)
    {
        await base.ImportDataAsync(data);
        
        if (data.ContainsKey("notesContent") && _notesTextBox != null)
        {
            _notesTextBox.Text = data["notesContent"]?.ToString() ?? "";
        }
        
        if (data.ContainsKey("notesFilePath"))
        {
            _notesFilePath = data["notesFilePath"]?.ToString() ?? _notesFilePath;
        }
    }
}