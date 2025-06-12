using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Input;
using Avalonia.Threading;

namespace Cycloside.Hotkeys;

/// <summary>
/// Windows implementation using RegisterHotKey API.
/// </summary>
internal sealed class WindowsGlobalHotkeyManager : IGlobalHotkeyManager
{
    private const int WM_HOTKEY = 0x0312;
    private IntPtr _window;
    private readonly Dictionary<int, Action> _callbacks = new();
    private int _id;
    private WndProcDelegate? _wndProcDelegate;

    public WindowsGlobalHotkeyManager()
    {
        CreateMessageWindow();
    }

    public void Register(KeyGesture gesture, Action callback)
    {
        var id = ++_id;
        _callbacks[id] = callback;
        RegisterHotKey(_window, id, (uint)ToModifiers(gesture.KeyModifiers), (uint)gesture.Key);
    }

    public void UnregisterAll()
    {
        foreach (var id in _callbacks.Keys)
            UnregisterHotKey(_window, id);
        _callbacks.Clear();
    }

    private void CreateMessageWindow()
    {
        _wndProcDelegate = WindowProc;
        var className = new string("CyclosideHotkeyWnd");
        WNDCLASS wndClass = new()
        {
            lpfnWndProc = _wndProcDelegate,
            lpszClassName = className
        };
        RegisterClass(ref wndClass);
        _window = CreateWindowEx(0, className, string.Empty, 0, 0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
    }

    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_callbacks.TryGetValue(id, out var cb))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    try { cb(); } catch (Exception ex) { Logger.Log($"Hotkey error: {ex.Message}"); }
                });
            }
        }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private static uint ToModifiers(KeyModifiers m)
    {
        uint mods = 0;
        if (m.HasFlag(KeyModifiers.Control)) mods |= MOD_CONTROL;
        if (m.HasFlag(KeyModifiers.Shift)) mods |= MOD_SHIFT;
        if (m.HasFlag(KeyModifiers.Alt)) mods |= MOD_ALT;
        if (m.HasFlag(KeyModifiers.Meta)) mods |= MOD_WIN;
        return mods;
    }

    #region Win32

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct WNDCLASS
    {
        public uint style;
        public WndProcDelegate lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName,
        int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
        IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    #endregion
}
