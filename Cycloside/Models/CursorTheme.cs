using Avalonia;
using Avalonia.Platform;
using System;
using System.Collections.Generic;

namespace Cycloside.Models
{
    /// <summary>
    /// Represents a cursor theme with multiple cursor types and optional animations.
    /// Inspired by Windows CursorFX and classic .cur/.ani cursor themes.
    /// </summary>
    public class CursorTheme
    {
        /// <summary>
        /// Theme metadata
        /// </summary>
        public string Name { get; set; } = "Default";
        public string Author { get; set; } = "Unknown";
        public string Version { get; set; } = "1.0";
        public string Description { get; set; } = "";

        /// <summary>
        /// Standard cursor types matching Windows cursor roles
        /// Each cursor can be a static image or an animated sequence
        /// </summary>

        // Arrow - Standard pointer
        public CursorDefinition? Arrow { get; set; }

        // Hand - Link/clickable element pointer
        public CursorDefinition? Hand { get; set; }

        // IBeam - Text selection cursor
        public CursorDefinition? IBeam { get; set; }

        // Wait - System busy (spinning/hourglass)
        public CursorDefinition? Wait { get; set; }

        // AppStarting - Background busy (arrow + hourglass)
        public CursorDefinition? AppStarting { get; set; }

        // Cross - Precision selection (crosshair)
        public CursorDefinition? Cross { get; set; }

        // Resize cursors (8 directions + size all)
        public CursorDefinition? SizeNS { get; set; }       // North-South (vertical resize)
        public CursorDefinition? SizeEW { get; set; }       // East-West (horizontal resize)
        public CursorDefinition? SizeNESW { get; set; }     // Northeast-Southwest diagonal
        public CursorDefinition? SizeNWSE { get; set; }     // Northwest-Southeast diagonal
        public CursorDefinition? SizeAll { get; set; }      // Move/pan in all directions

        // Special cursors
        public CursorDefinition? No { get; set; }           // Not allowed/invalid
        public CursorDefinition? Help { get; set; }         // Help (arrow + question mark)
        public CursorDefinition? UpArrow { get; set; }      // Alternative select

        /// <summary>
        /// Theme configuration
        /// </summary>
        public bool UseSystemCursorsAsFallback { get; set; } = true;
        public int AnimationFrameRate { get; set; } = 30; // FPS for animated cursors

        /// <summary>
        /// Returns all defined cursors in this theme
        /// </summary>
        public Dictionary<CursorType, CursorDefinition> GetAllCursors()
        {
            var cursors = new Dictionary<CursorType, CursorDefinition>();

            if (Arrow != null) cursors[CursorType.Arrow] = Arrow;
            if (Hand != null) cursors[CursorType.Hand] = Hand;
            if (IBeam != null) cursors[CursorType.IBeam] = IBeam;
            if (Wait != null) cursors[CursorType.Wait] = Wait;
            if (AppStarting != null) cursors[CursorType.AppStarting] = AppStarting;
            if (Cross != null) cursors[CursorType.Cross] = Cross;
            if (SizeNS != null) cursors[CursorType.SizeNS] = SizeNS;
            if (SizeEW != null) cursors[CursorType.SizeEW] = SizeEW;
            if (SizeNESW != null) cursors[CursorType.SizeNESW] = SizeNESW;
            if (SizeNWSE != null) cursors[CursorType.SizeNWSE] = SizeNWSE;
            if (SizeAll != null) cursors[CursorType.SizeAll] = SizeAll;
            if (No != null) cursors[CursorType.No] = No;
            if (Help != null) cursors[CursorType.Help] = Help;
            if (UpArrow != null) cursors[CursorType.UpArrow] = UpArrow;

            return cursors;
        }
    }

    /// <summary>
    /// Defines a single cursor (static or animated)
    /// </summary>
    public class CursorDefinition
    {
        /// <summary>
        /// Cursor frames (single frame for static, multiple for animated)
        /// </summary>
        public List<CursorFrame> Frames { get; set; } = new();

        /// <summary>
        /// Hotspot (click point) relative to cursor image top-left
        /// Default is (0, 0) which is top-left corner
        /// </summary>
        public PixelPoint Hotspot { get; set; } = new PixelPoint(0, 0);

        /// <summary>
        /// Whether this cursor is animated
        /// </summary>
        public bool IsAnimated => Frames.Count > 1;

