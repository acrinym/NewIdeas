using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Input;
using SharpHook;
using SharpHook.Native;

namespace Cycloside.Hotkeys;

internal sealed class SharpGlobalHotkeyManager : IDisposable
{
    private IGlobalHook? _hook;
    private readonly List<KeyGesture> _gestures = new();
    private bool _running;

    public event Action<KeyGesture>? HotKeyPressed;

    public void Register(KeyGesture gesture)
    {
        _gestures.Add(gesture);
        if (!_running)
            Start();
    }

    public void UnregisterAll()
    {
        _gestures.Clear();
        if (_running)
            Stop();
    }

    private void Start()
    {
        _hook ??= new TaskPoolGlobalHook();
        _hook.KeyPressed += OnKeyPressed;
        _hook.RunAsync();
        _running = true;
    }

    private void Stop()
    {
        if (_hook != null)
        {
            _hook.KeyPressed -= OnKeyPressed;
            _hook.Dispose();
            _hook = null;
        }
        _running = false;
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        var key = KeyFromCode(e.Data.KeyCode);
        var mods = ModifiersFromMask(e.RawEvent.Mask);
        foreach (var g in _gestures)
        {
            if (g.Key == key && g.KeyModifiers == mods)
            {
                HotKeyPressed?.Invoke(g);
                break;
            }
        }
    }

    private static KeyModifiers ModifiersFromMask(ModifierMask mask)
    {
        KeyModifiers mods = KeyModifiers.None;
        if (ModifierMaskExtensions.HasShift(mask)) mods |= KeyModifiers.Shift;
        if (ModifierMaskExtensions.HasCtrl(mask)) mods |= KeyModifiers.Control;
        if (ModifierMaskExtensions.HasAlt(mask)) mods |= KeyModifiers.Alt;
        if (ModifierMaskExtensions.HasMeta(mask)) mods |= KeyModifiers.Meta;
        return mods;
    }

    private static readonly Dictionary<KeyCode, Key> KeyMap = new()
    {
        { KeyCode.VcA, Key.A },{ KeyCode.VcB, Key.B },{ KeyCode.VcC, Key.C },
        { KeyCode.VcD, Key.D },{ KeyCode.VcE, Key.E },{ KeyCode.VcF, Key.F },
        { KeyCode.VcG, Key.G },{ KeyCode.VcH, Key.H },{ KeyCode.VcI, Key.I },
        { KeyCode.VcJ, Key.J },{ KeyCode.VcK, Key.K },{ KeyCode.VcL, Key.L },
        { KeyCode.VcM, Key.M },{ KeyCode.VcN, Key.N },{ KeyCode.VcO, Key.O },
        { KeyCode.VcP, Key.P },{ KeyCode.VcQ, Key.Q },{ KeyCode.VcR, Key.R },
        { KeyCode.VcS, Key.S },{ KeyCode.VcT, Key.T },{ KeyCode.VcU, Key.U },
        { KeyCode.VcV, Key.V },{ KeyCode.VcW, Key.W },{ KeyCode.VcX, Key.X },
        { KeyCode.VcY, Key.Y },{ KeyCode.VcZ, Key.Z },
        { KeyCode.Vc0, Key.D0 },{ KeyCode.Vc1, Key.D1 },{ KeyCode.Vc2, Key.D2 },
        { KeyCode.Vc3, Key.D3 },{ KeyCode.Vc4, Key.D4 },{ KeyCode.Vc5, Key.D5 },
        { KeyCode.Vc6, Key.D6 },{ KeyCode.Vc7, Key.D7 },{ KeyCode.Vc8, Key.D8 },
        { KeyCode.Vc9, Key.D9 },
        { KeyCode.VcEscape, Key.Escape },
        { KeyCode.VcEnter, Key.Enter },
        { KeyCode.VcSpace, Key.Space },
        { KeyCode.VcTab, Key.Tab },
        { KeyCode.VcBackspace, Key.Back },
        { KeyCode.VcMinus, Key.OemMinus },
        { KeyCode.VcEquals, Key.OemPlus },
        { KeyCode.VcOpenBracket, Key.OemOpenBrackets },
        { KeyCode.VcCloseBracket, Key.OemCloseBrackets },
        { KeyCode.VcSemicolon, Key.OemSemicolon },
        { KeyCode.VcQuote, Key.OemQuotes },
        { KeyCode.VcBackQuote, Key.OemTilde },
        { KeyCode.VcComma, Key.OemComma },
        { KeyCode.VcPeriod, Key.OemPeriod },
        { KeyCode.VcSlash, Key.Oem2 },
        { KeyCode.VcBackslash, Key.Oem5 },
        { KeyCode.VcF1, Key.F1 },{ KeyCode.VcF2, Key.F2 },{ KeyCode.VcF3, Key.F3 },
        { KeyCode.VcF4, Key.F4 },{ KeyCode.VcF5, Key.F5 },{ KeyCode.VcF6, Key.F6 },
        { KeyCode.VcF7, Key.F7 },{ KeyCode.VcF8, Key.F8 },{ KeyCode.VcF9, Key.F9 },
        { KeyCode.VcF10, Key.F10 },{ KeyCode.VcF11, Key.F11 },{ KeyCode.VcF12, Key.F12 }
    };

    private static Key KeyFromCode(KeyCode code)
    {
        return KeyMap.TryGetValue(code, out var k) ? k : Key.None;
    }

    public void Dispose() => Stop();
}
