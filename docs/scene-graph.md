# Scene Graph

**Date:** 2026-03-14

Effects operate on `ISceneTarget`; `WindowSceneAdapter` wraps `Window`.

---

## ISceneTarget

Abstraction for effect targets. Enables effects on Window, SceneNode, or future surfaces.

| Member | Type | Description |
|--------|------|-------------|
| Bounds | PixelRect | Target bounds |
| Position | PixelPoint | Position (get/set) |
| Opacity | double | Opacity (get/set) |
| IsVisible | bool | Visibility |
| Dispatcher | IDispatcher? | UI thread dispatcher |

---

## WindowSceneAdapter

Wraps an Avalonia `Window` as `ISceneTarget`.

```csharp
var adapter = WindowSceneAdapter.CreateFrom(window);
effect.Attach(adapter);
```

---

## IWindowEffect

Effects use `Attach(ISceneTarget target)` and `Detach(ISceneTarget target)`.

---

## Future: SceneGraph, SceneNode

Planned: hierarchical scene nodes, Z-order, IRenderTarget. Stub for Theater Mode and compositor work.
