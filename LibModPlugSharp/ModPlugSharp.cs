using System;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenMpt.Sharp
{
    public class ModPlugModule : IDisposable
    {
        private IntPtr _handle;

        public ModPlugModule(byte[] data)
        {
            _handle = NativeMethods.openmpt_module_create_from_memory(data, (UIntPtr)data.Length, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (_handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create module");
            }
        }

        public string GetTitle()
        {
            IntPtr ptr = NativeMethods.openmpt_module_get_metadata(_handle, "title");
            return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }

        public int Read(float[] buffer, int sampleRate)
        {
            return (int)NativeMethods.openmpt_module_read_interleaved_float_stereo(_handle, sampleRate, (UIntPtr)(buffer.Length / 2), buffer);
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.openmpt_module_destroy(_handle);
                _handle = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }
    }
}
