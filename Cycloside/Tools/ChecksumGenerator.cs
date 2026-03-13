using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Cycloside.Services;

namespace Cycloside.Tools
{
    /// <summary>
    /// Generates SHA-256 checksums for plugin manifest files (CYC-2026-030).
    /// Plugin authors run this before publishing to populate manifest.json Checksum fields.
    /// </summary>
    public static class ChecksumGenerator
    {
        private static readonly JsonSerializerOptions _jsonReadOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private static readonly JsonSerializerOptions _jsonWriteOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <summary>
        /// Read manifest.json, compute SHA-256 for each file in Files, update manifest, write back.
        /// </summary>
        /// <param name="pluginDir">Plugin directory containing manifest.json and plugin files.</param>
        /// <returns>True if successful; false if manifest missing or file not found.</returns>
        public static async Task<bool> GenerateManifestChecksumsAsync(string pluginDir)
        {
            var manifestPath = Path.Combine(pluginDir, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                return false;
            }

            var json = await File.ReadAllTextAsync(manifestPath);
            var manifest = JsonSerializer.Deserialize<PluginManifest>(json, _jsonReadOptions);
            if (manifest?.Files == null)
            {
                return false;
            }

            foreach (var file in manifest.Files)
            {
                var filePath = Path.Combine(pluginDir, file.Path);
                if (!File.Exists(filePath))
                {
                    return false;
                }
                var content = await File.ReadAllBytesAsync(filePath);
                file.Checksum = PluginRepository.ComputeSha256Hex(content);
                if (file.Size <= 0)
                {
                    file.Size = content.Length;
                }
            }

            var output = JsonSerializer.Serialize(manifest, _jsonWriteOptions);
            await File.WriteAllTextAsync(manifestPath, output);
            return true;
        }
    }
}
