# CyclosideThemeHost - Native Hook Host

## Purpose

CyclosideThemeHost.exe is a native C++ executable that:
1. Installs a global Windows system hook (WH_CALLWNDPROC)
2. Monitors window creation and painting events system-wide
3. Injects CyclosideTheme.dll into target processes
4. Communicates with Cycloside via IPC (shared memory)

## Why This Exists

C# can install hooks, but they don't work reliably for global system hooks because:
- The hook DLL must be native (not managed .NET)
- C# can't easily inject into other processes
- Performance requirements demand native code

## Architecture

```
Cycloside (C#)
     ↓ (launches)
CyclosideThemeHost.exe (C++ Native)
     ↓ (installs hook)
Windows System Hook
     ↓ (injects DLL)
CyclosideTheme.dll → Target Process
```

## Responsibilities

### 1. Hook Installation

```cpp
// Install WH_CALLWNDPROC hook
HHOOK g_hook = SetWindowsHookEx(
    WH_CALLWNDPROC,          // Hook type
    HookProc,                 // Callback function
    g_dllModule,              // Module containing callback
    0                         // 0 = all threads (global)
);
```

### 2. Window Event Monitoring

Monitor these messages:
- `WM_CREATE` - New window created
- `WM_NCCREATE` - Non-client area created (title bar)
- `WM_NCPAINT` - Non-client area painting
- `WM_ACTIVATE` - Window activation/deactivation

### 3. Process Injection

When a themed window is created:
```cpp
bool InjectDLL(DWORD processId, const wchar_t* dllPath)
{
    // 1. Open target process
    HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, processId);

    // 2. Allocate memory in target process
    LPVOID pRemotePath = VirtualAllocEx(hProcess, NULL, pathSize,
                                         MEM_COMMIT, PAGE_READWRITE);

    // 3. Write DLL path to target memory
    WriteProcessMemory(hProcess, pRemotePath, dllPath, pathSize, NULL);

    // 4. Create remote thread to call LoadLibrary
    HANDLE hThread = CreateRemoteThread(hProcess, NULL, 0,
                                         (LPTHREAD_START_ROUTINE)LoadLibraryW,
                                         pRemotePath, 0, NULL);

    // 5. Wait for injection to complete
    WaitForSingleObject(hThread, INFINITE);

    // 6. Cleanup
    VirtualFreeEx(hProcess, pRemotePath, 0, MEM_RELEASE);
    CloseHandle(hThread);
    CloseHandle(hProcess);

    return true;
}
```

### 4. Configuration Management

Read theming configuration from shared memory:
- Which processes to theme (whitelist/blacklist)
- Which theme to apply per app
- Global enable/disable state

### 5. IPC Communication

Communicate with Cycloside C# process:
- Read commands from shared memory
- Write status updates
- Signal events (new window themed, errors, etc.)

## Implementation Details

### Main Loop

```cpp
int main()
{
    // 1. Initialize
    InitializeSharedMemory();
    LoadConfiguration();

    // 2. Install hook
    InstallSystemHook();

    // 3. Message loop (keep process alive)
    MSG msg;
    while (GetMessage(&msg, NULL, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    // 4. Cleanup
    UninstallSystemHook();
    CleanupSharedMemory();

    return 0;
}
```

### Hook Callback

```cpp
LRESULT CALLBACK HookProc(int nCode, WPARAM wParam, LPARAM lParam)
{
    if (nCode < 0)
        return CallNextHookEx(NULL, nCode, wParam, lParam);

    CWPSTRUCT* cwp = (CWPSTRUCT*)lParam;

    // Handle specific messages
    switch (cwp->message)
    {
        case WM_NCCREATE:
            OnWindowCreated(cwp->hwnd);
            break;

        case WM_NCPAINT:
            OnWindowPaint(cwp->hwnd);
            break;

        case WM_ACTIVATE:
            OnWindowActivate(cwp->hwnd, wParam);
            break;
    }

    return CallNextHookEx(NULL, nCode, wParam, lParam);
}

void OnWindowCreated(HWND hwnd)
{
    // Get process ID
    DWORD processId = 0;
    GetWindowThreadProcessId(hwnd, &processId);

    // Get process name
    char processName[MAX_PATH];
    GetProcessName(processId, processName);

    // Check if should theme
    if (ShouldThemeProcess(processName))
    {
        // Inject DLL
        InjectDLL(processId, L"CyclosideTheme.dll");

        // Log event
        LogThemeEvent(processName, hwnd);
    }
}
```

### Shared Memory Structure

```cpp
struct SharedConfig
{
    // Magic number to verify valid data
    DWORD magic;  // 0xDECADE

    // Theming mode (0=All, 1=Whitelist, 2=Blacklist)
    DWORD mode;

    // Process lists
    DWORD whitelistCount;
    DWORD blacklistCount;
    char whitelist[256][MAX_PATH];
    char blacklist[256][MAX_PATH];

    // Per-app themes
    DWORD perAppThemeCount;
    struct {
        char processName[MAX_PATH];
        char themeName[256];
    } perAppThemes[256];

    // Global enable/disable
    BOOL enabled;

    // Status (written by host)
    DWORD hooked ProcessCount;
    DWORD errorCount;
    char lastError[512];
};
```

## Building

### Visual Studio

1. Open `CyclosideThemeHost.vcxproj`
2. Select **Release** | **x64**
3. Build → Build Solution
4. Output: `bin/Release/CyclosideThemeHost.exe`

### CMake

```bash
cd Native/CyclosideThemeHost
mkdir build && cd build
cmake ..
cmake --build . --config Release
```

## Testing

### Manual Testing

