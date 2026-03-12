using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Cycloside.Widgets.Themes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Cycloside.Widgets;

/// <summary>
/// Enhanced widget host view model that supports both IWidget and IWidgetV2 interfaces
/// </summary>
public class EnhancedWidgetHostViewModel : INotifyPropertyChanged
{
    private readonly WidgetHostWindow _window;
    private readonly WidgetManager _widgetManager;
    private readonly WidgetThemeManager _themeManager;
    private readonly ObservableCollection<EnhancedWidgetInstance> _widgets = new();
    private bool _hasWidgets;
    private bool _isInManagementMode;
    private bool _showGrid;
    private bool _snapToGrid;
    private string _currentTheme = "Default";

    public event PropertyChangedEventHandler? PropertyChanged;

    public EnhancedWidgetHostViewModel(WidgetHostWindow window)
    {
        _window = window;
        _widgetManager = new WidgetManager();
        _themeManager = new WidgetThemeManager();

        // Load available widgets
        _widgetManager.LoadBuiltIn();

        // Initialize commands
        AddWidgetCommand = new RelayCommand(AddWidget);
        SaveLayoutCommand = new RelayCommand(SaveLayout);
        LoadLayoutCommand = new RelayCommand(LoadLayout);
        ToggleManagementModeCommand = new RelayCommand(ToggleManagementMode);
        ChangeThemeCommand = new RelayCommand<string>(ChangeTheme);

        // Context menu commands
        ConfigureWidgetCommand = new RelayCommand<EnhancedWidgetInstance>(ConfigureWidget);
        ResizeWidgetCommand = new RelayCommand<EnhancedWidgetInstance>(ResizeWidget);
        ChangeWidgetSkinCommand = new RelayCommand<EnhancedWidgetInstance>(ChangeWidgetSkin);
        RemoveWidgetCommand = new RelayCommand<EnhancedWidgetInstance>(RemoveWidgetSync);
        ToggleLockWidgetCommand = new RelayCommand<EnhancedWidgetInstance>(ToggleLockWidget);
        CloneWidgetCommand = new RelayCommand<EnhancedWidgetInstance>(CloneWidget);
        ChangeAllSkinsCommand = new RelayCommand(ChangeAllSkins);
        ToggleSnapToGridCommand = new RelayCommand(ToggleSnapToGrid);
        ToggleGridVisibilityCommand = new RelayCommand(ToggleGridVisibility);
        RefreshWidgetCommand = new RelayCommand<EnhancedWidgetInstance>(RefreshWidget);
        ExportWidgetDataCommand = new RelayCommand<EnhancedWidgetInstance>(ExportWidgetData);
        ImportWidgetDataCommand = new RelayCommand<EnhancedWidgetInstance>(ImportWidgetData);

        // Load saved layout if available
        LoadLayout(null);
    }

    public ObservableCollection<EnhancedWidgetInstance> Widgets => _widgets;
    public WidgetThemeManager ThemeManager => _themeManager;
    public IEnumerable<string> AvailableThemes => _themeManager.AvailableThemeNames;

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

