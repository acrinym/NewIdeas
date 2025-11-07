# Cursor Themes

This directory contains cursor themes for Cycloside's CursorFX-style cursor customization system.

## What Are Cursor Themes?

Cursor themes replace the system mouse cursor with custom designs. They can include:
- Static cursors (single image)
- Animated cursors (multiple frames)
- All standard cursor types (arrow, hand, text, resize, etc.)

## Included Sample Themes

### 1. Modern Dark
**Style:** Minimalist, flat design
**Features:** Clean white cursors with shadows, modern appearance
**Animations:** Optional smooth spinner
**Best for:** Dark mode desktops, modern interfaces

### 2. Classic XP
**Style:** Windows XP Luna recreation
**Features:** 3D cursors with shading, iconic blue busy animation
**Animations:** 12-frame rotating circle, 8-frame appstarting
**Best for:** Nostalgia, retro computing, classic Windows feel

### 3. Animated Spinner
**Style:** Web-inspired Material Design
**Features:** High frame rate (60 FPS) animations, buttery smooth
**Animations:** 24-frame circular progress, gradient fade
**Best for:** Modern web-style interfaces, smooth animations

## Theme Structure

Each theme is a directory containing:

1. **theme.ini** - Configuration file with metadata and settings
2. **PNG files** - Cursor images (static or animation frames)

### Required Cursor Files

A complete theme includes these cursor types:

**Essential (minimum viable theme):**
- `arrow.png` - Standard pointer (most important)
- `hand.png` - Link/clickable element pointer
- `ibeam.png` - Text selection cursor
- `wait.png` - Busy/loading cursor

**Standard (recommended):**
- `appstarting.png` - Background busy (arrow + indicator)
- `cross.png` - Precision selection/crosshair
- `no.png` - Not allowed/invalid action

**Resize (for complete theme):**
- `size_ns.png` - Vertical resize (North-South)
- `size_ew.png` - Horizontal resize (East-West)
- `size_nesw.png` - Diagonal resize (NE-SW)
- `size_nwse.png` - Diagonal resize (NW-SE)
- `size_all.png` - Move/pan (all directions)

**Optional:**
- `help.png` - Help cursor (arrow + question mark)
- `uparrow.png` - Alternative select

**Minimum:** 4 files | **Recommended:** 10 files | **Complete:** 14 files

### Animation Support

For animated cursors, use frame numbering:

```
wait_001.png
wait_002.png
wait_003.png
...
wait_012.png
```

Frames are played in numerical order. You can mix static and animated cursors in the same theme.

## Theme.ini Format

```ini
[Theme]
Name=My Cursor Theme
Author=Your Name
Version=1.0
Description=A brief description of your theme

[Settings]
UseSystemCursorsAsFallback=true
AnimationFrameRate=30

[Cursors]
# List cursor files (documentation)
arrow=arrow.png
hand=hand.png
wait=wait_001.png  # Animated

[Hotspots]
# Hotspot coordinates (x,y) - the "click point"
arrow=0,0
hand=10,0
ibeam=16,16
wait=16,16

[Animations]
# Optional: Document animation details
wait=12 frames @ 100ms
```

## Creating Cursor Assets

### Tools

- **GIMP** (free) - Full-featured image editor
- **Inkscape** (free) - Vector graphics for clean scaling
- **Krita** (free) - Great for painting and animation
- **Photoshop** - Industry standard (paid)
- **Figma** - Design in browser, export PNGs

### Standard Sizes

- **Small:** 24x24 pixels (classic Windows size)
- **Normal:** 32x32 pixels (most common)
- **Large:** 48x48 pixels (modern, high DPI)

Choose one size and stick with it for consistency. For high DPI displays, use 48x48 or larger.

### Hotspot Guidelines

The hotspot is the "click point" of the cursor:

| Cursor Type | Recommended Hotspot | Explanation |
|-------------|---------------------|-------------|
| Arrow | (0, 0) or (2, 2) | Tip of arrow |
| Hand | (10, 0) or (12, 4) | Tip of pointing finger |
| I-Beam | (center, center) | Middle of vertical bar |
| Wait | (center, center) | Center of spinner/hourglass |
| Cross | (center, center) | Intersection point |
| Resize | (center, center) | Middle of double arrow |
| No | (center, center) | Center of circle |

