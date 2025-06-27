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

// Playback uses SendKeys on Windows. On Linux and macOS, SharpHook's
// EventSimulator is used to emulate key presses. These platforms may
// require additional permissions (e.g. accessibility or X11) for
// input simulation.

namespace Cycloside.Plugins.BuiltIn;

public class MacroPlugin : IPlugin
{
    private MacroWindow? _window;
    private ListBox? _macroList;
    private TextBox? _nameBox;
    private TextBox? _repeatBox;
    private TextBlock? _status;
    private Button? _playButton;
    private IGlobalHook? _hook;
    private readonly bool _isWindows = OperatingSystem.IsWindows();
    // Event simulator from SharpHook is used for cross-platform playback.
    // It falls back to Windows SendKeys when running on Windows.
    private readonly IEventSimulator _simulator = new EventSimulator();
    private readonly List<string> _recording = new();

    public string Name => "Macro Engine";
    public string Description => "Records keyboard macros (playback uses SendKeys on Windows and SharpHook elsewhere).";
    public Version Version => new(1,2,0);

    public Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;

    public void Start()
    {
        BuildUi();
        RefreshList();
        if (!_isWindows)
        {
            SetStatus("Playback uses SharpHook and may need extra permissions.");
        }
    }

    private void BuildUi()
    {
        _window = new MacroWindow();
        _macroList = _window.FindControl<ListBox>("MacroList");
        _nameBox = _window.FindControl<TextBox>("NameBox");
        _repeatBox = _window.FindControl<TextBox>("RepeatBox");
        _status = _window.FindControl<TextBlock>("StatusText");
        _playButton = _window.FindControl<Button>("PlayButton");

        _window.FindControl<Button>("RecordButton")?.AddHandler(Button.ClickEvent, (_, __) => StartRecording());
        _window.FindControl<Button>("StopButton")?.AddHandler(Button.ClickEvent, (_, __) => StopRecording());
        _playButton?.AddHandler(Button.ClickEvent, (_, __) => PlaySelected());
        _window.FindControl<Button>("SaveButton")?.AddHandler(Button.ClickEvent, (_, __) => { MacroManager.Save(); SetStatus("Saved"); });
        _window.FindControl<Button>("ReloadButton")?.AddHandler(Button.ClickEvent, (_, __) => { MacroManager.Reload(); RefreshList(); });
        _window.FindControl<Button>("DeleteButton")?.AddHandler(Button.ClickEvent, (_, __) => DeleteSelected());

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
                        if (_isWindows)
                        {
                            // Windows uses SendKeys for playback.
                            System.Windows.Forms.SendKeys.SendWait(key);
                        }
                        else if (Enum.TryParse<KeyCode>(key, out var code))
                        {
                            // Other platforms rely on SharpHook's event simulator.
                            _simulator.SimulateKeyPress(code);
                            _simulator.SimulateKeyRelease(code);
                        }
                        else
                        {
                            Logger.Log($"Unknown key code: {key}");
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
