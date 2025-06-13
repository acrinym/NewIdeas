using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Linq;

namespace Cycloside.Plugins.BuiltIn;

public class ProcessMonitorPlugin : IPlugin
{
    private Window? _window;
    private ListBox? _list;
    private DispatcherTimer? _timer;

    public string Name => "Process Monitor";
    public string Description => "List running processes with memory usage";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        _list = new ListBox();
        _window = new Window
        {
            Title = "Process Monitor",
            Width = 400,
            Height = 500,
            Content = _list
        };
        ThemeManager.ApplyFromSettings(_window, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(ProcessMonitorPlugin));
        _window.Show();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, __) =>
        {
            var items = Process.GetProcesses()
                .OrderBy(p => p.ProcessName)
                .Select(p =>
                {
                    try
                    {
                        return $"{p.ProcessName} - {p.WorkingSet64 / 1024 / 1024} MB";
                    }
                    catch { return $"{p.ProcessName} - ?"; }
                }).ToArray();
            _list!.ItemsSource = items;
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
