using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using Cycloside.Services;

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
    public bool ForceDefaultTheme => false;

    public void Start()
    {
        _window = new ClipboardManagerWindow();
        _list = _window.FindControl<ListBox>("HistoryList");
        _list.DoubleTapped += async (_, __) =>
        {
            if (_list.SelectedItem is string text && _window != null)
            {
                var cb = TopLevel.GetTopLevel(_window)?.Clipboard;
                if (cb != null)
                    await cb.SetTextAsync(text);
            }
        };
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
                _list!.ItemsSource = null;
                _list.ItemsSource = _history.ToArray();
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
