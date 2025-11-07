using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Cycloside.Models;
using Cycloside.Services;
using System;

namespace Cycloside.Controls;

public partial class DecoratedWindow : Window
{
    private Border? _titleBar;
    private Image? _titleBarLeftImage;
    private Image? _titleBarCenterImage;
    private Image? _titleBarRightImage;
    private Image? _minimizeButtonImage;
    private Image? _maximizeButtonImage;
    private Image? _closeButtonImage;
    private Border? _contentBorder;

    private Button? _minimizeButton;
    private Button? _maximizeButton;
    private Button? _closeButton;

    private WindowDecoration? _currentTheme;
    private bool _isMaximized;

    public DecoratedWindow()
    {
        InitializeComponent();

        // Get references to controls
        _titleBar = this.FindControl<Border>("TitleBar");
        _titleBarLeftImage = this.FindControl<Image>("TitleBarLeftImage");
        _titleBarCenterImage = this.FindControl<Image>("TitleBarCenterImage");
        _titleBarRightImage = this.FindControl<Image>("TitleBarRightImage");
        _minimizeButtonImage = this.FindControl<Image>("MinimizeButtonImage");
        _maximizeButtonImage = this.FindControl<Image>("MaximizeButtonImage");
        _closeButtonImage = this.FindControl<Image>("CloseButtonImage");
        _contentBorder = this.FindControl<Border>("ContentBorder");

        _minimizeButton = this.FindControl<Button>("MinimizeButton");
        _maximizeButton = this.FindControl<Button>("MaximizeButton");
        _closeButton = this.FindControl<Button>("CloseButton");

        // Subscribe to theme changes
        WindowDecorationManager.Instance.ThemeChanged += OnThemeChanged;

        // Apply current theme if available
        if (WindowDecorationManager.Instance.CurrentTheme != null)
        {
            ApplyTheme(WindowDecorationManager.Instance.CurrentTheme);
        }

        // Enable title bar dragging
        if (_titleBar != null)
        {
            _titleBar.PointerPressed += OnTitleBarPointerPressed;
        }

        // Add button hover effects
        if (_minimizeButton != null)
        {
            _minimizeButton.PointerEntered += (s, e) => UpdateButtonImage(_minimizeButtonImage, _currentTheme?.MinimizeButtonHover);
            _minimizeButton.PointerExited += (s, e) => UpdateButtonImage(_minimizeButtonImage, _currentTheme?.MinimizeButtonNormal);
            _minimizeButton.PointerPressed += (s, e) => UpdateButtonImage(_minimizeButtonImage, _currentTheme?.MinimizeButtonPressed);
            _minimizeButton.PointerReleased += (s, e) => UpdateButtonImage(_minimizeButtonImage, _currentTheme?.MinimizeButtonHover);
        }

        if (_maximizeButton != null)
        {
            _maximizeButton.PointerEntered += (s, e) => UpdateButtonImage(_maximizeButtonImage, _isMaximized ? _currentTheme?.RestoreButtonHover : _currentTheme?.MaximizeButtonHover);
            _maximizeButton.PointerExited += (s, e) => UpdateButtonImage(_maximizeButtonImage, _isMaximized ? _currentTheme?.RestoreButtonNormal : _currentTheme?.MaximizeButtonNormal);
            _maximizeButton.PointerPressed += (s, e) => UpdateButtonImage(_maximizeButtonImage, _isMaximized ? _currentTheme?.RestoreButtonPressed : _currentTheme?.MaximizeButtonPressed);
            _maximizeButton.PointerReleased += (s, e) => UpdateButtonImage(_maximizeButtonImage, _isMaximized ? _currentTheme?.RestoreButtonHover : _currentTheme?.MaximizeButtonHover);
        }

        if (_closeButton != null)
        {
            _closeButton.PointerEntered += (s, e) => UpdateButtonImage(_closeButtonImage, _currentTheme?.CloseButtonHover);
            _closeButton.PointerExited += (s, e) => UpdateButtonImage(_closeButtonImage, _currentTheme?.CloseButtonNormal);
            _closeButton.PointerPressed += (s, e) => UpdateButtonImage(_closeButtonImage, _currentTheme?.CloseButtonPressed);
            _closeButton.PointerReleased += (s, e) => UpdateButtonImage(_closeButtonImage, _currentTheme?.CloseButtonHover);
        }
    }

    private void OnThemeChanged(WindowDecoration? theme)
    {
        if (theme != null)
        {
            ApplyTheme(theme);
        }
        else
        {
            RemoveTheme();
        }
    }

    private void ApplyTheme(WindowDecoration theme)
    {
        _currentTheme = theme;

        // Apply title bar bitmaps
        if (_titleBarLeftImage != null && theme.TitleBarActiveLeft != null)
            _titleBarLeftImage.Source = theme.TitleBarActiveLeft;

        if (_titleBarCenterImage != null && theme.TitleBarActiveCenter != null)
            _titleBarCenterImage.Source = theme.TitleBarActiveCenter;

        if (_titleBarRightImage != null && theme.TitleBarActiveRight != null)
            _titleBarRightImage.Source = theme.TitleBarActiveRight;

        // Apply button bitmaps (normal state)
        UpdateButtonImage(_minimizeButtonImage, theme.MinimizeButtonNormal);
        UpdateButtonImage(_maximizeButtonImage, theme.MaximizeButtonNormal);
        UpdateButtonImage(_closeButtonImage, theme.CloseButtonNormal);

        // Update title bar height
        if (_titleBar != null)
        {
            _titleBar.Height = theme.TitleBarHeight;
        }

        // Update content margin to account for borders and title bar
        if (_contentBorder != null)
        {
            _contentBorder.Margin = new Thickness(
                theme.BorderWidth,
                theme.TitleBarHeight + theme.BorderWidth,
                theme.BorderWidth,
                theme.BorderWidth
            );
        }

        // Update button sizes
        if (_minimizeButton != null)
        {
            _minimizeButton.Width = theme.ButtonWidth;
            _minimizeButton.Height = theme.ButtonHeight;
        }

        if (_maximizeButton != null)
        {
            _maximizeButton.Width = theme.ButtonWidth;
            _maximizeButton.Height = theme.ButtonHeight;
        }

        if (_closeButton != null)
        {
            _closeButton.Width = theme.ButtonWidth;
            _closeButton.Height = theme.ButtonHeight;
        }

        Logger.Log($"✅ Applied window decoration theme: {theme.Name}");
    }

    private void RemoveTheme()
    {
        _currentTheme = null;

        // Reset to default/system chrome
        ExtendClientAreaToDecorationsHint = false;
        Logger.Log("🎨 Removed custom window decorations");
    }

    private void UpdateButtonImage(Image? image, Bitmap? bitmap)
    {
        if (image != null && bitmap != null)
        {
            image.Source = bitmap;
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnMinimize(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximize(object? sender, RoutedEventArgs e)
    {
        if (_isMaximized)
        {
            WindowState = WindowState.Normal;
            _isMaximized = false;

            // Update to maximize button
            UpdateButtonImage(_maximizeButtonImage, _currentTheme?.MaximizeButtonNormal);
        }
        else
        {
            WindowState = WindowState.Maximized;
            _isMaximized = true;

            // Update to restore button
            UpdateButtonImage(_maximizeButtonImage, _currentTheme?.RestoreButtonNormal);
        }
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unsubscribe from theme changes
        WindowDecorationManager.Instance.ThemeChanged -= OnThemeChanged;
        base.OnClosed(e);
    }
}
