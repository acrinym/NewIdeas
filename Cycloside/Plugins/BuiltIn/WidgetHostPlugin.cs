using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Cycloside.Widgets;
using System;

namespace Cycloside.Plugins.BuiltIn;

public class WidgetHostPlugin : IPlugin
{
    private WidgetHostWindow? _window;
    private WidgetManager? _manager;
    private readonly Plugins.PluginManager? _pluginManager;

    public WidgetHostPlugin()
    {
    }

    public WidgetHostPlugin(Plugins.PluginManager manager)
    {
        _pluginManager = manager;
    }

    public string Name => "Widget Host";
    public string Description => "Hosts movable desktop widgets";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;

    public void Start()
    {
        _manager = new WidgetManager();
        _manager.LoadBuiltIn();
        _window = new WidgetHostWindow();
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(WidgetHostPlugin));
        var canvas = _window.Root;
        double x = 10;
        double y = 10;
        foreach (var widget in _manager.Widgets)
        {
            var view = widget.BuildView();
            Canvas.SetLeft(view, x);
            Canvas.SetTop(view, y);
            EnableDrag(view);
            canvas.Children.Add(view);
            x += 120;
        }

        if (_pluginManager != null)
        {
            foreach (var plugin in _pluginManager.Plugins)
            {
                if (plugin == this) continue;
                var w = plugin.Widget;
                if (w != null)
                {
                    var view = w.BuildView();
                    Canvas.SetLeft(view, x);
                    Canvas.SetTop(view, y);
                    EnableDrag(view);
                    canvas.Children.Add(view);
                    x += 120;
                }
            }
        }
        _window.Show();
    }

    private void EnableDrag(Control ctrl)
    {
        Point? last = null;
        ctrl.PointerPressed += (s, e) =>
        {
            last = e.GetPosition(_window);
            e.Pointer.Capture(ctrl);
        };
        ctrl.PointerMoved += (s, e) =>
        {
            if (last.HasValue)
            {
                var pos = e.GetPosition(_window);
                var offset = pos - last.Value;
                var left = Canvas.GetLeft(ctrl) + offset.X;
                var top = Canvas.GetTop(ctrl) + offset.Y;
                Canvas.SetLeft(ctrl, left);
                Canvas.SetTop(ctrl, top);
                last = pos;
            }
        };
        ctrl.PointerReleased += (_, e) =>
        {
            e.Pointer.Capture(null);
            last = null;
        };
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
        _manager = null;
    }
}
