# CyclosideTheme.dll - Injected Theme Engine

## Purpose

CyclosideTheme.dll is the injected DLL that performs the actual window theming. It:
1. Gets injected into every themed process
2. Hooks uxtheme.dll functions to intercept window painting
3. Renders custom title bars, borders, and buttons
4. Handles user interactions (button clicks)
5. Integrates with DWM for hardware acceleration

## Injection Flow

```
1. Target App launches (e.g., Firefox)
2. CyclosideThemeHost detects new window
3. Host injects CyclosideTheme.dll into Firefox process
4. DLL's DllMain() executes
5. DLL installs hooks for uxtheme.dll
6. Firefox tries to paint title bar
7. Our hook intercepts the call
8. We render custom theme instead
```

## Core Responsibilities

### 1. API Hooking

Hook these uxtheme.dll functions:
- `DrawThemeBackground` - Title bar, borders, buttons
- `DrawThemeText` - Window title text
- `DrawThemeParentBackground` - Background
- `GetThemeBackgroundExtent` - Size calculations
- `GetThemePartSize` - Button sizes

### 2. Custom Rendering

When hooked functions are called:
```cpp
HRESULT WINAPI InterceptDrawThemeBackground(
    HTHEME hTheme,
    HDC hdc,
    int iPartId,
    int iStateId,
    LPCRECT pRect,
    LPCRECT pClipRect)
{
    // Check if this is a title bar element
    if (IsTitleBarPart(iPartId))
    {
        // Render our custom theme
        return RenderCustomTitleBar(hdc, iPartId, iStateId, pRect);
    }

    // Otherwise, call original function
    return RealDrawThemeBackground(hTheme, hdc, iPartId, iStateId, pRect, pClipRect);
}
```

### 3. Theme Data Management

Load theme data from shared memory:
```cpp
struct ThemeData
{
    // Bitmaps (stored as raw BGRA pixels)
    DWORD titleBarWidth, titleBarHeight;
    BYTE* titleBarBitmap;

    DWORD closeButtonWidth, closeButtonHeight;
    BYTE* closeButtonNormal;
    BYTE* closeButtonHover;
    BYTE* closeButtonPressed;

    // Colors
    COLORREF titleBarActiveColor;
    COLORREF titleBarInactiveColor;
    COLORREF borderColor;

    // Dimensions
    int titleBarHeight;
    int borderWidth;
    int cornerRadius;

    // Configuration
    BOOL enableGlass;
    BOOL enableGlow;
    BOOL enableShadow;
};
```

### 4. User Input Handling

Intercept mouse events on custom chrome:
```cpp
LRESULT CALLBACK SubclassProc(HWND hwnd, UINT msg,
                              WPARAM wParam, LPARAM lParam,
                              UINT_PTR subclassId, DWORD_PTR refData)
{
    switch (msg)
    {
        case WM_NCLBUTTONDOWN:
        {
            POINT pt = {GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam)};

            // Check if clicking close button
            if (IsPointInCloseButton(hwnd, pt))
            {
                PostMessage(hwnd, WM_CLOSE, 0, 0);
                return 0;
            }

            // Check if clicking maximize button
            if (IsPointInMaxButton(hwnd, pt))
            {
                ShowWindow(hwnd, IsMaximized(hwnd) ? SW_RESTORE : SW_MAXIMIZE);
                return 0;
            }

            // Check if clicking minimize button
            if (IsPointInMinButton(hwnd, pt))
            {
                ShowWindow(hwnd, SW_MINIMIZE);
                return 0;
            }

            break;
        }

        case WM_NCMOUSEMOVE:
        {
            // Update button hover states
            UpdateButtonHoverStates(hwnd, lParam);
            break;
        }
    }

    return DefSubclassProc(hwnd, msg, wParam, lParam);
}
```

### 5. DWM Integration

For hardware-accelerated glass effects:
```cpp
void EnableGlassFrame(HWND hwnd)
{
    // Extend frame into client area
    MARGINS margins = {0, 0, 0, titleBarHeight};
    DwmExtendFrameIntoClientArea(hwnd, &margins);

    // Enable blur behind
    DWM_BLURBEHIND bb = {0};
    bb.dwFlags = DWM_BB_ENABLE | DWM_BB_BLURREGION;
    bb.fEnable = TRUE;
    bb.hRgnBlur = CreateRectRgn(0, 0, width, titleBarHeight);
    DwmEnableBlurBehindWindow(hwnd, &bb);
}
```

