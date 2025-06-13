# WindowFX Plugin Example

WindowFX plugins add compositor or physics effects to Cycloside windows. The interface is defined in `Cycloside/Effects/IWindowEffect.cs`. Create a new class library and reference `Cycloside.dll`.

```csharp
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Animation.Easings;
using Cycloside.Effects;
using System;

namespace MyEffects;

public class FadeInEffect : IWindowEffect
{
    public string Name => "FadeIn";
    public string Description => "Fades the window in when shown";

    public void Attach(Window window)
    {
        window.Opacity = 0;
        var anim = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(300),
            Easing = new QuadraticEaseOut(),
            Children = { new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(Window.OpacityProperty, 1d) } } }
        };
        anim.RunAsync(window, null);
    }

    public void Detach(Window window) { }

    public void ApplyEvent(WindowEventType type, object? args) { }
}
```

Compile this into `FadeInEffect.dll` and place it inside the `Cycloside/Effects/` folder. Enable it with:

```csharp
WindowEffectsManager.EnableEffectFor("*", "FadeIn");
```

The effect will apply to every window registered with the manager.
