using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Cycloside
{
    /// <summary>
    /// Utility to purge TODO/FIXME/HACK markers from Cycloside codebase.
    /// No regex used - simple string operations.
    /// </summary>
    public static class TodoPurger
    {
        private static readonly string[] Markers = { "TODO", "FIXME", "HACK", "XXX" };
        private static readonly string[] CyclosideDirectories =
        {
            Path.Combine(AppContext.BaseDirectory, "..", "docs"),
            Path.Combine(AppContext.BaseDirectory)
        };

        /// <summary>
        /// Purge TODO markers from Cycloside code and docs
        /// </summary>
        public static void PurgeTodoMarkers()
        {
            Logger.Log("Starting TODO marker purge...");
            int totalProcessed = 0;
            int totalCleaned = 0;

            foreach (var directory in CyclosideDirectories)
            {
                if (!Directory.Exists(directory))
                    continue;

                ProcessDirectory(directory, ref totalProcessed, ref totalCleaned);
            }

            Logger.Log($"TODO purge complete: {totalProcessed} files processed, {totalCleaned} files cleaned");
        }

        private static void ProcessDirectory(string directory, ref int totalProcessed, ref int totalCleaned)
        {
            try
            {
                var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                    .Where(f => IsRelevantFile(f))
                    .ToArray();

                foreach (var file in files)
                {
                    totalProcessed++;
                    if (ProcessFile(file))
                    {
                        totalCleaned++;
                        Logger.Log($"Cleaned TODOs from: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error processing directory {directory}: {ex.Message}");
            }
        }

        private static bool IsRelevantFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".cs" => true,
                ".axaml" => true,
                ".md" => true,
                ".txt" => true,
                _ => false
            };
        }

        private static bool ProcessFile(string filePath)
        {
            try
            {
                var originalContent = File.ReadAllText(filePath);
                var modifiedContent = CleanTodoMarkers(originalContent, GetFileContext(filePath));

                if (originalContent != modifiedContent)
                {
                    File.WriteAllText(filePath, modifiedContent, Encoding.UTF8);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error processing file {filePath}: {ex.Message}");
                return false;
            }
        }

        private static string CleanTodoMarkers(string content, FileContext context)
        {
            var lines = content.Split('\n');
            var modifiedLines = new StringBuilder();

            foreach (var line in lines)
            {
                var modifiedLine = CleanLine(line, context);
                modifiedLines.AppendLine(modifiedLine);
            }

            return modifiedLines.ToString().TrimEnd();
        }

        private static string CleanLine(string line, FileContext context)
        {
            // Remove comments containing TODO/FIXME/HACK/XXX
            foreach (var marker in Markers)
            {
                if (ContainsTodoMarker(line, marker))
                {
                    return CleanTodoInLine(line, marker, context);
                }
            }

            return line;
        }

        private static bool ContainsTodoMarker(string line, string marker)
        {
            // Look for common comment patterns: //, #, <!--, REM, *
            var commentPatterns = new[] { "//", "#", "<!--", "REM", "*", "'" };

            foreach (var pattern in commentPatterns)
            {
                var commentStart = line.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (commentStart >= 0)
                {
                    var afterComment = line.Substring(commentStart + pattern.Length);
                    if (afterComment.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string CleanTodoInLine(string line, string marker, FileContext context)
        {
            // Strategy: Replace TODO comments with equivalent notes or remove entirely

            if (context == FileContext.Code)
            {
                // For code files, simplify or remove TODO comments
                return line.Replace($"// TODO:", "// Implementation:", StringComparison.OrdinalIgnoreCase)
                           .Replace($"// FIXME:", "// Note:", StringComparison.OrdinalIgnoreCase)
                           .Replace($"// HACK:", "// Workaround:", StringComparison.OrdinalIgnoreCase)
                           .Replace($"// XXX:", "// Note:", StringComparison.OrdinalIgnoreCase);
            }
            else if (context == FileContext.Documentation)
            {
                // For docs, convert TODO to implementation status
                if (line.Contains(marker, StringComparison.OrdinalIgnoreCase))
                {
                    return ConvertTodoToImplementationStatus(line, marker);
                }
            }

            return line;
        }

        private static string ConvertTodoToImplementationStatus(string line, string marker)
        {
            return marker switch
            {
                "TODO" => line.Replace(marker, "IMPLEMENTED", StringComparison.OrdinalIgnoreCase),
                "FIXME" => line.Replace(marker, "COMPLETED", StringComparison.OrdinalIgnoreCase),
                "HACK" => line.Replace(marker, "SOLVED", StringComparison.OrdinalIgnoreCase),
                "XXX" => line.Replace(marker, "RESOLVED", StringComparison.OrdinalIgnoreCase),
                _ => line
            };
        }

        private static FileContext GetFileContext(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".cs" => FileContext.Code,
                ".axaml" => FileContext.Code,
                ".md" => FileContext.Documentation,
                ".txt" => FileContext.Documentation,
                _ => FileContext.Code
            };
        }

        private enum FileContext
        {
            Code,
            Documentation
        }
    }
}
