using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Media;
using System;
using Cycloside;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn;

public class DateTimeOverlayPlugin : IPlugin
{
    private DateTimeOverlayWindow? _window;
    private DispatcherTimer? _timer;

    public string Name => "Date/Time Overlay";
    public string Description => "Displays the current date and time in a small overlay.";
    public Version Version => new(1,0,0);

    public Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;

    public void Start()
    {
        _window = new DateTimeOverlayWindow();
        CursorManager.ApplyFromSettings(_window, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(DateTimeOverlayPlugin));

        var text = _window.FindControl<TextBlock>("TimeText");
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) =>
        {
            var now = DateTime.Now;
            if (text != null)
                text.Text = now.ToString("yyyy-MM-dd HH:mm:ss");
            PluginBus.Publish("clock:tick", now);
        };
        _timer.Start();
        _window.Show();
    }

    public void Stop()
    {
        _timer?.Stop();
        _window?.Close();
        _timer = null;
        _window = null;
    }
}
