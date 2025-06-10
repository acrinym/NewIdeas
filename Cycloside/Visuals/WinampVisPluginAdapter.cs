using System;
using System.Runtime.InteropServices;
using Cycloside.Interop;

namespace Cycloside.Visuals;

public class WinampVisPluginAdapter
{
    private readonly string _path;
    private IntPtr _library;
    private winampVisModule _module;
    private delegate int InitDelegate(ref winampVisModule m);
    private delegate int RenderDelegate(ref winampVisModule m);
    private delegate void QuitDelegate(ref winampVisModule m);

    private InitDelegate? _init;
    private RenderDelegate? _render;
    private QuitDelegate? _quit;

    public string Description { get; private set; } = string.Empty;

    public WinampVisPluginAdapter(string path)
    {
        _path = path;
    }

    public bool Load()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        _library = NativeMethods.LoadLibrary(_path);
        if (_library == IntPtr.Zero)
            return false;

        var proc = NativeMethods.GetProcAddress(_library, "winampVisGetHeader");
        if (proc == IntPtr.Zero)
            return false;

        var getHeader = Marshal.GetDelegateForFunctionPointer<winampVisGetHeaderType>(proc);
        var headerPtr = getHeader();
        var header = Marshal.PtrToStructure<winampVisHeader>(headerPtr);
        Description = header.description ?? string.Empty;
        var getModule = Marshal.GetDelegateForFunctionPointer<getModuleDelegate>(header.getModule);
        var modPtr = getModule(0);
        _module = Marshal.PtrToStructure<winampVisModule>(modPtr);

        _init = Marshal.GetDelegateForFunctionPointer<InitDelegate>(_module.Init);
        _render = Marshal.GetDelegateForFunctionPointer<RenderDelegate>(_module.Render);
        _quit = Marshal.GetDelegateForFunctionPointer<QuitDelegate>(_module.Quit);

        _module.hwndParent = IntPtr.Zero;
        _module.sRate = 44100;
        _module.nCh = 2;
        _module.spectrumData = new byte[1152];
        _module.waveformData = new byte[1152];
        return true;
    }

    public bool Initialize()
    {
        if (_init == null) return false;
        return _init(ref _module) == 0;
    }

    public void Render()
    {
        _render?.Invoke(ref _module);
    }

    public void Quit()
    {
        _quit?.Invoke(ref _module);
        if (_library != IntPtr.Zero)
        {
            NativeMethods.FreeLibrary(_library);
            _library = IntPtr.Zero;
        }
    }
}
