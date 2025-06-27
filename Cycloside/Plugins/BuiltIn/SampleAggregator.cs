using System;
using NAudio.Wave;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// Provides audio samples to multiple consumers while keeping track of the
    /// most recently read data.
    /// </summary>
    public class SampleAggregator : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private float[] _lastSamples = Array.Empty<float>();

        public WaveFormat WaveFormat => _source.WaveFormat;

        public SampleAggregator(ISampleProvider source)
        {
            _source = source;
        }

        /// <summary>
        /// Reads samples from the wrapped provider and stores them so they can
        /// be retrieved later.
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            int read = _source.Read(buffer, offset, count);

            if (_lastSamples.Length != read)
                _lastSamples = new float[read];

            Array.Copy(buffer, offset, _lastSamples, 0, read);
            return read;
        }

        /// <summary>
        /// Copies the last samples read into <paramref name="buffer"/>.
        /// </summary>
        /// <returns>The number of samples copied.</returns>
        public int Read(float[] buffer)
        {
            int copy = Math.Min(buffer.Length, _lastSamples.Length);
            Array.Copy(_lastSamples, 0, buffer, 0, copy);
            return copy;
        }
    }
}
