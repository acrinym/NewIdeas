using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Cycloside.Plugins;
using Cycloside.Plugins.BuiltIn;
using Cycloside.Visuals.Managed;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using AvaloniaImage = Avalonia.Controls.Image;
using FrameDimension = System.Drawing.Imaging.FrameDimension;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

#if WINDOWS
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;
using VlcMedia = LibVLCSharp.Shared.Media;
#endif

namespace Cycloside.Services;

public static class AnimatedBackgroundManager
{
    private static readonly object SyncLock = new();
    private static readonly Dictionary<Window, AnimatedBackgroundBinding> Bindings = new();
    private static readonly Lazy<Dictionary<string, Type>> VisualizerCatalog = new(BuildVisualizerCatalog);

    public static IEnumerable<string> GetAvailableVisualizers()
    {
        return VisualizerCatalog.Value.Keys
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static void ApplyFromSettings(Window window, string componentName)
    {
        RegisterWindow(window, componentName, null);
        ApplyResolvedSettings(window, ResolveSettings(componentName, null));
    }

    public static void ApplyForPlugin(Window window, IPlugin plugin)
    {
        RegisterWindow(window, null, plugin.Name);
        ApplyResolvedSettings(window, ResolveSettings(null, plugin.Name));
    }

    public static void ReapplyAllWindows()
    {
        AnimatedBackgroundBinding[] bindings;

        lock (SyncLock)
        {
            bindings = Bindings.Values.ToArray();
        }

        foreach (var binding in bindings)
        {
            if (binding.Window == null)
            {
                continue;
            }

            ApplyResolvedSettings(binding.Window, ResolveSettings(binding.ComponentName, binding.PluginName));
        }
    }

    private static void ApplyResolvedSettings(Window window, AnimatedBackgroundSettings settings)
    {
        settings.Normalize();

        if (settings.IsDisabled())
        {
            GetOrCreateBinding(window).Clear();
            return;
        }

        var surface = CreateSurface(settings);
        if (surface == null)
        {
            GetOrCreateBinding(window).Clear();
            return;
        }

        GetOrCreateBinding(window).Apply(surface, settings);
    }

    private static AnimatedBackgroundSettings ResolveSettings(string? componentName, string? pluginName)
    {
        var defaults = SettingsManager.Settings.GlobalAnimatedBackground?.Clone() ?? new AnimatedBackgroundSettings();

        if (!string.IsNullOrWhiteSpace(pluginName) &&
            SettingsManager.Settings.PluginAnimatedBackgrounds.TryGetValue(pluginName, out var pluginSettings) &&
            pluginSettings != null)
        {
            var clone = pluginSettings.Clone();
            clone.Normalize();
            return clone;
        }

        if (!string.IsNullOrWhiteSpace(componentName) &&
            SettingsManager.Settings.ComponentAnimatedBackgrounds.TryGetValue(componentName, out var componentSettings) &&
            componentSettings != null)
        {
            var clone = componentSettings.Clone();
            clone.Normalize();
            return clone;
        }

        defaults.Normalize();
        return defaults;
    }

    private static IAnimatedBackgroundSurface? CreateSurface(AnimatedBackgroundSettings settings)
    {
        try
        {
            var normalizedMode = AnimatedBackgroundSettings.NormalizeMode(settings.Mode);

            if (string.Equals(normalizedMode, AnimatedBackgroundModes.Visualizer, StringComparison.OrdinalIgnoreCase))
            {
                var visualizer = CreateVisualizer(settings.Visualizer);
                if (visualizer == null)
                {
                    Logger.Log($"AnimatedBackgroundManager: visualizer '{settings.Visualizer}' is unavailable");
                    return null;
                }

                return new VisualizerBackgroundSurface(visualizer);
            }

            var sourcePath = ResolveSourcePath(settings.Source);
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                Logger.Log($"AnimatedBackgroundManager: background source is missing: {settings.Source}");
                return null;
            }

            var extension = Path.GetExtension(sourcePath);
            if (string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase))
            {
                return new GifBackgroundSurface(sourcePath);
            }

            if (IsStillImageExtension(extension))
            {
                return new StillImageBackgroundSurface(sourcePath);
            }

#if WINDOWS
            if (OperatingSystem.IsWindows())
            {
                return new VideoBackgroundSurface(sourcePath, settings);
            }
#endif

            Logger.Log($"AnimatedBackgroundManager: unsupported media type '{extension}'");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Log($"AnimatedBackgroundManager: failed to create background surface: {ex.Message}");
            return null;
        }
    }

    private static string ResolveSourcePath(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        if (Path.IsPathRooted(source))
        {
            return source;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, source));
    }

    private static bool IsStillImageExtension(string? extension)
    {
        return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".tif", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".tiff", StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, Type> BuildVisualizerCatalog()
    {
        var catalog = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        var assembly = typeof(AnimatedBackgroundManager).Assembly;

        foreach (var type in assembly.GetTypes())
        {
            if (!typeof(IManagedVisualizer).IsAssignableFrom(type) || type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)
            {
                continue;
            }

            try
            {
                if (Activator.CreateInstance(type) is not IManagedVisualizer visualizer)
                {
                    continue;
                }

                var name = visualizer.Name;
                if (!string.IsNullOrWhiteSpace(name) && !catalog.ContainsKey(name))
                {
                    catalog[name] = type;
                }

                visualizer.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log($"AnimatedBackgroundManager: failed to register visualizer '{type.Name}': {ex.Message}");
            }
        }

        return catalog;
    }

    private static IManagedVisualizer? CreateVisualizer(string? requestedName)
    {
        var fallbackName = VisualizerCatalog.Value.ContainsKey("Starfield")
            ? "Starfield"
            : VisualizerCatalog.Value.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).FirstOrDefault();

        var targetName = !string.IsNullOrWhiteSpace(requestedName) && VisualizerCatalog.Value.ContainsKey(requestedName)
            ? requestedName
            : fallbackName;

        if (string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        var type = VisualizerCatalog.Value[targetName];
        if (Activator.CreateInstance(type) is not IManagedVisualizer visualizer)
        {
            return null;
        }

        visualizer.Init();
        return visualizer;
    }

    private static void RegisterWindow(Window window, string? componentName, string? pluginName)
    {
        lock (SyncLock)
        {
            if (!Bindings.TryGetValue(window, out var binding))
            {
                binding = new AnimatedBackgroundBinding(window);
                Bindings[window] = binding;
                window.Closed += OnWindowClosed;
            }

            binding.ComponentName = componentName;
            binding.PluginName = pluginName;
        }
    }

    private static AnimatedBackgroundBinding GetOrCreateBinding(Window window)
    {
        lock (SyncLock)
        {
            if (!Bindings.TryGetValue(window, out var binding))
            {
                binding = new AnimatedBackgroundBinding(window);
                Bindings[window] = binding;
                window.Closed += OnWindowClosed;
            }

            return binding;
        }
    }

    private static void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is not Window window)
        {
            return;
        }

        lock (SyncLock)
        {
            if (Bindings.TryGetValue(window, out var binding))
            {
                binding.Dispose();
                Bindings.Remove(window);
            }
        }

        window.Closed -= OnWindowClosed;
    }

    private interface IAnimatedBackgroundSurface : IDisposable
    {
        Control CreateHost(Control overlayContent, double opacity);
    }

    private sealed class AnimatedBackgroundBinding : IDisposable
    {
        private IAnimatedBackgroundSurface? _surface;
        private Control? _hostControl;
        private ContentControl? _contentHost;
        private object? _originalContent;

        public AnimatedBackgroundBinding(Window window)
        {
            Window = window;
        }

        public Window Window { get; }

        public string? ComponentName { get; set; }

        public string? PluginName { get; set; }

        public void Apply(IAnimatedBackgroundSurface surface, AnimatedBackgroundSettings settings)
        {
            EnsureOverlayContent();

            DisposeSurface();

            _surface = surface;
            _hostControl = surface.CreateHost(_contentHost!, settings.Opacity);
            Window.Content = _hostControl;
        }

        public void Clear()
        {
            DisposeSurface();

            if (_hostControl != null && ReferenceEquals(Window.Content, _hostControl))
            {
                Window.Content = _originalContent;
            }

            _hostControl = null;
            _contentHost = null;
            _originalContent = null;
        }

        public void Dispose()
        {
            DisposeSurface();
            _hostControl = null;
            _contentHost = null;
            _originalContent = null;
        }

        private void EnsureOverlayContent()
        {
            var currentContent = Window.Content;
            if (_contentHost != null && _hostControl != null && ReferenceEquals(currentContent, _hostControl))
            {
                return;
            }

            _originalContent = currentContent;
            _contentHost = new ContentControl
            {
                Content = _originalContent,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
        }

        private void DisposeSurface()
        {
            _surface?.Dispose();
            _surface = null;
        }
    }

    private sealed class StillImageBackgroundSurface : IAnimatedBackgroundSurface
    {
        private readonly AvaloniaBitmap _bitmap;

        public StillImageBackgroundSurface(string sourcePath)
        {
            using var stream = File.OpenRead(sourcePath);
            _bitmap = new AvaloniaBitmap(stream);
        }

        public Control CreateHost(Control overlayContent, double opacity)
        {
            var image = new AvaloniaImage
            {
                Source = _bitmap,
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Opacity = Math.Clamp(opacity, 0.05, 1.0),
                IsHitTestVisible = false
            };

            var grid = new Grid();
            grid.Children.Add(image);
            grid.Children.Add(overlayContent);
            return grid;
        }

        public void Dispose()
        {
            _bitmap.Dispose();
        }
    }

    private sealed class GifBackgroundSurface : IAnimatedBackgroundSurface
    {
        private readonly List<AvaloniaBitmap> _frames = new();
        private readonly List<TimeSpan> _delays = new();
        private DispatcherTimer? _timer;
        private AvaloniaImage? _image;
        private int _frameIndex;

        public GifBackgroundSurface(string sourcePath)
        {
            LoadFrames(sourcePath);
        }

        public Control CreateHost(Control overlayContent, double opacity)
        {
            _image = new AvaloniaImage
            {
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Opacity = Math.Clamp(opacity, 0.05, 1.0),
                IsHitTestVisible = false
            };

            if (_frames.Count > 0)
            {
                _image.Source = _frames[0];
            }

            if (_frames.Count > 1)
            {
                _timer = new DispatcherTimer
                {
                    Interval = _delays[0]
                };
                _timer.Tick += OnTick;
                _timer.Start();
            }

            var grid = new Grid();
            grid.Children.Add(_image);
            grid.Children.Add(overlayContent);
            return grid;
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTick;
                _timer = null;
            }

            foreach (var frame in _frames)
            {
                frame.Dispose();
            }

            _frames.Clear();
            _delays.Clear();
            _image = null;
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (_frames.Count == 0 || _image == null || _timer == null)
            {
                return;
            }

            _frameIndex = (_frameIndex + 1) % _frames.Count;
            _image.Source = _frames[_frameIndex];
            _timer.Interval = _delays[_frameIndex];
        }

        private void LoadFrames(string sourcePath)
        {
            using var image = System.Drawing.Image.FromFile(sourcePath);
            var dimension = new FrameDimension(image.FrameDimensionsList[0]);
            var frameCount = image.GetFrameCount(dimension);
            var delays = TryGetFrameDelays(image, frameCount);

            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                image.SelectActiveFrame(dimension, frameIndex);
                using var frameBitmap = new System.Drawing.Bitmap(image.Width, image.Height);
                using (var graphics = System.Drawing.Graphics.FromImage(frameBitmap))
                {
                    graphics.DrawImage(image, 0, 0, image.Width, image.Height);
                }

                using var stream = new MemoryStream();
                frameBitmap.Save(stream, ImageFormat.Png);
                stream.Position = 0;
                _frames.Add(new AvaloniaBitmap(stream));
                _delays.Add(delays[frameIndex]);
            }
        }

        private static List<TimeSpan> TryGetFrameDelays(System.Drawing.Image image, int frameCount)
        {
            const int FrameDelayPropertyId = 0x5100;
            var delays = new List<TimeSpan>(frameCount);

            if (Array.IndexOf(image.PropertyIdList, FrameDelayPropertyId) >= 0)
            {
                var property = image.GetPropertyItem(FrameDelayPropertyId);
                var propertyValue = property?.Value;
                if (propertyValue != null && propertyValue.Length >= frameCount * 4)
                {
                    for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
                    {
                        var offset = frameIndex * 4;
                        var centiseconds = BitConverter.ToInt32(propertyValue, offset);
                        delays.Add(TimeSpan.FromMilliseconds(Math.Max(20, centiseconds * 10)));
                    }

                    return delays;
                }
            }

            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                delays.Add(TimeSpan.FromMilliseconds(100));
            }

            return delays;
        }
    }

    private sealed class VisualizerBackgroundSurface : IAnimatedBackgroundSurface
    {
        private readonly IManagedVisualizer _visualizer;
        private readonly Action<object?> _audioHandler;
        private readonly DateTime _start = DateTime.UtcNow;
        private DispatcherTimer? _timer;
        private VisualizerSurfaceControl? _surface;

        public VisualizerBackgroundSurface(IManagedVisualizer visualizer)
        {
            _visualizer = visualizer;
            _audioHandler = OnAudioData;
        }

        public Control CreateHost(Control overlayContent, double opacity)
        {
            _surface = new VisualizerSurfaceControl(_visualizer, _start)
            {
                Opacity = Math.Clamp(opacity, 0.05, 1.0),
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            PluginBus.Subscribe("audio:data", _audioHandler);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _timer.Tick += OnTick;
            _timer.Start();

            var grid = new Grid();
            grid.Children.Add(_surface);
            grid.Children.Add(overlayContent);
            return grid;
        }

        public void Dispose()
        {
            PluginBus.Unsubscribe("audio:data", _audioHandler);

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTick;
                _timer = null;
            }

            _surface = null;
            _visualizer.Dispose();
        }

        private void OnAudioData(object? payload)
        {
            if (payload is not AudioData audioData)
            {
                return;
            }

            Dispatcher.UIThread.Post(() => _visualizer.UpdateAudioData(audioData));
        }

        private void OnTick(object? sender, EventArgs e)
        {
            _surface?.InvalidateVisual();
        }
    }

    private sealed class VisualizerSurfaceControl : Control
    {
        private readonly IManagedVisualizer _visualizer;
        private readonly DateTime _start;

        public VisualizerSurfaceControl(IManagedVisualizer visualizer, DateTime start)
        {
            _visualizer = visualizer;
            _start = start;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            _visualizer.Render(context, Bounds.Size, DateTime.UtcNow - _start);
        }
    }

