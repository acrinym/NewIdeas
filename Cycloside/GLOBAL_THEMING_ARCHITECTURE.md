# Global Theming Architecture (WindowBlinds-Style)

## Overview

This document describes Cycloside's WindowBlinds-style global theming system that applies custom window decorations and effects to ALL windows on the desktop, not just Cycloside windows.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Cycloside Application                     │
│  (WindowDecorationManager, CursorThemeManager, etc.)        │
└───────────────────┬─────────────────────────────────────────┘
                    │
                    │ Loads themes
                    ↓
┌─────────────────────────────────────────────────────────────┐
│              GlobalThemeService (C# Service)                 │
│  • Installs system hook                                      │
│  • Manages DLL injection                                     │
│  • Provides theme data to injected DLL                       │
│  • Handles configuration (whitelist/blacklist)              │
└───────────────────┬─────────────────────────────────────────┘
                    │
                    │ Installs hook
                    ↓
┌─────────────────────────────────────────────────────────────┐
│              Windows System Hook (WH_CALLWNDPROC)            │
│  • Monitors window creation                                  │
│  • Intercepts WM_NCCREATE, WM_NCPAINT messages              │
│  • Triggers DLL injection into target process               │
└───────────────────┬─────────────────────────────────────────┘
                    │
                    │ Injects
                    ↓
┌─────────────────────────────────────────────────────────────┐
│           CyclosideTheme.dll (Native C++ DLL)                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Hook Functions (Detours/IAT hooking)              │   │
│  │  • InterceptDrawThemeBackground()                   │   │
│  │  • InterceptDrawThemeText()                         │   │
│  │  • InterceptDrawThemeParentBackground()             │   │
│  │  • Redirect uxtheme.dll calls → Custom renderer     │   │
│  └────────────────────┬────────────────────────────────┘   │
│                       │                                      │
│                       ↓                                      │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Theme Renderer (C++)                                │   │
│  │  • Uses theme data from shared memory                │   │
│  │  • Renders custom title bars, borders, buttons      │   │
│  │  • Handles button clicks, window controls           │   │
│  │  • DirectX/GDI+ rendering                            │   │
│  └────────────────────┬────────────────────────────────┘   │
│                       │                                      │
│                       ↓                                      │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  DWM Integration                                     │   │
│  │  • DwmExtendFrameIntoClientArea()                    │   │
│  │  • Hardware-accelerated composition                  │   │
│  │  • Blur behind, glass effects                        │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                    │
                    │ Renders to
                    ↓
┌─────────────────────────────────────────────────────────────┐
│              Target Application Window                       │
│  (Firefox, VSCode, Terminal, etc.)                          │
│  • Sees our custom title bar instead of native              │
│  • Unaware of theming (transparent to app)                  │
└─────────────────────────────────────────────────────────────┘
```

---

## Component Breakdown

### 1. GlobalThemeService (C# - Cycloside)

**Location:** `Cycloside/Services/GlobalThemeService.cs`

**Responsibilities:**
- Install/uninstall system hook
- Manage CyclosideTheme.dll injection
- Maintain theme configuration in shared memory
- Handle whitelist/blacklist for applications
- Provide UI for enabling/disabling global theming
- Monitor system for new windows

**Key Methods:**
```csharp
public class GlobalThemeService
{
    public void EnableGlobalTheming();
    public void DisableGlobalTheming();
    public void SetThemeForApp(string appName, string themeName);
    public void AddToWhitelist(string appName);
    public void AddToBlacklist(string appName);
}
```

**Implementation:**
- Uses P/Invoke to install Windows hook (`SetWindowsHookEx`)
- Spawns background thread to monitor window creation
- Creates shared memory region for theme data
- Launches CyclosideThemeHost.exe (native helper process)

---

### 2. CyclosideThemeHost.exe (Native C++ - Host Process)

**Location:** `CyclosideThemeHost/` (new project)

**Responsibilities:**
- Runs with same privileges as Cycloside
- Installs the actual system hook (WH_CALLWNDPROC)
- Injects CyclosideTheme.dll into target processes
- Communicates with GlobalThemeService via IPC

**Why separate process:**
- C# can't easily create global hooks
- Native code more reliable for system hooks
- Isolation (if it crashes, doesn't take down Cycloside)

**Hook Installation:**
```cpp
HHOOK g_hook = SetWindowsHookEx(
    WH_CALLWNDPROC,
    HookProc,
    GetModuleHandle("CyclosideTheme.dll"),
    0  // 0 = global hook (all threads)
);
```

---

### 3. CyclosideTheme.dll (Native C++ - Injected DLL)

**Location:** `CyclosideTheme/` (new project)

**Responsibilities:**
- Injected into EVERY process on the system
- Hooks uxtheme.dll functions
- Intercepts window painting calls
- Renders custom theme using shared memory data
- Handles button clicks (close, minimize, maximize)

**Key Hooks:**

```cpp
// Hook uxtheme.dll functions
HRESULT WINAPI InterceptDrawThemeBackground(
    HTHEME hTheme,
    HDC hdc,
    int iPartId,
    int iStateId,
    LPCRECT pRect,
    LPCRECT pClipRect)
{
    // If this is a title bar part (WP_CAPTION, etc.)
    if (iPartId == WP_CAPTION || iPartId == WP_CLOSEBUTTON) {
        // Don't call original uxtheme function
        // Render our custom theme instead
        return RenderCustomTheme(hdc, iPartId, iStateId, pRect);
    }

    // Otherwise call original
    return RealDrawThemeBackground(hTheme, hdc, iPartId, iStateId, pRect, pClipRect);
}
```

**Hooking Methods:**
- **Microsoft Detours:** Industry-standard hooking library
- **IAT Hooking:** Modify Import Address Table
- **Inline Hooking:** Patch function prologue (more invasive)

**Rendering:**
- Load theme data from shared memory
- Use GDI+ or DirectX to render title bar
- Composite with DWM for hardware acceleration

---

### 4. Shared Memory Region

**Purpose:** Pass theme data from Cycloside to injected DLLs

**Structure:**
```cpp
struct ThemeData {
    // Theme metadata
    char themeName[256];
    char themeVersion[64];

    // Colors
    uint32_t titleBarActiveColor;
    uint32_t titleBarInactiveColor;
    uint32_t borderColor;

    // Dimensions
    int titleBarHeight;
    int borderWidth;
    int cornerRadius;

    // Bitmap data (title bar, borders, buttons)
    // Stored as raw BGRA pixel data
    uint32_t titleBarBitmapSize;
    uint8_t titleBarBitmapData[1024 * 1024]; // 1MB for bitmaps

    // Button positions
    RECT closeButtonRect;
    RECT maxButtonRect;
    RECT minButtonRect;

    // Configuration
    bool enableGlow;
    bool enableShadow;
    bool enableBlur;
};
```

**Creation (C# side):**
```csharp
var mmf = MemoryMappedFile.CreateOrOpen(
    "CyclosideThemeData",
    sizeof(ThemeData),
    MemoryMappedFileAccess.ReadWrite
);

var accessor = mmf.CreateViewAccessor();
accessor.Write(0, themeData); // Write theme data
```

**Reading (C++ side):**
```cpp
HANDLE hMapFile = OpenFileMapping(
    FILE_MAP_READ,
    FALSE,
    "CyclosideThemeData"
);

ThemeData* pTheme = (ThemeData*)MapViewOfFile(
    hMapFile,
    FILE_MAP_READ,
    0, 0, sizeof(ThemeData)
);
```

---

## Security Considerations

### Administrator Privileges

**Required for:**
- Installing global system hooks
- Injecting DLLs into other processes
- Writing to protected system directories

**Mitigation:**
- Only request admin when enabling global theming
- Clearly explain why privileges are needed
- Allow user-level operation (Cycloside windows only) without admin

### Antivirus Detection

**Risk:** DLL injection + system hooks = classic malware behavior

**Mitigation:**
- Code signing certificate (sign all binaries)
- Open source (reviewable code)
- Submit to antivirus vendors for whitelisting
- Clear documentation explaining behavior
- Sandbox mode (theme only Cycloside windows) as default

### Process Stability

**Risk:** Injected code can crash host process

**Mitigation:**
- Extensive exception handling
- Minimal code in injected DLL
- Blacklist critical processes (services, system processes)
- Disable injection for elevated processes by default
- Watchdog to restart CyclosideThemeHost if it crashes

---

## DWM Integration

### Modern Windows (Vista+)

**Key APIs:**
```cpp
// Extend frame into client area (for custom chrome)
MARGINS margins = {-1, -1, -1, -1}; // Entire window
DwmExtendFrameIntoClientArea(hwnd, &margins);

// Enable blur behind
DWM_BLURBEHIND bb = {0};
bb.dwFlags = DWM_BB_ENABLE | DWM_BB_BLURREGION;
bb.fEnable = TRUE;
bb.hRgnBlur = CreateRectRgn(0, 0, width, height);
DwmEnableBlurBehindWindow(hwnd, &bb);

// Get composition status
BOOL enabled = FALSE;
DwmIsCompositionEnabled(&enabled);
```

**Hardware Acceleration:**
- DWM uses GPU for composition
- Our themed windows get automatic GPU acceleration
- Use DirectX surfaces for best performance

**Transparency:**
- DWM supports per-pixel alpha
- Glass effects require DWM composition
- Fallback to solid colors if composition disabled

---

## Configuration & Whitelist/Blacklist

### Application Filtering

**Whitelist Mode:**
```json
{
  "globalTheming": {
    "mode": "whitelist",
    "applications": [
      "firefox.exe",
      "code.exe",
      "WindowsTerminal.exe"
    ]
  }
}
```

**Blacklist Mode:**
```json
{
  "globalTheming": {
    "mode": "blacklist",
    "applications": [
      "csgo.exe",         // Games
      "explorer.exe",     // System
      "dwm.exe",          // DWM itself
      "services.exe"      // Critical services
    ]
  }
}
```

**Per-App Themes:**
```json
{
  "perAppThemes": {
    "firefox.exe": "AeroGlass",
    "code.exe": "ModernDark",
    "cmd.exe": "ClassicXP"
  }
}
```

---

## Performance Optimization

### Caching

- Cache rendered title bars (don't redraw every frame)
- Invalidate cache only on theme change or window resize
- Use DWM thumbnail API for window previews

### Selective Hooking

- Only hook visible windows
- Skip minimized/hidden windows
- Disable for fullscreen applications

### Lazy Loading

- Don't inject DLL immediately on window creation
- Wait for first paint message
- Unload DLL when window closes

---

## Fallback Mechanisms

### DWM Disabled

- Fall back to GDI rendering
- Disable transparency/blur effects
- Use solid colors instead of gradients

### Incompatible Applications

- Auto-detect problematic apps
- Add to blacklist automatically
- Log issues for user review

### Hook Failure

- Graceful degradation (theme only Cycloside windows)
- Show notification to user
- Provide troubleshooting steps

---

## Installation & Deployment

### Files Required

```
Cycloside/
├── Cycloside.exe                    # Main application
├── CyclosideThemeHost.exe           # Native hook host (C++)
├── CyclosideTheme.dll               # Injected theme DLL (C++)
├── Microsoft.Detours.dll            # Hooking library
└── Themes/
    ├── WindowDecorations/           # Theme files
    ├── Cursors/
    └── Audio/
```

### Registry Keys

```reg
; Enable global theming
HKEY_CURRENT_USER\Software\Cycloside
  "GlobalThemingEnabled" = DWORD:1
  "CurrentTheme" = "AeroGlass"
  "ThemingMode" = "whitelist"  ; or "blacklist" or "all"
```

### Installation Steps

1. Install Cycloside (user mode)
2. User enables "Global Theming" in settings
3. UAC prompt: Request administrator privileges
4. Install CyclosideThemeHost.exe as service (optional)
5. Register system hook
6. Apply themes to running applications

---

## Development Phases

### Phase 1: Proof of Concept ✅ (Current)
- Architecture design (this document)
- Basic hook installation
- Single window interception
- Simple theme rendering

### Phase 2: Core Implementation
- Complete DLL injection system
- Full uxtheme.dll hooking
- Theme data via shared memory
- Handle button clicks

### Phase 3: DWM Integration
- Hardware-accelerated rendering
- Glass effects
- Blur behind
- Performance optimization

### Phase 4: Polish & Security
- Code signing
- Antivirus whitelisting
- Extensive testing
- Crash recovery

### Phase 5: Distribution
- Installer package
- Documentation
- Video tutorials
- Community theme gallery

---

## Testing Strategy

### Unit Tests
- Hook installation/removal
- DLL injection mechanism
- Shared memory communication
- Theme data serialization

### Integration Tests
- Theme real applications (Firefox, VSCode, etc.)
- Multi-monitor scenarios
- High DPI displays
- Different Windows versions (7, 10, 11)

### Stress Tests
- 100+ windows open
- Rapid window creation/destruction
- Theme switching under load
- Memory leak detection

### Compatibility Tests
- Different Windows versions
- Various applications
- Antivirus software
- Virtualized environments

---

## Known Limitations

### Windows 11

- System apps (Settings, Store) may resist theming
- Snap layouts might conflict with custom chrome
- Rounded corners handled by system, not us

### UWP Apps

- May require different hooking approach
- Sandboxed environment complicates injection
- Consider per-app compatibility

### Performance

- Overhead: ~5-10MB per themed window
- CPU: <1% for static themes, ~2-5% for animated
- GPU: Depends on theme complexity

---

## Future Enhancements

### Linux Support

- X11 compositor integration
- Wayland protocol extensions
- KWin/Mutter plugin system

### macOS Support

- System Integrity Protection challenges
- Objective-C runtime hooking
- Accessibility API approach

### Advanced Features

- Animated title bars (weather, system monitor)
- Custom title bar widgets
- Per-window effects (wobbly, explode, etc.)
- Theme marketplace
- AI-generated themes

---

## References

- [WindowBlinds Technical Overview](https://www.stardock.com/products/windowblinds/)
- [Microsoft Detours Documentation](https://github.com/microsoft/Detours)
- [DWM API Reference](https://learn.microsoft.com/en-us/windows/win32/dwm/dwm-overview)
- [Windows Hooks Overview](https://learn.microsoft.com/en-us/windows/win32/winmsg/hooks)

---

## License & Legal

- Cycloside: Your chosen license
- Microsoft Detours: MIT License (free for open source)
- uxtheme.dll: Microsoft Windows API (legal to intercept)
- Code signing certificate: Required for production (~$200/year)

**Legal Note:** Intercepting system APIs is legal for legitimate purposes (theming, accessibility, etc.). Ensure compliance with Windows terms of service and application EULAs.
