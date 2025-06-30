using Avalonia.Controls;
using Avalonia.Layout;
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
/// Combines a rich UI for managing multiple macros with a high-fidelity recording engine.
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
    
    // --- Recording Data Fields (from the more accurate implementation) ---
    private readonly List<MacroEvent> _events = new();
    private Stopwatch? _timer;

    // --- IPlugin Properties ---
    public string Name => "Macro Engine";
    public string Description => "Records and plays back keyboard macros with accurate timing.";
    public Version Version => new(2, 0, 0); // Version bumped due to significant merge
    public Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;

    /// <summary>
    /// A private class to hold the data for a single recorded keyboard event.
    /// </summary>
    private class MacroEvent
    {
        public bool IsPress { get; set; }
        public KeyCode Code { get; set; }
        public int Delay { get; set; } // Delay in milliseconds since the previous event
    }

    public void Start()
    {
        // This logic is from the feature-rich 'main' branch.
        BuildUi();
        RefreshList();
        if (!_isWindows)
        {
            SetStatus("Playback uses SharpHook and may need extra permissions.");
        }
    }

    public void Stop()
    {
        // Combined cleanup from both branches.
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

        _recordButton?.AddHandler(Button.ClickEvent, (_, __) => StartRecording());
        _stopButton?.AddHandler(Button.ClickEvent, (_, __) => StopRecording());
        _playButton?.AddHandler(Button.ClickEvent, (_, __) => PlaySelected());
        _window.FindControl<AvaloniaButton>("SaveButton")?.AddHandler(Button.ClickEvent, (_, __) => { MacroManager.Save(); SetStatus("Saved"); });
        _window.FindControl<AvaloniaButton>("ReloadButton")?.AddHandler(Button.ClickEvent, (_, __) => { MacroManager.Reload(); RefreshList(); });
        _window.FindControl<AvaloniaButton>("DeleteButton")?.AddHandler(Button.ClickEvent, (_, __) => DeleteSelected());

        WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(MacroPlugin));
        _window.Show();
    }

    #region Recording Logic (Merged)

    private void StartRecording()
    {
        // Combined logic: Check if already recording, then clear old events and start the timer/hook.
        if (_hook != null) return;

        _events.Clear();
        _timer = Stopwatch.StartNew();
        _hook = new TaskPoolGlobalHook();
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
        _hook.RunAsync();

        SetStatus("Recording...");
        if(_recordButton != null) _recordButton.IsEnabled = false;
        if(_stopButton != null) _stopButton.IsEnabled = true;
    }

    private void StopRecording()
    {
        // Combined logic: Stop the hook/timer, then save the recorded events to the MacroManager.
        if (_hook == null) return;

        _hook.Dispose();
        _hook = null;
        _timer?.Stop();

        var name = _nameBox?.Text;
        if (!string.IsNullOrWhiteSpace(name) && _events.Any())
        {
            // This assumes the Macro class in MacroManager is adapted to store List<MacroEvent>
            MacroManager.Add(new Macro { Name = name!, Events = _events.ToList() });
            RefreshList();
            SetStatus($"Recorded '{name}' with {_events.Count} events.");
        }
        else
        {
            SetStatus("Recording stopped. No name provided or no events recorded.");
        }
        
        if(_recordButton != null) _recordButton.IsEnabled = true;
        if(_stopButton != null) _stopButton.IsEnabled = false;
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

    #region Playback Logic (Merged)

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
        // This high-fidelity playback logic is from the 'codex' branch.
        foreach (var ev in macro.Events)
        {
            if (ev.Delay > 0)
            {
                await Task.Delay(ev.Delay);
            }

            // SharpHook's simulator is cross-platform and more reliable than SendKeys.
            if (ev.IsPress)
                _simulator.SimulateKeyPress(ev.Code);
            else
                _simulator.SimulateKeyRelease(ev.Code);
        }
    }

    #endregion

    #region UI Helpers (from main)

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
            // This assumes MacroManager.Macros is available and holds the macro objects.
            _macroList.ItemsSource = MacroManager.Macros.Select(m => m.Name).ToList();
        }
    }

    private void SetStatus(string msg)
    {
        // Use the dispatcher to ensure UI updates happen on the UI thread.
        Dispatcher.UIThread.InvokeAsync(() => { if (_status != null) _status.Text = msg; });
    }

    #endregion
}

// NOTE: This assumes a `Macro` class and a static `MacroManager` exist elsewhere,
// and that the `Macro` class has been updated to hold a `List<MacroEvent>`:
/*
public class Macro
{
    public string Name { get; set; }
    public List<MacroPlugin.MacroEvent> Events { get; set; }
}
*/
