using System;
using System.Collections.Generic;

namespace Cycloside.Models
{
    /// <summary>
    /// Represents an audio theme with system sounds and event audio.
    /// Inspired by Windows sound schemes and audio customization.
    /// </summary>
    public class AudioTheme
    {
        /// <summary>
        /// Theme metadata
        /// </summary>
        public string Name { get; set; } = "Default";
        public string Author { get; set; } = "Unknown";
        public string Version { get; set; } = "1.0";
        public string Description { get; set; } = "";

        /// <summary>
        /// System event sounds
        /// Each sound event can have one or more audio files
        /// </summary>

        // Windows system sounds
        public string? SystemStartup { get; set; }          // System start
        public string? SystemShutdown { get; set; }         // System shutdown
        public string? SystemLogon { get; set; }            // User logon
        public string? SystemLogoff { get; set; }           // User logoff

        // UI feedback sounds
        public string? UIClick { get; set; }                // Button/UI click
        public string? UIHover { get; set; }                // Hover over interactive element
        public string? UIOpen { get; set; }                 // Open window/dialog
        public string? UIClose { get; set; }                // Close window/dialog
        public string? UIMinimize { get; set; }             // Minimize window
        public string? UIMaximize { get; set; }             // Maximize window
        public string? UIRestore { get; set; }              // Restore window

        // Notifications
        public string? NotificationInfo { get; set; }       // Information
        public string? NotificationWarning { get; set; }    // Warning
        public string? NotificationError { get; set; }      // Error
        public string? NotificationSuccess { get; set; }    // Success/completion
        public string? NotificationIncoming { get; set; }   // Incoming message/alert

        // File operations
        public string? FileOpen { get; set; }               // Open file
        public string? FileSave { get; set; }               // Save file
        public string? FileDelete { get; set; }             // Delete file
        public string? FileMove { get; set; }               // Move/rename file
        public string? FileCopy { get; set; }               // Copy file

        // Navigation
        public string? NavigateForward { get; set; }        // Navigate forward
        public string? NavigateBackward { get; set; }       // Navigate backward
        public string? NavigateRefresh { get; set; }        // Refresh

        // Dialog boxes
        public string? DialogOpen { get; set; }             // Dialog appears
        public string? DialogClose { get; set; }            // Dialog closes
        public string? DialogOK { get; set; }               // OK button
        public string? DialogCancel { get; set; }           // Cancel button

        // Menu interactions
        public string? MenuOpen { get; set; }               // Menu opens
        public string? MenuClose { get; set; }              // Menu closes
        public string? MenuClick { get; set; }              // Menu item clicked

        // Special events
        public string? AchievementUnlocked { get; set; }    // Achievement/milestone
        public string? LevelUp { get; set; }                // Progress/level up
        public string? TaskComplete { get; set; }           // Task completed
        public string? TaskFailed { get; set; }             // Task failed

        // Application-specific (Cycloside)
        public string? PluginStart { get; set; }            // Plugin started
        public string? PluginStop { get; set; }             // Plugin stopped
        public string? ThemeChanged { get; set; }           // Theme changed
        public string? SettingsSaved { get; set; }          // Settings saved

        /// <summary>
        /// Theme configuration
        /// </summary>
        public float MasterVolume { get; set; } = 1.0f;     // 0.0 to 1.0
        public bool EnableSounds { get; set; } = true;
        public bool PlayStartupSound { get; set; } = true;
        public bool PlayShutdownSound { get; set; } = true;

        /// <summary>
        /// Sound categories for volume control
        /// </summary>
        public float SystemVolume { get; set; } = 1.0f;     // System event sounds
        public float UIVolume { get; set; } = 0.5f;         // UI feedback sounds
        public float NotificationVolume { get; set; } = 0.8f; // Notification sounds
        public float EffectsVolume { get; set; } = 0.7f;    // Special effects

