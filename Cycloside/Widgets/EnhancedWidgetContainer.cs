using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Cycloside.Widgets.Animations;
using System;
using System.Threading.Tasks;

namespace Cycloside.Widgets;

/// <summary>
/// Enhanced widget container that supports IWidgetV2 features
/// </summary>
public class EnhancedWidgetContainer : UserControl
{
    private readonly EnhancedWidgetInstance _instance;
    private readonly EnhancedWidgetHostViewModel _hostViewModel;
    private Border? _managementOverlay;
    private Grid? _rootGrid;
    private ContentPresenter? _widgetPresenter;
    private bool _showManagementOverlay;
    private bool _isInResizeMode;
    private Point _lastPointerPosition;
    private bool _isDragging;
    private bool _isResizing;

    public EnhancedWidgetContainer(EnhancedWidgetInstance instance, EnhancedWidgetHostViewModel hostViewModel)
    {
        _instance = instance ?? throw new ArgumentNullException(nameof(instance));
        _hostViewModel = hostViewModel ?? throw new ArgumentNullException(nameof(hostViewModel));
        
        InitializeContainer();
        SetupEventHandlers();
        _ = Task.Run(async () => await ApplyTheme());
    }

    public bool ShowManagementOverlay
    {
        get => _showManagementOverlay;
        set
        {
            if (_showManagementOverlay != value)
            {
                _showManagementOverlay = value;
                UpdateManagementOverlay();
            }
        }
    }

    public bool IsInResizeMode
    {
        get => _isInResizeMode;
        set
        {
            if (_isInResizeMode != value)
            {
                _isInResizeMode = value;
                UpdateResizeMode();
            }
        }
    }

    private void InitializeContainer()
    {
        // Set default size
        var defaultSize = _instance.DefaultSize;
        Width = defaultSize.Width;
        Height = defaultSize.Height;

        var minSize = _instance.MinimumSize;
        MinWidth = minSize.Width;
        MinHeight = minSize.Height;

        // Create root grid
        _rootGrid = new Grid();
        Content = _rootGrid;

        // Create widget presenter wrapped in ScrollViewer
        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            AllowAutoHide = false, // Prevents scrollbars from auto-hiding
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        
        _widgetPresenter = new ContentPresenter
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        
        scrollViewer.Content = _widgetPresenter;
        _rootGrid.Children.Add(scrollViewer);

        // Create management overlay
        CreateManagementOverlay();

        // Set widget content
        if (_instance.View != null)
        {
            _widgetPresenter.Content = _instance.View;
        }

        // Update instance reference
        _instance.Container = this;
    }

    private void CreateManagementOverlay()
    {
        _managementOverlay = new Border
        {
            Background = new SolidColorBrush(Colors.Blue, 0.2),
            BorderBrush = new SolidColorBrush(Colors.Blue),
            BorderThickness = new Thickness(2),
            IsVisible = false,
            ZIndex = 1000
        };

        var overlayGrid = new Grid();
        _managementOverlay.Child = overlayGrid;

        // Add title bar
        var titleBar = new Border
        {
            Background = new SolidColorBrush(Colors.Blue, 0.8),
            Height = 30,
            VerticalAlignment = VerticalAlignment.Top
        };

        var titlePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(5),
            VerticalAlignment = VerticalAlignment.Center
        };

        var titleText = new TextBlock
        {
            Text = _instance.DisplayName,
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };

