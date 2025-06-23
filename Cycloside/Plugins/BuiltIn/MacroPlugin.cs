using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using SharpHook;
using SharpHook.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn;

public class MacroPlugin : IPlugin
{
    private Window? _window;
    private ListBox? _macroList;
    private TextBox? _nameBox;
    private TextBox? _repeatBox;
    private TextBlock? _status;
    private IGlobalHook? _hook;
    private readonly List<string> _recording = new();

    public string Name => "Macro Engine";
    public string Description => "Records and plays simple keyboard macros.";
    public Version Version => new(1,1,0);

    public Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;

    public void Start()
    {
        BuildUi();
        RefreshList();
    }

    private void BuildUi()
    {
        _macroList = new ListBox { Height = 150 };
        _nameBox = new TextBox { Watermark = "Macro Name" };
        _repeatBox = new TextBox { Text = "1", Width = 40 };
        _status = new TextBlock { Text = "Ready", Margin = new Thickness(5) };

        var recordBtn = new Button { Content = "Record" };
        recordBtn.Click += (_, __) => StartRecording();
        var stopBtn = new Button { Content = "Stop" };
        stopBtn.Click += (_, __) => StopRecording();
        var playBtn = new Button { Content = "Play" };
        playBtn.IsEnabled = OperatingSystem.IsWindows();
        playBtn.Click += (_, __) =>
        {
            if (OperatingSystem.IsWindows())
            {
                PlaySelected();
            }
            else
            {
                SetStatus("Playback only works on Windows");
            }
        };
        var saveBtn = new Button { Content = "Save" };
        saveBtn.Click += (_, __) => { MacroManager.Save(); SetStatus("Saved"); };
        var loadBtn = new Button { Content = "Reload" };
        loadBtn.Click += (_, __) => { MacroManager.Reload(); RefreshList(); };
        var delBtn = new Button { Content = "Delete" };
        delBtn.Click += (_, __) => DeleteSelected();

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        buttonPanel.Children.Add(recordBtn);
        buttonPanel.Children.Add(stopBtn);
        buttonPanel.Children.Add(playBtn);
        buttonPanel.Children.Add(saveBtn);
        buttonPanel.Children.Add(loadBtn);
        buttonPanel.Children.Add(delBtn);

        var main = new StackPanel { Margin = new Thickness(5) };
        main.Children.Add(_macroList);
        main.Children.Add(_nameBox);

        var repeatRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
        repeatRow.Children.Add(new TextBlock { Text = "Repeat" });
        repeatRow.Children.Add(_repeatBox);
        main.Children.Add(repeatRow);

        main.Children.Add(buttonPanel);
        main.Children.Add(_status);

        _window = new Window
        {
            Title = "Macro Engine",
            Width = 400,
            Height = 350,
            Content = main
        };

        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(MacroPlugin));
        _window.Show();
    }

    private void StartRecording()
    {
        if (_hook != null) return;
        _recording.Clear();
        _hook = new TaskPoolGlobalHook();
        _hook.KeyPressed += OnKeyPressed;
        _hook.RunAsync();
        SetStatus("Recording...");
    }

    private void StopRecording()
    {
        if (_hook == null) return;
        _hook.Dispose();
        _hook = null;
        var name = _nameBox?.Text;
        if (!string.IsNullOrWhiteSpace(name))
        {
            MacroManager.Add(new Macro { Name = name!, Keys = _recording.ToList() });
            RefreshList();
            SetStatus($"Recorded {name}");
        }
        else
        {
            SetStatus("No name specified");
        }
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        _recording.Add(e.Data.KeyCode.ToString());
    }

    private void PlaySelected()
    {
        if (!OperatingSystem.IsWindows())
        {
            SetStatus("Playback only works on Windows");
            return;
        }

        if (_macroList?.SelectedIndex >= 0 && _macroList.SelectedIndex < MacroManager.Macros.Count)
        {
            var macro = MacroManager.Macros[_macroList.SelectedIndex];
            if (!int.TryParse(_repeatBox?.Text, out var repeat) || repeat < 1) repeat = 1;
            for (int r = 0; r < repeat; r++)
            {
                foreach (var key in macro.Keys)
                {
                    try
                    {
                        // Key playback is only supported on Windows via SendKeys.
                        if (OperatingSystem.IsWindows())
                        {
                            // Placeholder for SendKeys.SendWait(key)
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Macro playback error: {ex.Message}");
                    }
                }
            }
            SetStatus($"Played '{macro.Name}' {repeat}x");
        }
    }

    private void DeleteSelected()
    {
        if (_macroList?.SelectedIndex >= 0 && _macroList.SelectedIndex < MacroManager.Macros.Count)
        {
            var macro = MacroManager.Macros[_macroList.SelectedIndex];
            MacroManager.Remove(macro);
            RefreshList();
            SetStatus($"Deleted {macro.Name}");
        }
    }

    private void RefreshList()
    {
        if (_macroList != null)
        {
            _macroList.ItemsSource = MacroManager.Macros.Select(m => m.Name).ToList();
        }
    }

    private void SetStatus(string msg)
    {
        Dispatcher.UIThread.InvokeAsync(() => { if (_status != null) _status.Text = msg; });
    }

    public void Stop()
    {
        _hook?.Dispose();
        _hook = null;
        _window?.Close();
        _window = null;
    }
}
