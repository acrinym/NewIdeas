using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Cycloside.Services;
using SharpHook;
using Microsoft.Win32;
using System.Runtime.Versioning;
using Cycloside.Plugins.BuiltIn.ScreenSaverModules;

namespace Cycloside.Plugins.BuiltIn
{
    #region Plugin Entry Point

    public class ScreenSaverPlugin : IPlugin, IDisposable
    {
        private ScreenSaverWindow? _window;
        private IGlobalHook? _hook;
        private DispatcherTimer? _idleTimer;
        private DateTime _lastInputTime;
        private TimeSpan _idleTimeout;
        private bool _isDisposed;
        private bool _isSystemSleeping;

        // Configuration (will be moved to settings later)
        private string _activeSaver = ScreenSaverModuleRegistry.ModuleNames.FirstOrDefault() ?? string.Empty;
        
        public string Name => "ScreenSaver Host";
        public string Description => "Runs full-screen screensavers after a period of inactivity.";
        public Version Version => new(1, 4, 1); // Version bump for stability improvements
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => true;

        public void Start()
        {
            try
            {
                _idleTimeout = TimeSpan.FromSeconds(60);
                _lastInputTime = DateTime.Now;
                
                _hook = new TaskPoolGlobalHook();
                _hook.MouseMoved += OnMouseMoved;
                _hook.KeyPressed += OnKeyPressed;
                _hook.RunAsync();

                _idleTimer = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Background, CheckIdleTime);
                _idleTimer.Start();

                // Register for system power events
                if (OperatingSystem.IsWindows())
                {
                    SystemEvents.PowerModeChanged += OnPowerModeChanged;
                }
            }
            catch (Exception ex)
            {
                Cycloside.Services.Logger.Error($"Failed to start ScreenSaver: {ex.Message}");
                Stop();
            }
        }

        private void OnMouseMoved(object? sender, MouseHookEventArgs e)
        {
            if (!_isSystemSleeping)
            {
                Dispatcher.UIThread.Post(() => ResetIdleTimer());
            }
        }