## Implementation Details

### DllMain

```cpp
BOOL APIENTRY DllMain(HMODULE hModule,
                      DWORD  ul_reason_for_call,
                      LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);
        g_hModule = hModule;

        // Initialize
        InitializeSharedMemory();
        LoadThemeData();
        InstallHooks();
        break;

    case DLL_PROCESS_DETACH:
        // Cleanup
        UninstallHooks();
        CleanupSharedMemory();
        break;
    }
    return TRUE;
}
```

### Hook Installation (Microsoft Detours)

```cpp
#include <detours.h>

// Original function pointers
static auto RealDrawThemeBackground = DrawThemeBackground;
static auto RealDrawThemeText = DrawThemeText;

void InstallHooks()
{
    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());

    // Hook uxtheme functions
    DetourAttach(&(PVOID&)RealDrawThemeBackground, InterceptDrawThemeBackground);
    DetourAttach(&(PVOID&)RealDrawThemeText, InterceptDrawThemeText);

    LONG error = DetourTransactionCommit();
    if (error == NO_ERROR)
    {
        Log(L"Hooks installed successfully");
    }
    else
    {
        Log(L"Failed to install hooks: %d", error);
    }
}

void UninstallHooks()
{
    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());

    DetourDetach(&(PVOID&)RealDrawThemeBackground, InterceptDrawThemeBackground);
    DetourDetach(&(PVOID&)RealDrawThemeText, InterceptDrawThemeText);

    DetourTransactionCommit();
}
```

### Custom Title Bar Rendering

```cpp
HRESULT RenderCustomTitleBar(HDC hdc, int iPartId, int iStateId, LPCRECT pRect)
{
    // Get theme data
    ThemeData* theme = GetThemeData();
    if (!theme)
        return E_FAIL;

    // Create compatible DC for double-buffering
    HDC hdcMem = CreateCompatibleDC(hdc);
    HBITMAP hbmMem = CreateCompatibleBitmap(hdc,
                                             pRect->right - pRect->left,
                                             pRect->bottom - pRect->top);
    HBITMAP hbmOld = (HBITMAP)SelectObject(hdcMem, hbmMem);

    // Render based on part ID
    switch (iPartId)
    {
        case WP_CAPTION:  // Title bar background
            RenderTitleBarBackground(hdcMem, pRect, iStateId);
            break;

        case WP_CLOSEBUTTON:  // Close button
            RenderCloseButton(hdcMem, pRect, iStateId);
            break;

        case WP_MAXBUTTON:  // Maximize button
            RenderMaxButton(hdcMem, pRect, iStateId);
            break;

        case WP_MINBUTTON:  // Minimize button
            RenderMinButton(hdcMem, pRect, iStateId);
            break;
    }

    // Blit to screen
    BitBlt(hdc, pRect->left, pRect->top,
           pRect->right - pRect->left,
           pRect->bottom - pRect->top,
           hdcMem, 0, 0, SRCCOPY);

    // Cleanup
    SelectObject(hdcMem, hbmOld);
    DeleteObject(hbmMem);
    DeleteDC(hdcMem);

    return S_OK;
}

void RenderTitleBarBackground(HDC hdc, LPCRECT pRect, int iStateId)
{
    ThemeData* theme = GetThemeData();

    // Determine if window is active
    BOOL isActive = (iStateId == CS_ACTIVE);

    // Draw background
    HBRUSH hBrush = CreateSolidBrush(isActive ?
                                      theme->titleBarActiveColor :
                                      theme->titleBarInactiveColor);
    FillRect(hdc, pRect, hBrush);
    DeleteObject(hBrush);

    // Draw bitmap if available
    if (theme->titleBarBitmap)
    {
        // Create bitmap
        HBITMAP hbm = CreateBitmapFromData(theme->titleBarBitmap,
                                            theme->titleBarWidth,
                                            theme->titleBarHeight);

        // Stretch blit to fit
        HDC hdcMem = CreateCompatibleDC(hdc);
        HBITMAP hbmOld = (HBITMAP)SelectObject(hdcMem, hbm);

        StretchBlt(hdc, pRect->left, pRect->top,
                   pRect->right - pRect->left,
                   pRect->bottom - pRect->top,
                   hdcMem, 0, 0,
                   theme->titleBarWidth,
                   theme->titleBarHeight,
                   SRCCOPY);

        SelectObject(hdcMem, hbmOld);
        DeleteObject(hbm);
        DeleteDC(hdcMem);
    }

    // Add glow effect if enabled
    if (theme->enableGlow && isActive)
    {
        // Draw subtle glow at top
        RECT glowRect = *pRect;
        glowRect.bottom = glowRect.top + 3;

        // Create gradient brush
        TRIVERTEX vertex[2];
        vertex[0].x = glowRect.left;
        vertex[0].y = glowRect.top;
        vertex[0].Red = 0xFFFF;
        vertex[0].Green = 0xFFFF;
        vertex[0].Blue = 0xFFFF;
        vertex[0].Alpha = 0x8000;

        vertex[1].x = glowRect.right;
        vertex[1].y = glowRect.bottom;
        vertex[1].Red = 0xFFFF;
        vertex[1].Green = 0xFFFF;
        vertex[1].Blue = 0xFFFF;
        vertex[1].Alpha = 0x0000;

        GRADIENT_RECT gRect = {0, 1};
        GradientFill(hdc, vertex, 2, &gRect, 1, GRADIENT_FILL_RECT_V);
    }
}
```

