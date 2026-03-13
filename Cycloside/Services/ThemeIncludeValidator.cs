using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Cycloside.Services
{
    /// <summary>
    /// Validates AXAML include graphs for circular references and depth. CYC-2026-031.
    /// </summary>
    public static class ThemeIncludeValidator
    {
        private const int MaxDepth = 10;

        public static bool ValidateGraph(string rootPath, int maxDepth = MaxDepth)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var currentPath = new Stack<string>();
            return ValidateRecursive(rootPath, visited, currentPath, maxDepth);
        }

        private static bool ValidateRecursive(string filePath, HashSet<string> visited,
            Stack<string> currentPath, int maxDepth)
        {
            var normalized = Path.GetFullPath(filePath);
            if (!File.Exists(normalized))
                return true;

            var normalizedLower = normalized.ToLowerInvariant();

            if (currentPath.Contains(normalizedLower))
            {
                Logger.Log($"ThemeIncludeValidator: circular reference detected: {filePath}");
                return false;
            }

            if (currentPath.Count >= maxDepth)
            {
                Logger.Log($"ThemeIncludeValidator: max depth {maxDepth} exceeded");
                return false;
            }

            if (visited.Contains(normalizedLower))
                return true;

            visited.Add(normalizedLower);
            currentPath.Push(normalizedLower);

            try
            {
                var includes = ExtractIncludeReferences(normalized);
                var baseDir = Path.GetDirectoryName(normalized) ?? "";

                foreach (var include in includes)
                {
                    var resolved = ResolveIncludePath(baseDir, include);
                    if (resolved != null && !ValidateRecursive(resolved, visited, currentPath, maxDepth))
                        return false;
                }
                return true;
            }
            finally
            {
                currentPath.Pop();
            }
        }

        private static List<string> ExtractIncludeReferences(string filePath)
        {
            var refs = new List<string>();
            try
            {
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    MaxCharactersFromEntities = 1024
                };
                using var reader = XmlReader.Create(filePath, settings);
                var doc = XDocument.Load(reader);

                foreach (var el in doc.Descendants())
                {
                    var src = el.Attribute("Source")?.Value;
                    if (string.IsNullOrWhiteSpace(src)) continue;

                    if (el.Name.LocalName.Equals("StyleInclude", StringComparison.OrdinalIgnoreCase) ||
                        el.Name.LocalName.Equals("ResourceInclude", StringComparison.OrdinalIgnoreCase))
                    {
                        refs.Add(src.Trim());
                    }
                }

                var merged = doc.Descendants().FirstOrDefault(d =>
                    d.Name.LocalName.Equals("MergedDictionaries", StringComparison.OrdinalIgnoreCase));
                if (merged != null)
                {
                    foreach (var child in merged.Elements())
                    {
                        var src = child.Attribute("Source")?.Value;
                        if (!string.IsNullOrWhiteSpace(src))
                            refs.Add(src.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"ThemeIncludeValidator: failed to parse {filePath}: {ex.Message}");
            }
            return refs;
        }

        private static string? ResolveIncludePath(string baseDir, string include)
        {
            if (string.IsNullOrWhiteSpace(include) || include.Contains(".."))
                return null;

            if (include.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                include = include.Substring(8).Replace('/', Path.DirectorySeparatorChar);
            else if (include.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
                return null;

            var full = Path.GetFullPath(Path.Combine(baseDir, include));
            return File.Exists(full) ? full : null;
        }
    }
}
