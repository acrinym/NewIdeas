using System;
using System.IO;

namespace Cycloside.Services
{
    /// <summary>
    /// Validates binary file formats before passing to parsers (CYC-2026-024, 026, 027, 028).
    /// Rejects polyglots, oversized chunks, and format confusion.
    /// </summary>
    public static class BinaryFormatValidator
    {
        /// <summary>
        /// Reject data: URIs in asset paths (CYC-2026-023).
        /// </summary>
        public static bool IsDataUri(string pathOrUri)
        {
            return !string.IsNullOrEmpty(pathOrUri) && pathOrUri.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validate ICO/CUR header: reserved 0x0000, type must match expected (1=ICO, 2=CUR).
        /// </summary>
        private static bool ValidateIcoCurHeader(byte[] header, ushort expectedType)
        {
            if (header == null || header.Length < 6) return false;
            if (header[0] != 0 || header[1] != 0) return false;
            ushort type = (ushort)(header[2] | (header[3] << 8));
            return type == expectedType;
        }

        /// <summary>
        /// Validate ICO file: reserved 0x0000, type 0x0001.
        /// </summary>
        public static bool ValidateIcoMagic(byte[] header)
        {
            return ValidateIcoCurHeader(header, 1);
        }

        /// <summary>
        /// Validate CUR file: reserved 0x0000, type 0x0002.
        /// </summary>
        public static bool ValidateCurMagic(byte[] header)
        {
            return ValidateIcoCurHeader(header, 2);
        }

        /// <summary>
        /// Validate WAV file: RIFF header, WAVE format, sane chunk sizes.
        /// </summary>
        public static bool ValidateWavStructure(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                if (stream.Length < 12) return false;

                var header = new byte[12];
                if (stream.Read(header, 0, 12) != 12) return false;

                if (header[0] != 'R' || header[1] != 'I' || header[2] != 'F' || header[3] != 'F')
                    return false;
                if (header[8] != 'W' || header[9] != 'A' || header[10] != 'V' || header[11] != 'E')
                    return false;

                uint riffSize = (uint)(header[4] | (header[5] << 8) | (header[6] << 16) | (header[7] << 24));
                if (riffSize > stream.Length || riffSize < 4) return false;

                while (stream.Position < stream.Length - 8)
                {
                    var chunkHeader = new byte[8];
                    if (stream.Read(chunkHeader, 0, 8) != 8) break;

                    uint chunkSize = (uint)(chunkHeader[4] | (chunkHeader[5] << 8) | (chunkHeader[6] << 16) | (chunkHeader[7] << 24));
                    if (chunkSize > int.MaxValue) return false;
                    if (stream.Position + chunkSize > stream.Length) return false;

                    stream.Seek(chunkSize, SeekOrigin.Current);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"BinaryFormatValidator.ValidateWavStructure: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validate ICO/CUR file before loading. Reject RIFF-based or wrong type.
        /// Uses shared header validation (no duplicated logic).
        /// </summary>
        public static bool ValidateIcoCurFile(string path)
        {
            try
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                var header = new byte[6];
                using (var fs = File.OpenRead(path))
                {
                    if (fs.Length < 6) return false;
                    if (fs.Read(header, 0, 6) != 6) return false;
                }

                if (header[0] != 0 || header[1] != 0) return false;
                ushort type = (ushort)(header[2] | (header[3] << 8));

                if (ext == ".ico") return ValidateIcoCurHeader(header, 1);
                if (ext == ".cur") return ValidateIcoCurHeader(header, 2);
                return type == 1 || type == 2;
            }
            catch (Exception ex)
            {
                Logger.Log($"BinaryFormatValidator.ValidateIcoCurFile: {ex.Message}");
                return false;
            }
        }
    }
}
