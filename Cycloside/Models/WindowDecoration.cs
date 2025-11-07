using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

namespace Cycloside.Models;

/// <summary>
/// Represents a custom window decoration theme (WindowBlinds-style)
/// Allows complete customization of window frames, title bars, and buttons
/// </summary>
public class WindowDecoration
{
    /// <summary>Theme name</summary>
    public string Name { get; set; } = "Default";

    /// <summary>Theme author</summary>
    public string? Author { get; set; }

    /// <summary>Theme version</summary>
    public string? Version { get; set; }

    /// <summary>Theme description</summary>
    public string? Description { get; set; }

    // --- Title Bar Components ---

    /// <summary>Active title bar background (left edge)</summary>
    public Bitmap? TitleBarActiveLeft { get; set; }

    /// <summary>Active title bar background (center/fill)</summary>
    public Bitmap? TitleBarActiveCenter { get; set; }

    /// <summary>Active title bar background (right edge)</summary>
    public Bitmap? TitleBarActiveRight { get; set; }

    /// <summary>Inactive title bar background (left edge)</summary>
    public Bitmap? TitleBarInactiveLeft { get; set; }

    /// <summary>Inactive title bar background (center/fill)</summary>
    public Bitmap? TitleBarInactiveCenter { get; set; }

    /// <summary>Inactive title bar background (right edge)</summary>
    public Bitmap? TitleBarInactiveRight { get; set; }

    // --- Window Frame Components ---

    /// <summary>Left border bitmap (tileable)</summary>
    public Bitmap? BorderLeft { get; set; }

    /// <summary>Right border bitmap (tileable)</summary>
    public Bitmap? BorderRight { get; set; }

    /// <summary>Top border bitmap (tileable)</summary>
    public Bitmap? BorderTop { get; set; }

    /// <summary>Bottom border bitmap (tileable)</summary>
    public Bitmap? BorderBottom { get; set; }

    /// <summary>Top-left corner bitmap</summary>
    public Bitmap? CornerTopLeft { get; set; }

    /// <summary>Top-right corner bitmap</summary>
    public Bitmap? CornerTopRight { get; set; }

    /// <summary>Bottom-left corner bitmap</summary>
    public Bitmap? CornerBottomLeft { get; set; }

    /// <summary>Bottom-right corner bitmap</summary>
    public Bitmap? CornerBottomRight { get; set; }

    // --- Button Components (Normal, Hover, Pressed states) ---

    /// <summary>Close button - normal state</summary>
    public Bitmap? CloseButtonNormal { get; set; }

    /// <summary>Close button - hover state</summary>
    public Bitmap? CloseButtonHover { get; set; }

    /// <summary>Close button - pressed state</summary>
    public Bitmap? CloseButtonPressed { get; set; }

    /// <summary>Maximize button - normal state</summary>
    public Bitmap? MaximizeButtonNormal { get; set; }

    /// <summary>Maximize button - hover state</summary>
    public Bitmap? MaximizeButtonHover { get; set; }

    /// <summary>Maximize button - pressed state</summary>
    public Bitmap? MaximizeButtonPressed { get; set; }

    /// <summary>Minimize button - normal state</summary>
    public Bitmap? MinimizeButtonNormal { get; set; }

    /// <summary>Minimize button - hover state</summary>
    public Bitmap? MinimizeButtonHover { get; set; }

    /// <summary>Minimize button - pressed state</summary>
    public Bitmap? MinimizeButtonPressed { get; set; }

    /// <summary>Restore button - normal state (for maximized windows)</summary>
    public Bitmap? RestoreButtonNormal { get; set; }

    /// <summary>Restore button - hover state</summary>
    public Bitmap? RestoreButtonHover { get; set; }

    /// <summary>Restore button - pressed state</summary>
    public Bitmap? RestoreButtonPressed { get; set; }

    // --- Configuration Properties ---

    /// <summary>Title bar height in pixels</summary>
    public int TitleBarHeight { get; set; } = 30;

    /// <summary>Border width in pixels</summary>
    public int BorderWidth { get; set; } = 4;

    /// <summary>Button width in pixels</summary>
    public int ButtonWidth { get; set; } = 46;

    /// <summary>Button height in pixels</summary>
    public int ButtonHeight { get; set; } = 30;

    /// <summary>Spacing between buttons in pixels</summary>
    public int ButtonSpacing { get; set; } = 2;

    /// <summary>Title text color when window is active</summary>
    public Color TitleColorActive { get; set; } = Colors.White;

    /// <summary>Title text color when window is inactive</summary>
    public Color TitleColorInactive { get; set; } = Colors.Gray;

    /// <summary>Title font family</summary>
    public string TitleFontFamily { get; set; } = "Segoe UI";

    /// <summary>Title font size</summary>
    public double TitleFontSize { get; set; } = 12;

    /// <summary>Title font weight (Normal, Bold, etc.)</summary>
    public string TitleFontWeight { get; set; } = "SemiBold";

    /// <summary>Left margin for title text</summary>
    public int TitleMarginLeft { get; set; } = 10;

    /// <summary>Right margin for title text (before buttons)</summary>
    public int TitleMarginRight { get; set; } = 10;

    /// <summary>Enable drop shadow on title text</summary>
    public bool TitleTextShadow { get; set; } = true;

    /// <summary>Title text shadow color</summary>
    public Color TitleTextShadowColor { get; set; } = Colors.Black;

    /// <summary>Enable window glow effect</summary>
    public bool EnableGlow { get; set; } = false;

    /// <summary>Glow color for active window</summary>
    public Color GlowColor { get; set; } = Color.FromArgb(255, 0, 120, 215);

    /// <summary>Window corners rounded or square</summary>
    public bool RoundedCorners { get; set; } = false;

    /// <summary>Corner radius in pixels (if rounded)</summary>
    public int CornerRadius { get; set; } = 8;

    /// <summary>Custom bitmaps by name (for extensions)</summary>
    public Dictionary<string, Bitmap> CustomBitmaps { get; set; } = new();

    /// <summary>Additional configuration properties</summary>
    public Dictionary<string, string> Properties { get; set; } = new();
}

/// <summary>
/// Button position enumeration
/// </summary>
public enum ButtonPosition
{
    Left,
    Right
}

/// <summary>
/// Window decoration configuration
/// </summary>
public class WindowDecorationConfig
{
    /// <summary>Enable custom window decorations</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Active theme name</summary>
    public string? ActiveTheme { get; set; }

    /// <summary>Apply to all windows or only specific ones</summary>
    public bool ApplyToAllWindows { get; set; } = true;

    /// <summary>Window names/types to include (if not applying to all)</summary>
    public List<string> IncludedWindows { get; set; } = new();

    /// <summary>Window names/types to exclude</summary>
    public List<string> ExcludedWindows { get; set; } = new();

    /// <summary>Button position (left or right side of title bar)</summary>
    public ButtonPosition ButtonPosition { get; set; } = ButtonPosition.Right;

    /// <summary>Button order (e.g., "minimize,maximize,close")</summary>
    public string ButtonOrder { get; set; } = "minimize,maximize,close";

    /// <summary>Enable animations for buttons</summary>
    public bool EnableButtonAnimations { get; set; } = true;

    /// <summary>Animation duration in milliseconds</summary>
    public int AnimationDuration { get; set; } = 150;

    /// <summary>Enable window fade in/out</summary>
    public bool EnableWindowFade { get; set; } = true;

    /// <summary>Fade duration in milliseconds</summary>
    public int FadeDuration { get; set; } = 200;
}
