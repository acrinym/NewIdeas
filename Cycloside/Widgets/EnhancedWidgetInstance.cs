using Avalonia.Controls;
using Cycloside.Widgets.Themes;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Cycloside.Widgets;

/// <summary>
/// Enhanced widget instance that manages individual widget state and properties
/// </summary>
public class EnhancedWidgetInstance : INotifyPropertyChanged
{
    private string _skin = "Default";
    private bool _isLocked;
    private Control? _view;
    private UserControl? _container;

    public event PropertyChangedEventHandler? PropertyChanged;

    public EnhancedWidgetInstance(IWidget widget, WidgetThemeManager themeManager)
    {
        Widget = widget ?? throw new ArgumentNullException(nameof(widget));
        ThemeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
        InstanceId = Guid.NewGuid().ToString();
        Configuration = new Dictionary<string, object>();
    }

    /// <summary>
    /// The widget instance
    /// </summary>
    public IWidget Widget { get; }

    /// <summary>
    /// Theme manager for this widget
    /// </summary>
    public WidgetThemeManager ThemeManager { get; }

    /// <summary>
    /// Unique instance identifier
    /// </summary>
    public string InstanceId { get; set; }

    /// <summary>
    /// Current skin name
    /// </summary>
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

    /// <summary>
    /// Whether the widget is locked (cannot be moved/resized)
    /// </summary>
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

    /// <summary>
    /// Widget configuration data
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; }

    /// <summary>
    /// The widget's view control
    /// </summary>
    public Control? View
    {
        get => _view;
        set
        {
            if (_view != value)
            {
                _view = value;
                OnPropertyChanged(nameof(View));
            }
        }
    }

    /// <summary>
    /// The widget's container
    /// </summary>
    public UserControl? Container
    {
        get => _container;
        set
        {
            if (_container != value)
            {
                _container = value;
                OnPropertyChanged(nameof(Container));
            }
        }
    }

    /// <summary>
    /// Widget display name
    /// </summary>
    public string DisplayName => Widget.Name;

    /// <summary>
    /// Widget description
    /// </summary>
    public string Description => Widget.Description;

    /// <summary>
    /// Widget category
    /// </summary>
    public string Category => Widget is IWidgetV2 v2 ? v2.Category : "General";

    /// <summary>
    /// Widget icon
    /// </summary>
    public string Icon => Widget is IWidgetV2 v2 ? v2.Icon : "🔧";

    /// <summary>
    /// Whether this widget supports multiple instances
    /// </summary>
    public bool SupportsMultipleInstances => Widget is IWidgetV2 v2 ? v2.SupportsMultipleInstances : false;

    /// <summary>
    /// Widget's default size
    /// </summary>
    public (double Width, double Height) DefaultSize => Widget is IWidgetV2 v2 ? v2.DefaultSize : (200, 150);

    /// <summary>
    /// Widget's minimum size
    /// </summary>
    public (double Width, double Height) MinimumSize => Widget is IWidgetV2 v2 ? v2.MinimumSize : (100, 75);

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}