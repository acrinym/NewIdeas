using Avalonia.Controls;
using Avalonia;
using Cycloside;
using Cycloside.Services;
using System;
using System.Linq;

namespace Cycloside.Plugins.BuiltIn;

public class QuickLauncherPlugin : IPlugin
{
    private readonly Plugins.PluginManager _manager;
    private Views.QuickLauncherWindow? _window;

    public QuickLauncherPlugin(Plugins.PluginManager manager)
    {
        _manager = manager;
    }

    public string Name => "Quick Launcher";
    public string Description => "Launch built-in tools from a single bar";
    public Version Version => new(0, 1, 0);
    public Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;

    public void Start()
    {
        _window = new Views.QuickLauncherWindow();
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(QuickLauncherPlugin));

        var panel = _window.FindControl<StackPanel>("ButtonsPanel");
        if (panel is null)
        {
            Logger.Log("QuickLauncher: ButtonsPanel not found.");
            return;
        }

        foreach (var plugin in _manager.Plugins.Where(p => p != this))
        {
            var button = new Button { Content = plugin.Name, Margin = new Thickness(0, 0, 4, 0) };
            button.Click += (_, _) =>
            {
                if (_manager.IsEnabled(plugin))
                    _manager.DisablePlugin(plugin);
                else
                    _manager.EnablePlugin(plugin);
            };
            panel.Children.Add(button);
        }
        _window.Show();
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
    }
}