        /// <summary>
        /// Returns all sound mappings in this theme
        /// </summary>
        public Dictionary<SoundEvent, string> GetAllSounds()
        {
            var sounds = new Dictionary<SoundEvent, string>();

            if (SystemStartup != null) sounds[SoundEvent.SystemStartup] = SystemStartup;
            if (SystemShutdown != null) sounds[SoundEvent.SystemShutdown] = SystemShutdown;
            if (SystemLogon != null) sounds[SoundEvent.SystemLogon] = SystemLogon;
            if (SystemLogoff != null) sounds[SoundEvent.SystemLogoff] = SystemLogoff;

            if (UIClick != null) sounds[SoundEvent.UIClick] = UIClick;
            if (UIHover != null) sounds[SoundEvent.UIHover] = UIHover;
            if (UIOpen != null) sounds[SoundEvent.UIOpen] = UIOpen;
            if (UIClose != null) sounds[SoundEvent.UIClose] = UIClose;
            if (UIMinimize != null) sounds[SoundEvent.UIMinimize] = UIMinimize;
            if (UIMaximize != null) sounds[SoundEvent.UIMaximize] = UIMaximize;
            if (UIRestore != null) sounds[SoundEvent.UIRestore] = UIRestore;

            if (NotificationInfo != null) sounds[SoundEvent.NotificationInfo] = NotificationInfo;
            if (NotificationWarning != null) sounds[SoundEvent.NotificationWarning] = NotificationWarning;
            if (NotificationError != null) sounds[SoundEvent.NotificationError] = NotificationError;
            if (NotificationSuccess != null) sounds[SoundEvent.NotificationSuccess] = NotificationSuccess;
            if (NotificationIncoming != null) sounds[SoundEvent.NotificationIncoming] = NotificationIncoming;

            if (FileOpen != null) sounds[SoundEvent.FileOpen] = FileOpen;
            if (FileSave != null) sounds[SoundEvent.FileSave] = FileSave;
            if (FileDelete != null) sounds[SoundEvent.FileDelete] = FileDelete;
            if (FileMove != null) sounds[SoundEvent.FileMove] = FileMove;
            if (FileCopy != null) sounds[SoundEvent.FileCopy] = FileCopy;

            if (NavigateForward != null) sounds[SoundEvent.NavigateForward] = NavigateForward;
            if (NavigateBackward != null) sounds[SoundEvent.NavigateBackward] = NavigateBackward;
            if (NavigateRefresh != null) sounds[SoundEvent.NavigateRefresh] = NavigateRefresh;

            if (DialogOpen != null) sounds[SoundEvent.DialogOpen] = DialogOpen;
            if (DialogClose != null) sounds[SoundEvent.DialogClose] = DialogClose;
            if (DialogOK != null) sounds[SoundEvent.DialogOK] = DialogOK;
            if (DialogCancel != null) sounds[SoundEvent.DialogCancel] = DialogCancel;

            if (MenuOpen != null) sounds[SoundEvent.MenuOpen] = MenuOpen;
            if (MenuClose != null) sounds[SoundEvent.MenuClose] = MenuClose;
            if (MenuClick != null) sounds[SoundEvent.MenuClick] = MenuClick;

            if (AchievementUnlocked != null) sounds[SoundEvent.AchievementUnlocked] = AchievementUnlocked;
            if (LevelUp != null) sounds[SoundEvent.LevelUp] = LevelUp;
            if (TaskComplete != null) sounds[SoundEvent.TaskComplete] = TaskComplete;
            if (TaskFailed != null) sounds[SoundEvent.TaskFailed] = TaskFailed;

            if (PluginStart != null) sounds[SoundEvent.PluginStart] = PluginStart;
            if (PluginStop != null) sounds[SoundEvent.PluginStop] = PluginStop;
            if (ThemeChanged != null) sounds[SoundEvent.ThemeChanged] = ThemeChanged;
            if (SettingsSaved != null) sounds[SoundEvent.SettingsSaved] = SettingsSaved;

            return sounds;
        }
    }

