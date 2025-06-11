using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace Cycloside.Visuals;

public class VisPluginManager : IDisposable
{
    private readonly List<WinampVisPluginAdapter> _plugins = new();
    private WinampVisPluginAdapter? _active;
    private Timer? _renderTimer;
    private VisHostWindow? _window;

    public IReadOnlyList<WinampVisPluginAdapter> Plugins => _plugins;

    public void Load(string directory)
    {
        if (!OperatingSystem.IsWindows())
            return;

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        foreach (var dll in Directory.GetFiles(directory, "vis_*.dll"))
        {
            var plugin = new WinampVisPluginAdapter(dll);
            if (plugin.Load())
            {
                _plugins.Add(plugin);
            }
        }
    }

    public bool StartFirst()
    {
        var plugin = _plugins.FirstOrDefault();
        if (plugin == null)
            return false;

        _window = new VisHostWindow();
        _window.Show();

        plugin.SetParent(_window.GetHandle());

        if (!plugin.Initialize())
            return false;

        _active = plugin;
        _renderTimer = new Timer(33);
        _renderTimer.Elapsed += (_, _) => plugin.Render();
        _renderTimer.Start();
        return true;
    }

    public void Dispose()
    {
        _renderTimer?.Stop();
        _window?.Close();
        _active?.Quit();
        foreach (var p in _plugins)
            p.Quit();
    }
}
