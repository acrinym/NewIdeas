using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace Cycloside.Widgets;

public class WidgetHostViewModel : INotifyPropertyChanged
{
    private readonly WidgetHostWindow _window;
    private readonly WidgetManager _widgetManager;
    private readonly ObservableCollection<WidgetInstance> _widgets = new();
    private bool _hasWidgets;
    private bool _isInManagementMode;
    private bool _showGrid;
    private bool _snapToGrid;

    public event PropertyChangedEventHandler? PropertyChanged;

    public WidgetHostViewModel(WidgetHostWindow window)
    {
        _window = window;
        _widgetManager = new WidgetManager();

        // Load available widgets
        _widgetManager.LoadBuiltIn();

        // Initialize commands
        AddWidgetCommand = new RelayCommand(AddWidget);
        SaveLayoutCommand = new RelayCommand(SaveLayout);
        LoadLayoutCommand = new RelayCommand(LoadLayout);
        ToggleManagementModeCommand = new RelayCommand(ToggleManagementMode);

        // Context menu commands
        ConfigureWidgetCommand = new RelayCommand<WidgetInstance>(ConfigureWidget);
        ResizeWidgetCommand = new RelayCommand<WidgetInstance>(ResizeWidget);
        ChangeWidgetSkinCommand = new RelayCommand<WidgetInstance>(ChangeWidgetSkin);
        RemoveWidgetCommand = new RelayCommand<WidgetInstance>(RemoveWidget);
        ToggleLockWidgetCommand = new RelayCommand<WidgetInstance>(ToggleLockWidget);
        CloneWidgetCommand = new RelayCommand<WidgetInstance>(CloneWidget);
        ChangeAllSkinsCommand = new RelayCommand(ChangeAllSkins);
        ToggleSnapToGridCommand = new RelayCommand(ToggleSnapToGrid);
        ToggleGridVisibilityCommand = new RelayCommand(ToggleGridVisibility);

        // Load saved layout if available
        LoadLayout(null);
    }

    public ObservableCollection<WidgetInstance> Widgets => _widgets;

    public bool HasWidgets
    {
        get => _hasWidgets;
        private set
        {
            if (_hasWidgets != value)
            {
                _hasWidgets = value;
                OnPropertyChanged(nameof(HasWidgets));
            }
        }
    }

    public bool IsInManagementMode
    {
        get => _isInManagementMode;
        private set
        {
            if (_isInManagementMode != value)
            {
                _isInManagementMode = value;
                OnPropertyChanged(nameof(IsInManagementMode));
            }
        }
    }

    public bool ShowGrid
    {
        get => _showGrid;
        private set
        {
            if (_showGrid != value)
            {
                _showGrid = value;
                OnPropertyChanged(nameof(ShowGrid));
            }
        }
    }

    public bool SnapToGrid
    {
        get => _snapToGrid;
        private set
        {
            if (_snapToGrid != value)
            {
                _snapToGrid = value;
                OnPropertyChanged(nameof(SnapToGrid));
            }
        }
    }

    // Commands
    public ICommand AddWidgetCommand { get; }
    public ICommand SaveLayoutCommand { get; }
    public ICommand LoadLayoutCommand { get; }
    public ICommand ToggleManagementModeCommand { get; }
    public ICommand ConfigureWidgetCommand { get; }
    public ICommand ResizeWidgetCommand { get; }
    public ICommand ChangeWidgetSkinCommand { get; }
    public ICommand RemoveWidgetCommand { get; }
    public ICommand ChangeAllSkinsCommand { get; }
    public ICommand ToggleSnapToGridCommand { get; }
    public ICommand ToggleGridVisibilityCommand { get; }
    public ICommand ToggleLockWidgetCommand { get; }
    public ICommand CloneWidgetCommand { get; }

    private void AddWidget(object? parameter)
    {
        // Show widget selection dialog or menu
        // For now, add a clock widget as example
        var clockWidget = _widgetManager.Widgets.FirstOrDefault(w => w.Name == "Clock");
        if (clockWidget != null)
        {
            AddWidget(clockWidget);
        }
    }

    public void AddWidget(IWidget widget)
    {
        var instance = new WidgetInstance(widget);
        _widgets.Add(instance);

        // Add to canvas
        var container = CreateWidgetContainer(instance);
        _window.Root.Children.Add(container);

        // Position randomly for now (in real implementation, use saved position or default)
        Canvas.SetLeft(container, _widgets.Count * 150 + 50);
        Canvas.SetTop(container, 100);

        HasWidgets = true;
        Logger.Log($"Widget added: {widget.Name}");
    }

    public void RemoveWidget(IWidget widget)
    {
        var instance = _widgets.FirstOrDefault(w => w.Widget == widget);
        if (instance != null)
        {
            RemoveWidget(instance);
        }
    }

    public void RemoveWidget(WidgetInstance? instance)
    {
        if (instance == null) return;
        _widgets.Remove(instance);

        // Remove from canvas
        var container = _window.Root.Children.OfType<WidgetContainer>()
            .FirstOrDefault(c => c.WidgetInstance == instance);

        if (container != null)
        {
            _window.Root.Children.Remove(container);
        }

        HasWidgets = _widgets.Any();
        Logger.Log($"Widget removed: {instance.Widget.Name}");
    }

    private WidgetContainer CreateWidgetContainer(WidgetInstance instance)
    {
        var container = new WidgetContainer(instance, this);
        container.DataContext = instance;
        return container;
    }

