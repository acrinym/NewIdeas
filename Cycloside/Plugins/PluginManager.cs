using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Cycloside.Plugins;

public class PluginManager
{
    private readonly List<IPlugin> _plugins = new();
    private FileSystemWatcher? _watcher;
    private readonly object _lock = new();

    public string PluginDirectory { get; }

    public PluginManager(string pluginDirectory)
    {
        PluginDirectory = pluginDirectory;
    }

    public void StartWatching()
    {
        if (_watcher != null)
            return;

        _watcher = new FileSystemWatcher(PluginDirectory, "*.dll")
        {
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };

        _watcher.Created += (_, _) => ReloadPlugins();
        _watcher.Changed += (_, _) => ReloadPlugins();
        _watcher.Deleted += (_, _) => ReloadPlugins();
    }

    public void LoadPlugins()
    {
        if (!Directory.Exists(PluginDirectory))
            Directory.CreateDirectory(PluginDirectory);

        foreach (var dll in Directory.GetFiles(PluginDirectory, "*.dll"))
        {
            try
            {
                var asm = Assembly.LoadFrom(dll);
                var types = asm.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);
                foreach (var type in types)
                {
                    if (Activator.CreateInstance(type) is IPlugin plugin)
                    {
                        _plugins.Add(plugin);
                        plugin.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load plugin {dll}: {ex.Message}");
            }
        }
    }

    public void ReloadPlugins()
    {
        lock (_lock)
        {
            StopAll();
            _plugins.Clear();
            LoadPlugins();
        }
    }

    public void StopAll()
    {
        foreach (var p in _plugins)
        {
            try
            {
                p.Stop();
            }
            catch
            {
                // ignore
            }
        }

        _watcher?.Dispose();
        _watcher = null;
    }
}
