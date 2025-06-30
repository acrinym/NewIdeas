using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using Cycloside.Plugins.BuiltIn; // Needed for the AudioData record

namespace Cycloside.Visuals
{
    public class VisPluginManager : IDisposable
    {
        private const string AudioDataTopic = "audio:data";

        private readonly List<WinampVisPluginAdapter> _plugins = new();
        private WinampVisPluginAdapter? _active;
        private System.Timers.Timer? _renderTimer;
        private VisHostWindow? _window;
        private readonly Action<object?> _busHandler;

        public IReadOnlyList<WinampVisPluginAdapter> Plugins => _plugins;

        public VisPluginManager()
        {
            // Create a single handler instance to be able to subscribe and unsubscribe correctly.
            _busHandler = OnAudioDataReceived;
        }

        public void Load(string directory)
        {
            if (!OperatingSystem.IsWindows()) return;
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
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
            return plugin != null && StartPlugin(plugin);
        }

        public bool StartPlugin(WinampVisPluginAdapter plugin)
        {
            if (!_plugins.Contains(plugin)) return false;

            _window?.Close();
            _window = new VisHostWindow();
            _window.Closed += (_, _) => StopPlugin();
            _window.Show();
            
            plugin.SetParent(_window.GetHandle());

            if (!plugin.Initialize()) return false;
            _active = plugin;
            
            // Subscribe to the audio data when the plugin starts
            PluginBus.Subscribe(AudioDataTopic, _busHandler);

            _renderTimer?.Stop();
            _renderTimer = new System.Timers.Timer(33);
            _renderTimer.Elapsed += (_, _) => _active?.Render();
            _renderTimer.Start();
            return true;
        }

        private void StopPlugin()
        {
            PluginBus.Unsubscribe(AudioDataTopic, _busHandler);
            _renderTimer?.Stop();
            _active?.Quit();
            _active = null;
            _window = null;
        }

        /// <summary>
        /// This method is called by the PluginBus whenever the MP3 player sends new data.
        /// </summary>
        private void OnAudioDataReceived(object? payload)
        {
            // If we have an active visualization and the data is the correct type, update it.
            if (_active != null && payload is AudioData audioData)
            {
                _active.UpdateAudioData(audioData);
            }
        }

        public void Dispose()
        {
            StopPlugin();
            foreach (var p in _plugins)
            {
                p.Quit();
            }
        }
    }
}
