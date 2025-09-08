using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

namespace Cycloside.Plugins.BuiltIn
{
    internal static class JezzballSound
    {
        private static readonly string SoundDirectory = Path.Combine(AppContext.BaseDirectory, "Resources", "Sounds", "Jezzball");

        // A map from the sound event to the filename.
        private static readonly Dictionary<JezzballSoundEvent, string> SoundFiles = new()
        {
            [JezzballSoundEvent.Click] = "click.wav",
            [JezzballSoundEvent.WallBuild] = "build.wav",
            [JezzballSoundEvent.WallHit] = "hit.wav",
            [JezzballSoundEvent.WallBreak] = "break.wav",
            [JezzballSoundEvent.BallBounce] = "bounce.wav",
            [JezzballSoundEvent.LevelComplete] = "complete.wav"
        };

        public static void Play(JezzballSoundEvent ev)
        {
            try
            {
                if (!SoundFiles.TryGetValue(ev, out var fileName)) return;

                var filePath = Path.Combine(SoundDirectory, fileName);
                if (!File.Exists(filePath))
                {
                    // Silently fail if sound file is missing.
                    return;
                }

                // Use a new player each time to allow overlapping sounds.
                var player = new WaveOutEvent { DesiredLatency = 100 };
                var reader = new AudioFileReader(filePath);

                player.Init(reader);
                player.PlaybackStopped += (s, a) => { reader.Dispose(); player.Dispose(); };
                player.Play();
            }
            catch (Exception)
            {
                // Ignore sound errors to prevent game crashes.
            }
        }
    }
}