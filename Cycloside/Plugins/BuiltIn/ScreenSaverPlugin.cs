using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Text;
using Avalonia.Threading;
using Cycloside.Services;
using SharpHook;

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

        // Configuration (will be moved to settings later)
        private ScreenSaverType _activeSaver = ScreenSaverType.Text;
        
        public string Name => "ScreenSaver Host";
        public string Description => "Runs full-screen screensavers after a period of inactivity.";
        public Version Version => new(1, 3, 0); // Version bump for 3D Text
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => true;

        public void Start()
        {
            _idleTimeout = TimeSpan.FromSeconds(60);
            _lastInputTime = DateTime.Now;
            
            _hook = new TaskPoolGlobalHook();
            _hook.MouseMoved += (s, e) => ResetIdleTimer();
            _hook.KeyPressed += (s, e) => ResetIdleTimer();
            _hook.RunAsync();

            _idleTimer = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Background, CheckIdleTime);
            _idleTimer.Start();
        }

        private void CheckIdleTime(object? sender, EventArgs e)
        {
            if (_window != null) return;

            if (DateTime.Now - _lastInputTime > _idleTimeout)
            {
                ShowSaver();
            }
        }

        private void ResetIdleTimer()
        {
            _lastInputTime = DateTime.Now;
            if (_window != null)
            {
                HideSaver();
            }
        }

        private void ShowSaver()
        {
            if (_window != null) return;
            
            _window = new ScreenSaverWindow(_activeSaver);
            _window.Closed += (s, e) => _window = null;
            _window.Show();
        }

        private void HideSaver() => _window?.Close();
        public void Stop() => Dispose();

        public void Dispose()
        {
            _idleTimer?.Stop();
            _hook?.Dispose();
            _window?.Close();
            GC.SuppressFinalize(this);
        }
    }

    #endregion

    #region ScreenSaver Window and Control

    public enum ScreenSaverType { FlowerBox, WindowsLogo, Twist, Text }

    internal class ScreenSaverWindow : Window
    {
        public ScreenSaverWindow(ScreenSaverType type)
        {
            SystemDecorations = SystemDecorations.None;
            WindowState = WindowState.Maximized;
            Topmost = true;
            Background = Brushes.Black;
            Cursor = new Cursor(StandardCursorType.None);
            Content = new ScreenSaverControl(type);

            PointerPressed += (s, e) => Close();
            KeyDown += (s, e) => Close();
        }
    }

    internal class ScreenSaverControl : Control
    {
        private readonly DispatcherTimer _renderTimer;
        private readonly IScreenSaverAnimation _animation;

        public ScreenSaverControl(ScreenSaverType type)
        {
            _animation = type switch
            {
                ScreenSaverType.WindowsLogo => new WindowsLogoAnimation(),
                ScreenSaverType.Twist => new LemniscateAnimation(),
                ScreenSaverType.Text => new TextAnimation(),
                _ => new FlowerBoxAnimation()
            };

            _renderTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Normal, OnTick);
            _renderTimer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            _animation.Update();
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            _animation.Render(context, Bounds);
        }
    }

    #endregion
    
    #region Animation Interface and Core Mesh Logic
    
    internal interface IScreenSaverAnimation
    {
        void Update();
        void Render(DrawingContext context, Rect bounds);
    }

    internal class Mesh
    {
        public List<Point3D> Vertices { get; } = new();
        public List<Face> Faces { get; } = new();

        public void Draw(DrawingContext context, IBrush[] materials, Pen? wireframePen = null)
        {
            foreach (var face in Faces)
            {
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
    internal class FlowerBoxAnimation : IScreenSaverAnimation
    {
        private readonly FlowerBoxGeometry _geom;
        private double _xr, _yr, _zr;
        private float _sf, _sfi;

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
                    new Rotate3DTransform(new Vector3D(1, 0, 0), _xr),
                    new Rotate3DTransform(new Vector3D(0, 1, 0), _yr),
                    new Rotate3DTransform(new Vector3D(0, 0, 1), _zr),
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
    internal class WindowsLogoAnimation : IScreenSaverAnimation
    {
        private readonly Mesh _flagMesh;
        private readonly IBrush[] _materials;
        private double _myrot = 23.0;
        private double _myrotInc = 0.5;
        private float _wavePhase = 0.0f;

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
                    new Rotate3DTransform(new Vector3D(0, 1, 0), _myrot),
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
    internal class LemniscateAnimation : IScreenSaverAnimation
    {
        private readonly Mesh _mesh;
        private double _mxrot, _myrot, _zrot;
        private double _myrotInc = 0.3, _zrotInc = 0.03;

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
                    new Rotate3DTransform(new Vector3D(1, 0, 0), _mxrot),
                    new Rotate3DTransform(new Vector3D(0, 1, 0), _myrot),
                    new Rotate3DTransform(new Vector3D(0, 0, 1), _zrot),
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
    internal class TextAnimation : IScreenSaverAnimation
    {
        private readonly Mesh _textMesh;
        private readonly IBrush[] _materials;
        private double _rotX, _rotY, _rotZ;
        private double _rotIncY = 0.5;

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
                    new Rotate3DTransform(new Vector3D(1, 0, 0), _rotX),
                    new Rotate3DTransform(new Vector3D(0, 1, 0), _rotY),
                    new Rotate3DTransform(new Vector3D(0, 0, 1), _rotZ),
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

            var formattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(fontFamily),
                size,
                Brushes.White
            );

            var textGeometry = formattedText.BuildGeometry(new Point(0, 0));
            var centeredGeometry = textGeometry.WithTransform(Matrix.CreateTranslation(-textGeometry.Bounds.Center));
            var path = PathGeometry.CreateFrom(centeredGeometry);
            var tessellated = path.Tessellate();

            var frontVertices = tessellated.Select(p => new Point3D(p.X, p.Y, -depth / 2.0)).ToList();
            var backVertices = tessellated.Select(p => new Point3D(p.X, p.Y, depth / 2.0)).ToList();

            mesh.Vertices.AddRange(frontVertices);
            mesh.Vertices.AddRange(backVertices);
            int backStartIndex = frontVertices.Count;

            for (int i = 0; i < frontVertices.Count; i += 3)
            {
                mesh.Faces.Add(new Face { P0 = i, P1 = i + 1, P2 = i + 2, P3 = i + 2, MaterialId = 0 });
                mesh.Faces.Add(new Face { P0 = backStartIndex + i, P1 = backStartIndex + i + 2, P2 = backStartIndex + i + 1, P3 = backStartIndex + i + 1, MaterialId = 0 });
            }

            for (int i = 0; i < frontVertices.Count - 1; i++)
            {
                 mesh.Faces.Add(new Face
                 {
                     P0 = i, P1 = i + 1,
                     P2 = backStartIndex + i + 1, P3 = backStartIndex + i,
                     MaterialId = 1
                 });
            }
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
