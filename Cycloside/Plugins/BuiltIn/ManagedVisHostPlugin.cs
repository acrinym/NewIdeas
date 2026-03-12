using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Cycloside.Plugins.BuiltIn; // AudioData
using Cycloside.Services;
using Cycloside.Visuals.Managed;
using Cycloside.Visuals.Managed.Visualizers;

namespace Cycloside.Plugins.BuiltIn;

/// <summary>
/// Hosts managed (C#) visualizers that render using Avalonia.
/// Avoids native Winamp plugin dependencies entirely.
/// </summary>
public class ManagedVisHostPlugin : IPlugin, IDisposable
{
    private ManagedVisHostWindow? _window;
    private bool _disposed;

    public string Name => "Managed Visual Host";
    public string Description => "Runs C# visualizers (spectrum, oscilloscope)";
    public Version Version => new(0, 1, 0);
    public Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;
        public PluginCategory Category => PluginCategory.DesktopCustomization;

    public void Start()
    {
        if (_window != null)
        {
            _window.Activate();
            return;
        }

        // Discover visualizers by reflection (public, parameterless constructor)
        var asm = typeof(ManagedVisHostPlugin).Assembly;
        var visualizers = new List<IManagedVisualizer>();
        foreach (var t in asm.GetTypes())
        {
            try
            {
                if (typeof(IManagedVisualizer).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                {
                    if (Activator.CreateInstance(t) is IManagedVisualizer v)
                    {
                        safeInit(v);
                        visualizers.Add(v);
                    }
                }
            }
            catch (Exception ex) { Logger.Log($"Visualizer load failed for {t.Name}: {ex.Message}"); }
        }
        // Sort by name for stable order
        visualizers.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        _window = new ManagedVisHostWindow(visualizers);
        ThemeManager.ApplyForPlugin(_window, this);
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, Name);
        _window.Closed += (_, __) => _window = null;
        _window.Show();

        PluginBus.Subscribe("audio:data", OnAudioData);
    }

    public void Stop()
    {
        if (_window != null)
        {
            _window.Close();
            _window = null;
        }
        PluginBus.Unsubscribe("audio:data", OnAudioData);
    }

    private void OnAudioData(object? payload)
    {
        if (_window == null) return;
        if (payload is AudioData data)
        {
            // marshal to UI thread to keep visualizers single-threaded
            Dispatcher.UIThread.Post(() => _window?.UpdateAudio(data));
        }
    }

    private static void safeInit(IManagedVisualizer v)
    {
        try { v.Init(); }
        catch (Exception ex) { Logger.Log($"Visualizer init failed: {v.Name}: {ex.Message}"); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        GC.SuppressFinalize(this);
    }
}
