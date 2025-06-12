using Avalonia.Controls;
using System;
using System.Diagnostics;

namespace Cycloside.Plugins.BuiltIn;

public class TaskSchedulerPlugin : IPlugin
{
    private Window? _window;
    private TextBox? _cmdBox;
    private TextBox? _timeBox;

    public string Name => "Task Scheduler";
    public string Description => "Schedule commands with cron or Task Scheduler";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;

    public void Start()
    {
        _cmdBox = new TextBox { Watermark = "Command" };
        _timeBox = new TextBox { Watermark = "Time/Cron" };
        var addButton = new Button { Content = "Add" };
        addButton.Click += (_, __) =>
        {
            if (!string.IsNullOrWhiteSpace(_cmdBox!.Text) && !string.IsNullOrWhiteSpace(_timeBox!.Text))
                AddTask(_cmdBox.Text!, _timeBox.Text!);
        };
        var panel = new StackPanel();
        panel.Children.Add(_cmdBox);
        panel.Children.Add(_timeBox);
        panel.Children.Add(addButton);

        _window = new Window
        {
            Title = "Task Scheduler",
            Width = 400,
            Height = 150,
            Content = panel
        };
        ThemeManager.ApplyFromSettings(_window, "Plugins");
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(TaskSchedulerPlugin));
        _window.Show();
    }

    private void AddTask(string cmd, string time)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start("schtasks", $"/Create /SC ONCE /TR \"{cmd}\" /ST {time} /F");
            }
            else
            {
                var entry = $"{time} {cmd}";
                Process.Start("bash", $"-c \"(crontab -l; echo '{entry}') | crontab -\"");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Task scheduler error: {ex.Message}");
        }
    }

    public void Stop()
    {
        _window?.Close();
        _window = null;
    }
}