    public string CurrentTheme
    {
        get => _currentTheme;
        private set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                OnPropertyChanged(nameof(CurrentTheme));
            }
        }
    }

    // Commands
    public ICommand AddWidgetCommand { get; }
    public ICommand SaveLayoutCommand { get; }
    public ICommand LoadLayoutCommand { get; }
    public ICommand ToggleManagementModeCommand { get; }
    public ICommand ChangeThemeCommand { get; }
    public ICommand ConfigureWidgetCommand { get; }
    public ICommand ResizeWidgetCommand { get; }
    public ICommand ChangeWidgetSkinCommand { get; }
    public ICommand RemoveWidgetCommand { get; }
    public ICommand ChangeAllSkinsCommand { get; }
    public ICommand ToggleSnapToGridCommand { get; }
    public ICommand ToggleGridVisibilityCommand { get; }
    public ICommand ToggleLockWidgetCommand { get; }
    public ICommand CloneWidgetCommand { get; }
    public ICommand RefreshWidgetCommand { get; }
    public ICommand ExportWidgetDataCommand { get; }
    public ICommand ImportWidgetDataCommand { get; }

    private void AddWidget(object? parameter)
    {
        // Show widget selection dialog or menu
        // For demonstration, add different widget types
        var widgetTypes = new[] { "SystemMonitor", "NetworkMonitor", "QuickNotes", "Calculator" };
        var random = new Random();
        var selectedType = widgetTypes[random.Next(widgetTypes.Length)];
        
        _ = AddWidgetByType(selectedType);
    }

    public async Task AddWidgetByType(string widgetType)
    {
        try
        {
            IWidget? widget = widgetType switch
            {
                "SystemMonitor" => new SystemMonitorWidget(),
                "NetworkMonitor" => new NetworkMonitorWidget(),
                "QuickNotes" => new QuickNotesWidget(),
                "Calculator" => new CalculatorWidget(),
                _ => _widgetManager.Widgets.FirstOrDefault(w => w.Name == widgetType)
            };

            if (widget != null)
            {
                await AddWidget(widget);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to add widget {widgetType}: {ex.Message}");
        }
    }

    public async Task AddWidget(IWidget widget)
    {
        var instance = new EnhancedWidgetInstance(widget, _themeManager);
        _widgets.Add(instance);

        // Initialize the widget if it's IWidgetV2
        if (widget is IWidgetV2 widgetV2)
        {
            var context = CreateWidgetContext(instance);
            await widgetV2.OnInitializeAsync(context);
        }

        // Add to canvas
        var container = CreateWidgetContainer(instance);
        _window.Root.Children.Add(container);

        // Position widgets in a grid layout
        var gridSize = 200;
        var columns = (int)Math.Ceiling(Math.Sqrt(_widgets.Count));
        var row = (_widgets.Count - 1) / columns;
        var col = (_widgets.Count - 1) % columns;
        
        Canvas.SetLeft(container, col * gridSize + 50);
        Canvas.SetTop(container, row * gridSize + 100);

        // Activate the widget if it's IWidgetV2
        if (widget is IWidgetV2 widgetV2Active)
        {
            await widgetV2Active.OnActivateAsync();
        }

        HasWidgets = true;
        Logger.Log($"Widget added: {widget.Name}");
    }

    public async Task RemoveWidget(IWidget widget)
    {
        var instance = _widgets.FirstOrDefault(w => w.Widget == widget);
        if (instance != null)
        {
            await RemoveWidget(instance);
        }
    }

    private void RemoveWidgetSync(EnhancedWidgetInstance? instance)
    {
        _ = RemoveWidget(instance);
    }

    public async Task RemoveWidget(EnhancedWidgetInstance? instance)
    {
        if (instance == null) return;

        // Deactivate and destroy the widget if it's IWidgetV2
        if (instance.Widget is IWidgetV2 widgetV2)
        {
            var context = CreateWidgetContext(instance);
            await widgetV2.OnDeactivateAsync();
            await widgetV2.OnDestroyAsync();
        }

        _widgets.Remove(instance);

        // Remove from canvas
        var container = _window.Root.Children.OfType<WidgetContainer>()
            .FirstOrDefault(c => c.DataContext == instance);

        if (container != null)
        {
            _window.Root.Children.Remove(container);
        }

        HasWidgets = _widgets.Any();
        Logger.Log($"Widget removed: {instance.Widget.Name}");
    }

    private EnhancedWidgetContainer CreateWidgetContainer(EnhancedWidgetInstance instance)
    {
        var container = new EnhancedWidgetContainer(instance, this);
        container.DataContext = instance;

        // Build the widget view
        if (instance.Widget is IWidgetV2 widgetV2)
        {
            var context = CreateWidgetContext(instance);
            var view = widgetV2.BuildView(context);
            instance.View = view;
        }

        return container;
    }

    private WidgetContext CreateWidgetContext(EnhancedWidgetInstance instance)
    {
        return new WidgetContext
        {
            ThemeManager = _themeManager,
            Configuration = instance.Configuration,
            InstanceId = instance.InstanceId,
            HostViewModel = this
        };
    }

    private async void ChangeTheme(string? themeName)
    {
        if (string.IsNullOrEmpty(themeName)) return;

        var oldTheme = _themeManager.GetCurrentTheme();
        _themeManager.SetCurrentTheme(themeName);
        var newTheme = _themeManager.GetCurrentTheme();
        
        CurrentTheme = themeName;

        // Notify all IWidgetV2 widgets about theme change
        foreach (var instance in _widgets)
        {
            if (instance.Widget is IWidgetV2 widgetV2)
            {
                await widgetV2.OnThemeChangedAsync(themeName);
            }
        }

        Logger.Log($"Theme changed to: {themeName}");
    }

    public void SaveLayout(object? parameter)
    {
        try
        {
            var layout = new EnhancedWidgetLayout
            {
                Theme = CurrentTheme,
                Widgets = _widgets.Where(w => w.Container != null).Select(w => new EnhancedWidgetLayout.WidgetData
                {
                    Type = w.Widget.GetType().Name,
                    InstanceId = w.InstanceId,
                    X = Canvas.GetLeft(w.Container!),
                    Y = Canvas.GetTop(w.Container!),
                    Width = w.Container!.Bounds.Width,
                    Height = w.Container!.Bounds.Height,
                    Skin = w.Skin,
                    Configuration = w.Configuration,
                    IsLocked = w.IsLocked
                }).ToList()
            };

            var json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("enhanced_widget_layout.json", json);

            Logger.Log("Enhanced widget layout saved");
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to save widget layout: {ex.Message}");
        }
    }

    public async void LoadLayout(object? parameter)
    {
        try
        {
            if (!File.Exists("enhanced_widget_layout.json"))
                return;

            var json = File.ReadAllText("enhanced_widget_layout.json");
            var layout = JsonSerializer.Deserialize<EnhancedWidgetLayout>(json);

            if (layout == null) return;

            // Clear existing widgets
            foreach (var widget in _widgets.ToList())
            {
                await RemoveWidget(widget);
            }

            // Set theme
            if (!string.IsNullOrEmpty(layout.Theme))
            {
                ChangeTheme(layout.Theme);
            }

            // Load saved widgets
            foreach (var widgetData in layout.Widgets)
            {
                IWidget? widget = widgetData.Type switch
                {
                    "SystemMonitorWidget" => new SystemMonitorWidget(),
                    "NetworkMonitorWidget" => new NetworkMonitorWidget(),
                    "QuickNotesWidget" => new QuickNotesWidget(),
                    "CalculatorWidget" => new CalculatorWidget(),
                    _ => _widgetManager.Widgets.FirstOrDefault(w => w.GetType().Name == widgetData.Type)
                };

                if (widget != null)
                {
                    var instance = new EnhancedWidgetInstance(widget, _themeManager)
                    {
                        InstanceId = widgetData.InstanceId,
                        Skin = widgetData.Skin,
                        Configuration = widgetData.Configuration,
                        IsLocked = widgetData.IsLocked
                    };

                    _widgets.Add(instance);

                    // Initialize and activate
                    if (widget is IWidgetV2 widgetV2)
                    {
                        var context = CreateWidgetContext(instance);
                        await widgetV2.OnInitializeAsync(context);
                        
                        // Import configuration if available
                        if (widgetData.Configuration.Any())
                        {
                            await widgetV2.ImportDataAsync(widgetData.Configuration);
                        }
                    }

                    var container = CreateWidgetContainer(instance);
                    _window.Root.Children.Add(container);

                    Canvas.SetLeft(container, widgetData.X);
                    Canvas.SetTop(container, widgetData.Y);
                    container.Width = widgetData.Width;
                    container.Height = widgetData.Height;

                    // Activate the widget
                    if (widget is IWidgetV2 widgetV2Active)
                    {
                        await widgetV2Active.OnActivateAsync();
                    }

                    HasWidgets = true;
                }
            }

            Logger.Log($"Loaded enhanced widget layout: {layout.Widgets.Count} widgets");
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

    private void ConfigureWidget(EnhancedWidgetInstance? instance)
    {
        if (instance?.Widget is IWidgetV2 widgetV2)
        {
            try
            {
                // Show widget-specific configuration dialog
                var configUI = widgetV2.GetConfigurationView(CreateWidgetContext(instance));
                if (configUI != null)
                {
                    // Implementation would show configuration window with the UI
                    Logger.Log($"Configure widget: {instance.Widget.Name}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to configure widget: {ex.Message}");
            }
        }
    }

    private void ResizeWidget(EnhancedWidgetInstance? instance)
    {
        if (instance?.Container is WidgetContainer container)
        {
            // Enter resize mode for the widget
            container.IsInResizeMode = true;
            Logger.Log($"Resize widget: {instance.Widget.Name}");
        }
    }

    private void ChangeWidgetSkin(EnhancedWidgetInstance? instance)
    {
        if (instance != null)
        {
            // Show skin selection dialog
            Logger.Log($"Change skin for widget: {instance.Widget.Name}");
            // Implementation would show skin selection
        }
    }

    private void ToggleLockWidget(EnhancedWidgetInstance? instance)
    {
        if (instance != null)
        {
            instance.IsLocked = !instance.IsLocked;
            Logger.Log($"Widget {(instance.IsLocked ? "locked" : "unlocked")}: {instance.Widget.Name}");
        }
    }

    private async void CloneWidget(EnhancedWidgetInstance? instance)
    {
        if (instance != null)
        {
            try
            {
                // Export data from original widget
                var exportedData = new Dictionary<string, object>();
                if (instance.Widget is IWidgetV2 widgetV2)
                {
                    exportedData = await widgetV2.ExportDataAsync();
                }

                // Create a copy of the widget
                IWidget? newWidget = instance.Widget.GetType().Name switch
                {
                    "SystemMonitorWidget" => new SystemMonitorWidget(),
                    "NetworkMonitorWidget" => new NetworkMonitorWidget(),
                    "QuickNotesWidget" => new QuickNotesWidget(),
                    "CalculatorWidget" => new CalculatorWidget(),
                    _ => null
                };

                if (newWidget != null)
                {
                    await AddWidget(newWidget);

                    // Import data to cloned widget
                    if (newWidget is IWidgetV2 newWidgetV2 && exportedData.Any())
                    {
                        await newWidgetV2.ImportDataAsync(exportedData);
                    }

                    // Position slightly offset from original
                    var newInstance = _widgets.Last();
                    if (instance.Container != null && newInstance.Container != null)
                    {
                        var originalLeft = Canvas.GetLeft(instance.Container);
                        var originalTop = Canvas.GetTop(instance.Container);
                        Canvas.SetLeft(newInstance.Container, originalLeft + 20);
                        Canvas.SetTop(newInstance.Container, originalTop + 20);
                    }

                    Logger.Log($"Cloned widget: {instance.Widget.Name}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to clone widget: {ex.Message}");
            }
        }
    }

    private async void RefreshWidget(EnhancedWidgetInstance? instance)
    {
        if (instance?.Widget is IWidgetV2 widgetV2)
        {
            try
            {
                await widgetV2.OnDeactivateAsync();
                await widgetV2.OnActivateAsync();
                Logger.Log($"Refreshed widget: {instance.Widget.Name}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to refresh widget: {ex.Message}");
            }
        }
    }

    private async void ExportWidgetData(EnhancedWidgetInstance? instance)
    {
        if (instance?.Widget is IWidgetV2 widgetV2)
        {
            try
            {
                var data = await widgetV2.ExportDataAsync();
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                var fileName = $"{instance.Widget.Name}_{instance.InstanceId}_export.json";
                File.WriteAllText(fileName, json);
                Logger.Log($"Exported widget data to: {fileName}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to export widget data: {ex.Message}");
            }
        }
    }

    private void ImportWidgetData(EnhancedWidgetInstance? instance)
    {
        if (instance?.Widget is IWidgetV2 widgetV2)
        {
            try
            {
                // Implementation would show file dialog to select import file
                // For now, just log the action
                Logger.Log($"Import widget data for: {instance.Widget.Name}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to import widget data: {ex.Message}");
            }
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