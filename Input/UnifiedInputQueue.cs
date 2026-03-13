using System;
using System.Collections.Generic;

namespace Cycloside.Input;

/// <summary>
/// WebTV-inspired unified input queue (05-WebTV Phase 1).
/// Ring buffer, device-agnostic, auto-repeat suppression, modifier tracking.
/// </summary>
public sealed class UnifiedInputQueue
{
    private const int QueueSize = 32;
    private readonly InputEvent?[] _buffer = new InputEvent?[QueueSize];
    private int _head;
    private int _tail;
    private int _count;
    private InputModifiers _globalMods;
    private readonly object _lock = new object();
    private Action? _wakeUp;

    /// <summary>
    /// Global modifier state across all devices.
    /// </summary>
    public InputModifiers GlobalModifiers
    {
        get => _globalMods;
        set => _globalMods = value;
    }

    /// <summary>
    /// Register callback to trigger UI refresh when input is posted.
    /// </summary>
    public void SetWakeUp(Action callback)
    {
        _wakeUp = callback;
    }

    /// <summary>
    /// Post input event. Suppresses auto-repeat when queue has pending events.
    /// </summary>
    public void PostInput(InputEvent evt)
    {
        lock (_lock)
        {
            if (evt.IsAutoRepeat && _count > 0)
                return;

            _globalMods = evt.Modifiers;
            if (_count >= QueueSize)
                return;

            _buffer[_tail] = evt;
            _tail = (_tail + 1) % QueueSize;
            _count++;
        }
        _wakeUp?.Invoke();
    }

    /// <summary>
    /// Try to dequeue next input event.
    /// </summary>
    public bool TryGetNextInput(out InputEvent evt)
    {
        lock (_lock)
        {
            if (_count == 0)
            {
                evt = default;
                return false;
            }
            evt = _buffer[_head]!.Value;
            _buffer[_head] = null;
            _head = (_head + 1) % QueueSize;
            _count--;
            return true;
        }
    }

    public int PendingCount
    {
        get { lock (_lock) return _count; }
    }

    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_buffer, 0, QueueSize);
            _head = _tail = _count = 0;
        }
    }
}

/// <summary>
/// Input event for unified queue.
/// </summary>
public struct InputEvent
{
    public InputSource Source;
    public InputKind Kind;
    public int KeyOrButton;
    public InputModifiers Modifiers;
    public bool IsAutoRepeat;
    public int X;
    public int Y;
}

/// <summary>
/// Input source device.
/// </summary>
public enum InputSource
{
    Keyboard,
    Gamepad,
    Mouse,
    Midi,
    Osc,
    Other
}

/// <summary>
/// Input kind (key down, button press, etc.).
/// </summary>
public enum InputKind
{
    KeyDown,
    KeyUp,
    ButtonDown,
    ButtonUp,
    Axis,
    Other
}

/// <summary>
/// Global modifier flags.
/// </summary>
[Flags]
public enum InputModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
    CapsLock = 8,
}