        /// <summary>
        /// Display duration for each frame (in milliseconds)
        /// Only used for animated cursors
        /// </summary>
        public int FrameDuration { get; set; } = 100; // Default 100ms per frame
    }

    /// <summary>
    /// Single frame of a cursor (for animation support)
    /// </summary>
    public class CursorFrame
    {
        /// <summary>
        /// Path to the cursor image file (PNG recommended)
        /// </summary>
        public string ImagePath { get; set; } = "";

        /// <summary>
        /// Optional: Duration override for this specific frame (in milliseconds)
        /// If null, uses the CursorDefinition's FrameDuration
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// Cached bitmap data (loaded at runtime)
        /// </summary>
        public byte[]? ImageData { get; set; }
    }

    /// <summary>
    /// Cursor types matching standard Windows cursor roles
    /// </summary>
    public enum CursorType
    {
        Arrow,          // Standard pointer
        Hand,           // Link/clickable
        IBeam,          // Text selection
        Wait,           // Busy
        AppStarting,    // Background busy
        Cross,          // Precision
        SizeNS,         // Vertical resize
        SizeEW,         // Horizontal resize
        SizeNESW,       // Diagonal resize NE-SW
        SizeNWSE,       // Diagonal resize NW-SE
        SizeAll,        // Move/pan
        No,             // Not allowed
        Help,           // Help
        UpArrow         // Alternative select
    }

    /// <summary>
    /// Maps CursorType to common cursor names used in theme files
    /// </summary>
    public static class CursorTypeNames
    {
        private static readonly Dictionary<string, CursorType> NameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // Standard names
            { "arrow", CursorType.Arrow },
            { "pointer", CursorType.Arrow },
            { "default", CursorType.Arrow },

            { "hand", CursorType.Hand },
            { "link", CursorType.Hand },
            { "pointing_hand", CursorType.Hand },

            { "ibeam", CursorType.IBeam },
            { "text", CursorType.IBeam },
            { "xterm", CursorType.IBeam },

            { "wait", CursorType.Wait },
            { "busy", CursorType.Wait },
            { "hourglass", CursorType.Wait },
            { "watch", CursorType.Wait },

            { "appstarting", CursorType.AppStarting },
            { "progress", CursorType.AppStarting },
            { "working", CursorType.AppStarting },

            { "cross", CursorType.Cross },
            { "crosshair", CursorType.Cross },
            { "precision", CursorType.Cross },

            { "size_ns", CursorType.SizeNS },
            { "size_ver", CursorType.SizeNS },
            { "ns-resize", CursorType.SizeNS },
            { "row-resize", CursorType.SizeNS },

            { "size_ew", CursorType.SizeEW },
            { "size_hor", CursorType.SizeEW },
            { "ew-resize", CursorType.SizeEW },
            { "col-resize", CursorType.SizeEW },

            { "size_nesw", CursorType.SizeNESW },
            { "nesw-resize", CursorType.SizeNESW },

            { "size_nwse", CursorType.SizeNWSE },
            { "nwse-resize", CursorType.SizeNWSE },

            { "size_all", CursorType.SizeAll },
            { "move", CursorType.SizeAll },
            { "all-scroll", CursorType.SizeAll },

            { "no", CursorType.No },
            { "not-allowed", CursorType.No },
            { "forbidden", CursorType.No },
            { "circle-slash", CursorType.No },

            { "help", CursorType.Help },
            { "question", CursorType.Help },

            { "uparrow", CursorType.UpArrow },
            { "up_arrow", CursorType.UpArrow }
        };

        public static CursorType? FromString(string name)
        {
            return NameMap.TryGetValue(name, out var type) ? type : null;
        }

        public static string ToString(CursorType type)
        {
            return type switch
            {
                CursorType.Arrow => "arrow",
                CursorType.Hand => "hand",
                CursorType.IBeam => "ibeam",
                CursorType.Wait => "wait",
                CursorType.AppStarting => "appstarting",
                CursorType.Cross => "cross",
                CursorType.SizeNS => "size_ns",
                CursorType.SizeEW => "size_ew",
                CursorType.SizeNESW => "size_nesw",
                CursorType.SizeNWSE => "size_nwse",
                CursorType.SizeAll => "size_all",
                CursorType.No => "no",
                CursorType.Help => "help",
                CursorType.UpArrow => "uparrow",
                _ => "arrow"
            };
        }
    }
}
