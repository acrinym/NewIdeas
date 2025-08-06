using System.Collections.Generic;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn;

internal static class JezzballSound
{
    public static readonly Dictionary<JezzballSoundEvent, string> Paths = new();

    public static void Play(JezzballSoundEvent ev)
    {
        if (Paths.TryGetValue(ev, out var path))
        {
            AudioService.Play(path);
        }
    }
}
