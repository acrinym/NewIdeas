using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Cycloside.Plugins
{
    public class PluginManager
    {
        private readonly List<IPlugin> _plugins = new();
        private readonly Dictionary<IPlugin, bool> _enabled = new();
        private FileSystemWatcher? _watcher;
        private readonly object _lock = new();

        private readonly Action<string>? _notify;

        public string PluginDirectory { get; }
        public IReadOnlyList<IPlugin> Plugins => _plugins.AsReadOnly();

        public PluginManager(string pluginDirectory, Action<string>? notify = null)
        {
            PluginDirectory = pluginDirectory;
            _notify = notify;
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
                            AddPlugin(plugin);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to load plugin {dll}: {ex.Message}");
                }
            }
        }

        public void ReloadPlugins()
        {
            lock (_lock)
            {
                StopAll();
                _plugins.Clear();
                _enabled.Clear();
                LoadPlugins();
            }
        }

        public void StopAll()
        {
            foreach (var p in _plugins)
            {
                DisablePlugin(p);
            }

            _watcher?.Dispose();
            _watcher = null;
        }

        public void AddPlugin(IPlugin plugin)
        {
            _plugins.Add(plugin);
            EnablePlugin(plugin);
        }

        public void EnablePlugin(IPlugin plugin)
        {
            try
            {
                plugin.Start();
                _enabled[plugin] = true;
            }
            catch (Exception ex)
            {
                _enabled[plugin] = false;
                Logger.Log($"{plugin.Name} crashed: {ex.Message}");
                _notify?.Invoke($"[{plugin.Name}] crashed and was disabled.");
            }
        }

        public void DisablePlugin(IPlugin plugin)
        {
            if (!_enabled.TryGetValue(plugin, out var enabled) || !enabled)
                return;

            try
            {
                plugin.Stop();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error stopping {plugin.Name}: {ex.Message}");
            }

            _enabled[plugin] = false;
        }

        public bool IsEnabled(IPlugin plugin) => _enabled.TryGetValue(plugin, out var e) && e;
    }
}