    /// <summary>
    /// Sound event types for audio theme system
    /// </summary>
    public enum SoundEvent
    {
        // System events
        SystemStartup,
        SystemShutdown,
        SystemLogon,
        SystemLogoff,

        // UI feedback
        UIClick,
        UIHover,
        UIOpen,
        UIClose,
        UIMinimize,
        UIMaximize,
        UIRestore,

        // Notifications
        NotificationInfo,
        NotificationWarning,
        NotificationError,
        NotificationSuccess,
        NotificationIncoming,

        // File operations
        FileOpen,
        FileSave,
        FileDelete,
        FileMove,
        FileCopy,

        // Navigation
        NavigateForward,
        NavigateBackward,
        NavigateRefresh,

        // Dialogs
        DialogOpen,
        DialogClose,
        DialogOK,
        DialogCancel,

        // Menus
        MenuOpen,
        MenuClose,
        MenuClick,

        // Special events
        AchievementUnlocked,
        LevelUp,
        TaskComplete,
        TaskFailed,

        // Application-specific
        PluginStart,
        PluginStop,
        ThemeChanged,
        SettingsSaved
    }

    /// <summary>
    /// Maps sound event names to enum values
    /// </summary>
    public static class SoundEventNames
    {
        private static readonly Dictionary<string, SoundEvent> NameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // System events
            { "system_startup", SoundEvent.SystemStartup },
            { "startup", SoundEvent.SystemStartup },
            { "system_shutdown", SoundEvent.SystemShutdown },
            { "shutdown", SoundEvent.SystemShutdown },
            { "system_logon", SoundEvent.SystemLogon },
            { "logon", SoundEvent.SystemLogon },
            { "system_logoff", SoundEvent.SystemLogoff },
            { "logoff", SoundEvent.SystemLogoff },

            // UI feedback
            { "ui_click", SoundEvent.UIClick },
            { "click", SoundEvent.UIClick },
            { "ui_hover", SoundEvent.UIHover },
            { "hover", SoundEvent.UIHover },
            { "ui_open", SoundEvent.UIOpen },
            { "open", SoundEvent.UIOpen },
            { "ui_close", SoundEvent.UIClose },
            { "close", SoundEvent.UIClose },
            { "ui_minimize", SoundEvent.UIMinimize },
            { "minimize", SoundEvent.UIMinimize },
            { "ui_maximize", SoundEvent.UIMaximize },
            { "maximize", SoundEvent.UIMaximize },
            { "ui_restore", SoundEvent.UIRestore },
            { "restore", SoundEvent.UIRestore },

            // Notifications
            { "notification_info", SoundEvent.NotificationInfo },
            { "info", SoundEvent.NotificationInfo },
            { "notification_warning", SoundEvent.NotificationWarning },
            { "warning", SoundEvent.NotificationWarning },
            { "notification_error", SoundEvent.NotificationError },
            { "error", SoundEvent.NotificationError },
            { "notification_success", SoundEvent.NotificationSuccess },
            { "success", SoundEvent.NotificationSuccess },
            { "notification_incoming", SoundEvent.NotificationIncoming },
            { "incoming", SoundEvent.NotificationIncoming },

            // File operations
            { "file_open", SoundEvent.FileOpen },
            { "file_save", SoundEvent.FileSave },
            { "save", SoundEvent.FileSave },
            { "file_delete", SoundEvent.FileDelete },
            { "delete", SoundEvent.FileDelete },
            { "file_move", SoundEvent.FileMove },
            { "move", SoundEvent.FileMove },
            { "file_copy", SoundEvent.FileCopy },
            { "copy", SoundEvent.FileCopy },

            // Navigation
            { "navigate_forward", SoundEvent.NavigateForward },
            { "forward", SoundEvent.NavigateForward },
            { "navigate_backward", SoundEvent.NavigateBackward },
            { "backward", SoundEvent.NavigateBackward },
            { "navigate_refresh", SoundEvent.NavigateRefresh },
            { "refresh", SoundEvent.NavigateRefresh },

