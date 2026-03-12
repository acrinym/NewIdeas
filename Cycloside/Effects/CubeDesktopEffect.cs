using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Cycloside.Effects;

public class CubeDesktopEffect : IWindowEffect
{
    public string Name => "CubeDesktop";
    public string Description => "3D cube desktop rotation effect for workspace switching";

    private readonly List<Window> _trackedWindows = new();
    private readonly Dictionary<Window, Transform> _originalTransforms = new();
    private bool _isRotating = false;
    private double _currentRotation = 0.0;
    private readonly int _faceCount = 4; // 4 desktop faces
    private int _currentFace = 0;
    private DispatcherTimer? _rotationTimer;

    public void Attach(Window window)
    {
        if (!_trackedWindows.Contains(window))
        {
            _trackedWindows.Add(window);
            _originalTransforms[window] = (Transform)(window.RenderTransform ?? new MatrixTransform());
            
            // Set up 3D perspective for the window
            SetupWindowFor3D(window);
            
            // Position window on current cube face
            PositionWindowOnCubeFace(window, _currentFace);

            window.KeyDown += OnKeyDown;
            window.PointerPressed += OnPointerPressed;
        }
    }

    public void Detach(Window window)
    {
        window.KeyDown -= OnKeyDown;
        window.PointerPressed -= OnPointerPressed;
        
        if (_trackedWindows.Contains(window))
        {
            // Restore original transform
            if (_originalTransforms.TryGetValue(window, out var originalTransform))
            {
                window.RenderTransform = originalTransform;
                _originalTransforms.Remove(window);
            }
            
            _trackedWindows.Remove(window);
        }
        
        // Clean up timer
        _rotationTimer?.Stop();
        _rotationTimer = null;
    }

    public void ApplyEvent(WindowEventType type, object? args) { }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_isRotating) return;

        // Ctrl+Alt+Left/Right for cube rotation
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && 
            e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            switch (e.Key)
            {
                case Key.Left:
                    RotateCube(-1);
                    e.Handled = true;
                    break;
                case Key.Right:
                    RotateCube(1);
                    e.Handled = true;
                    break;
            }
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Middle mouse button for cube rotation
        if (e.GetCurrentPoint(sender as Visual).Properties.IsMiddleButtonPressed)
        {
            RotateCube(1);
            e.Handled = true;
        }
    }

    private async void RotateCube(int direction)
    {
        if (_isRotating) return;
        
        _isRotating = true;
        var targetFace = (_currentFace + direction + _faceCount) % _faceCount;
        var rotationAngle = direction * 90.0;

        // Animate all windows simultaneously
        var animationTasks = _trackedWindows.Select(window => 
            AnimateWindowRotation(window, rotationAngle)).ToArray();

        await Task.WhenAll(animationTasks);

        _currentFace = targetFace;
        _currentRotation += rotationAngle;
        _isRotating = false;
    }

    private async Task AnimateWindowRotation(Window window, double rotationAngle)
    {
        var startRotation = _currentRotation;
        var endRotation = _currentRotation + rotationAngle;
        
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(600),
            Easing = new CubicEaseInOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(Window.RenderTransformProperty, CreateCubeTransform(startRotation, window))
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(Window.RenderTransformProperty, CreateCubeTransform(endRotation, window))
                    }
                }
            }
        };

        await animation.RunAsync(window);
    }

    private void SetupWindowFor3D(Window window)
    {
        // Set transform origin to center for proper 3D rotation
        window.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        
        // Apply initial 3D perspective
        var perspective = new Matrix(
            1, 0, 0, 1,
            0, 0
        );
        
        window.RenderTransform = new MatrixTransform(perspective);
    }

    private void PositionWindowOnCubeFace(Window window, int faceIndex)
    {
        // Calculate rotation angle for the face (90 degrees per face)
        var rotationAngle = faceIndex * 90.0;
        
        // Create 3D rotation transform
        var transform = CreateCubeTransform(rotationAngle, window);
        window.RenderTransform = transform;
    }

    private Transform CreateCubeTransform(double rotationY, Window window)
    {
        // Simulate 3D rotation using 2D transforms
        // This is a simplified version - real 3D would require more complex math
        
        var radians = rotationY * Math.PI / 180.0;
        var cosAngle = Math.Cos(radians);
        var sinAngle = Math.Sin(radians);
        
        // Create perspective effect
        var scaleX = Math.Abs(cosAngle);
        var skewX = sinAngle * 0.3; // Perspective skew
        
        var transformGroup = new TransformGroup();
        
        // Add perspective scaling
        transformGroup.Children.Add(new ScaleTransform(scaleX, 1.0));
        
        // Add skew for 3D effect
        if (Math.Abs(skewX) > 0.01)
        {
            transformGroup.Children.Add(new SkewTransform(skewX * 15, 0));
        }
        
        // Add depth-based opacity
        var opacity = 0.3 + (scaleX * 0.7); // Fade based on angle
        window.Opacity = Math.Max(0.1, opacity);
        
        return transformGroup;
    }
}