using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Cycloside;

public static class HotkeyManager
{
    private static readonly Dictionary<int, Action> _handlers = new();
    private static int _id = 1;
    private static HotkeyMessageWindow? _window;

    public static bool Register(uint modifiers, uint key, Action callback)
    {
        if (!OperatingSystem.IsWindows())
            return false;
        _window ??= new HotkeyMessageWindow();
        int id = _id++;
        if (RegisterHotKey(IntPtr.Zero, id, modifiers, key))
        {
            _handlers[id] = callback;
            return true;
        }
        return false;
    }

    public static void UnregisterAll()
    {
        if (!OperatingSystem.IsWindows())
            return;
        foreach (var id in _handlers.Keys)
            UnregisterHotKey(IntPtr.Zero, id);
        _handlers.Clear();
        _window?.Dispose();
        _window = null;
    }

    public static void ProcessHotkey(int msg, IntPtr wParam)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY && _handlers.TryGetValue(wParam.ToInt32(), out var cb))
        {
            try { cb(); } catch (Exception ex) { Logger.Log($"Hotkey error: {ex.Message}"); }
        }
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

internal class HotkeyMessageWindow : System.Windows.Forms.NativeWindow, IDisposable
{
    public HotkeyMessageWindow()
    {
        CreateHandle(new System.Windows.Forms.CreateParams());
    }

    protected override void WndProc(ref System.Windows.Forms.Message m)
    {
        HotkeyManager.ProcessHotkey(m.Msg, m.WParam);
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        DestroyHandle();
    }
}
