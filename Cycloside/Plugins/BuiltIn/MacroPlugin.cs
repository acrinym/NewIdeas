using Avalonia.Controls;
using Avalonia.Threading;
using SharpHook;
using SharpHook.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Cycloside.Services;

// Alias commonly used Avalonia control types to avoid ambiguity with WinForms
using AvaloniaListBox = Avalonia.Controls.ListBox;
using AvaloniaTextBox = Avalonia.Controls.TextBox;
using AvaloniaTextBlock = Avalonia.Controls.TextBlock;
using AvaloniaButton = Avalonia.Controls.Button;

#if WINDOWS
// Only needed for SendKeys on Windows; no other WinForms types should be referenced.
using System.Windows.Forms;
#endif

namespace Cycloside.Plugins.BuiltIn;

/// <summary>
/// A plugin that records and plays back keyboard macros with accurate timing.
/// </summary>
public class MacroPlugin : IPlugin
{
    // --- UI and State Fields ---
    private MacroWindow? _window;
    private AvaloniaListBox? _macroList;
    private AvaloniaTextBox? _nameBox;
    private AvaloniaTextBox? _repeatBox;
    private AvaloniaTextBlock? _status;
    private AvaloniaButton? _playButton;
    private AvaloniaButton? _recordButton;
    private AvaloniaButton? _stopButton;

    // --- Hooking and Simulation Fields ---
    private IGlobalHook? _hook;
    private readonly IEventSimulator _simulator = new EventSimulator();
    private readonly bool _isWindows = OperatingSystem.IsWindows();

    // --- Recording Data Fields ---
    private readonly List<MacroEvent> _events = new();
    private Stopwatch? _timer;

    // --- IPlugin Properties ---
    public string Name => "Macro Engine";
    public string Description => "Records and plays back keyboard macros with accurate timing.";
    public Version Version => new(2, 0, 0);
    public Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;
        public PluginCategory Category => PluginCategory.TinkererTools;

    // FIXED: The internal 'MacroEvent' class has been removed from this file.
    // It now uses the public 'MacroEvent' class defined in MacroManager.cs.

    public void Start()
    {
        BuildUi();
        RefreshList();
        if (!_isWindows)
        {
            SetStatus("Playback uses SharpHook and may need extra permissions.");
        }
    }

    public void Stop()
    {
        _hook?.Dispose();
        _hook = null;
        _timer?.Stop();
        _timer = null;
        _window?.Close();
        _window = null;
        _events.Clear();
    }

    private void BuildUi()
    {
        _window = new MacroWindow();
        _macroList = _window.FindControl<AvaloniaListBox>("MacroList");
        _nameBox = _window.FindControl<AvaloniaTextBox>("NameBox");
        _repeatBox = _window.FindControl<AvaloniaTextBox>("RepeatBox");
        _status = _window.FindControl<AvaloniaTextBlock>("StatusText");
        _playButton = _window.FindControl<AvaloniaButton>("PlayButton");
        _recordButton = _window.FindControl<AvaloniaButton>("RecordButton");
        _stopButton = _window.FindControl<AvaloniaButton>("StopButton");

        // FIXED: Explicitly use AvaloniaButton.ClickEvent to resolve ambiguity.
        _recordButton?.AddHandler(AvaloniaButton.ClickEvent, (_, __) => StartRecording());
        _stopButton?.AddHandler(AvaloniaButton.ClickEvent, (_, __) => StopRecording());
        _playButton?.AddHandler(AvaloniaButton.ClickEvent, (_, __) => PlaySelected());
        _window.FindControl<AvaloniaButton>("SaveButton")?.AddHandler(AvaloniaButton.ClickEvent, (_, __) => { MacroManager.Save(); SetStatus("Saved"); });
        _window.FindControl<AvaloniaButton>("ReloadButton")?.AddHandler(AvaloniaButton.ClickEvent, (_, __) => { MacroManager.Reload(); RefreshList(); });
        _window.FindControl<AvaloniaButton>("DeleteButton")?.AddHandler(AvaloniaButton.ClickEvent, (_, __) => DeleteSelected());

        ThemeManager.ApplyForPlugin(_window, this);
        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(MacroPlugin));
        _window.Show();
    }

    #region Recording Logic

    private void StartRecording()
    {
        if (_hook != null) return;

        _events.Clear();
        _timer = Stopwatch.StartNew();
        _hook = new TaskPoolGlobalHook();
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
        _hook.RunAsync();

        SetStatus("Recording...");
        if (_recordButton != null) _recordButton.IsEnabled = false;
        if (_stopButton != null) _stopButton.IsEnabled = true;
    }

    private void StopRecording()
    {
        if (_hook == null) return;

        _hook.Dispose();
        _hook = null;
        _timer?.Stop();

        var name = _nameBox?.Text;
        if (!string.IsNullOrWhiteSpace(name) && _events.Any())
        {
            MacroManager.Add(new Macro { Name = name!, Events = _events.ToList() });
            RefreshList();
            SetStatus($"Recorded '{name}' with {_events.Count} events.");
        }
        else
        {
            SetStatus("Recording stopped. No name provided or no events recorded.");
        }

        if (_recordButton != null) _recordButton.IsEnabled = true;
        if (_stopButton != null) _stopButton.IsEnabled = false;
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e) => AddEvent(true, e.Data.KeyCode);
    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e) => AddEvent(false, e.Data.KeyCode);

    private void AddEvent(bool press, KeyCode code)
    {
        if (_timer == null) return;
        var delay = (int)_timer.ElapsedMilliseconds;
        _timer.Restart();
        _events.Add(new MacroEvent { IsPress = press, Code = code, Delay = delay });
    }

    #endregion

    #region Playback Logic

    private async void PlaySelected()
    {
        if (_macroList?.SelectedItem is not string selectedName)
        {
            SetStatus("No macro selected.");
            return;
        }

        var macro = MacroManager.Macros.FirstOrDefault(m => m.Name == selectedName);
        if (macro == null)
        {
            SetStatus($"Error: Could not find macro '{selectedName}'.");
            return;
        }

        if (!int.TryParse(_repeatBox?.Text, out var repeat) || repeat < 1)
        {
            repeat = 1;
        }

        if (_playButton != null) _playButton.IsEnabled = false;
        SetStatus($"Playing '{macro.Name}' {repeat}x...");

        for (int i = 0; i < repeat; i++)
        {
            await PlayAsync(macro);
        }

        SetStatus($"Finished playing '{macro.Name}'.");
        if (_playButton != null) _playButton.IsEnabled = true;
    }

    private async Task PlayAsync(Macro macro)
    {
        foreach (var ev in macro.Events)
        {
            if (ev.Delay > 0)
            {
                await Task.Delay(ev.Delay);
            }

            if (ev.IsPress)
                _simulator.SimulateKeyPress(ev.Code);
            else
                _simulator.SimulateKeyRelease(ev.Code);
        }
    }

    #endregion

    #region UI Helpers

    private void DeleteSelected()
    {
        if (_macroList?.SelectedItem is not string selectedName) return;

        var macro = MacroManager.Macros.FirstOrDefault(m => m.Name == selectedName);
        if (macro != null)
        {
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

    #endregion
}
