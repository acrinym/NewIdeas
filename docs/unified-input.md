# Unified Input Queue

**Date:** 2026-03-14
**Source:** [05-WebTV Source Reconnaissance](Cycloside/Campfires/05-WebTV-Source-Reconnaissance.md)

---

## Overview

WebTV-inspired input queue: ring buffer (32 events), device-agnostic, auto-repeat suppression, global modifier tracking.

---

## UnifiedInputQueue

| Member | Description |
|--------|-------------|
| PostInput(evt) | Enqueue event; suppresses auto-repeat when queue has pending events |
| TryGetNextInput(out evt) | Dequeue next event |
| GlobalModifiers | Shift, Control, Alt, CapsLock state |
| SetWakeUp(callback) | Register UI refresh callback |
| PendingCount | Number of queued events |

---

## InputEvent

| Field | Type | Description |
|-------|------|-------------|
| Source | InputSource | Keyboard, Gamepad, Mouse, Midi, Osc |
| Kind | InputKind | KeyDown, KeyUp, ButtonDown, ButtonUp, Axis |
| KeyOrButton | int | Key/button code |
| Modifiers | InputModifiers | Shift, Control, Alt, CapsLock |
| IsAutoRepeat | bool | True if key repeat |
| X, Y | int | Position (for pointer) |

---

## Usage

```csharp
var queue = new UnifiedInputQueue();
queue.SetWakeUp(() => Dispatcher.UIThread.Post(Refresh));

queue.PostInput(new InputEvent
{
    Source = InputSource.Keyboard,
    Kind = InputKind.KeyDown,
    KeyOrButton = 65,
    Modifiers = InputModifiers.None,
    IsAutoRepeat = false
});

if (queue.TryGetNextInput(out var evt))
{
    // Handle evt
}
```

---

## Location

`Cycloside.Input.UnifiedInputQueue` in the Input project.