#if WINDOWS
    private sealed class VideoBackgroundSurface : IAnimatedBackgroundSurface
    {
        private static readonly object VideoInitLock = new();
        private static bool _coreInitialized;

        private readonly LibVLC _libVlc;
        private readonly MediaPlayer _mediaPlayer;
        private readonly VideoView _videoView;
        private readonly VlcMedia _media;

        public VideoBackgroundSurface(string sourcePath, AnimatedBackgroundSettings settings)
        {
            EnsureLibVlcLoaded();

            _libVlc = new LibVLC("--no-video-title-show");
            _mediaPlayer = new MediaPlayer(_libVlc);
            _videoView = new VideoView
            {
                MediaPlayer = _mediaPlayer,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                IsHitTestVisible = false
            };

            _media = new VlcMedia(_libVlc, new Uri(sourcePath));
            if (settings.Loop)
            {
                _media.AddOption(":input-repeat=-1");
            }

            if (settings.MuteVideo)
            {
                _media.AddOption(":no-audio");
                _mediaPlayer.Mute = true;
            }

            _mediaPlayer.Play(_media);
        }

        public Control CreateHost(Control overlayContent, double opacity)
        {
            var overlayGrid = new Grid();
            if (opacity < 1.0)
            {
                var overlayAlpha = (byte)Math.Clamp((1.0 - Math.Clamp(opacity, 0.05, 1.0)) * 180.0, 0, 180);
                overlayGrid.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(overlayAlpha, 0, 0, 0)),
                    IsHitTestVisible = false
                });
            }

            overlayGrid.Children.Add(overlayContent);
            _videoView.Content = overlayGrid;
            return _videoView;
        }

        public void Dispose()
        {
            _mediaPlayer.Stop();
            _videoView.Content = null;
            _media.Dispose();
            _mediaPlayer.Dispose();
            _libVlc.Dispose();
        }

        private static void EnsureLibVlcLoaded()
        {
            lock (VideoInitLock)
            {
                if (_coreInitialized)
                {
                    return;
                }

                Core.Initialize();
                _coreInitialized = true;
            }
        }
    }
#endif
}
