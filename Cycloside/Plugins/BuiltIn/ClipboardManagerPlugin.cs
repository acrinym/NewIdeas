using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace Cycloside.Plugins.BuiltIn;

public class ClipboardManagerPlugin : IPlugin
{
    private Window? _window;
    private ListBox? _list;
    private readonly List<string> _history = new();
    private DispatcherTimer? _timer;

    public string Name => "Clipboard Manager";
    public string Description => "Stores clipboard history";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        _list = new ListBox();
        _list.DoubleTapped += async (_, __) =>
        {
            if (_list.SelectedItem is string text && _window != null)
                if (TopLevel.GetTopLevel(_window)?.Clipboard is { } cb)
                    await cb.SetTextAsync(text);
        };

        _window = new Window
        {
            Title = "Clipboard History",
            Width = 300,
            Height = 400,
            Content = _list
        };
        ThemeManager.ApplyFromSettings(_window, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(ClipboardManagerPlugin));
        _window.Show();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += async (_, __) =>
        {
            var cb = _window != null ? TopLevel.GetTopLevel(_window)?.Clipboard : null;
            var text = cb != null ? await cb.GetTextAsync() : null;
            if (!string.IsNullOrEmpty(text) && (_history.Count == 0 || _history[^1] != text))
            {
                _history.Add(text);
                if (_history.Count > 20)
                    _history.RemoveAt(0);
                _list!.Items.Clear();
                foreach (var h in _history)
                    _list.Items.Add(h);
            }
        };
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
        _window?.Close();
        _window = null;
        _list = null;
    }
}
