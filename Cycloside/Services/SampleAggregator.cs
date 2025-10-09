// ============================================================================
// SAMPLE AGGREGATOR - Based on NAudio.Dsp.SampleAggregator
// ============================================================================
// Purpose: Aggregates audio samples for real-time analysis
// Features: FFT-ready sample buffering, notification system
// Dependencies: NAudio.Wave (open source)
// ============================================================================

using NAudio.Wave;
using System;

namespace Cycloside.Services;

public class SampleAggregator : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float[] _sampleBuffer;
    private int _sampleBufferPosition;

    public event EventHandler<SampleAggregatorEventArgs>? SampleAvailable;

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int BufferSize { get; }

    public SampleAggregator(ISampleProvider source, int bufferSize = 1024)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        BufferSize = bufferSize;
        _sampleBuffer = new float[bufferSize];
    }

    public int Read(float[] buffer, int offset, int count)
    {
        var samplesRead = _source.Read(buffer, offset, count);

        for (int i = 0; i < samplesRead; i++)
        {
            _sampleBuffer[_sampleBufferPosition] = buffer[offset + i];
            _sampleBufferPosition++;

            if (_sampleBufferPosition >= _sampleBuffer.Length)
            {
                // Buffer is full, notify listeners
                var tempBuffer = new float[_sampleBuffer.Length];
                Array.Copy(_sampleBuffer, tempBuffer, _sampleBuffer.Length);

                SampleAvailable?.Invoke(this, new SampleAggregatorEventArgs(tempBuffer));

                _sampleBufferPosition = 0;
            }
        }

        return samplesRead;
    }

    public float[] GetSampleBuffer()
    {
        // Return a copy of the current buffer
        var buffer = new float[Math.Min(_sampleBufferPosition, _sampleBuffer.Length)];
        Array.Copy(_sampleBuffer, buffer, buffer.Length);
        return buffer;
    }
}

public class SampleAggregatorEventArgs : EventArgs
{
    public float[] Samples { get; }

    public SampleAggregatorEventArgs(float[] samples)
    {
        Samples = samples ?? throw new ArgumentNullException(nameof(samples));
    }
}
