using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;

namespace Cycloside.Plugins
{
    /// <summary>
    /// A private helper class to hold all information about a loaded plugin.
    /// This is crucial for correctly managing the plugin's lifecycle, especially for unloading.
    /// </summary>
    internal class PluginInfo
    {
        public IPlugin Instance { get; }
        public bool IsEnabled { get; set; }
        public PluginChangeStatus Status { get; set; }

        // These are only set for plugins loaded from a DLL, not for built-in ones.
        public AssemblyLoadContext? LoadContext { get; }
        public string? FilePath { get; }
        public WeakReference? WeakRefToContext { get; }

        public PluginInfo(IPlugin instance, AssemblyLoadContext? context = null, string? filePath = null)
        {
            Instance = instance;
            LoadContext = context;
            FilePath = filePath;
            IsEnabled = false; // Plugins start as disabled until explicitly enabled.
            Status = PluginChangeStatus.None;
            if (context != null)
            {
                WeakRefToContext = new WeakReference(context, trackResurrection: true);
            }
        }
    }
    
    public class PluginManager
    {
        // FIX: The core data structure is now a list of PluginInfo objects.
        // This allows us to track not just the plugin, but its load context and state.
        private readonly List<PluginInfo> _pluginInfos = new();
        private readonly object _pluginLock = new();
        private FileSystemWatcher? _watcher;
        private readonly Action<string>? _notify;
        private Timer? _reloadTimer;

        public string PluginDirectory { get; }
        public bool IsolationEnabled { get; set; }
        public bool CrashLoggingEnabled { get; set; }

        // The public list of plugins is now derived from our internal list.
        public IReadOnlyList<IPlugin> Plugins => _pluginInfos.Select(p => p.Instance).ToList().AsReadOnly();

        public PluginManager(string pluginDirectory, Action<string>? notify = null)
        {
            PluginDirectory = pluginDirectory;
            _notify = notify;
            // Assuming SettingsManager is available
            IsolationEnabled = SettingsManager.Settings.PluginIsolation;
            CrashLoggingEnabled = SettingsManager.Settings.PluginCrashLogging;
        }

        public void StartWatching()
        {
            if (_watcher != null || !Directory.Exists(PluginDirectory)) return;

            _watcher = new FileSystemWatcher(PluginDirectory, "*.dll")
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
            };

            // Use a timer to debounce file system events. This prevents multiple reloads
            // when a file is saved, as it can trigger several events in quick succession.
            _reloadTimer = new Timer(_ => ReloadPlugins(), null, Timeout.Infinite, Timeout.Infinite);

            var eventHandler = new FileSystemEventHandler((s, e) => _reloadTimer.Change(500, Timeout.Infinite));
            _watcher.Created += eventHandler;
            _watcher.Changed += eventHandler;
            _watcher.Deleted += eventHandler;
            _watcher.Renamed += new RenamedEventHandler((s, e) => _reloadTimer.Change(500, Timeout.Infinite));
        }

        public void LoadPlugins()
        {
            lock (_pluginLock)
            {
                if (!Directory.Exists(PluginDirectory))
                {
                    Directory.CreateDirectory(PluginDirectory);
                }

                foreach (var dll in Directory.GetFiles(PluginDirectory, "*.dll"))
                {
                    try
                    {
                        AssemblyLoadContext? context = null;
                        Assembly asm;

                        if (IsolationEnabled)
                        {
                            context = new PluginLoadContext(dll);
                            asm = context.LoadFromAssemblyPath(dll);
                        }
                        else
                        {
                            asm = Assembly.LoadFrom(dll);
                        }

                        var types = asm.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);
                        foreach (var type in types)
                        {
                            if (Activator.CreateInstance(type) is IPlugin plugin)
                            {
                                // Pass the context to the PluginInfo object so we can unload it later.
                                var info = new PluginInfo(plugin, context, dll);
                                _pluginInfos.Add(info);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Failed to load plugin {Path.GetFileName(dll)}: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// The new, correct implementation for hot-reloading plugins.
        /// </summary>
        public void ReloadPlugins()
        {
            lock (_pluginLock)
            {
                StopAll(); // This now also unloads the assemblies.

                // Clear internal state
                _pluginInfos.Clear();
                
                // It's crucial to force garbage collection here to finalize the unloading process.
                GC.Collect();
                GC.WaitForPendingFinalizers();

                LoadPlugins();
                _notify?.Invoke("Plugins have been reloaded.");
                
                // Re-apply settings to the newly loaded plugins
                WorkspaceProfiles.Apply(SettingsManager.Settings.ActiveProfile, this);
            }
        }

        public void StopAll()
        {
            lock (_pluginLock)
            {
                foreach (var info in _pluginInfos)
                {
                    DisablePlugin(info.Instance);
                }
                
                // This is the critical new step: Unload the contexts.
                foreach (var info in _pluginInfos.Where(p => p.LoadContext != null))
                {
                    info.LoadContext!.Unload();
                }

                _watcher?.Dispose();
                _watcher = null;
                _reloadTimer?.Dispose();
                _reloadTimer = null;
            }
        }

        public void AddPlugin(IPlugin plugin)
        {
            lock (_pluginLock)
            {
                if (_pluginInfos.Any(p => p.Instance.Name == plugin.Name)) return; // Don't add duplicates
                
                var info = new PluginInfo(plugin); // Built-in plugins have no context or path.
                
                var versions = SettingsManager.Settings.PluginVersions;
                if (!versions.TryGetValue(plugin.Name, out var ver))
                {
                    info.Status = PluginChangeStatus.New;
                }
                else if (ver != plugin.Version.ToString())
                {
                    info.Status = PluginChangeStatus.Updated;
                }

                versions[plugin.Name] = plugin.Version.ToString();
                SettingsManager.Save(); // Consider debouncing this if it's slow

                _pluginInfos.Add(info);
            }
        }
        
        private PluginInfo? GetInfo(IPlugin plugin) => _pluginInfos.FirstOrDefault(p => p.Instance == plugin);

        public void EnablePlugin(IPlugin plugin)
        {
            var info = GetInfo(plugin);
            if (info == null || info.IsEnabled) return;

            try
            {
                plugin.Start();
                info.IsEnabled = true;
            }
            catch (Exception ex)
            {
                info.IsEnabled = false;
                if (CrashLoggingEnabled)
                {
                    Logger.Log($"{plugin.Name} crashed on start: {ex.Message}");
                }
                _notify?.Invoke($"[{plugin.Name}] crashed and was disabled.");
            }
        }

        // StartPlugin is kept for backward compatibility with older code.
        // It simply calls EnablePlugin, which starts and tracks the plugin.
        public void StartPlugin(IPlugin plugin) => EnablePlugin(plugin);

        public void DisablePlugin(IPlugin plugin)
        {
            var info = GetInfo(plugin);
            if (info == null || !info.IsEnabled) return;

            try
            {
                plugin.Stop();
            }
            catch (Exception ex)
            {
                if (CrashLoggingEnabled)
                {
                    Logger.Log($"Error stopping {plugin.Name}: {ex.Message}");
                }
            }
            finally
            {
                info.IsEnabled = false;
            }
        }

        public bool IsEnabled(IPlugin plugin) => GetInfo(plugin)?.IsEnabled ?? false;
        public PluginChangeStatus GetStatus(IPlugin plugin) => GetInfo(plugin)?.Status ?? PluginChangeStatus.None;
    }

}