            // Dialogs
            { "dialog_open", SoundEvent.DialogOpen },
            { "dialog_close", SoundEvent.DialogClose },
            { "dialog_ok", SoundEvent.DialogOK },
            { "ok", SoundEvent.DialogOK },
            { "dialog_cancel", SoundEvent.DialogCancel },
            { "cancel", SoundEvent.DialogCancel },

            // Menus
            { "menu_open", SoundEvent.MenuOpen },
            { "menu_close", SoundEvent.MenuClose },
            { "menu_click", SoundEvent.MenuClick },

            // Special events
            { "achievement_unlocked", SoundEvent.AchievementUnlocked },
            { "achievement", SoundEvent.AchievementUnlocked },
            { "level_up", SoundEvent.LevelUp },
            { "levelup", SoundEvent.LevelUp },
            { "task_complete", SoundEvent.TaskComplete },
            { "complete", SoundEvent.TaskComplete },
            { "task_failed", SoundEvent.TaskFailed },
            { "failed", SoundEvent.TaskFailed },

            // Application-specific
            { "plugin_start", SoundEvent.PluginStart },
            { "plugin_stop", SoundEvent.PluginStop },
            { "theme_changed", SoundEvent.ThemeChanged },
            { "settings_saved", SoundEvent.SettingsSaved }
        };

        public static SoundEvent? FromString(string name)
        {
            return NameMap.TryGetValue(name, out var soundEvent) ? soundEvent : null;
        }

        public static string ToString(SoundEvent soundEvent)
        {
            return soundEvent switch
            {
                SoundEvent.SystemStartup => "system_startup",
                SoundEvent.SystemShutdown => "system_shutdown",
                SoundEvent.SystemLogon => "system_logon",
                SoundEvent.SystemLogoff => "system_logoff",
                SoundEvent.UIClick => "ui_click",
                SoundEvent.UIHover => "ui_hover",
                SoundEvent.UIOpen => "ui_open",
                SoundEvent.UIClose => "ui_close",
                SoundEvent.UIMinimize => "ui_minimize",
                SoundEvent.UIMaximize => "ui_maximize",
                SoundEvent.UIRestore => "ui_restore",
                SoundEvent.NotificationInfo => "notification_info",
                SoundEvent.NotificationWarning => "notification_warning",
                SoundEvent.NotificationError => "notification_error",
                SoundEvent.NotificationSuccess => "notification_success",
                SoundEvent.NotificationIncoming => "notification_incoming",
                SoundEvent.FileOpen => "file_open",
                SoundEvent.FileSave => "file_save",
                SoundEvent.FileDelete => "file_delete",
                SoundEvent.FileMove => "file_move",
                SoundEvent.FileCopy => "file_copy",
                SoundEvent.NavigateForward => "navigate_forward",
                SoundEvent.NavigateBackward => "navigate_backward",
                SoundEvent.NavigateRefresh => "navigate_refresh",
                SoundEvent.DialogOpen => "dialog_open",
                SoundEvent.DialogClose => "dialog_close",
                SoundEvent.DialogOK => "dialog_ok",
                SoundEvent.DialogCancel => "dialog_cancel",
                SoundEvent.MenuOpen => "menu_open",
                SoundEvent.MenuClose => "menu_close",
                SoundEvent.MenuClick => "menu_click",
                SoundEvent.AchievementUnlocked => "achievement_unlocked",
                SoundEvent.LevelUp => "level_up",
                SoundEvent.TaskComplete => "task_complete",
                SoundEvent.TaskFailed => "task_failed",
                SoundEvent.PluginStart => "plugin_start",
                SoundEvent.PluginStop => "plugin_stop",
                SoundEvent.ThemeChanged => "theme_changed",
                SoundEvent.SettingsSaved => "settings_saved",
                _ => "ui_click"
            };
        }
    }
}