        var lockIcon = new TextBlock
        {
            Text = _instance.IsLocked ? "🔒" : "🔓",
            Foreground = Brushes.White,
            Margin = new Thickness(5, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        titlePanel.Children.Add(titleText);
        titlePanel.Children.Add(lockIcon);
        titleBar.Child = titlePanel;
        overlayGrid.Children.Add(titleBar);

        // Add resize handles
        AddResizeHandles(overlayGrid);

        // Add close button
        var closeButton = new Button
        {
            Content = "✕",
            Width = 25,
            Height = 25,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 30, 0), // Offset to make room for menu button
            Background = new SolidColorBrush(Colors.Red, 0.8),
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold
        };
        closeButton.Click += OnCloseButtonClick;
        overlayGrid.Children.Add(closeButton);

        // Add context menu button
        var menuButton = new Button
        {
            Content = "⚙️",
            Width = 25,
            Height = 25,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 2, 0),
            Background = new SolidColorBrush(Colors.Blue, 0.8),
            Foreground = Brushes.White
        };
        menuButton.Click += ShowContextMenu;
        overlayGrid.Children.Add(menuButton);

        _rootGrid?.Children.Add(_managementOverlay);
    }

    private void AddResizeHandles(Grid overlayGrid)
    {
        // Corner resize handles
        var handles = new[]
        {
            (HorizontalAlignment.Left, VerticalAlignment.Top, "↖️"),
            (HorizontalAlignment.Right, VerticalAlignment.Top, "↗️"),
            (HorizontalAlignment.Left, VerticalAlignment.Bottom, "↙️"),
            (HorizontalAlignment.Right, VerticalAlignment.Bottom, "↘️")
        };

        foreach (var (h, v, icon) in handles)
        {
            var handle = new Border
            {
                Width = 20,
                Height = 20,
                Background = new SolidColorBrush(Colors.Blue),
                HorizontalAlignment = h,
                VerticalAlignment = v,
                Margin = new Thickness(2),
                Child = new TextBlock
                {
                    Text = icon,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 10
                }
            };

            handle.PointerPressed += (s, e) => StartResize(e);
            handle.PointerMoved += (s, e) => HandleResize(e);
            handle.PointerReleased += (s, e) => EndResize(e);

            overlayGrid.Children.Add(handle);
        }
    }

    private void SetupEventHandlers()
    {
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;

        // Subscribe to instance property changes
        _instance.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(EnhancedWidgetInstance.IsLocked))
            {
                UpdateManagementOverlay();
            }
        };
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_instance.IsLocked || !ShowManagementOverlay) return;

        _lastPointerPosition = e.GetPosition(Parent as Visual);
        _isDragging = true;
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || _instance.IsLocked) return;

        var currentPosition = e.GetPosition(Parent as Visual);
        var deltaX = currentPosition.X - _lastPointerPosition.X;
        var deltaY = currentPosition.Y - _lastPointerPosition.Y;

        var newLeft = Canvas.GetLeft(this) + deltaX;
        var newTop = Canvas.GetTop(this) + deltaY;

        // Apply snap to grid if enabled
        if (_hostViewModel.SnapToGrid)
        {
            var gridSize = 20;
            newLeft = Math.Round(newLeft / gridSize) * gridSize;
            newTop = Math.Round(newTop / gridSize) * gridSize;
        }

        Canvas.SetLeft(this, newLeft);
        Canvas.SetTop(this, newTop);

        _lastPointerPosition = currentPosition;
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        _isResizing = false;
        e.Handled = true;
    }

    private void StartResize(PointerPressedEventArgs e)
    {
        if (_instance.IsLocked) return;

        _isResizing = true;
        _lastPointerPosition = e.GetPosition(Parent as Visual);
        e.Handled = true;
    }

    private void HandleResize(PointerEventArgs e)
    {
        if (!_isResizing || _instance.IsLocked) return;

        var currentPosition = e.GetPosition(Parent as Visual);
        var deltaX = currentPosition.X - _lastPointerPosition.X;
        var deltaY = currentPosition.Y - _lastPointerPosition.Y;

        var newWidth = Math.Max(MinWidth, Width + deltaX);
        var newHeight = Math.Max(MinHeight, Height + deltaY);

        Width = newWidth;
        Height = newHeight;

        _lastPointerPosition = currentPosition;
        e.Handled = true;
    }

    private void EndResize(PointerReleasedEventArgs e)
    {
        _isResizing = false;
        e.Handled = true;
    }

    private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
    {
        // Remove the widget using the host view model's remove command
        _hostViewModel.RemoveWidgetCommand.Execute(_instance);
        e.Handled = true;
    }

    private void UpdateManagementOverlay()
    {
        if (_managementOverlay != null)
        {
            _managementOverlay.IsVisible = ShowManagementOverlay && !_instance.IsLocked;
        }
    }

    private void UpdateResizeMode()
    {
        // Update visual indicators for resize mode
        if (_managementOverlay != null)
        {
            _managementOverlay.BorderBrush = IsInResizeMode 
                ? new SolidColorBrush(Colors.Orange) 
                : new SolidColorBrush(Colors.Blue);
        }
    }

    private void ShowContextMenu(object? sender, RoutedEventArgs e)
    {
        try
        {
            var contextMenu = new ContextMenu();

            // Configure
            var configureItem = new MenuItem { Header = "Configure" };
            configureItem.Click += (s, e) => _hostViewModel.ConfigureWidgetCommand.Execute(_instance);
            contextMenu.Items.Add(configureItem);

            // Resize
            var resizeItem = new MenuItem { Header = "Resize" };
            resizeItem.Click += (s, e) => _hostViewModel.ResizeWidgetCommand.Execute(_instance);
            contextMenu.Items.Add(resizeItem);

            // Lock/Unlock
            var lockItem = new MenuItem { Header = _instance.IsLocked ? "Unlock" : "Lock" };
            lockItem.Click += (s, e) => _hostViewModel.ToggleLockWidgetCommand.Execute(_instance);
            contextMenu.Items.Add(lockItem);

            contextMenu.Items.Add(new Separator());

            // Clone
            var cloneItem = new MenuItem { Header = "Clone" };
            cloneItem.Click += (s, e) => _hostViewModel.CloneWidgetCommand.Execute(_instance);
            contextMenu.Items.Add(cloneItem);

            // Refresh
            var refreshItem = new MenuItem { Header = "Refresh" };
            refreshItem.Click += (s, e) => _hostViewModel.RefreshWidgetCommand.Execute(_instance);
            contextMenu.Items.Add(refreshItem);

            contextMenu.Items.Add(new Separator());

            // Export Data
            var exportItem = new MenuItem { Header = "Export Data" };
            exportItem.Click += (s, e) => _hostViewModel.ExportWidgetDataCommand.Execute(_instance);
            contextMenu.Items.Add(exportItem);

            // Import Data
            var importItem = new MenuItem { Header = "Import Data" };
            importItem.Click += (s, e) => _hostViewModel.ImportWidgetDataCommand.Execute(_instance);
            contextMenu.Items.Add(importItem);

            contextMenu.Items.Add(new Separator());

            // Remove
            var removeItem = new MenuItem { Header = "Remove" };
            removeItem.Click += (s, e) => _hostViewModel.RemoveWidgetCommand.Execute(_instance);
            contextMenu.Items.Add(removeItem);

            contextMenu.Open(this);
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to show context menu: {ex.Message}");
        }
    }

    public async Task ApplyTheme()
    {
        try
        {
            var theme = _hostViewModel.ThemeManager.GetCurrentTheme();
            
            // Apply theme to container
            Background = theme.GetBrush("WidgetBackground");
            
            if (_managementOverlay != null)
            {
                _managementOverlay.BorderBrush = theme.GetBrush("AccentColor");
            }

            // Apply fade-in animation when theme changes
            await WidgetAnimations.FadeInAsync(this, TimeSpan.FromMilliseconds(300));
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to apply theme to widget container: {ex.Message}");
        }
    }

    public async Task ShowAsync()
    {
        IsVisible = true;
        await WidgetAnimations.FadeInAsync(this, TimeSpan.FromMilliseconds(300));
    }

    public async Task HideAsync()
    {
        await WidgetAnimations.FadeOutAsync(this, TimeSpan.FromMilliseconds(300));
        IsVisible = false;
    }
}