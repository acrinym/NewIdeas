using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Input;

namespace Cycloside.Hotkeys;

internal sealed class MacGlobalHotkeyManager
{
    private delegate void NativeCallback(ushort keyCode, ulong modifiers);

    [DllImport("libHotkeyMonitor", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr RegisterGlobalHotkeyMonitor(NativeCallback cb);

    [DllImport("libHotkeyMonitor", CallingConvention = CallingConvention.Cdecl)]
    private static extern void UnregisterGlobalHotkeyMonitor(IntPtr handle);

    private readonly List<KeyGesture> _gestures = new();
    private NativeCallback? _callback;
    private IntPtr _monitor;

    public event Action<KeyGesture>? HotKeyPressed;

    public void Register(KeyGesture gesture)
    {
        _gestures.Add(gesture);
        if (_monitor == IntPtr.Zero)
            Start();
    }

    public void UnregisterAll()
    {
        _gestures.Clear();
        if (_monitor != IntPtr.Zero)
        {
            try { UnregisterGlobalHotkeyMonitor(_monitor); } catch (Exception ex) { Logger.Log($"Mac monitor cleanup failed: {ex.Message}"); }
            _monitor = IntPtr.Zero;
        }
        _callback = null;
    }

    private void Start()
    {
        _callback = OnKeyEvent;
        _monitor = RegisterGlobalHotkeyMonitor(_callback);
    }

    private void OnKeyEvent(ushort keyCode, ulong modifiers)
    {
        var key = KeyFromCode(keyCode);
        var mods = ModifiersFromFlags(modifiers);
        foreach (var g in _gestures)
        {
            if (g.Key == key && g.KeyModifiers == mods)
            {
                HotKeyPressed?.Invoke(g);
                break;
            }
        }
    }

    private static KeyModifiers ModifiersFromFlags(ulong flags)
    {
        KeyModifiers mods = KeyModifiers.None;
        if ((flags & (1UL << 17)) != 0) mods |= KeyModifiers.Shift;
        if ((flags & (1UL << 18)) != 0) mods |= KeyModifiers.Control;
        if ((flags & (1UL << 19)) != 0) mods |= KeyModifiers.Alt;
        if ((flags & (1UL << 20)) != 0) mods |= KeyModifiers.Meta;
        return mods;
    }

    private static Key KeyFromCode(ushort code)
    {
        return code switch
        {
            0 => Key.A,
            1 => Key.S,
            2 => Key.D,
            3 => Key.F,
            4 => Key.H,
            5 => Key.G,
            6 => Key.Z,
            7 => Key.X,
            8 => Key.C,
            9 => Key.V,
            11 => Key.B,
            12 => Key.Q,
            13 => Key.W,
            14 => Key.E,
            15 => Key.R,
            16 => Key.Y,
            17 => Key.T,
            18 => Key.D1,
            19 => Key.D2,
            20 => Key.D3,
            21 => Key.D4,
            22 => Key.D6,
            23 => Key.D5,
            24 => Key.OemMinus,
            25 => Key.D9,
            26 => Key.D7,
            27 => Key.D8,
            28 => Key.D0,
            29 => Key.OemCloseBrackets,
            30 => Key.O,
            31 => Key.U,
            32 => Key.OemOpenBrackets,
            33 => Key.I,
            34 => Key.P,
            35 => Key.Return,
            36 => Key.L,
            37 => Key.J,
            38 => Key.OemQuotes,
            39 => Key.K,
            40 => Key.OemSemicolon,
            41 => Key.Back,
            42 => Key.OemComma,
            43 => Key.OemPeriod,
            44 => Key.Oem2,
            45 => Key.RightShift,
            46 => Key.N,
            47 => Key.M,
            48 => Key.Tab,
            49 => Key.Space,
            50 => Key.Escape,
            _ => Key.None
        };
    }
}
