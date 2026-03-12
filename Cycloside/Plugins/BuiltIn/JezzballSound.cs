using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cycloside.Services;

namespace Cycloside.Plugins.BuiltIn;

internal static class JezzballSound
{
    public static readonly Dictionary<JezzballSoundEvent, string> Paths = new();
    public static bool Enabled { get; set; } = true;

    private const uint OkTone = 0x00000000;
    private const uint WarningTone = 0x00000030;
    private const uint ErrorTone = 0x00000010;
    private const uint CelebrationTone = 0x00000040;

    public static void Play(JezzballSoundEvent ev)
    {
        if (!Enabled)
        {
            return;
        }

        if (Paths.TryGetValue(ev, out var path))
        {
            AudioService.Play(path);
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            MessageBeep(ev switch
            {
                JezzballSoundEvent.WallHit => WarningTone,
                JezzballSoundEvent.WallBreak => ErrorTone,
                JezzballSoundEvent.LevelComplete => CelebrationTone,
                _ => OkTone
            });
        }
    }

    [DllImport("user32.dll", SetLastError = false)]
    private static extern bool MessageBeep(uint type);
}