### Button Click Handling

```cpp
void SubclassWindow(HWND hwnd)
{
    // Subclass window to intercept messages
    SetWindowSubclass(hwnd, SubclassProc, 0, 0);
}

LRESULT CALLBACK SubclassProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam,
                              UINT_PTR subclassId, DWORD_PTR refData)
{
    switch (msg)
    {
        case WM_NCHITTEST:
        {
            // Hit test for custom chrome
            POINT pt = {GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam)};
            ScreenToClient(hwnd, &pt);

            // Check button rects
            if (PtInRect(&GetCloseButtonRect(hwnd), pt))
                return HTCLOSE;
            if (PtInRect(&GetMaxButtonRect(hwnd), pt))
                return HTMAXBUTTON;
            if (PtInRect(&GetMinButtonRect(hwnd), pt))
                return HTMINBUTTON;

            // Check if in title bar (for dragging)
            if (pt.y < GetTitleBarHeight())
                return HTCAPTION;

            break;
        }

        case WM_NCMOUSEMOVE:
        {
            // Track mouse for hover effects
            UpdateButtonStates(hwnd, lParam);
            InvalidateRect(hwnd, NULL, FALSE);
            break;
        }
    }

    return DefSubclassProc(hwnd, msg, wParam, lParam);
}
```

## Performance Optimization

### Caching

```cpp
// Cache rendered title bars
struct CachedTitleBar
{
    HWND hwnd;
    int width;
    int height;
    BOOL isActive;
    HBITMAP cachedBitmap;
};

std::map<HWND, CachedTitleBar> g_cache;

HBITMAP GetCachedTitleBar(HWND hwnd, int width, int height, BOOL isActive)
{
    auto it = g_cache.find(hwnd);
    if (it != g_cache.end())
    {
        auto& cached = it->second;
        if (cached.width == width &&
            cached.height == height &&
            cached.isActive == isActive)
        {
            return cached.cachedBitmap;
        }
    }

    // Not cached, render new
    HBITMAP hbm = RenderTitleBarToBitmap(width, height, isActive);

    // Update cache
    g_cache[hwnd] = {hwnd, width, height, isActive, hbm};

    return hbm;
}
```

### Lazy Initialization

```cpp
// Don't hook immediately on DLL_PROCESS_ATTACH
// Wait for first relevant window message

BOOL g_initialized = FALSE;

void EnsureInitialized()
{
    if (!g_initialized)
    {
        LoadThemeData();
        InstallHooks();
        g_initialized = TRUE;
    }
}

HRESULT WINAPI InterceptDrawThemeBackground(...)
{
    EnsureInitialized();  // Initialize on first use

    // ... rest of function
}
```

