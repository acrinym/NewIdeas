using System;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// Utility FFT related helpers.
    /// </summary>
    public static class FftExtensions
    {
        /// <summary>
        /// Calculates the Blackman-Harris window coefficient.
        /// </summary>
        public static double BlackmanHarrisWindow(int n, int length)
        {
            const double a0 = 0.35875;
            const double a1 = 0.48829;
            const double a2 = 0.14128;
            const double a3 = 0.01168;

            if (length <= 1)
                return 1.0;

            double ratio = (2.0 * Math.PI * n) / (length - 1);
            return a0
                - a1 * Math.Cos(ratio)
                + a2 * Math.Cos(2 * ratio)
                - a3 * Math.Cos(3 * ratio);
        }
    }
}
