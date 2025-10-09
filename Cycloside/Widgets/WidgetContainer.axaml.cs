using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;

namespace Cycloside.Widgets;

public partial class WidgetContainer : UserControl
{
    private WidgetInstance _widgetInstance;
    private WidgetHostViewModel _viewModel;
    private Point _lastMousePosition;
    private bool _isDragging;
    private bool _isResizing;
    private bool _showManagementOverlay;

    public WidgetContainer(WidgetInstance widgetInstance, WidgetHostViewModel viewModel)
    {
        _widgetInstance = widgetInstance;
        _viewModel = viewModel;
        _widgetInstance.Container = this;

        InitializeComponent();

        // Set initial size based on widget content
        UpdateSizeFromContent();
    }

    public WidgetContainer()
    {
        // Parameterless constructor for Avalonia XAML/resource usage
        _widgetInstance = null!;
        _viewModel = null!;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public WidgetInstance WidgetInstance => _widgetInstance;

    public bool ShowManagementOverlay
    {
        get => _showManagementOverlay;
        set
        {
            _showManagementOverlay = value;
            UpdateVisualState();
        }
    }

    public bool IsInResizeMode
    {
        get => _isResizing;
        set
        {
            _isResizing = value;
            UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        var classes = this.Classes;
        classes.Clear();

        if (ShowManagementOverlay)
            classes.Add("managementMode");

        if (_isDragging)
            classes.Add("dragging");

        if (_isResizing)
            classes.Add("resizing");
    }

    private void UpdateSizeFromContent()
    {
        // Measure the widget content and set appropriate size
        var content = _widgetInstance.Widget.BuildView();
        content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        Width = Math.Max(200, content.DesiredSize.Width + 20);
        Height = Math.Max(100, content.DesiredSize.Height + 50); // +50 for management overlay
    }

    // Event handlers for dragging
    private void MoveHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(this);
            e.Pointer.Capture(this);
            UpdateVisualState();
        }
    }

    private void MoveHandle_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging && e.Pointer.Captured == this && !_widgetInstance.IsLocked)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _lastMousePosition;

            var newX = Canvas.GetLeft(this) + delta.X;
            var newY = Canvas.GetTop(this) + delta.Y;

            // Apply snap-to-grid if enabled
            if (ShouldSnapToGrid())
            {
                (newX, newY) = SnapToGrid(newX, newY);
            }

            // Update position
            Canvas.SetLeft(this, newX);
            Canvas.SetTop(this, newY);

            _lastMousePosition = currentPosition;
        }
    }

    private void MoveHandle_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            UpdateVisualState();
        }
    }

    // Event handlers for resizing
    private void ResizeHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isResizing = true;
            _lastMousePosition = e.GetPosition(this);
            e.Pointer.Capture(this);
            UpdateVisualState();
        }
    }

    private void ResizeHandle_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isResizing && e.Pointer.Captured == this)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _lastMousePosition;

            var newWidth = Width + delta.X;
            var newHeight = Height + delta.Y;

            // Enforce minimum size
            newWidth = Math.Max(150, newWidth);
            newHeight = Math.Max(80, newHeight);

            Width = newWidth;
            Height = newHeight;

            _lastMousePosition = currentPosition;
        }
    }

    private void ResizeHandle_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isResizing)
        {
            _isResizing = false;
            e.Pointer.Capture(null);
            UpdateVisualState();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // Bring to front when clicked (but not on management controls)
        if (!ShowManagementOverlay)
        {
            // Find the canvas and move this control to the end (top)
            var canvas = this.Parent as Canvas;
            if (canvas != null)
            {
                canvas.Children.Remove(this);
                canvas.Children.Add(this);
            }
        }

        base.OnPointerPressed(e);
    }

    private bool ShouldSnapToGrid()
    {
        // Check if the parent window has snap-to-grid enabled
        var window = this.VisualRoot as WidgetHostWindow;
        if (window?.DataContext is WidgetHostViewModel viewModel)
        {
            return viewModel.SnapToGrid;
        }
        return false;
    }

    private (double x, double y) SnapToGrid(double x, double y)
    {
        const int gridSize = 20; // 20px grid

        var snappedX = Math.Round(x / gridSize) * gridSize;
        var snappedY = Math.Round(y / gridSize) * gridSize;

        return (snappedX, snappedY);
    }
}
