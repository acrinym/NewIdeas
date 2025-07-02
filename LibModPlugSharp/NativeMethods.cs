using System;
using System.Runtime.InteropServices;

namespace OpenMpt.Sharp
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
        public static extern UIntPtr openmpt_module_read_interleaved_float_stereo(IntPtr module, int samplerate, UIntPtr count, [Out] float[] buffer);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int openmpt_module_get_num_channels(IntPtr module);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int openmpt_module_get_num_patterns(IntPtr module);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int openmpt_module_get_num_samples(IntPtr module);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int openmpt_module_get_current_pattern(IntPtr module);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int openmpt_module_get_current_row(IntPtr module);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int openmpt_module_get_pattern_num_rows(IntPtr module, int pattern);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr openmpt_module_format_pattern_row_channel(IntPtr module, int pattern, int row, int channel, UIntPtr width, int pad);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double openmpt_module_set_position_order_row(IntPtr module, int order, int row);
    }
}
