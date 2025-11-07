# Window Decoration Themes

This directory contains window decoration themes for Cycloside's WindowBlinds-style theming system.

## What Are Window Decoration Themes?

Window decoration themes customize the appearance of window chrome (title bar, borders, corners, and buttons). They provide a way to give your desktop a unique look inspired by classic Windows versions (XP, 7) or modern design aesthetics.

## Included Sample Themes

### 1. Modern Dark
**Style:** Clean, minimalist dark theme
**Inspiration:** Modern Windows 11 / VS Code dark mode
**Features:** Flat design, subtle shadows, 1px borders, accent color highlights

### 2. Classic XP
**Style:** Windows XP Luna recreation
**Inspiration:** Windows XP (2001-2009)
**Features:** Blue gradient title bars, 3D borders, generous rounded corners, iconic button design

### 3. Aero Glass
**Style:** Windows 7 Aero-inspired
**Inspiration:** Windows Vista/7 (2006-2012)
**Features:** Glass transparency, blur effects, outer glow, reflection effects

## Theme Structure

Each theme is a directory containing:

1. **theme.ini** - Configuration file with all theme settings
2. **PNG files** - Bitmap assets for visual components (need to be created)

### Required PNG Assets

All themes need these bitmap files:

**Title Bar (6 files):**
- `titlebar_active_left.png` - Left edge for focused window
- `titlebar_active_center.png` - Center section (tiles horizontally)
- `titlebar_active_right.png` - Right edge for focused window
- `titlebar_inactive_left.png` - Left edge for unfocused window
- `titlebar_inactive_center.png` - Center section for unfocused
- `titlebar_inactive_right.png` - Right edge for unfocused

**Borders (4 files):**
- `border_top.png` - Top edge (tiles horizontally)
- `border_bottom.png` - Bottom edge (tiles horizontally)
- `border_left.png` - Left edge (tiles vertically)
- `border_right.png` - Right edge (tiles vertically)

**Corners (4 files):**
- `corner_topleft.png` - Top-left corner
- `corner_topright.png` - Top-right corner
- `corner_bottomleft.png` - Bottom-left corner
- `corner_bottomright.png` - Bottom-right corner

**Buttons (12 files - 3 states each):**
- `button_close_normal.png` / `_hover.png` / `_pressed.png`
- `button_maximize_normal.png` / `_hover.png` / `_pressed.png`
- `button_minimize_normal.png` / `_hover.png` / `_pressed.png`
- `button_restore_normal.png` / `_hover.png` / `_pressed.png`

**Total: 30 PNG files per theme**

## Creating Bitmap Assets

### Tools Needed

- **Image Editor:** GIMP (free), Photoshop, Paint.NET, or Krita
- **Transparency Support:** Must support PNG with alpha channel
- **Precision:** Pixel-perfect alignment is important for seamless tiling

### General Guidelines

1. **Use PNG format** with full alpha channel (transparency support)
2. **Match dimensions** specified in theme.ini
3. **Tile-friendly edges** for center sections and borders (seamless repeating)
4. **Consistent style** across all components
5. **Test against varied backgrounds** especially for transparent themes

### Design Tips by Theme

**Modern Dark:**
- Simple solid colors, minimal gradients
- 1x1 pixel PNGs work for most borders (solid color)
- Focus on button icon design (use UTF-8 symbols or simple shapes)
- Borders can be achieved with CSS-style single pixel lines

