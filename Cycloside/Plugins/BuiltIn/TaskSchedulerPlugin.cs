using Avalonia.Controls;
using System;
using System.Diagnostics;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn;

public class TaskSchedulerPlugin : IPlugin
{
    private TaskSchedulerWindow? _window;
    private TextBox? _cmdBox;
    private TextBox? _timeBox;

    public string Name => "Task Scheduler";
    public string Description => "Schedule commands with cron or Task Scheduler";
    public Version Version => new(0,1,0);
    public Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;

    public void Start()
    {
        _window = new TaskSchedulerWindow();
        _cmdBox = _window.FindControl<TextBox>("CmdBox");
        _timeBox = _window.FindControl<TextBox>("TimeBox");
        var addButton = _window.FindControl<Button>("AddButton");
        addButton?.AddHandler(Button.ClickEvent, (_, __) =>
        {
            if (!string.IsNullOrWhiteSpace(_cmdBox?.Text) && !string.IsNullOrWhiteSpace(_timeBox?.Text))
                AddTask(_cmdBox!.Text, _timeBox!.Text);
        });
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
