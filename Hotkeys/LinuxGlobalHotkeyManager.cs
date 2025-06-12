using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Hotkeys;

/// <summary>
/// Minimal X11-based global hotkey registration manager.
/// </summary>
public class LinuxGlobalHotkeyManager : IDisposable
{
    private IntPtr _display;
    private IntPtr _rootWindow;
    private readonly Dictionary<(int keycode, uint modifiers), Action> _callbacks = new();
    private Thread? _eventThread;
    private bool _running;

    private const int GrabModeAsync = 1;
    private const int KeyPress = 2;
    private const long KeyPressMask = 1 << 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct XKeyEvent
    {
        public int type;
        public IntPtr serial;
        public bool send_event;
        public IntPtr display;
        public IntPtr window;
        public IntPtr root;
        public IntPtr subwindow;
        public IntPtr time;
        public int x, y;
        public int x_root, y_root;
        public uint state;
        public uint keycode;
        public int same_screen;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct XEvent
    {
        [FieldOffset(0)] public int type;
        [FieldOffset(0)] public XKeyEvent xkey;
    }

    [DllImport("libX11")] private static extern IntPtr XOpenDisplay(IntPtr display);
    [DllImport("libX11")] private static extern int XCloseDisplay(IntPtr display);
    [DllImport("libX11")] private static extern IntPtr XDefaultRootWindow(IntPtr display);
    [DllImport("libX11")] private static extern int XGrabKey(IntPtr display, int keycode, uint modifiers,
        IntPtr grab_window, int owner_events, int pointer_mode, int keyboard_mode);
    [DllImport("libX11")] private static extern int XUngrabKey(IntPtr display, int keycode, uint modifiers, IntPtr grab_window);
    [DllImport("libX11")] private static extern int XNextEvent(IntPtr display, out XEvent xevent);
    [DllImport("libX11")] private static extern int XSelectInput(IntPtr display, IntPtr window, long mask);
    [DllImport("libX11")] private static extern IntPtr XStringToKeysym(string str);
    [DllImport("libX11")] private static extern int XKeysymToKeycode(IntPtr display, IntPtr keysym);

    public LinuxGlobalHotkeyManager()
    {
        _display = XOpenDisplay(IntPtr.Zero);
        if (_display == IntPtr.Zero)
            throw new InvalidOperationException("Unable to open X display");
        _rootWindow = XDefaultRootWindow(_display);
        XSelectInput(_display, _rootWindow, KeyPressMask);
        _running = true;
        _eventThread = new Thread(EventLoop) { IsBackground = true };
        _eventThread.Start();
    }

    public void Register(string keysym, uint modifiers, Action callback)
    {
        var sym = XStringToKeysym(keysym);
        var keycode = XKeysymToKeycode(_display, sym);
        _callbacks[(keycode, modifiers)] = callback;
        XGrabKey(_display, keycode, modifiers, _rootWindow, 1, GrabModeAsync, GrabModeAsync);
    }

    private void EventLoop()
    {
        while (_running)
        {
            if (_display == IntPtr.Zero) break;
            XNextEvent(_display, out var e);
            if (e.type == KeyPress)
            {
                var key = ( (int)e.xkey.keycode, e.xkey.state );
                if (_callbacks.TryGetValue(key, out var cb))
                {
                    try { cb(); } catch { }
                }
            }
        }
    }

    public void Dispose()
    {
        _running = false;
        if (_display != IntPtr.Zero)
        {
            foreach (var kvp in _callbacks)
                XUngrabKey(_display, kvp.Key.keycode, kvp.Key.modifiers, _rootWindow);
            XCloseDisplay(_display);
            _display = IntPtr.Zero;
        }
    }
}
