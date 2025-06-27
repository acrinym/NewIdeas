using Avalonia.Controls;
// Alias commonly used Avalonia control types to avoid ambiguity with WinForms
using AvaloniaListBox = Avalonia.Controls.ListBox;
using AvaloniaTextBox = Avalonia.Controls.TextBox;
using AvaloniaTextBlock = Avalonia.Controls.TextBlock;
using AvaloniaButton = Avalonia.Controls.Button;
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
#if WINDOWS
// Only needed for SendKeys on Windows; no other WinForms types should be referenced.
using System.Windows.Forms;
#endif

// Playback uses SendKeys on Windows. On Linux and macOS, SharpHook's
// EventSimulator is used to emulate key presses. These platforms may
// require additional permissions (e.g. accessibility or X11) for
// input simulation.

namespace Cycloside.Plugins.BuiltIn;

public class MacroPlugin : IPlugin
{
    private MacroWindow? _window;
    // Explicitly qualify Avalonia types to avoid conflicts with
    // Windows Forms global using directives when building for
    // net8.0-windows.
    private Avalonia.Controls.ListBox? _macroList;
    private Avalonia.Controls.TextBox? _nameBox;
    private Avalonia.Controls.TextBox? _repeatBox;
    private Avalonia.Controls.TextBlock? _status;
    private Avalonia.Controls.Button? _playButton;
    private AvaloniaListBox? _macroList;
    private AvaloniaTextBox? _nameBox;
    private AvaloniaTextBox? _repeatBox;
    private AvaloniaTextBlock? _status;
    private AvaloniaButton? _playButton;
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
        _macroList = _window.FindControl<AvaloniaListBox>("MacroList");
        _nameBox = _window.FindControl<AvaloniaTextBox>("NameBox");
        _repeatBox = _window.FindControl<AvaloniaTextBox>("RepeatBox");
        _status = _window.FindControl<AvaloniaTextBlock>("StatusText");
        _playButton = _window.FindControl<AvaloniaButton>("PlayButton");

        _window.FindControl<AvaloniaButton>("RecordButton")?.AddHandler(AvaloniaButton.ClickEvent, (_, __) => StartRecording());
        _window.FindControl<AvaloniaButton>("StopButton")?.AddHandler(AvaloniaButton.ClickEvent, (_, __) => StopRecording());
        _playButton?.AddHandler(AvaloniaButton.ClickEvent, (_, __) => PlaySelected());
        _window.FindControl<AvaloniaButton>("SaveButton")?.AddHandler(AvaloniaButton.ClickEvent, (_, __) => { MacroManager.Save(); SetStatus("Saved"); });
        _window.FindControl<AvaloniaButton>("ReloadButton")?.AddHandler(AvaloniaButton.ClickEvent, (_, __) => { MacroManager.Reload(); RefreshList(); });
        _window.FindControl<AvaloniaButton>("DeleteButton")?.AddHandler(AvaloniaButton.ClickEvent, (_, __) => DeleteSelected());

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
                        if (_isWindows)
                        {
#if WINDOWS
                            // Windows uses SendKeys for playback.
                            System.Windows.Forms.SendKeys.SendWait(key);

                            SendKeys.SendWait(key);
#endif
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
