using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cycloside.Services
{
    /// <summary>
    /// Resolves theme dependency order. Detects circular dependencies.
    /// </summary>
    public static class ThemeDependencyResolver
    {
        private const int MaxDepth = ThemeConstants.MaxDependencyDepth;

        /// <summary>
        /// Returns theme names in load order (dependencies first). Throws on cycle or max depth.
        /// </summary>
        public static List<string> ResolveOrder(ThemeManifest manifest, string themeDir)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            ResolveRecursive(manifest, themeDir, visited, visiting, order, 0);
            return order;
        }

        private static void ResolveRecursive(ThemeManifest manifest, string themeDir,
            HashSet<string> visited, HashSet<string> visiting, List<string> order, int depth)
        {
            if (depth >= MaxDepth)
                throw new InvalidOperationException($"Theme dependency depth exceeded {MaxDepth}");

            var themeName = Path.GetFileName(themeDir);
            if (string.IsNullOrEmpty(themeName))
                themeName = manifest.Name;

            if (visiting.Contains(themeName))
                throw new InvalidOperationException($"Circular theme dependency: {themeName}");

            if (visited.Contains(themeName))
                return;

            visiting.Add(themeName);

            var deps = manifest.Dependencies?.RequiredThemes;
            if (deps?.Count > 0)
            {
                var themesBase = Path.Combine(AppContext.BaseDirectory, "Themes");
                foreach (var dep in deps)
                {
                    var depDir = Path.Combine(themesBase, dep);
                    var depManifest = ThemeManifest.Load(depDir);
                    if (depManifest != null)
                        ResolveRecursive(depManifest, depDir, visited, visiting, order, depth + 1);
                }
            }

            visiting.Remove(themeName);
            visited.Add(themeName);
            if (!order.Contains(themeName, StringComparer.OrdinalIgnoreCase))
                order.Add(themeName);
        }
    }
}
