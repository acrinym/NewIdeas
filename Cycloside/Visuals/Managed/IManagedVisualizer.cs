using System;
using Avalonia;
using Avalonia.Media;
using Cycloside.Plugins.BuiltIn; // AudioData record

namespace Cycloside.Visuals.Managed;

public interface IManagedVisualizer : IDisposable
{
    string Name { get; }
    string Description { get; }

    void Init();
    void UpdateAudioData(AudioData data);
    void Render(DrawingContext context, Size size, TimeSpan elapsed);
}