```cmd
# Run host manually (as administrator)
CyclosideThemeHost.exe

# In another window, create test window
notepad.exe

# Host should inject DLL into notepad
# Check Process Explorer to verify DLL is loaded
```

### Debugging

1. Launch CyclosideThemeHost.exe
2. Attach Visual Studio debugger
3. Set breakpoints in hook callback
4. Create/move windows to trigger hooks
5. Step through code

**Debugging Tips:**
- Use `OutputDebugString()` for logging
- View output in DebugView or VS Output window
- Check Windows Event Viewer for errors
- Use Process Monitor to see DLL injection

## Error Handling

### Common Errors

**"Access Denied" when injecting:**
- Target process may be elevated (UAC)
- Run host as administrator
- Or add to blacklist (don't theme elevated apps)

**"DLL not found":**
- Verify CyclosideTheme.dll exists in same directory
- Check path in injection code
- Use absolute path instead of relative

**Hook doesn't install:**
- Check return value of SetWindowsHookEx
- Verify DLL exists and is accessible
- Check that DLL exports are correct

**High CPU usage:**
- Hook callback might be too slow
- Optimize callback (early returns)
- Add throttling for rapid messages

## Performance Considerations

### Hook Overhead

The hook callback runs for EVERY window message on the system!

**Optimization strategies:**
- Early return for uninteresting messages
- Cache configuration (don't read shared memory every call)
- Inject DLL only once per process
- Use thread-local storage for per-thread data

### Example Optimized Callback

```cpp
LRESULT CALLBACK HookProc(int nCode, WPARAM wParam, LPARAM lParam)
{
    // Fast path: skip if not our message
    if (nCode < 0)
        return CallNextHookEx(NULL, nCode, wParam, lParam);

    CWPSTRUCT* cwp = (CWPSTRUCT*)lParam;

    // Only care about specific messages
    if (cwp->message != WM_NCCREATE &&
        cwp->message != WM_NCPAINT &&
        cwp->message != WM_ACTIVATE)
    {
        return CallNextHookEx(NULL, nCode, wParam, lParam);
    }

    // Check if already processed this window
    if (IsWindowAlreadyProcessed(cwp->hwnd))
        return CallNextHookEx(NULL, nCode, wParam, lParam);

    // Do actual work...
    HandleWindowEvent(cwp);

    return CallNextHookEx(NULL, nCode, wParam, lParam);
}
```

## Security Considerations

### Privilege Requirements

- Must run as **Administrator** to inject into other processes
- Some processes (services, system) can't be injected even as admin
- Add these to automatic blacklist

### Antivirus Detection

- DLL injection = classic malware technique
- Expect antivirus alerts
- Mitigation:
  - Code signing certificate (REQUIRED)
  - Submit to antivirus vendors for whitelisting
  - Open source code for review

### Crash Prevention

If the hook crashes, it can crash other processes!

**Safety measures:**
- Extensive exception handling (`__try`/`__except`)
- Validate all pointers
- Never throw exceptions from hook callback
- Log errors instead of crashing

```cpp
LRESULT CALLBACK HookProc(int nCode, WPARAM wParam, LPARAM lParam)
{
    __try
    {
        // Hook logic here
    }
    __except(EXCEPTION_EXECUTE_HANDLER)
    {
        // Log error but don't crash
        OutputDebugString(L"Hook exception caught!");
    }

    return CallNextHookEx(NULL, nCode, wParam, lParam);
}
```

## Logging

### Debug Logging

```cpp
void Log(const wchar_t* format, ...)
{
    wchar_t buffer[1024];
    va_list args;
    va_start(args, format);
    vswprintf_s(buffer, format, args);
    va_end(args);

    // Output to debugger
    OutputDebugString(L"[CyclosideThemeHost] ");
    OutputDebugString(buffer);
    OutputDebugString(L"\n");

    // Also write to file (optional)
    FILE* f = _wfopen(L"C:\\ProgramData\\Cycloside\\theme_host.log", L"a");
    if (f)
    {
        fwprintf(f, L"%s\n", buffer);
        fclose(f);
    }
}
```

### Event Tracing

Use Windows Event Tracing (ETW) for production:
```cpp
// Register event provider
REGHANDLE hProvider;
EventRegister(&CYCLOSIDE_PROVIDER_GUID, NULL, NULL, &hProvider);

// Log events
EventWrite(hProvider, &HOOK_INSTALLED_EVENT, 0, NULL);
EventWrite(hProvider, &DLL_INJECTED_EVENT, 2, eventData);
```

## Dependencies

- **kernel32.dll** - Process/memory management
- **user32.dll** - Window hooks
- **advapi32.dll** - Process token manipulation (if needed)

## File Structure

```
CyclosideThemeHost/
├── main.cpp              # Entry point, message loop
├── hook.cpp              # Hook installation/callback
├── hook.h
├── injection.cpp         # DLL injection logic
├── injection.h
├── shared_memory.cpp     # Shared memory IPC
├── shared_memory.h
├── config.cpp            # Configuration reading
├── config.h
├── utils.cpp             # Helper functions
├── utils.h
├── resource.rc           # Version info, icon
└── CMakeLists.txt        # Build configuration
```

## Next Steps

1. Implement basic hook installation
2. Add DLL injection
3. Test with Notepad
4. Add configuration reading
5. Integrate with Cycloside C# service
6. Test with real applications
7. Optimize performance
8. Add error handling

## References

- [SetWindowsHookEx](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexa)
- [CreateRemoteThread](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createremotethread)
- [DLL Injection Tutorial](https://www.codeproject.com/Articles/4610/Three-Ways-to-Inject-Your-Code-into-Another-Proces)
