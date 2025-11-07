using Cycloside.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cycloside.Services
{
    /// <summary>
    /// Manages audio themes for system sounds and event audio.
    /// Provides theme loading, scanning, and playback services.
    /// </summary>
    public class AudioThemeManager
    {
        private static AudioThemeManager? _instance;
        public static AudioThemeManager Instance => _instance ??= new AudioThemeManager();

        private AudioTheme? _currentTheme;
        private readonly string _themesDirectory;
        private readonly List<string> _availableThemes = new();

        /// <summary>
        /// Event fired when the audio theme changes
        /// </summary>
        public event Action<AudioTheme?>? ThemeChanged;

        /// <summary>
        /// Current active audio theme
        /// </summary>
        public AudioTheme? CurrentTheme => _currentTheme;

        /// <summary>
        /// List of available theme paths
        /// </summary>
        public IReadOnlyList<string> AvailableThemes => _availableThemes.AsReadOnly();

        private AudioThemeManager()
        {
            // Determine themes directory based on platform
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _themesDirectory = Path.Combine(appDataPath, "Cycloside", "Themes", "Audio");

            // Ensure directory exists
            Directory.CreateDirectory(_themesDirectory);

            // Scan for available themes
            RefreshAvailableThemes();

            Logger.Log($"📁 Audio themes directory: {_themesDirectory}");
        }

        /// <summary>
        /// Refresh the list of available themes by scanning the themes directory
        /// </summary>
        public void RefreshAvailableThemes()
        {
            _availableThemes.Clear();

            try
            {
                // Scan for theme directories containing theme.ini
                var directories = Directory.GetDirectories(_themesDirectory);
                foreach (var dir in directories)
                {
                    if (File.Exists(Path.Combine(dir, "theme.ini")))
                    {
                        _availableThemes.Add(dir);
                    }
                }

                Logger.Log($"✅ Found {_availableThemes.Count} audio theme(s)");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Error scanning for audio themes: {ex.Message}");
            }
        }

        /// <summary>
        /// Load an audio theme from a directory
        /// </summary>
        /// <param name="path">Path to theme directory</param>
        /// <returns>True if theme loaded successfully</returns>
        public bool LoadTheme(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Logger.Log($"❌ Audio theme directory not found: {path}");
                    return false;
                }

                var theme = LoadThemeFromDirectory(path);
                if (theme == null)
                {
                    Logger.Log($"❌ Failed to load audio theme from: {path}");
                    return false;
                }

                _currentTheme = theme;
                Logger.Log($"✅ Loaded audio theme: {theme.Name}");

                // Notify subscribers that theme changed
                ThemeChanged?.Invoke(theme);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Error loading audio theme: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load theme from directory (simple INI parser)
        /// </summary>
        private AudioTheme? LoadThemeFromDirectory(string directoryPath)
        {
            var theme = new AudioTheme();
            var configFile = Path.Combine(directoryPath, "theme.ini");

            if (!File.Exists(configFile))
            {
                Logger.Log($"⚠️ No theme.ini found in audio theme directory");
                return null;
            }

            var lines = File.ReadAllLines(configFile);
            string currentSection = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip comments and empty lines
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                    continue;

                // Section header
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2).ToLower();
                    continue;
                }

                // Key=Value pair
                var parts = trimmed.Split('=', 2);
                if (parts.Length != 2) continue;

                var key = parts[0].Trim().ToLower();
                var value = parts[1].Trim();

                // Parse based on section
                if (currentSection == "theme")
                {
                    ParseThemeMetadata(theme, key, value);
                }
                else if (currentSection == "sounds")
                {
                    ParseSoundMapping(theme, key, value, directoryPath);
                }
                else if (currentSection == "settings")
                {
                    ParseSettings(theme, key, value);
                }
            }

            return theme;
        }

        private void ParseThemeMetadata(AudioTheme theme, string key, string value)
        {
            switch (key)
            {
                case "name":
                    theme.Name = value;
                    break;
                case "author":
                    theme.Author = value;
                    break;
                case "version":
                    theme.Version = value;
                    break;
                case "description":
                    theme.Description = value;
                    break;
            }
        }

        private void ParseSoundMapping(AudioTheme theme, string key, string value, string basePath)
        {
            // Resolve full path to sound file
            var soundPath = Path.Combine(basePath, value);

            var soundEvent = SoundEventNames.FromString(key);
            if (!soundEvent.HasValue) return;

            // Map to theme property
            switch (soundEvent.Value)
            {
                case SoundEvent.SystemStartup: theme.SystemStartup = soundPath; break;
                case SoundEvent.SystemShutdown: theme.SystemShutdown = soundPath; break;
                case SoundEvent.SystemLogon: theme.SystemLogon = soundPath; break;
                case SoundEvent.SystemLogoff: theme.SystemLogoff = soundPath; break;
                case SoundEvent.UIClick: theme.UIClick = soundPath; break;
                case SoundEvent.UIHover: theme.UIHover = soundPath; break;
                case SoundEvent.UIOpen: theme.UIOpen = soundPath; break;
                case SoundEvent.UIClose: theme.UIClose = soundPath; break;
                case SoundEvent.UIMinimize: theme.UIMinimize = soundPath; break;
                case SoundEvent.UIMaximize: theme.UIMaximize = soundPath; break;
                case SoundEvent.UIRestore: theme.UIRestore = soundPath; break;
                case SoundEvent.NotificationInfo: theme.NotificationInfo = soundPath; break;
                case SoundEvent.NotificationWarning: theme.NotificationWarning = soundPath; break;
                case SoundEvent.NotificationError: theme.NotificationError = soundPath; break;
                case SoundEvent.NotificationSuccess: theme.NotificationSuccess = soundPath; break;
                case SoundEvent.NotificationIncoming: theme.NotificationIncoming = soundPath; break;
                case SoundEvent.FileOpen: theme.FileOpen = soundPath; break;
                case SoundEvent.FileSave: theme.FileSave = soundPath; break;
                case SoundEvent.FileDelete: theme.FileDelete = soundPath; break;
                case SoundEvent.FileMove: theme.FileMove = soundPath; break;
                case SoundEvent.FileCopy: theme.FileCopy = soundPath; break;
                case SoundEvent.NavigateForward: theme.NavigateForward = soundPath; break;
                case SoundEvent.NavigateBackward: theme.NavigateBackward = soundPath; break;
                case SoundEvent.NavigateRefresh: theme.NavigateRefresh = soundPath; break;
                case SoundEvent.DialogOpen: theme.DialogOpen = soundPath; break;
                case SoundEvent.DialogClose: theme.DialogClose = soundPath; break;
                case SoundEvent.DialogOK: theme.DialogOK = soundPath; break;
                case SoundEvent.DialogCancel: theme.DialogCancel = soundPath; break;
                case SoundEvent.MenuOpen: theme.MenuOpen = soundPath; break;
                case SoundEvent.MenuClose: theme.MenuClose = soundPath; break;
                case SoundEvent.MenuClick: theme.MenuClick = soundPath; break;
                case SoundEvent.AchievementUnlocked: theme.AchievementUnlocked = soundPath; break;
                case SoundEvent.LevelUp: theme.LevelUp = soundPath; break;
                case SoundEvent.TaskComplete: theme.TaskComplete = soundPath; break;
                case SoundEvent.TaskFailed: theme.TaskFailed = soundPath; break;
                case SoundEvent.PluginStart: theme.PluginStart = soundPath; break;
                case SoundEvent.PluginStop: theme.PluginStop = soundPath; break;
                case SoundEvent.ThemeChanged: theme.ThemeChanged = soundPath; break;
                case SoundEvent.SettingsSaved: theme.SettingsSaved = soundPath; break;
            }
        }

        private void ParseSettings(AudioTheme theme, string key, string value)
        {
            switch (key)
            {
                case "mastervolume":
                    theme.MasterVolume = float.TryParse(value, out var mv) ? mv : 1.0f;
                    break;
                case "enablesounds":
                    theme.EnableSounds = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "playstartupsound":
                    theme.PlayStartupSound = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "playshutdownsound":
                    theme.PlayShutdownSound = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "systemvolume":
                    theme.SystemVolume = float.TryParse(value, out var sv) ? sv : 1.0f;
                    break;
                case "uivolume":
                    theme.UIVolume = float.TryParse(value, out var uiv) ? uiv : 0.5f;
                    break;
                case "notificationvolume":
                    theme.NotificationVolume = float.TryParse(value, out var nv) ? nv : 0.8f;
                    break;
                case "effectsvolume":
                    theme.EffectsVolume = float.TryParse(value, out var ev) ? ev : 0.7f;
                    break;
            }
        }

        /// <summary>
        /// Get sound file path for a specific event
        /// </summary>
        public string? GetSoundForEvent(SoundEvent soundEvent)
        {
            if (_currentTheme == null) return null;

            var sounds = _currentTheme.GetAllSounds();
            return sounds.TryGetValue(soundEvent, out var path) ? path : null;
        }

        /// <summary>
        /// Play a sound for a specific event (placeholder - actual implementation would use audio library)
        /// </summary>
        public void PlaySound(SoundEvent soundEvent, float volumeMultiplier = 1.0f)
        {
            if (_currentTheme == null || !_currentTheme.EnableSounds)
                return;

            var soundPath = GetSoundForEvent(soundEvent);
            if (soundPath == null || !File.Exists(soundPath))
                return;

            // Calculate final volume
            var categoryVolume = GetCategoryVolume(soundEvent);
            var finalVolume = _currentTheme.MasterVolume * categoryVolume * volumeMultiplier;

            // TODO: Implement actual audio playback using NAudio, AvaloniaAudio, or similar
            Logger.Log($"🔊 Playing sound: {Path.GetFileName(soundPath)} (volume: {finalVolume:F2})");
        }

        private float GetCategoryVolume(SoundEvent soundEvent)
        {
            if (_currentTheme == null) return 1.0f;

            // Determine which volume category this sound belongs to
            return soundEvent switch
            {
                SoundEvent.SystemStartup or SoundEvent.SystemShutdown or
                SoundEvent.SystemLogon or SoundEvent.SystemLogoff
                    => _currentTheme.SystemVolume,

                SoundEvent.UIClick or SoundEvent.UIHover or SoundEvent.UIOpen or
                SoundEvent.UIClose or SoundEvent.UIMinimize or SoundEvent.UIMaximize or
                SoundEvent.UIRestore or SoundEvent.DialogOpen or SoundEvent.DialogClose or
                SoundEvent.DialogOK or SoundEvent.DialogCancel or SoundEvent.MenuOpen or
                SoundEvent.MenuClose or SoundEvent.MenuClick
                    => _currentTheme.UIVolume,

                SoundEvent.NotificationInfo or SoundEvent.NotificationWarning or
                SoundEvent.NotificationError or SoundEvent.NotificationSuccess or
                SoundEvent.NotificationIncoming
                    => _currentTheme.NotificationVolume,

                _ => _currentTheme.EffectsVolume
            };
        }

        /// <summary>
        /// Unload the current theme (disable sounds)
        /// </summary>
        public void UnloadTheme()
        {
            _currentTheme = null;
            Logger.Log("✅ Unloaded audio theme (sounds disabled)");
            ThemeChanged?.Invoke(null);
        }
    }
}
