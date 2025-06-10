using System;
using System.Runtime.InteropServices;

namespace Cycloside.Interop;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct winampVisModule
{
    public string description;
    public IntPtr hwndParent;
    public IntPtr hDllInstance;
    public int sRate;
    public int nCh;
    public int latencyMs;
    public int delayMs;
    public int spectrumNch;
    public int waveformNch;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1152)]
    public byte[] spectrumData;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1152)]
    public byte[] waveformData;
    public IntPtr Config;
    public IntPtr Init;
    public IntPtr Render;
    public IntPtr Quit;
    public IntPtr userData;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct winampVisHeader
{
    public int version;
    public string description;
    public IntPtr getModule;
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate IntPtr winampVisGetHeaderType();

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate IntPtr getModuleDelegate(int which);

internal static class NativeMethods
{
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32", SetLastError = true)]
    public static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
}
