# Scene Graph

**Status:** Phase 1 foundation. Full compositing pipeline planned for later.

## Overview

The scene graph provides a hierarchy of renderable nodes for future compositing, effects, and plugin integration.

## Types

### ISceneTarget

Abstraction for anything that can receive window effects (position, opacity, bounds). Implemented by:

- `WindowSceneAdapter` — wraps Avalonia `Window`
- `SceneNode` — scene graph node (for future use)

### SceneGraph

Singleton root (`SceneGraph.Instance`) managing the node hierarchy. Use `GetRenderOrder()` for layer- and z-order-sorted enumeration.

### SceneNode

Implements `ISceneTarget`. Supports:

- Parent/child hierarchy
- `Layer` (Desktop=0, Plugin=100, Dialog=200, Overlay=300)
- `ZIndex` for ordering within a layer
- Optional `IRenderTarget` for offscreen rendering (stub)

### IRenderTarget

Stub interface for future offscreen rendering.

## Effect Migration

All 17 window effects now use `ISceneTarget` via `IWindowEffect.Attach(ISceneTarget)` and `Detach(ISceneTarget)`. `WindowEffectsManager` creates `WindowSceneAdapter` from `Window` when attaching effects.

## Future Work

- Wire SceneGraph into main window lifecycle
- Plugin-created SceneNodes for custom overlays
- IRenderTarget implementation for compositing
