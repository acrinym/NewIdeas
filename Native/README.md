# Native Components for Global Theming

This directory contains the native (C++) components required for WindowBlinds-style global theming.

## Overview

Cycloside's global theming requires two native components:

1. **CyclosideThemeHost.exe** - Native host process that installs system hooks
2. **CyclosideTheme.dll** - Injected DLL that performs the actual theming

## Why Native Code?

C# (.NET) cannot easily:
- Install global system hooks (WH_CALLWNDPROC)
- Inject DLLs into other processes reliably
- Hook low-level Windows APIs (uxtheme.dll)
- Achieve the performance needed for theming

Native C++ is required for these low-level operations.

## Build Requirements

### Tools

- **Visual Studio 2022** (or later) with C++ Desktop Development workload
- **Windows SDK** 10.0.19041.0 or later
- **CMake** 3.20+ (optional, for cross-platform builds)

### Libraries

- **Microsoft Detours** (MIT License) - For API hooking
  - Download: https://github.com/microsoft/Detours
  - Install: `vcpkg install detours`

- **Windows API** (included with Windows SDK)
  - User32.dll
  - Gdi32.dll
  - Dwmapi.dll
  - UxTheme.dll (this is what we intercept)

## Project Structure

```
Native/
├── README.md                    # This file
├── CyclosideThemeHost/          # Host process project
│   ├── CMakeLists.txt
│   ├── main.cpp
│   ├── hook.cpp
│   ├── injection.cpp
│   └── README.md
├── CyclosideTheme/              # Injected DLL project
│   ├── CMakeLists.txt
│   ├── dllmain.cpp
│   ├── hooks.cpp
│   ├── renderer.cpp
│   ├── shared_memory.cpp
│   └── README.md
└── Common/                      # Shared code
    ├── theme_data.h             # Theme data structure
    └── ipc.h                    # IPC helpers
```

## Build Instructions

### Using Visual Studio

1. Open `Native.sln` in Visual Studio
2. Set build configuration to **Release** | **x64**
3. Build Solution (Ctrl+Shift+B)
4. Output files will be in `Native/bin/Release/`

### Using CMake (Command Line)

```bash
cd Native
mkdir build
cd build
cmake ..
cmake --build . --config Release
```

### Output Files

After building:
- `CyclosideThemeHost.exe` - Copy to `Cycloside/` directory
- `CyclosideTheme.dll` - Copy to `Cycloside/` directory

## Development Workflow

1. **Modify C++ code** in Native/ directory
2. **Build** using Visual Studio or CMake
3. **Copy binaries** to Cycloside directory
4. **Run Cycloside** and test global theming

## Debugging

### Debugging the Host Process

1. In Visual Studio: Debug → Attach to Process
2. Find `CyclosideThemeHost.exe`
3. Set breakpoints in host code
4. Trigger hook installation from Cycloside

### Debugging the Injected DLL

1. Find target process (e.g., Firefox)
2. Attach debugger to target process
3. Load symbols for `CyclosideTheme.dll`
4. Set breakpoints in DLL code
5. Trigger window painting (resize, move, etc.)

**Tip:** Use `OutputDebugString()` in C++ code, visible in DebugView or Visual Studio Output window.

## Security Notes

### Code Signing

For production use, **code signing is required**:
- Windows Defender/SmartScreen will block unsigned DLLs
- Users will see scary warnings
- Some antivirus will quarantine unsigned injected code

**Get a code signing certificate:**
- DigiCert, Sectigo, etc. (~$200/year)
- Sign both CyclosideThemeHost.exe and CyclosideTheme.dll

### Antivirus Whitelisting

Submit to antivirus vendors:
- Microsoft Defender
- Norton
- McAfee
- Avast
- Kaspersky

Provide:
- Source code (open source helps)
- Detailed explanation of purpose
- Code signing certificate info

## Testing Strategy

### Unit Tests

Create a test project that:
- Verifies hook installation/removal
- Tests DLL injection mechanism
- Validates shared memory communication

### Integration Tests

Test with real applications:
- Notepad (simple, safe)
- Firefox (complex, common)
- Visual Studio Code (Electron app)
- Windows Terminal (modern UWP-style)

### Compatibility Tests

Test on different Windows versions:
- Windows 7 (if still supporting)
- Windows 10 (21H2, 22H2)
- Windows 11 (21H2, 22H2, 23H2)

Test with different DPI settings:
- 100% (standard)
- 125%, 150%, 175% (common)
- 200%+ (high DPI displays)

## Troubleshooting

### Hook Not Installing

**Symptoms:** GlobalThemeService enables but nothing happens

**Fixes:**
- Check if running as administrator
- Verify CyclosideThemeHost.exe exists
- Check Windows Event Viewer for errors
- Run `CyclosideThemeHost.exe` manually to see errors

### DLL Not Injecting

**Symptoms:** Hook installs but windows don't theme

**Fixes:**
- Check if DLL is 64-bit (must match target process)
- Verify DLL exists in Cycloside directory
- Check Process Explorer to see if DLL is loaded
- Review blacklist (app might be excluded)

### Crashes

**Symptoms:** Target application crashes after theming

**Fixes:**
- Add application to blacklist
- Check exception handling in DLL
- Use debugger to find crash location
- Review compatibility with app

### Performance Issues

**Symptoms:** System sluggish, high CPU usage

**Fixes:**
- Reduce theme complexity (smaller bitmaps)
- Enable caching in renderer
- Profile with Windows Performance Analyzer
- Consider disabling for specific apps

## Platform Support

### Windows

- ✅ **Windows 10/11** - Primary target
- ⚠️ **Windows 7/8** - Possible but not tested
- ❌ **Windows XP** - Not supported (use modern APIs)

### Linux

- ❌ **Not currently supported**
- **Future:** X11 compositor integration
- **Future:** Wayland protocol extensions

### macOS

- ❌ **Not currently supported**
- **Future:** Accessibility API approach
- **Challenges:** System Integrity Protection

## Performance Benchmarks

Target performance metrics:

- **Hook overhead:** <1ms per window creation
- **Injection time:** <10ms per process
- **Render time:** <5ms per title bar paint
- **Memory:** <10MB per themed window
- **CPU:** <1% idle, <5% during window operations

## Contributing

When contributing to native code:

1. Follow C++ coding standards
2. Add comments explaining Windows API calls
3. Handle all exceptions (don't crash host app!)
4. Test on multiple Windows versions
5. Profile performance impact
6. Update documentation

## License

Same license as Cycloside main project.

**Dependencies:**
- Microsoft Detours: MIT License
- Windows API: Microsoft license (legal to use)

## References

- [Microsoft Detours Documentation](https://github.com/microsoft/Detours/wiki)
- [Windows Hooks Reference](https://learn.microsoft.com/en-us/windows/win32/winmsg/hooks)
- [DLL Injection Techniques](https://www.codeproject.com/Articles/4610/Three-Ways-to-Inject-Your-Code-into-Another-Proces)
- [DWM API Reference](https://learn.microsoft.com/en-us/windows/win32/dwm/dwm-overview)
- [UxTheme API](https://learn.microsoft.com/en-us/windows/win32/controls/themesfileformat-overview)

## Support

- **Issues:** File issues on Cycloside GitHub
- **Questions:** Cycloside discussions forum
- **Security:** security@cycloside.project (for vulnerability reports)

---

**Note:** These native components are essential for global theming but optional for Cycloside's core functionality. Cycloside works fine without them (themes only Cycloside windows).
