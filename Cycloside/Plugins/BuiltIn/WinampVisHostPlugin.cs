using System;
using System.IO;
using Cycloside.Visuals;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn;

public class WinampVisHostPlugin : IPlugin
{
    private VisPluginManager? _manager;
    private bool _isEnabled = false;
    private VisHostWindow? _hostWindow;

    public string Name => "Winamp Visual Host";
    public string Description => "Hosts Winamp visualization plugins";
    public Version Version => new(0, 1, 0);
    public Widgets.IWidget? Widget => null; // For UI widget host support
    public bool ForceDefaultTheme => false;

    public bool IsEnabled => _isEnabled;

    public void Start()
    {
        // Don't auto-start - wait for MP3layer to enable it
        Logger.Log("Winamp Visual Host started - waiting for MP3able visualization");

        // FIXED: Create the host window but don't show it yet
        _hostWindow = new VisHostWindow();
        ThemeManager.ApplyForPlugin(_hostWindow, this);
    }

    public void Stop()
    {
        DisableVisualization();
        _manager?.Dispose();
        _manager = null;
        _hostWindow?.Close();
        _hostWindow = null;
    }

    /// <summary>
    /// Enables visualization and loads available Winamp plugins
    /// </summary>
    public void EnableVisualization()
    {
        if (_isEnabled) return;

        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "Plugins", "Winamp");
            _manager = new VisPluginManager();
            _manager.Load(dir);

            if (_manager.Plugins.Count == 0)
            {
                Logger.Log("No Winamp visualization plugins found in Plugins/Winamp directory");
                return;
            }

            _isEnabled = true;
            Logger.Log($"Winamp Visual Host enabled with {_manager.Plugins.Count} plugins");

            // FIXED: Show the host window when visualization is enabled
            if (_hostWindow != null)
            {
                _hostWindow.Show();
                Logger.Log("Winamp Visual Host window displayed");
            }

            // Auto-start the first plugin if only one is available
            if (_manager.Plugins.Count == 1)
            {
                _manager.StartPlugin(_manager.Plugins[0]);
                Logger.Log($"Auto-started plugin: {_manager.Plugins[0]}");
            }
            else
            {
                // Show plugin picker for multiple plugins
                var picker = new VisPluginPickerWindow(_manager);
                picker.Show();
                Logger.Log("Showing plugin picker for multiple plugins");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to enable Winamp visualization: {ex.Message}");
        }
    }

    /// <summary>
    /// Disables visualization and stops any active plugins
    /// </summary>
    public void DisableVisualization()
    {
        if (!_isEnabled) return;

        try
        {
            _manager?.StopPlugin();
            _isEnabled = false;

            // FIXED: Hide the host window when visualization is disabled
            if (_hostWindow != null)
            {
                _hostWindow.Hide();
                Logger.Log("Winamp Visual Host window hidden");
            }

            Logger.Log("Winamp Visual Host disabled");
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to disable Winamp visualization: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggles visualization on/off
    /// </summary>
    public void ToggleVisualization()
    {
        if (_isEnabled)
            DisableVisualization();
        else
            EnableVisualization();
    }

    /// <summary>
    /// Gets the current status of the visualization
    /// </summary>
    public string GetStatus()
    {
        if (!_isEnabled) return "Disabled";
        if (_manager?.Plugins.Count == 0) return "No plugins found";
        return $"Active ({_manager?.Plugins.Count} plugins)";
    }
}