**Center** means half of cursor width/height. For 32x32, center is (16, 16).

### Design Guidelines

**Visibility:**
- Add 1-2px outline for contrast on any background
- White cursors work best with black outline
- Black cursors work best with white outline
- Test on both light and dark backgrounds

**Size:**
- Arrow should be 16-24px tall (on 32x32 canvas)
- Leave padding around edges (don't touch canvas edge)
- Larger cursors are easier to see but can feel clunky

**Style consistency:**
- Use same outline style for all cursors
- Match shadow/highlight direction across theme
- Keep color palette limited (2-3 colors max)

**Animations:**
- 8-12 frames minimum for smooth rotation
- 24-30 frames for very smooth (60 FPS style)
- Each frame: 16-100ms duration typical
- Full animation: 0.5-2 seconds total

### Quick Start: Creating a Simple Theme

1. **Start with arrow.png**
   - 32x32 canvas
   - Draw arrow shape (~20px tall)
   - Add black outline (1-2px)
   - Fill with white or your chosen color
   - Save as PNG with transparency

2. **Create hand.png**
   - Use arrow as base
   - Modify to pointing hand shape
   - Keep similar style (outline, colors)

3. **Create ibeam.png**
   - Draw vertical line (2px wide)
   - Add horizontal serifs at top/bottom
   - Center on 32x32 canvas

4. **Create wait.png (static or animated)**
   - Static: Hourglass or spinner circle
   - Animated: 12 frames of rotating shape
   - Center on canvas

5. **Create theme.ini**
   ```ini
   [Theme]
   Name=My First Theme
   [Settings]
   UseSystemCursorsAsFallback=true
   ```

6. **Test in Cycloside**
   - Place in `~/.local/share/Cycloside/Themes/Cursors/MyTheme/`
   - Load through Desktop Customization plugin

## Animation Tutorial: Rotating Spinner

### Simple 12-Frame Spinner

1. **Create base frame (frame 1):**
   - 32x32 canvas
   - Draw 3/4 circle arc (270°)
   - Stroke: 4px, blue color
   - Arc starts at 12 o'clock position

2. **Rotate for each frame:**
   - Frame 2: Rotate 30° clockwise
   - Frame 3: Rotate 60° clockwise
   - ...
   - Frame 12: Rotate 330° clockwise

3. **Export frames:**
   - Save as `wait_001.png` through `wait_012.png`
   - Ensure transparent background

4. **Test animation:**
   - Play through frames at 30 FPS
   - Adjust if too fast/slow

### Advanced: Gradient Fade Spinner

1. Apply alpha gradient along arc (tail=100%, head=0%)
2. Creates elegant "chasing tail" effect
3. Very smooth appearance

### Animation Tools

- **GIMP:** Use Filters > Animation > Rotate
- **Photoshop:** Timeline panel for frame animation
- **CSS/Canvas:** Generate frames programmatically
- **Blender:** 3D rotate, render 2D frames

## Installing Themes

### Method 1: Manual Install

Copy theme directory to:
```
Linux:   ~/.local/share/Cycloside/Themes/Cursors/
Windows: %APPDATA%/Cycloside/Themes/Cursors/
macOS:   ~/Library/Application Support/Cycloside/Themes/Cursors/
```

### Method 2: ZIP Package

1. Create ZIP with theme.ini and all PNG files at root
2. Name: `ThemeName.zip`
3. Import via Cycloside's Desktop Customization plugin

### Method 3: Import Via Code

```csharp
var manager = CursorThemeManager.Instance;
manager.ImportTheme("/path/to/theme");
```

## Using Themes

### Load Theme
```csharp
using Cycloside.Services;

// Load theme
var manager = CursorThemeManager.Instance;
manager.LoadTheme("path/to/theme/directory");

// Listen for changes
manager.ThemeChanged += (theme) => {
    Logger.Log($"Cursor theme changed: {theme?.Name}");
};
```

### Get Specific Cursor
```csharp
var arrowCursor = manager.GetCursor(CursorType.Arrow);
if (arrowCursor != null && arrowCursor.IsAnimated)
{
    Logger.Log($"Arrow has {arrowCursor.Frames.Count} frames");
}
```

### Unload Theme (Revert to System)
```csharp
manager.UnloadTheme();
```

## Troubleshooting

**Cursors not appearing:**
- Check file names match exactly (case-sensitive on Linux)
- Ensure PNGs have transparency (not white background)
- Verify theme.ini is valid

**Hotspot feels wrong:**
- Review [Hotspots] section in theme.ini
- Test by clicking on small targets
- Adjust coordinates and reload theme

**Animation too fast/slow:**
- Adjust `AnimationFrameRate` in theme.ini
- Or change frame duration per cursor
- Test at different speeds

**Cursors look pixelated:**
- Create cursors at higher resolution
- Use 48x48 or 64x64 canvas
- Enable anti-aliasing in image editor

**Cursors hard to see:**
- Add stronger outline (2-3px)
- Increase contrast
- Test on varied backgrounds

## Theme Examples

### Minimal 4-File Theme

**Files:**
- arrow.png
- hand.png
- ibeam.png
- wait.png
- theme.ini

**Use case:** Quick theme, essential cursors only

### Standard 10-File Theme

**Files:**
- 4 essential + appstarting, cross, no
- 3 resize cursors (NS, EW, All)
- theme.ini

**Use case:** Complete theme for most users

### Full 14-File Theme

**Files:**
- All standard cursors
- All resize directions
- Help and UpArrow
- theme.ini

**Use case:** Professional theme, full coverage

### Animated Theme

**Files:**
- arrow.png (static)
- hand.png (static)
- ibeam.png (static)
- wait_001.png through wait_012.png (animated, 12 frames)
- appstarting_001.png through appstarting_008.png (animated, 8 frames)
- Other cursors (static)
- theme.ini

**Use case:** Theme with smooth animations

## Tips for Best Results

1. **Start simple:** 4 cursors is enough to start
2. **Test frequently:** Load theme after each cursor
3. **Be consistent:** Use same style across all cursors
4. **Mind the hotspot:** Incorrect hotspots feel terrible
5. **Add outline:** Makes cursors visible anywhere
6. **Consider size:** Larger = easier to see, smaller = less intrusive
7. **Animation smoothness:** More frames = smoother (but more work)
8. **Test on content:** Try on text, links, resizable windows
9. **High DPI:** Consider 2x resolution for sharp rendering
10. **Document:** Add design notes to theme.ini

## Resources

### Inspiration
- **Windows XP/7 cursors:** Classic designs to reference
- **macOS cursors:** Clean, modern aesthetic
- **Web cursors:** Material Design, custom CSS cursors
- **Gaming cursors:** Often have creative animations

### Color Schemes
- **Modern Dark:** White with shadow (#FFFFFF + outline)
- **Classic XP:** White with blue accent (#FFFFFF + #0078D4)
- **Material:** Accent color (#2196F3, #E91E63, etc.)
- **Retro:** Green (#00FF00), amber (#FFBF00)

### Testing Checklist
- [ ] Arrow works on desktop background
- [ ] Hand appears on links/buttons
- [ ] I-Beam shows in text fields
- [ ] Wait animation is smooth
- [ ] Hotspots feel natural (click small targets)
- [ ] Visible on both light and dark backgrounds
- [ ] No white "box" around cursor (PNG transparency)
- [ ] Animation isn't too fast or too slow
- [ ] Resize cursors work in all directions
- [ ] Theme loads without errors

## Contributing

Want to share your cursor theme?

1. Ensure all required files are included
2. Test thoroughly on multiple backgrounds
3. Write clear Design Notes in theme.ini
4. Include example screenshots
5. Submit to Cycloside theme gallery

## License

Sample themes (Modern Dark, Classic XP, Animated Spinner) are provided as examples under the same license as Cycloside. You're free to modify, redistribute, or use as basis for your own themes.

Note: Windows XP is a trademark of Microsoft. Theme recreations are for educational/nostalgic purposes only.
