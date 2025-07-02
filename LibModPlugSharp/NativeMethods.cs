using System;
using System.Runtime.InteropServices;

namespace LibModPlugSharp
{
    internal static class NativeMethods
    {
        private const string LibraryName = "libopenmpt";

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr openmpt_module_create_from_memory(byte[] data, UIntPtr size, IntPtr logFunc, IntPtr logUser, IntPtr ctls);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void openmpt_module_destroy(IntPtr module);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr openmpt_module_get_metadata(IntPtr module, string key);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int openmpt_module_read_interleaved_stereo(IntPtr module, int samplerate, UIntPtr count, float[] buffer);
    }
}
