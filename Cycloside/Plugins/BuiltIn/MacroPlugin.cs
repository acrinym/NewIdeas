using Avalonia.Controls;
using Avalonia.Layout;
using SharpHook;
using SharpHook.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn;

public class MacroPlugin : IPlugin
{
    private Window? _window;
    private Button? _recordButton;
    private Button? _playButton;
    private TextBlock? _status;
    private TaskPoolGlobalHook? _hook;
    private EventSimulator? _simulator;
    private readonly List<MacroEvent> _events = new();
    private Stopwatch? _timer;

    public string Name => "Macro Engine";
    public string Description => "Records and plays simple keyboard macros.";
    public Version Version => new(1,0,0);

    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        _recordButton = new Button { Content = "Record" };
        _playButton = new Button { Content = "Play", IsEnabled = false };
        _status = new TextBlock { Text = "Idle", Margin = new Thickness(5) };

        _recordButton.Click += (_, _) => ToggleRecording();
        _playButton.Click += async (_, _) => await PlayAsync();

        var panel = new StackPanel { Spacing = 5, Margin = new Thickness(10) };
        panel.Children.Add(_recordButton);
        panel.Children.Add(_playButton);
        panel.Children.Add(_status);

        _window = new Window
        {
            Title = "Macro Engine",
            Width = 200,
            Height = 140,
            Content = panel
        };
        ThemeManager.ApplyFromSettings(_window, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(MacroPlugin));
        _window.Show();
    }

    private void ToggleRecording()
    {
        if (_hook != null)
        {
            StopRecording();
            return;
        }

        _events.Clear();
        _timer = Stopwatch.StartNew();
        _hook = new TaskPoolGlobalHook();
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
        _hook.RunAsync();
        _recordButton!.Content = "Stop";
        _playButton!.IsEnabled = false;
        if (_status != null) _status.Text = "Recording...";
    }

    private void StopRecording()
    {
        _hook?.Dispose();
        _hook = null;
        _timer?.Stop();
        _recordButton!.Content = "Record";
        _playButton!.IsEnabled = _events.Count > 0;
        if (_status != null) _status.Text = $"Recorded {_events.Count} events";
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        AddEvent(true, e.Data.KeyCode);
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        AddEvent(false, e.Data.KeyCode);
    }

    private void AddEvent(bool press, KeyCode code)
    {
        if (_timer == null) return;
        var delay = (int)_timer.ElapsedMilliseconds;
        _timer.Restart();
        _events.Add(new MacroEvent { IsPress = press, Code = code, Delay = delay });
    }

    private async Task PlayAsync()
    {
        _simulator ??= new EventSimulator();
        if (_status != null) _status.Text = "Playing...";
        foreach (var ev in _events)
        {
            await Task.Delay(ev.Delay);
            if (ev.IsPress)
                _simulator.SimulateKeyPress(ev.Code);
            else
                _simulator.SimulateKeyRelease(ev.Code);
        }
        if (_status != null) _status.Text = "Done";
    }

    public void Stop()
    {
        _hook?.Dispose();
        _window?.Close();
        _hook = null;
        _window = null;
        _recordButton = null;
        _playButton = null;
        _simulator = null;
        _timer = null;
        _events.Clear();
    }

    private class MacroEvent
    {
        public bool IsPress { get; set; }
        public KeyCode Code { get; set; }
        public int Delay { get; set; }
    }
}
