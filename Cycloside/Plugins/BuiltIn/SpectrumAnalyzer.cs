// --- NEW: SpectrumAnalyzer.cs ---

using NAudio.Dsp;
using NAudio.Wave;
using System;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// A helper class to perform a Fast Fourier Transform (FFT) on an audio stream
    /// to generate spectrum data for visualizations.
    /// </summary>
    public class SpectrumAnalyzer
    {
        private readonly ISampleProvider _source;
        private readonly Complex[] _fftBuffer;
        private readonly float[] _sampleBuffer;
        private readonly int _fftLength;
        private int _sampleIndex;

        public SpectrumAnalyzer(ISampleProvider source, int fftLength = 1024)
        {
            if (fftLength % 2 != 0)
                throw new ArgumentException("FFT length must be a power of 2.");

            _source = source;
            _fftLength = fftLength;
            _fftBuffer = new Complex[fftLength];
            _sampleBuffer = new float[fftLength];
        }

        /// <summary>
        /// Reads the latest audio samples and calculates FFT data.
        /// </summary>
        /// <param name="fftData">The byte array to fill with spectrum data.</param>
        public void GetFftData(byte[] fftData)
        {
            // Read samples from the source into our buffer
            int read = _source.Read(_sampleBuffer, 0, _fftLength);
            if (read == 0) return;

            // Fill the FFT buffer with the latest samples
            for (int i = 0; i < read; i++)
            {
                _fftBuffer[i].X = (float)(_sampleBuffer[i] * FastFourierTransform.BlackmanHarrisWindow(i, _fftLength));
                _fftBuffer[i].Y = 0;
            }
            // Zero out the rest of the buffer
            for (int i = read; i < _fftLength; i++)
            {
                _fftBuffer[i].X = 0;
                _fftBuffer[i].Y = 0;
            }

            // Perform the FFT
            FastFourierTransform.FFT(true, (int)Math.Log(_fftLength, 2.0), _fftBuffer);

            // Calculate magnitude and scale it for the visualizer
            for (int i = 0; i < fftData.Length; i++)
            {
                // Calculate magnitude in decibels
                double magnitude = Math.Sqrt(_fftBuffer[i].X * _fftBuffer[i].X + _fftBuffer[i].Y * _fftBuffer[i].Y);
                double decibels = 20 * Math.Log10(magnitude);
                
                // Scale the dB value to a byte (0-255) for the visualizer
                // We'll map a range of -90dB to 0dB to our byte range.
                double scaledValue = (90 + decibels) * (255.0 / 90.0);
                fftData[i] = (byte)Math.Max(0, Math.Min(255, scaledValue));
            }
        }
        
        /// <summary>
        /// Provides the raw waveform data from the last buffer read.
        /// </summary>
        /// <param name="waveformData">The byte array to fill with waveform data.</param>
        public void GetWaveformData(byte[] waveformData)
        {
            for (int i = 0; i < waveformData.Length; i++)
            {
                // Scale the float sample (-1.0 to 1.0) to a byte (0-255)
                waveformData[i] = (byte)((_sampleBuffer[i] + 1.0) * 127.5);
            }
        }
    }
}
