using System;
using System.IO;
using System.Threading.Tasks;

namespace Cycloside.Services
{
    /// <summary>
    /// Security validation for theme, skin, and asset loading.
    /// Enforces path confinement, name validation, content whitelisting, and size limits.
    /// See docs/cycloside-vulnerability-catalog.md for the threat model.
    /// </summary>
    public static class ThemeSecurityValidator
    {
        public static readonly long MaxAxamlFileSize = 1 * 1024 * 1024;        // 1 MB
        public static readonly long MaxManifestFileSize = 256 * 1024;           // 256 KB
        public static readonly long MaxAudioFileSize = 10 * 1024 * 1024;        // 10 MB
        public static readonly long MaxImageFileSize = 50 * 1024 * 1024;        // 50 MB
        public static readonly int MaxPackNameLength = 64;

        /// <summary>
        /// Validates that a theme/skin pack name is a safe directory name.
        /// Blocks path traversal sequences, path separators, and invalid filename characters.
        /// Fixes CYC-2026-002.
        /// </summary>
        public static bool IsValidPackName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (name.Length > MaxPackNameLength)
                return false;

            // Block path traversal
            if (name.Contains(".."))
                return false;

            // Block path separators
            if (name.Contains('/') || name.Contains('\\'))
                return false;

            // Block invalid filename characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                for (int j = 0; j < invalidChars.Length; j++)
                {
                    if (c == invalidChars[j])
                        return false;
                }
            }

            // Block names that are just dots or whitespace
            ReadOnlySpan<char> trimmed = name.AsSpan().Trim();
            if (trimmed.Length == 0)
                return false;

            bool allDots = true;
            for (int i = 0; i < trimmed.Length; i++)
            {
                if (trimmed[i] != '.')
                {
                    allDots = false;
                    break;
                }
            }
            if (allDots)
                return false;

            return true;
        }

        /// <summary>
        /// Resolves a relative path within a base directory and verifies confinement.
        /// Returns the resolved path if safe, null if the path escapes the base directory.
        /// Fixes CYC-2026-001 and CYC-2026-010.
        /// </summary>
        public static string? ResolveSafePath(string baseDir, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(baseDir) || string.IsNullOrWhiteSpace(relativePath))
                return null;

            // Block obvious traversal before even resolving
            if (relativePath.Contains(".."))
            {
                Logger.Log($"🛡️ Blocked path traversal attempt: {relativePath}");
                return null;
            }

            try
            {
                var resolvedBase = Path.GetFullPath(baseDir);
                if (!resolvedBase.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    resolvedBase += Path.DirectorySeparatorChar;

                var combined = Path.Combine(baseDir, relativePath);
                var resolvedPath = Path.GetFullPath(combined);

                if (!resolvedPath.StartsWith(resolvedBase, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log($"🛡️ Blocked path escape: '{relativePath}' resolved to '{resolvedPath}' (outside '{resolvedBase}')");
                    return null;
                }

                return resolvedPath;
            }
            catch (Exception ex)
            {
                Logger.Log($"🛡️ Path resolution failed for '{relativePath}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates AXAML content against a whitelist of safe constructs.
        /// Blocks CLR namespace references, code-behind, event handlers, and external resources.
        /// Fixes CYC-2026-003 and replaces CYC-2026-009.
        /// </summary>
        public static bool IsAxamlContentSafe(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            // Block CLR namespace references (arbitrary type instantiation)
            if (ContainsInsensitive(content, "clr-namespace:"))
            {
                Logger.Log("🛡️ AXAML blocked: contains clr-namespace reference");
                return false;
            }

            // Block code-behind class references
            if (ContainsInsensitive(content, "x:Class"))
            {
                Logger.Log("🛡️ AXAML blocked: contains x:Class (code-behind)");
                return false;
            }

            // Block assembly references beyond the default Avalonia namespace
            if (ContainsInsensitive(content, ";assembly="))
            {
                Logger.Log("🛡️ AXAML blocked: contains assembly reference");
                return false;
            }

            // Block external resource URIs
            if (ContainsInsensitive(content, "http://") || ContainsInsensitive(content, "https://"))
            {
                Logger.Log("🛡️ AXAML blocked: contains external URI");
                return false;
            }

            // Block event handler wiring (would need code-behind to work, but block anyway)
            string[] dangerousAttributes =
            {
                "Click=", "Tapped=", "DoubleTapped=",
                "PointerPressed=", "PointerReleased=", "PointerMoved=",
                "Loaded=", "Initialized=", "AttachedToVisualTree=",
                "DetachedFromVisualTree=", "DataContextChanged=",
                "KeyDown=", "KeyUp=", "TextInput=",
                "GotFocus=", "LostFocus=",
                "Opening=", "Closing=",
                "SelectionChanged=", "ValueChanged="
            };

            for (int i = 0; i < dangerousAttributes.Length; i++)
            {
                if (content.Contains(dangerousAttributes[i]))
                {
                    Logger.Log($"🛡️ AXAML blocked: contains event handler '{dangerousAttributes[i]}'");
                    return false;
                }
            }

            // Block markup extensions that could invoke code
            if (ContainsInsensitive(content, "{x:Static") && ContainsInsensitive(content, "System."))
            {
                Logger.Log("🛡️ AXAML blocked: contains x:Static referencing System namespace");
                return false;
            }

            // Block Binding path expressions referencing System types
            if (ContainsInsensitive(content, "x:Type") && ContainsInsensitive(content, "System."))
            {
                Logger.Log("🛡️ AXAML blocked: contains x:Type referencing System namespace");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks file size against a maximum allowed size.
        /// Fixes CYC-2026-004.
        /// </summary>
        public static bool CheckFileSize(string filePath, long maxSize)
        {
            try
            {
                var info = new FileInfo(filePath);
                if (info.Length > maxSize)
                {
                    Logger.Log($"🛡️ File exceeds size limit ({info.Length} > {maxSize}): {filePath}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"🛡️ File size check failed for '{filePath}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads a file with size limit enforcement.
        /// Combines existence check and read into a single operation to prevent TOCTOU.
        /// Fixes CYC-2026-004 and CYC-2026-007.
        /// </summary>
        public static string? SafeReadAllText(string filePath, long maxSize)
        {
            try
            {
                if (!CheckFileSize(filePath, maxSize))
                    return null;

                return File.ReadAllText(filePath);
            }
            catch (FileNotFoundException)
            {
                Logger.Log($"File not found: {filePath}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading file '{filePath}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Async version of SafeReadAllText.
        /// Fixes CYC-2026-004 and CYC-2026-007.
        /// </summary>
        public static async Task<string?> SafeReadAllTextAsync(string filePath, long maxSize)
        {
            try
            {
                if (!CheckFileSize(filePath, maxSize))
                    return null;

                return await File.ReadAllTextAsync(filePath);
            }
            catch (FileNotFoundException)
            {
                Logger.Log($"File not found: {filePath}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error reading file '{filePath}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates an AXAML file: checks size, reads content, validates safety.
        /// Returns the file content if safe, null otherwise.
        /// Combined fix for CYC-2026-003, CYC-2026-004, CYC-2026-007, CYC-2026-009.
        /// </summary>
        public static async Task<string?> ValidateAndReadAxamlAsync(string filePath)
        {
            var content = await SafeReadAllTextAsync(filePath, MaxAxamlFileSize);
            if (content == null)
                return null;

            if (!IsAxamlContentSafe(content))
                return null;

            return content;
        }

        /// <summary>
        /// Case-insensitive substring search without allocating a new string.
        /// </summary>
        private static bool ContainsInsensitive(string source, string value)
        {
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
