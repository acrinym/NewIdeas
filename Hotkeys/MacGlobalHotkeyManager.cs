using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Hotkeys;

/// <summary>
/// Global hotkey manager for macOS based on a CGEventTap.
/// </summary>
public sealed class MacGlobalHotkeyManager : IDisposable
{
    // CGEvent definitions
    private enum CGEventType : uint
    {
        KeyDown = 10,
    }

    private enum CGEventTapLocation
    {
        HID = 0,
        Session = 1,
        AnnotatedSession = 2
    }

    private enum CGEventTapPlacement
    {
        HeadInsert = 0,
        TailAppend = 1
    }

    [Flags]
    private enum CGEventTapOptions
    {
        Default = 0x00000000
    }

    [Flags]
    public enum CGEventFlags : ulong
    {
        MaskShift = 0x00020000,
        MaskControl = 0x00040000,
        MaskAlternate = 0x00080000,
        MaskCommand = 0x00100000
    }

    private enum CGEventField : int
    {
        KeyboardEventKeycode = 9
    }

    private delegate IntPtr CGEventCallback(IntPtr proxy, CGEventType type, IntPtr eventRef, IntPtr userInfo);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGEventTapCreate(CGEventTapLocation tap, CGEventTapPlacement place,
        CGEventTapOptions options, ulong eventsOfInterest, CGEventCallback callback, IntPtr userInfo);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGEventTapEnable(IntPtr tap, bool enable);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern long CGEventGetIntegerValueField(IntPtr @event, CGEventField field);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern ulong CGEventGetFlags(IntPtr @event);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CFMachPortCreateRunLoopSource(IntPtr allocator, IntPtr port, int order);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CFMachPortInvalidate(IntPtr port);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFRunLoopGetCurrent();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopAddSource(IntPtr rl, IntPtr source, IntPtr mode);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopRemoveSource(IntPtr rl, IntPtr source, IntPtr mode);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopRun();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopStop(IntPtr rl);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr obj);

    private const ulong KeyDownMask = 1UL << (int)CGEventType.KeyDown;
    private const CGEventFlags ModifierMask = CGEventFlags.MaskCommand | CGEventFlags.MaskControl |
                                              CGEventFlags.MaskAlternate | CGEventFlags.MaskShift;

    private readonly Dictionary<(int Key, CGEventFlags Mods), Action> _handlers = new();
    private readonly CGEventCallback _callback;
    private readonly Thread _thread;
    private IntPtr _tap;
    private IntPtr _runLoop;
    private IntPtr _source;
    private bool _running;

    public MacGlobalHotkeyManager()
    {
        _callback = HandleEvent;
        _thread = new Thread(EventThread) { IsBackground = true };
        _thread.Start();
        while (_runLoop == IntPtr.Zero)
            Thread.Sleep(10);
    }

    public void Register(int keyCode, CGEventFlags modifiers, Action action)
    {
        _handlers[(keyCode, modifiers & ModifierMask)] = action;
    }

    public void Unregister(int keyCode, CGEventFlags modifiers)
    {
        _handlers.Remove((keyCode, modifiers & ModifierMask));
    }

    private IntPtr HandleEvent(IntPtr proxy, CGEventType type, IntPtr eventRef, IntPtr userInfo)
    {
        if (type != CGEventType.KeyDown)
            return eventRef;

        int key = (int)CGEventGetIntegerValueField(eventRef, CGEventField.KeyboardEventKeycode);
        var mods = (CGEventFlags)CGEventGetFlags(eventRef) & ModifierMask;

        if (_handlers.TryGetValue((key, mods), out var cb))
            cb();

        return eventRef;
    }

    private void EventThread()
    {
        _tap = CGEventTapCreate(CGEventTapLocation.Session, CGEventTapPlacement.HeadInsert,
            CGEventTapOptions.Default, KeyDownMask, _callback, IntPtr.Zero);
        if (_tap == IntPtr.Zero)
            return;
        _source = CFMachPortCreateRunLoopSource(IntPtr.Zero, _tap, 0);
        _runLoop = CFRunLoopGetCurrent();
        CFRunLoopAddSource(_runLoop, _source, IntPtr.Zero);
        CGEventTapEnable(_tap, true);
        _running = true;
        CFRunLoopRun();
        _running = false;
        CGEventTapEnable(_tap, false);
        CFRunLoopRemoveSource(_runLoop, _source, IntPtr.Zero);
        CFMachPortInvalidate(_tap);
        CFRelease(_source);
        CFRelease(_tap);
    }

    public void Dispose()
    {
        if (_running)
        {
            CFRunLoopStop(_runLoop);
            _thread.Join();
        }
    }
}

