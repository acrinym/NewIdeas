using System;
using System.Runtime.InteropServices;

namespace OpenMpt.Sharp
{
    public class Module : IDisposable
    {
        public class Settings { }

        private IntPtr _handle;

        private Module(IntPtr handle)
        {
            _handle = handle;
        }

        public static Module Create(byte[] data, Settings settings)
        {
            IntPtr handle = NativeMethods.openmpt_module_create_from_memory(data, (UIntPtr)data.Length, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to load module");
            }
            return new Module(handle);
        }

        public string GetMetadata(string key)
        {
            IntPtr ptr = NativeMethods.openmpt_module_get_metadata(_handle, key);
            return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }

        public int GetNumberOfChannels() => NativeMethods.openmpt_module_get_num_channels(_handle);
        public int GetNumberOfPatterns() => NativeMethods.openmpt_module_get_num_patterns(_handle);
        public int GetNumberOfSamples() => NativeMethods.openmpt_module_get_num_samples(_handle);
        public int GetCurrentPattern() => NativeMethods.openmpt_module_get_current_pattern(_handle);
        public int GetCurrentRow() => NativeMethods.openmpt_module_get_current_row(_handle);
        public int GetPatternRowCount(int pattern) => NativeMethods.openmpt_module_get_pattern_num_rows(_handle, pattern);

        public string FormatPatternRowChannel(int pattern, int row, int channel, int width)
        {
            IntPtr ptr = NativeMethods.openmpt_module_format_pattern_row_channel(_handle, pattern, row, channel, (UIntPtr)width, 0);
            return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }

        public int ReadInterleavedStereo(int sampleRate, int frameCount, byte[] buffer, int offset)
        {
            float[] temp = new float[frameCount * 2];
            int frames = (int)NativeMethods.openmpt_module_read_interleaved_float_stereo(_handle, sampleRate, (UIntPtr)frameCount, temp);
            Buffer.BlockCopy(temp, 0, buffer, offset, frames * 2 * sizeof(float));
            return frames * 2 * sizeof(float);
        }

        public void SetPosition(int order)
        {
            NativeMethods.openmpt_module_set_position_order_row(_handle, order, 0);
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.openmpt_module_destroy(_handle);
                _handle = IntPtr.Zero;
            }
        }
    }
}