    public void SaveLayout(object? parameter)
    {
        try
        {
            var layout = new WidgetLayout
            {
                Widgets = _widgets.Select(w => new WidgetLayout.WidgetData
                {
                    Type = w.Widget.Name,
                    X = Canvas.GetLeft(w.Container),
                    Y = Canvas.GetTop(w.Container),
                    Width = w.Container.Bounds.Width,
                    Height = w.Container.Bounds.Height,
                    Skin = w.Skin,
                    Settings = w.Settings
                }).ToList()
            };

            var json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("widget_layout.json", json);

            Logger.Log("Widget layout saved");
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to save widget layout: {ex.Message}");
        }
    }

    public void LoadLayout(object? parameter)
    {
        try
        {
            if (!File.Exists("widget_layout.json"))
                return;

            var json = File.ReadAllText("widget_layout.json");
            var layout = JsonSerializer.Deserialize<WidgetLayout>(json);

            if (layout == null) return;

            // Clear existing widgets
            foreach (var widget in _widgets.ToList())
            {
                RemoveWidget(widget);
            }

            // Load saved widgets
            foreach (var widgetData in layout.Widgets)
            {
                var widget = _widgetManager.Widgets.FirstOrDefault(w => w.Name == widgetData.Type);
                if (widget != null)
                {
                    var instance = new WidgetInstance(widget);
                    _widgets.Add(instance);

                    var container = CreateWidgetContainer(instance);
                    _window.Root.Children.Add(container);

                    Canvas.SetLeft(container, widgetData.X);
                    Canvas.SetTop(container, widgetData.Y);
                    container.Width = widgetData.Width;
                    container.Height = widgetData.Height;

                    if (!string.IsNullOrEmpty(widgetData.Skin))
                        instance.Skin = widgetData.Skin;

                    HasWidgets = true;
                }
            }

            Logger.Log($"Loaded widget layout: {layout.Widgets.Count} widgets");
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to load widget layout: {ex.Message}");
        }
    }

    private void ToggleManagementMode(object? parameter)
    {
        IsInManagementMode = !IsInManagementMode;

        // Show/hide management overlays on all widgets
        foreach (var widget in _widgets)
        {
            if (widget.Container is WidgetContainer container)
            {
                container.ShowManagementOverlay = IsInManagementMode;
            }
        }
    }

    private void ConfigureWidget(WidgetInstance? instance)
    {
        if (instance != null)
        {
            // Show widget-specific configuration dialog
            Logger.Log($"Configure widget: {instance.Widget.Name}");
            // Implementation would show a configuration window
        }
    }

    private void ResizeWidget(WidgetInstance? instance)
    {
        if (instance?.Container is WidgetContainer container)
        {
            // Enter resize mode for the widget
            container.IsInResizeMode = true;
            Logger.Log($"Resize widget: {instance.Widget.Name}");
        }
    }

    private void ChangeWidgetSkin(WidgetInstance? instance)
    {
        if (instance != null)
        {
            // Show skin selection dialog
            Logger.Log($"Change skin for widget: {instance.Widget.Name}");
            // Implementation would show skin selection
        }
    }

    private void ToggleLockWidget(WidgetInstance? instance)
    {
        if (instance != null)
        {
            instance.IsLocked = !instance.IsLocked;
            Logger.Log($"Widget {(instance.IsLocked ? "locked" : "unlocked")}: {instance.Widget.Name}");
        }
    }

    private void CloneWidget(WidgetInstance? instance)
    {
        if (instance != null)
        {
            // Create a copy of the widget
            var newInstance = new WidgetInstance(instance.Widget)
            {
                Skin = instance.Skin,
                Settings = instance.Settings
            };

            _widgets.Add(newInstance);

            var container = CreateWidgetContainer(newInstance);
            _window.Root.Children.Add(container);

            // Position slightly offset from original
            var originalLeft = Canvas.GetLeft(instance.Container);
            var originalTop = Canvas.GetTop(instance.Container);
            Canvas.SetLeft(container, originalLeft + 20);
            Canvas.SetTop(container, originalTop + 20);

            HasWidgets = true;
            Logger.Log($"Cloned widget: {instance.Widget.Name}");
        }
    }

    private void ChangeAllSkins(object? parameter)
    {
        // Show global skin selection dialog
        Logger.Log("Change skins for all widgets");
        // Implementation would show global skin selection
    }


    private void ToggleGridVisibility(object? parameter)
    {
        ShowGrid = !ShowGrid;
        Logger.Log($"Grid visibility: {ShowGrid}");
    }

    private void ToggleSnapToGrid(object? parameter)
    {
        SnapToGrid = !SnapToGrid;
        Logger.Log($"Snap to grid: {SnapToGrid}");
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// Supporting classes
public class WidgetInstance : INotifyPropertyChanged
{
    private string _skin = "Default";
    private string _settings = "{}";
    private bool _isLocked;

    public event PropertyChangedEventHandler? PropertyChanged;

    public WidgetInstance(IWidget widget)
    {
        Widget = widget;
        Container = null!; // Will be set when added to canvas
    }

    public IWidget Widget { get; }
    public Control Container { get; set; } = null!;

    public string Skin
    {
        get => _skin;
        set
        {
            if (_skin != value)
            {
                _skin = value;
                OnPropertyChanged(nameof(Skin));
            }
        }
    }

    public string Settings
    {
        get => _settings;
        set
        {
            if (_settings != value)
            {
                _settings = value;
                OnPropertyChanged(nameof(Settings));
            }
        }
    }

    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (_isLocked != value)
            {
                _isLocked = value;
                OnPropertyChanged(nameof(IsLocked));
            }
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class WidgetLayout
{
    public List<WidgetData> Widgets { get; set; } = new();

    public class WidgetData
    {
        public string Type { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Skin { get; set; } = "Default";
        public string Settings { get; set; } = "{}";
    }
}