## Debugging

### Debug Output

```cpp
void DebugLog(const wchar_t* format, ...)
{
#ifdef _DEBUG
    wchar_t buffer[1024];
    va_list args;
    va_start(args, format);
    vswprintf_s(buffer, format, args);
    va_end(args);

    OutputDebugString(L"[CyclosideTheme] ");
    OutputDebugString(buffer);
    OutputDebugString(L"\n");
#endif
}
```

### Attach Debugger to Target Process

1. Launch target app (e.g., Notepad)
2. Trigger DLL injection
3. In Visual Studio: Debug → Attach to Process
4. Select Notepad.exe
5. Load symbols for CyclosideTheme.dll
6. Set breakpoints
7. Resize/move window to trigger painting

## Error Handling

```cpp
HRESULT WINAPI InterceptDrawThemeBackground(...)
{
    __try
    {
        // Theming logic
    }
    __except(EXCEPTION_EXECUTE_HANDLER)
    {
        // Log exception but don't crash host app
        DebugLog(L"Exception in InterceptDrawThemeBackground: 0x%X",
                 GetExceptionCode());

        // Fall back to original function
        return RealDrawThemeBackground(hTheme, hdc, iPartId, iStateId, pRect, pClipRect);
    }

    return S_OK;
}
```

## Building

### Visual Studio

Project settings:
- **Configuration Type:** Dynamic Library (.dll)
- **Platform:** x64 (must match target processes)
- **C++ Standard:** C++17 or later
- **Character Set:** Unicode
- **Additional Dependencies:** detours.lib, gdi32.lib, user32.lib, dwmapi.lib, uxtheme.lib

### CMake

```cmake
add_library(CyclosideTheme SHARED
    dllmain.cpp
    hooks.cpp
    renderer.cpp
    shared_memory.cpp
)

target_link_libraries(CyclosideTheme
    detours
    gdi32
    user32
    dwmapi
    uxtheme
)
```

## File Structure

```
CyclosideTheme/
├── dllmain.cpp           # DLL entry point
├── hooks.cpp             # API hooking
├── hooks.h
├── renderer.cpp          # Custom rendering
├── renderer.h
├── shared_memory.cpp     # Theme data loading
├── shared_memory.h
├── dwm_integration.cpp   # DWM/glass effects
├── dwm_integration.h
├── input_handling.cpp    # Mouse/keyboard events
├── input_handling.h
├── cache.cpp             # Performance caching
├── cache.h
├── utils.cpp             # Helpers
├── utils.h
└── CMakeLists.txt
```

## Testing

### Test Applications

Start with simple apps:
1. **Notepad** - Simple, safe
2. **Calculator** - Slightly more complex
3. **Paint** - Has toolbar/ribbon
4. **Firefox** - Complex, real-world
5. **VSCode** - Electron app (different rendering)

### Test Scenarios

- [ ] Title bar renders correctly
- [ ] Buttons (close, min, max) work
- [ ] Window dragging works
- [ ] Window resizing works
- [ ] Active/inactive states
- [ ] High DPI displays (150%, 200%)
- [ ] Multi-monitor setup
- [ ] Minimized windows
- [ ] Maximized windows
- [ ] Fullscreen apps (should disable)

## Dependencies

- **Microsoft Detours** (MIT License)
- **Windows GDI32** (graphics)
- **User32** (window management)
- **UxTheme** (what we intercept)
- **DWMapi** (composition)

## Next Steps

1. Implement DllMain
2. Add hook installation (Detours)
3. Implement basic title bar rendering
4. Add button click handling
5. Test with Notepad
6. Add DWM integration
7. Optimize performance
8. Test with complex apps

## References

- [Microsoft Detours Tutorial](https://github.com/microsoft/Detours/wiki/Using-Detours)
- [UxTheme API](https://learn.microsoft.com/en-us/windows/win32/controls/themesfileformat-overview)
- [DWM Composition](https://learn.microsoft.com/en-us/windows/win32/dwm/dwm-overview)
- [Window Subclassing](https://learn.microsoft.com/en-us/windows/win32/controls/subclassing-overview)
