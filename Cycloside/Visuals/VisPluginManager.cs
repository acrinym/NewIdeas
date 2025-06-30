using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using Cycloside.Plugins.BuiltIn; // For AudioData
using Cycloside.Services;

namespace Cycloside.Visuals
{
    /// <summary>
    /// Manages the loading, selection, and lifecycle of Winamp visualization plugins.
    /// </summary>
    public class VisPluginManager : IDisposable
    {
        private readonly List<WinampVisPluginAdapter> _plugins = new();
        private WinampVisPluginAdapter? _activePlugin;
        private VisHostWindow? _window;
        private DispatcherTimer? _renderTimer;
        private readonly Action<object?> _audioHandler;

        public IReadOnlyList<WinampVisPluginAdapter> Plugins => _plugins.AsReadOnly();

        public VisPluginManager()
        {
            // Subscribe to the audio data stream published by the MP3PlayerPlugin
            _audioHandler = OnAudioData;
            PluginBus.Subscribe("audio:data", _audioHandler);
        }

        /// <summary>
        /// Loads all valid Winamp visualization plugins from a given directory.
        /// </summary>
        public void Load(string directory)
        {
            if (!Directory.Exists(directory)) return;

            foreach (var dll in Directory.GetFiles(directory, "*.dll"))
            {
                try
                {
                    var adapter = new WinampVisPluginAdapter(dll);
                    if (adapter.Load())
                    {
                        _plugins.Add(adapter);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to load Winamp visualization '{Path.GetFileName(dll)}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Starts a selected visualization plugin, creating a window for it and beginning the render loop.
        /// </summary>
        public void StartPlugin(WinampVisPluginAdapter plugin)
        {
            if (_activePlugin != null)
            {
                StopPlugin();
            }

            _window = new VisHostWindow { Title = plugin.Description };
            _window.Show();
            _window.Closed += (s, e) => StopPlugin();

            plugin.SetParent(_window.GetHandle());
            if (plugin.Initialize())
            {
                _activePlugin = plugin;
                _renderTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Normal, (_, _) =>
                {
                    _activePlugin?.Render();
                });
                _renderTimer.Start();
            }
            else
            {
                _window.Close();
            }
        }

        /// <summary>
        /// Handles incoming audio data from the PluginBus and passes it to the active visualization.
        /// </summary>
        private void OnAudioData(object? payload)
        {
            if (payload is AudioData data && _activePlugin != null)
            {
                _activePlugin.UpdateAudioData(data);
            }
        }

        /// <summary>
        /// Stops the currently active visualization plugin and closes its window.
        /// </summary>
        public void StopPlugin()
        {
            _renderTimer?.Stop();
            _activePlugin?.Quit();
            _activePlugin = null;
            _window?.Close();
            _window = null;
        }

        /// <summary>
        /// Disposes of all resources, quits all loaded plugins, and unsubscribes from the PluginBus.
        /// </summary>
        public void Dispose()
        {
            StopPlugin();
            PluginBus.Unsubscribe("audio:data", _audioHandler);
            foreach (var plugin in _plugins)
            {
                plugin.Quit();
            }
            _plugins.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