**Classic XP:**
- Study original Windows XP Luna theme screenshots
- Gradient tool for title bar (blue: #0054E3 → #2E8BE6)
- 3D effect requires highlight and shadow layers
- Buttons have visible borders and backgrounds

**Aero Glass:**
- Most complex - requires understanding of transparency layers
- Use Gaussian blur for glass effect simulation
- Glow extends beyond window bounds (oversized PNGs with transparent padding)
- Reflection gradients add depth
- Consider adding subtle noise texture for realistic glass

### Quick Start: Modern Dark (Easiest)

1. Create a 1x1 PNG with #1E1E1E color → `titlebar_active_center.png`
2. Create 32x32 PNG with #1E1E1E and rounded top-left corner → `titlebar_active_left.png`
3. Create 32x32 PNG with #1E1E1E and rounded top-right corner → `titlebar_active_right.png`
4. Repeat for inactive (use #2D2D2D)
5. Create 1x1 PNGs with #0078D4 for borders
6. Create button PNGs (46x32) with icons (×, □, −)

## Installing Themes

### From This Directory
Themes in this directory are automatically scanned by Cycloside on startup.

### User Themes
You can add your own themes to:
```
~/.local/share/Cycloside/Themes/WindowDecorations/  (Linux)
%APPDATA%/Cycloside/Themes/WindowDecorations/       (Windows)
~/Library/Application Support/Cycloside/Themes/WindowDecorations/  (macOS)
```

### Theme Packages (ZIP)
You can also create ZIP archives:
1. Create ZIP with theme.ini and all PNG files at root level
2. Name it `ThemeName.zip`
3. Import through Cycloside's theme manager

## Using Themes

### In Code
```csharp
// Load a theme
var manager = WindowDecorationManager.Instance;
manager.LoadTheme("path/to/theme/directory");

// Or load from ZIP
manager.LoadTheme("path/to/theme.zip");

// Apply to a window
var decoratedWindow = new DecoratedWindow();
decoratedWindow.Show();
```

### Configuration
Edit the theme's `theme.ini` to customize:
- Colors (title bar, borders, buttons)
- Dimensions (height, width, corner radius)
- Effects (glow, shadow, transparency)
- Behavior (which windows to theme)

## Theme.ini Reference

### [Theme] Section
- `Name` - Display name
- `Author` - Creator name
- `Version` - Version number
- `Description` - Brief description

### [Dimensions] Section
- `TitleBarHeight` - Height in pixels (default: 30)
- `BorderWidth` - Width in pixels (default: 4)
- `CornerRadius` - Corner rounding in pixels (default: 0)

### [Colors] Section
- Support formats: `#RRGGBB` or `R,G,B`
- `TitleColorActive` - Active window title text color
- `TitleColorInactive` - Inactive window title text color
- `BorderColorActive` - Active window border color
- `BorderColorInactive` - Inactive window border color
- `GlowColorActive` - Active window glow color

### [Effects] Section
- `EnableGlow` - Boolean (true/false)
- `EnableShadow` - Boolean
- `EnableTransparency` - Boolean
- `TitleBarOpacity` - Float (0.0 to 1.0)

### [Buttons] Section
- `ButtonWidth` - Button width in pixels
- `ButtonHeight` - Button height in pixels
- `ButtonSpacing` - Space between buttons in pixels
- `CloseButtonHover` - Close button hover color
- `MaximizeButtonHover` - Maximize button hover color
- `MinimizeButtonHover` - Minimize button hover color

### [Behavior] Section
- `ExcludedWindows` - Comma-separated list of window titles to exclude
- `IncludedWindows` - If set, only these windows get themed
- `ApplyToAllWindows` - Boolean (default: true)

## Creating Your Own Theme

1. **Copy a sample theme directory** as starting point
2. **Edit theme.ini** with your desired settings
3. **Create bitmap assets** using image editor
4. **Test iteratively** - load theme and check appearance
5. **Refine** colors, dimensions, and bitmaps until satisfied

## Tips for Best Results

- **Start simple:** Begin with solid colors before adding gradients/effects
- **Test inactive state:** Many themes look great active but poor inactive
- **Consider contrast:** Title text must be readable against title bar
- **Button visibility:** Ensure buttons are clearly visible in all states
- **Edge cases:** Test with very narrow windows and maximized state
- **Multi-monitor:** Verify theme works on different DPI settings

## Resources

### Color Schemes
- **Modern Dark:** VS Code Dark+, GitHub Dark
- **Classic XP:** Windows XP Luna Blue (primary color: #0054E3)
- **Aero Glass:** Windows 7 default (primary color: #4D9EE8)

### Reference Screenshots
Study these for accurate recreation:
- Windows XP: Focus on start menu, explorer windows
- Windows 7: Note the subtle glass blur and glow effects
- Windows 11: Clean, flat design with rounded corners

### Tools
- **GIMP:** Free, powerful, supports all needed features
- **Paint.NET:** Windows-only, simpler than GIMP
- **Krita:** Great for detailed painting and effects
- **Figma:** Design in browser, export to PNG

## Troubleshooting

**Theme doesn't load:**
- Check theme.ini syntax (no typos in section names)
- Verify PNG filenames match exactly (case-sensitive on Linux)
- Ensure PNGs are valid (not corrupted)

**Borders don't tile seamlessly:**
- Edges must be pixel-perfect identical
- Use "offset" tool in GIMP to check tiling
- Consider using 1x1 solid color for simple borders

**Buttons look pixelated:**
- Increase button dimensions in theme.ini
- Use vector tools for icons, then rasterize
- Export at 2x resolution if needed

**Glass effect doesn't look right:**
- Aero requires multiple layers (blur, tint, reflection)
- Test against varied backgrounds
- May need to simulate blur in static image

## Contributing

Want to share your theme? Consider:
1. Ensure all 30 PNGs are included
2. Write comprehensive Design Notes in theme.ini
3. Test on multiple platforms (Linux, Windows, macOS)
4. Include screenshots in theme directory
5. Submit to Cycloside theme gallery

## License

Sample themes (Modern Dark, Classic XP, Aero Glass) are provided as examples under the same license as Cycloside. You're free to modify, redistribute, or use them as basis for your own themes.

Note: Windows XP and Windows 7 are trademarks of Microsoft. Theme recreations are for educational/nostalgic purposes only.