        private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            if (!_isSystemSleeping)
            {
                Dispatcher.UIThread.Post(() => ResetIdleTimer());
            }
        }

        [SupportedOSPlatform("windows")]
        private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    _isSystemSleeping = true;
                    HideSaver();
                    break;
                case PowerModes.Resume:
                    _isSystemSleeping = false;
                    _lastInputTime = DateTime.Now;
                    break;
            }
        }

        private void CheckIdleTime(object? sender, EventArgs e)
        {
            if (_isDisposed || _isSystemSleeping || _window != null) return;

            try
            {
                if (DateTime.Now - _lastInputTime > _idleTimeout)
                {
                    ShowSaver();
                }
            }
            catch (Exception ex)
            {
                Cycloside.Services.Logger.Error($"Error checking idle time: {ex.Message}");
            }
        }

        private void ResetIdleTimer()
        {
            if (_isDisposed) return;
            _lastInputTime = DateTime.Now;
            if (_window != null)
            {
                HideSaver();
            }
        }

        private void ShowSaver()
        {
            if (_isDisposed || _window != null) return;

            try
            {
                var module = ScreenSaverModuleRegistry.Create(_activeSaver);
                _window = new ScreenSaverWindow(module);
                _window.Closed += (s, e) => _window = null;
                _window.Show();
                _window.Activate();
            }
            catch (Exception ex)
            {
                Cycloside.Services.Logger.Error($"Failed to show screensaver: {ex.Message}");
                _window = null;
            }
        }

        private void HideSaver()
        {
            try
            {
                if (_window != null)
                {
                    var window = _window;
                    _window = null;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                Cycloside.Services.Logger.Error($"Error hiding screensaver: {ex.Message}");
                _window = null;
            }
        }

        public void Stop() => Dispose();

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                _idleTimer?.Stop();
                if (_hook != null)
                {
                    _hook.MouseMoved -= OnMouseMoved;
                    _hook.KeyPressed -= OnKeyPressed;
                    _hook.Dispose();
                }
                if (OperatingSystem.IsWindows())
                {
                    SystemEvents.PowerModeChanged -= OnPowerModeChanged;
                }
                HideSaver();
            }
            catch (Exception ex)
            {
                Cycloside.Services.Logger.Error($"Error disposing ScreenSaver: {ex.Message}");
            }
            GC.SuppressFinalize(this);
        }
    }

    #endregion

    #region ScreenSaver Window and Control

    internal class ScreenSaverWindow : Window
    {
        public ScreenSaverWindow(IScreenSaverModule module)
        {
            SystemDecorations = SystemDecorations.None;
            WindowState = WindowState.FullScreen;
            Topmost = true;
            ShowInTaskbar = false;
            Background = Brushes.Black;
            Cursor = new Cursor(StandardCursorType.None);
            Content = new ScreenSaverControl(module);

            PointerPressed += (s, e) => Close();
            KeyDown += (s, e) => Close();
        }
    }

    internal class ScreenSaverControl : Control
    {
        private readonly DispatcherTimer _renderTimer;
        private readonly IScreenSaverModule _module;
        private bool _isDisposed;
        private int _errorCount;
        private const int MaxErrors = 3;

        public ScreenSaverControl(IScreenSaverModule module)
        {
            try
            {
                _module = module;
                _renderTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Normal, OnTick);
                _renderTimer.Start();
            }
            catch (Exception ex)
            {
                Cycloside.Services.Logger.Error($"Failed to initialize screensaver animation: {ex.Message}");
                throw;
            }
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (_isDisposed) return;

            try
            {
                _module.Update();
                InvalidateVisual();
                _errorCount = 0; // Reset error count on successful update
            }
            catch (Exception ex)
            {
                Cycloside.Services.Logger.Error($"Error updating animation: {ex.Message}");
                _errorCount++;
                
                if (_errorCount >= MaxErrors)
                {
                    Cycloside.Services.Logger.Error("Too many animation errors, stopping screensaver");
                    if (Parent is Window window)
                    {
                        window.Close();
                    }
                }
            }
        }

        public override void Render(DrawingContext context)
        {
            if (_isDisposed) return;

            try
            {
                _module.Render(context, Bounds);
            }
            catch (Exception ex)
            {
                Cycloside.Services.Logger.Error($"Error rendering animation: {ex.Message}");
                _errorCount++;
                
                if (_errorCount >= MaxErrors)
                {
                    Cycloside.Services.Logger.Error("Too many rendering errors, stopping screensaver");
                    if (Parent is Window window)
                    {
                        window.Close();
                    }
                }
            }
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            _isDisposed = true;
            _renderTimer.Stop();
            (_module as IDisposable)?.Dispose();
        }
    }

    #endregion

    #region Core Mesh Logic

    internal class Mesh
    {
        public List<Point3D> Vertices { get; } = new();
        public List<Face> Faces { get; } = new();
        private const int MaxVertices = 10000; // Safety limit
        private const int MaxFaces = 10000;

        public void Draw(DrawingContext context, IBrush[] materials, Pen? wireframePen = null)
        {
            try
            {
                if (Vertices.Count > MaxVertices || Faces.Count > MaxFaces)
                {
                    Cycloside.Services.Logger.Warning($"Mesh exceeds safety limits: {Vertices.Count} vertices, {Faces.Count} faces");
                    return;
                }

                foreach (var face in Faces)
                {
                    if (face.P0 >= Vertices.Count || face.P1 >= Vertices.Count || 
                        face.P2 >= Vertices.Count || face.P3 >= Vertices.Count)
                    {
                        Cycloside.Services.Logger.Error("Invalid face vertex indices");
                        continue;
                    }

                    var geometry = new StreamGeometry();
                    using (var gc = geometry.Open())
                    {
                        gc.BeginFigure(Vertices[face.P0].ToPoint(), true);
                        gc.LineTo(Vertices[face.P1].ToPoint());
                        gc.LineTo(Vertices[face.P2].ToPoint());
                        gc.LineTo(Vertices[face.P3].ToPoint());
                    }
                    var brush = materials[face.MaterialId % materials.Length];
                    context.DrawGeometry(brush, wireframePen, geometry);
                }
            }
            catch (OutOfMemoryException ex)
            {
                Cycloside.Services.Logger.Error($"Out of memory while rendering mesh: {ex.Message}");
            }
            catch (Exception ex)
            {
                Cycloside.Services.Logger.Error($"Error rendering mesh: {ex.Message}");
            }
        }
    }

    internal struct Face
    {
        public int P0, P1, P2, P3;
        public int MaterialId;
    }
    
    #endregion

    #region Animation Implementations

    /// <summary>
    /// Port of the "3D FlowerBox" screensaver.
    /// </summary>
    internal class FlowerBoxAnimation : IScreenSaverModule
    {
        private readonly FlowerBoxGeometry _geom;
        private double _xr, _yr, _zr;
        private float _sf, _sfi;

        public string Name => "FlowerBox";

        public FlowerBoxAnimation()
        {
            _geom = new FlowerBoxGeometry(FlowerBoxShapeType.Cube);
            _sf = 0.0f;
            _sfi = _geom.ScaleFactorIncrement;
        }

        public void Update()
        {
            _xr += 1.2; _yr += 0.8; _zr += 0.5;
            _sf += _sfi;
            if (_sf > _geom.MaxScaleFactor || _sf < _geom.MinScaleFactor) _sfi = -_sfi;
            _geom.UpdatePoints(_sf);
        }

        public void Render(DrawingContext context, Rect bounds)
        {
            var transform = new TransformGroup
            {
                Children =
                {
                    new ScaleTransform(bounds.Width / 2.5, bounds.Height / 2.5),
                    new Rotate3DTransform(_xr, 0, 0, 0, 0, 0, 1),
                    new Rotate3DTransform(0, _yr, 0, 0, 0, 0, 1),
                    new Rotate3DTransform(0, 0, _zr, 0, 0, 0, 1),
                    new TranslateTransform(bounds.Width / 2, bounds.Height / 2)
                }
            };
            using (context.PushTransform(transform.Value))
            {
                _geom.Draw(context);
            }
        }
    }

    /// <summary>
    /// Port of the "3D Flying Objects - Windows Logo" style.
    /// </summary>
    internal class WindowsLogoAnimation : IScreenSaverModule
    {
        private readonly Mesh _flagMesh;
        private readonly IBrush[] _materials;
        private double _myrot = 23.0;
        private double _myrotInc = 0.5;
        private float _wavePhase = 0.0f;

        public string Name => "WindowsLogo";

        public WindowsLogoAnimation()
        {
            _flagMesh = new Mesh();
            _materials = new IBrush[] { Brushes.Gray, Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Yellow };
        }

        public void Update()
        {
            _myrot += _myrotInc;
            if (_myrot < -45.0 || _myrot > 45.0) _myrotInc = -_myrotInc;
            _wavePhase = (_wavePhase + 0.05f) % ((float)Math.PI * 2);
            GenerateWindowsFlag(_flagMesh, _wavePhase);
        }

        public void Render(DrawingContext context, Rect bounds)
        {
            var transform = new TransformGroup
            {
                Children =
                {
                    new ScaleTransform(bounds.Width / 2.0, bounds.Height / 2.0),
                    new Rotate3DTransform(0, _myrot, 0, 0, 0, 0, 1),
                    new TranslateTransform(bounds.Width / 2, bounds.Height / 2)
                }
            };
            using (context.PushTransform(transform.Value))
            {
                _flagMesh.Draw(context, _materials);
            }
        }

        private static void GenerateWindowsFlag(Mesh mesh, float wavePhase)
        {
            mesh.Vertices.Clear();
            mesh.Faces.Clear();
            
            float GetZPos(float x) => (float)(Math.Sin(wavePhase + (x * 4.0)) * 0.1);

            void AddPanel(float x, float y, float w, float h, int matId)
            {
                int baseIndex = mesh.Vertices.Count;
                var v0 = new Point3D(x, y, GetZPos(x));
                var v1 = new Point3D(x + w, y, GetZPos(x + w));
                var v2 = new Point3D(x + w, y + h, GetZPos(x + w));
                var v3 = new Point3D(x, y + h, GetZPos(x));
                mesh.Vertices.AddRange(new[] { v0, v1, v2, v3 });
                mesh.Faces.Add(new Face { P0 = baseIndex, P1 = baseIndex + 1, P2 = baseIndex + 2, P3 = baseIndex + 3, MaterialId = matId });
            }

            const float w = 0.45f;
            const float h = 0.45f;
            const float gap = 0.1f;
            AddPanel(-w - gap/2, h + gap/2, w, h, 1); // Red (top-left)
            AddPanel(gap/2, h + gap/2, w, h, 3);      // Green (top-right)
            AddPanel(-w - gap/2, -h - gap/2, w, h, 2); // Blue (bottom-left)
            AddPanel(gap/2, -h - gap/2, w, h, 4);     // Yellow (bottom-right)
        }
    }

    /// <summary>
    /// Port of the "3D Flying Objects - Twist" (Lemniscate) style.
    /// </summary>
    internal class LemniscateAnimation : IScreenSaverModule
    {
        private readonly Mesh _mesh;
        private double _mxrot, _myrot, _zrot;
        private double _myrotInc = 0.3, _zrotInc = 0.03;

        public string Name => "Twist";

        public LemniscateAnimation()
        {
            _mesh = new Mesh();
            GenerateLemniscate(_mesh);
        }
        
        public void Update()
        {
            _mxrot += 0.2; _myrot += _myrotInc; _zrot += _zrotInc;
            if (_zrot > 45 || _zrot < -45) _zrotInc = -_zrotInc;
        }

        public void Render(DrawingContext context, Rect bounds)
        {
            var transform = new TransformGroup
            {
                Children =
                {
                    new ScaleTransform(bounds.Width / 3.0, bounds.Height / 3.0),
                    new Rotate3DTransform(_mxrot, 0, 0, 0, 0, 0, 1),
                    new Rotate3DTransform(0, _myrot, 0, 0, 0, 0, 1),
                    new Rotate3DTransform(0, 0, _zrot, 0, 0, 0, 1),
                    new TranslateTransform(bounds.Width / 2, bounds.Height / 2)
                }
            };
            
            using (context.PushTransform(transform.Value))
            {
                var geometry = new StreamGeometry();
                using (var gc = geometry.Open())
                {
                    gc.BeginFigure(_mesh.Vertices[0].ToPoint(), false);
                    for (int i = 1; i < _mesh.Vertices.Count; i++)
                    {
                        gc.LineTo(_mesh.Vertices[i].ToPoint());
                    }
                }
                context.DrawGeometry(null, new Pen(Brushes.CornflowerBlue, 0.05), geometry);
            }
        }
        
        private static void GenerateLemniscate(Mesh mesh)
        {
            mesh.Vertices.Clear();
            for (double t = 0; t <= 2 * Math.PI; t += 0.01)
            {
                var a = Math.Sqrt(Math.Abs(Math.Cos(2 * t)));
                var x = a * Math.Cos(t);
                var y = a * Math.Sin(t);
                mesh.Vertices.Add(new Point3D(x, y, 0));
            }
        }
    }

    /// <summary>
    /// Port of the "3D Text" screensaver from sstext3d.c.
    /// </summary>
    internal class TextAnimation : IScreenSaverModule
    {
        private readonly Mesh _textMesh;
        private readonly IBrush[] _materials;
        private double _rotX, _rotY, _rotZ;
        private double _rotIncY = 0.5;

        public string Name => "Text";

        public TextAnimation()
        {
            _textMesh = new Mesh();
            Generate3DTextGeometry(_textMesh, "Cycloside", "Arial", 1.0f, 0.2f);
            
            _materials = new IBrush[] { Brushes.CornflowerBlue, Brushes.DarkSlateBlue };
        }

        public void Update()
        {
            _rotY += _rotIncY;
            _rotX += 0.2;
            _rotZ += 0.1;
        }

        public void Render(DrawingContext context, Rect bounds)
        {
            var transform = new TransformGroup
            {
                Children =
                {
                    new ScaleTransform(bounds.Height / 2.0, bounds.Height / 2.0),
                    new Rotate3DTransform(_rotX, 0, 0, 0, 0, 0, 1),
                    new Rotate3DTransform(0, _rotY, 0, 0, 0, 0, 1),
                    new Rotate3DTransform(0, 0, _rotZ, 0, 0, 0, 1),
                    new TranslateTransform(bounds.Width / 2, bounds.Height / 2)
                }
            };
            
            using (context.PushTransform(transform.Value))
            {
                _textMesh.Draw(context, _materials);
            }
        }

        private static void Generate3DTextGeometry(Mesh mesh, string text, string fontFamily, float size, float depth)
        {
            mesh.Vertices.Clear();
            mesh.Faces.Clear();

            float spacing = size * 0.6f;
            float charWidth = spacing * 0.5f;
            float x = -text.Length * spacing / 2f;

            foreach (char c in text)
            {
                // simple block letter for each character
                var v0 = new Point3D(x, 0, -depth / 2);
                var v1 = new Point3D(x + charWidth, 0, -depth / 2);
                var v2 = new Point3D(x + charWidth, size, -depth / 2);
                var v3 = new Point3D(x, size, -depth / 2);
                var v4 = new Point3D(x, 0, depth / 2);
                var v5 = new Point3D(x + charWidth, 0, depth / 2);
                var v6 = new Point3D(x + charWidth, size, depth / 2);
                var v7 = new Point3D(x, size, depth / 2);

                int baseIndex = mesh.Vertices.Count;
                mesh.Vertices.AddRange(new[] { v0, v1, v2, v3, v4, v5, v6, v7 });

                // front
                mesh.Faces.Add(new Face { P0 = baseIndex, P1 = baseIndex + 1, P2 = baseIndex + 2, P3 = baseIndex + 3, MaterialId = 0 });
                // back
                mesh.Faces.Add(new Face { P0 = baseIndex + 4, P1 = baseIndex + 7, P2 = baseIndex + 6, P3 = baseIndex + 5, MaterialId = 0 });
                // sides
                mesh.Faces.Add(new Face { P0 = baseIndex, P1 = baseIndex + 4, P2 = baseIndex + 5, P3 = baseIndex + 1, MaterialId = 1 });
                mesh.Faces.Add(new Face { P0 = baseIndex + 1, P1 = baseIndex + 5, P2 = baseIndex + 6, P3 = baseIndex + 2, MaterialId = 1 });
                mesh.Faces.Add(new Face { P0 = baseIndex + 2, P1 = baseIndex + 6, P2 = baseIndex + 7, P3 = baseIndex + 3, MaterialId = 1 });
                mesh.Faces.Add(new Face { P0 = baseIndex + 3, P1 = baseIndex + 7, P2 = baseIndex + 4, P3 = baseIndex, MaterialId = 1 });

                x += spacing;
            }
        }
    }

    /// <summary>
    /// Simple starfield animation inspired by classic screensavers.
    /// </summary>
    internal class StarFieldAnimation : IScreenSaverModule
    {
        private class Star
        {
            public double X;
            public double Y;
            public double Z;
        }

        private readonly List<Star> _stars = new();
        private readonly Random _random = new();
        private const int StarCount = 200;
        public string Name => "Starfield";

        public void Update()
        {
            for (int i = 0; i < _stars.Count; i++)
            {
                var star = _stars[i];
                star.Z -= 0.02;
                if (star.Z <= 0)
                {
                    ResetStar(star);
                }
            }
        }

        public void Render(DrawingContext context, Rect bounds)
        {
            if (_stars.Count == 0)
            {
                for (int i = 0; i < StarCount; i++)
                {
                    var star = new Star();
                    ResetStar(star);
                    _stars.Add(star);
                }
            }

            foreach (var star in _stars)
            {
                double x = bounds.Width / 2 + (star.X / star.Z) * bounds.Width / 2;
                double y = bounds.Height / 2 + (star.Y / star.Z) * bounds.Height / 2;
                byte shade = (byte)(255 * (1 - star.Z));
                var brush = new SolidColorBrush(Color.FromArgb(255, shade, shade, shade));
                context.FillRectangle(brush, new Rect(x, y, 2, 2));
            }
        }

        private void ResetStar(Star star)
        {
            star.X = _random.NextDouble() * 2 - 1;
            star.Y = _random.NextDouble() * 2 - 1;
            star.Z = _random.NextDouble();
        }
    }

    #endregion
    
    #region Geometry and Helpers

    // A simple 3D point/vector struct for our math
    internal struct Point3D
    {
        public double X, Y, Z;
        public Point3D(double x, double y, double z) { X = x; Y = y; Z = z; }
    }

    internal static class Point3DExtensions
    {
        public static Point ToPoint(this Point3D p) => new(p.X, p.Y);
    }
    
    internal class FlowerBoxGeometry
    {
        private readonly Point3D[] _basePoints;
        private readonly Point3D[] _transformedPoints;
        private readonly float[] _vlen;
        private readonly int[][] _triangleStrips;
        private readonly IBrush[] _sideColors;

        public float MinScaleFactor { get; }
        public float MaxScaleFactor { get; }
        public float ScaleFactorIncrement { get; }

        public FlowerBoxGeometry(FlowerBoxShapeType type)
        {
            _basePoints = FlowerBoxGeomData.CubeVertices;
            _triangleStrips = FlowerBoxGeomData.CubeStrips;
            MinScaleFactor = -1.1f;
            MaxScaleFactor = 5.1f;
            ScaleFactorIncrement = 0.05f;

            _transformedPoints = new Point3D[_basePoints.Length];
            _vlen = new float[_basePoints.Length];
            
            for (int i = 0; i < _basePoints.Length; i++)
            {
                var p = _basePoints[i];
                var d = (float)Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z);
                d *= 2.0f;
                _vlen[i] = (1.0f - d) / d;
            }
            
            _sideColors = new IBrush[] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Magenta, Brushes.Cyan, Brushes.Yellow };
            UpdatePoints(0.0f);
        }

        public void UpdatePoints(float scaleFactor)
        {
            for (int i = 0; i < _basePoints.Length; i++)
            {
                var f = (_vlen[i] * scaleFactor) + 1;
                _transformedPoints[i] = new Point3D(_basePoints[i].X * f, _basePoints[i].Y * f, _basePoints[i].Z * f);
            }
        }

        public void Draw(DrawingContext context)
        {
            for (int i = 0; i < _triangleStrips.Length; i++)
            {
                var strip = _triangleStrips[i];
                var geometry = new StreamGeometry();
                using (var gc = geometry.Open())
                {
                    gc.BeginFigure(_transformedPoints[strip[0]].ToPoint(), true);
                    for (int j = 1; j < strip.Length; j++)
                    {
                        gc.LineTo(_transformedPoints[strip[j]].ToPoint());
                    }
                }
                context.DrawGeometry(_sideColors[i % _sideColors.Length], null, geometry);
            }
        }
    }

    public enum FlowerBoxShapeType { Cube, Tetrahedron, Pyramids }

    internal static class FlowerBoxGeomData
    {
        public static readonly Point3D[] CubeVertices =
        {
            new(-0.5, -0.5, 0.5), new(0.5, -0.5, 0.5), new(0.5, 0.5, 0.5), new(-0.5, 0.5, 0.5),
            new(-0.5, -0.5, -0.5), new(0.5, -0.5, -0.5), new(0.5, 0.5, -0.5), new(-0.5, 0.5, -0.5)
        };

        public static readonly int[][] CubeStrips =
        {
            new[] { 0, 1, 3, 2 }, new[] { 1, 5, 2, 6 }, new[] { 5, 4, 6, 7 },
            new[] { 4, 0, 7, 3 }, new[] { 3, 2, 7, 6 }, new[] { 4, 5, 0, 1 }
        };
    }

    #endregion
}
