# Cycloside WindowFX/Compositor Effects – Technical Design

## I. Core Concept
Provide a cross-platform system for adding compositor and physics effects to Cycloside windows, widgets and plugin panels. Effects are modular, configurable and may be extended via plugins.

## II. Core Architecture
### A. Effects Manager
`WindowEffectsManager` tracks which windows or widgets have effects enabled, loads effect plugins and applies them on demand.

### B. Effect Interface
```csharp
public interface IWindowEffect
{
    string Name { get; }
    string Description { get; }
    void Attach(Avalonia.Controls.Window window);
    void Detach(Avalonia.Controls.Window window);
    void ApplyEvent(WindowEventType type, object? args);
}
```

### C. Effect Configuration
Extend the settings file with a dictionary where the key is the component name and the value is a list of enabled effect names:
```csharp
public Dictionary<string, List<string>> WindowEffects { get; set; }
```

## III. Built-In Effects
- Roll‑up/shade
- Wobbly windows
- Explode/minimize/close animations
- Live shadows and blur
- Magic lamp style minimize
- Transparency/opacity control
- Shake/physics distortions

## IV. Technical Notes
- Use Avalonia animation primitives for easing and keyframes.
- Wrap windows or panels in an `EffectHost` so effects can be toggled at runtime.
- GUI settings allow per-component configuration.
- Effects can be distributed as plugins and loaded from an `Effects` directory.

## V. Example: Wobbly Windows
Hook into drag events and apply a spring function so the window lags slightly behind the cursor, snapping smoothly when released.

## VI. Usage Example
```csharp
WindowEffectsManager.RegisterEffect("Wobbly", new WobblyWindowEffect());
WindowEffectsManager.EnableEffectFor("PluginHostWindow", "Wobbly");
```

## VII. Open Sourcing
Place the engine in a `Cycloside.WindowFX` subproject and document the plugin API for community contributions.
