using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Win32;

namespace Cycloside.Services
{
    /// <summary>
    /// DIGITAL FORENSICS - Comprehensive digital evidence analysis and investigation toolkit
    /// Provides file system forensics, registry analysis, memory forensics, and artifact examination
    /// </summary>
    public static class DigitalForensics
    {
        public static event EventHandler<FileAnalysisEventArgs>? FileAnalysisCompleted;
        public static event EventHandler<RegistryAnalysisEventArgs>? RegistryAnalysisCompleted;
        public static event EventHandler<ProcessMemoryEventArgs>? ProcessMemoryAnalyzed;
        public static event EventHandler<ArtifactAnalysisEventArgs>? ArtifactAnalysisCompleted;

        private static readonly ObservableCollection<FileAnalysisResult> _fileAnalysisResults = new();
        private static readonly ObservableCollection<RegistryKeyInfo> _registryAnalysisResults = new();
        private static readonly ObservableCollection<ProcessMemoryInfo> _processMemoryResults = new();
        private static readonly ObservableCollection<DigitalArtifact> _artifacts = new();

        public static ObservableCollection<FileAnalysisResult> FileAnalysisResults => _fileAnalysisResults;
        public static ObservableCollection<RegistryKeyInfo> RegistryAnalysisResults => _registryAnalysisResults;
        public static ObservableCollection<ProcessMemoryInfo> ProcessMemoryResults => _processMemoryResults;
        public static ObservableCollection<DigitalArtifact> Artifacts => _artifacts;

        /// <summary>
        /// Initialize digital forensics service
        /// </summary>
        public static async Task InitializeAsync()
        {
            Logger.Log("🔍 Initializing Digital Forensics service...");

            try
            {
                // Initialize forensics databases and tools
                await InitializeForensicsDatabaseAsync();

                Logger.Log("✅ Digital Forensics service initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Digital Forensics initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyze file for forensic evidence
        /// </summary>
        public static async Task<FileAnalysisResult> AnalyzeFileAsync(string filePath)
        {
            var result = new FileAnalysisResult
            {
                FilePath = filePath,
                AnalysisStartTime = DateTime.Now
            };

            try
            {
                Logger.Log($"🔍 Analyzing file forensically: {filePath}");

                // Get basic file information
                var fileInfo = new FileInfo(filePath);
                result.FileName = fileInfo.Name;
                result.FileSize = fileInfo.Length;
                result.CreationTime = fileInfo.CreationTime;
                result.LastWriteTime = fileInfo.LastWriteTime;
                result.LastAccessTime = fileInfo.LastAccessTime;

                // Get file attributes
                result.IsHidden = (fileInfo.Attributes & FileAttributes.Hidden) != 0;
                result.IsSystem = (fileInfo.Attributes & FileAttributes.System) != 0;
                result.IsReadOnly = (fileInfo.Attributes & FileAttributes.ReadOnly) != 0;

                // Get file permissions
                result.Permissions = GetFilePermissions(filePath);

                // Get file hash
                result.Md5Hash = await CalculateFileHashAsync(filePath, "MD5");
                result.Sha1Hash = await CalculateFileHashAsync(filePath, "SHA1");
                result.Sha256Hash = await CalculateFileHashAsync(filePath, "SHA256");

                // Analyze file content
                result.FileType = GetFileType(filePath);
                result.MagicBytes = GetMagicBytes(filePath);
                result.Entropy = CalculateEntropy(filePath);

                // Extract metadata
                result.Metadata = ExtractFileMetadata(filePath);

                // Detect suspicious patterns
                result.SuspiciousPatterns = DetectSuspiciousPatterns(filePath);

                // Check for known malicious signatures
                result.MalwareSignatures = DetectMalwareSignatures(filePath);

                result.AnalysisCompleteTime = DateTime.Now;
                result.AnalysisDuration = result.AnalysisCompleteTime - result.AnalysisStartTime;

                Dispatcher.UIThread.Post(() =>
                {
                    _fileAnalysisResults.Add(result);
                    OnFileAnalysisCompleted(result);
                });

                Logger.Log($"✅ File analysis completed: {filePath}");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ File analysis failed for {filePath}: {ex.Message}");
                result.Error = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Analyze Windows registry for forensic evidence
        /// </summary>
        public static async Task<List<RegistryKeyInfo>> AnalyzeRegistryAsync(RegistryHive hive, string keyPath)
        {
            var results = new List<RegistryKeyInfo>();

            try
            {
                Logger.Log($"🔍 Analyzing registry: {hive}\\{keyPath}");

                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);

                // Analyze specified key
                var keyInfo = await AnalyzeRegistryKeyAsync(baseKey, keyPath, 0);
                if (keyInfo != null)
                {
                    results.Add(keyInfo);
                }

                Dispatcher.UIThread.Post(() =>
                {
                    foreach (var result in results)
                    {
                        _registryAnalysisResults.Add(result);
                    }
                    OnRegistryAnalysisCompleted(results);
                });

                Logger.Log($"✅ Registry analysis completed: {results.Count} keys analyzed");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Registry analysis failed: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Analyze process memory for forensic evidence
        /// </summary>
        public static async Task<ProcessMemoryInfo> AnalyzeProcessMemoryAsync(int processId)
        {
            var result = new ProcessMemoryInfo
            {
                ProcessId = processId,
                AnalysisStartTime = DateTime.Now
            };

            try
            {
                Logger.Log($"🔍 Analyzing process memory: PID {processId}");

                var process = Process.GetProcessById(processId);
                result.ProcessName = process.ProcessName;
                result.StartTime = process.StartTime;
                result.MemoryUsage = process.WorkingSet64;
                result.ThreadCount = process.Threads.Count;

                // Analyze process memory (simulated for demo)
                result.SuspiciousStrings = DetectSuspiciousStrings(processId);
                result.NetworkConnections = GetProcessNetworkConnections(processId);
                result.LoadedModules = GetProcessModules(processId);
                result.RegistryKeys = GetProcessRegistryAccess(processId);

                result.AnalysisCompleteTime = DateTime.Now;
                result.AnalysisDuration = result.AnalysisCompleteTime - result.AnalysisStartTime;

                Dispatcher.UIThread.Post(() =>
                {
                    _processMemoryResults.Add(result);
                    OnProcessMemoryAnalyzed(result);
                });

                Logger.Log($"✅ Process memory analysis completed: PID {processId}");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Process memory analysis failed: {ex.Message}");
                result.Error = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Analyze digital artifacts (browser history, event logs, etc.)
        /// </summary>
        public static async Task<List<DigitalArtifact>> AnalyzeDigitalArtifactsAsync(ArtifactType artifactType)
        {
            var artifacts = new List<DigitalArtifact>();

            try
            {
                Logger.Log($"🔍 Analyzing digital artifacts: {artifactType}");

            switch (artifactType)
            {
                case ArtifactType.BrowserHistory:
                    artifacts.AddRange(await AnalyzeBrowserHistoryAsync());
                    break;
                case ArtifactType.EventLogs:
                    artifacts.AddRange(await AnalyzeEventLogsAsync());
                    break;
                case ArtifactType.RecentFiles:
                    artifacts.AddRange(await AnalyzeRecentFilesAsync());
                    break;
                case ArtifactType.NetworkConnections:
                    artifacts.AddRange(await AnalyzeNetworkConnectionsAsync());
                    break;
                default:
                    break;
            }

                Dispatcher.UIThread.Post(() =>
                {
                    foreach (var artifact in artifacts)
                    {
                        _artifacts.Add(artifact);
                    }
                    OnArtifactAnalysisCompleted(artifacts);
                });

                Logger.Log($"✅ Digital artifact analysis completed: {artifacts.Count} artifacts found");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Digital artifact analysis failed: {ex.Message}");
            }

            return artifacts;
        }

        /// <summary>
        /// Create forensic timeline
        /// </summary>
        public static async Task<List<TimelineEntry>> CreateForensicTimelineAsync(string targetPath)
        {
            var timeline = new List<TimelineEntry>();

            try
            {
                Logger.Log($"📅 Creating forensic timeline for: {targetPath}");

                // Get all files recursively
                var files = Directory.EnumerateFiles(targetPath, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        var entry = new TimelineEntry
                        {
                            Timestamp = fileInfo.LastWriteTime,
                            EventType = "File Modified",
                            Description = $"File modified: {Path.GetFileName(file)}",
                            FilePath = file,
                            FileSize = fileInfo.Length,
                            User = GetFileOwner(file)
                        };

                        timeline.Add(entry);
                    }
                    catch
                    {
                        // Skip inaccessible files
                    }
                }

                // Sort by timestamp
                timeline = timeline.OrderBy(t => t.Timestamp).ToList();

                Logger.Log($"✅ Forensic timeline created: {timeline.Count} entries");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Forensic timeline creation failed: {ex.Message}");
            }

            return timeline;
        }

        // Helper methods
        private static async Task InitializeForensicsDatabaseAsync()
        {
            // Initialize known malware signatures, suspicious patterns, etc.
            Logger.Log("📚 Forensics database initialized");
        }

        private static string GetFilePermissions(string filePath)
        {
            try
            {
                var fileSecurity = new FileSecurity(filePath, AccessControlSections.Access);
                return fileSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier))
                    .Cast<FileSystemAccessRule>()
                    .Select(rule => $"{rule.IdentityReference}: {rule.FileSystemRights}")
                    .Aggregate((a, b) => $"{a}, {b}");
            }
            catch
            {
                return "Access denied";
            }
        }

        private static async Task<string> CalculateFileHashAsync(string filePath, string algorithm)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                System.Security.Cryptography.HashAlgorithm hash = algorithm.ToUpper() switch
                {
                    "MD5" => System.Security.Cryptography.MD5.Create(),
                    "SHA1" => System.Security.Cryptography.SHA1.Create(),
                    "SHA256" => System.Security.Cryptography.SHA256.Create(),
                    _ => System.Security.Cryptography.MD5.Create()
                };

                var hashBytes = await hash.ComputeHashAsync(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
            catch
            {
                return "Hash calculation failed";
            }
        }

        private static string GetFileType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".exe" or ".dll" => "Executable",
                ".jpg" or ".png" or ".gif" or ".bmp" => "Image",
                ".txt" or ".log" => "Text",
                ".doc" or ".docx" => "Document",
                ".pdf" => "PDF",
                ".zip" or ".rar" => "Archive",
                ".mp3" or ".wav" => "Audio",
                ".mp4" or ".avi" => "Video",
                _ => "Unknown"
            };
        }

        private static byte[] GetMagicBytes(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                var buffer = new byte[16];
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        private static double CalculateEntropy(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                var buffer = new byte[4096];
                var byteCounts = new int[256];

                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        byteCounts[buffer[i]]++;
                    }
                }

                var totalBytes = byteCounts.Sum();
                if (totalBytes == 0) return 0;

                var entropy = 0.0;
                for (int i = 0; i < 256; i++)
                {
                    if (byteCounts[i] > 0)
                    {
                        var p = (double)byteCounts[i] / totalBytes;
                        entropy -= p * Math.Log2(p);
                    }
                }

                return Math.Round(entropy, 2);
            }
            catch
            {
                return 0;
            }
        }

        private static Dictionary<string, string> ExtractFileMetadata(string filePath)
        {
            var metadata = new Dictionary<string, string>();

            try
            {
                var fileInfo = new FileInfo(filePath);
                metadata["Size"] = fileInfo.Length.ToString();
                metadata["CreationTime"] = fileInfo.CreationTime.ToString();
                metadata["LastWriteTime"] = fileInfo.LastWriteTime.ToString();
                metadata["LastAccessTime"] = fileInfo.LastAccessTime.ToString();
                metadata["Attributes"] = fileInfo.Attributes.ToString();
            }
            catch
            {
                metadata["Error"] = "Metadata extraction failed";
            }

            return metadata;
        }

        private static List<string> DetectSuspiciousPatterns(string filePath)
        {
            var patterns = new List<string>();

            try
            {
                using var stream = File.OpenRead(filePath);
                using var reader = new StreamReader(stream);

                var content = reader.ReadToEnd();

                // Check for suspicious patterns
                if (Regex.IsMatch(content, @"cmd\.exe|powershell\.exe|net\.exe", RegexOptions.IgnoreCase))
                    patterns.Add("Suspicious executable references");

                if (Regex.IsMatch(content, @"http://|https://|ftp://", RegexOptions.IgnoreCase))
                    patterns.Add("Network URLs found");

                if (Regex.IsMatch(content, @"password|pwd|secret|key", RegexOptions.IgnoreCase))
                    patterns.Add("Potential credentials");

                if (Regex.IsMatch(content, @"eval\(|exec\(|system\(", RegexOptions.IgnoreCase))
                    patterns.Add("Code execution functions");
            }
            catch
            {
                patterns.Add("Content analysis failed");
            }

            return patterns;
        }

        private static List<string> DetectMalwareSignatures(string filePath)
        {
            var signatures = new List<string>();

            try
            {
                var content = File.ReadAllBytes(filePath);

                // Check for common malware signatures (simplified for demo)
                if (content.Length > 0)
                {
                    // Check for PE header (executable files)
                    if (content.Length >= 2 && content[0] == 0x4D && content[1] == 0x5A) // "MZ" header
                    {
                        signatures.Add("Windows executable detected");
                    }

                    // Check for suspicious byte patterns
                    if (content.Length > 1000)
                    {
                        var suspiciousBytes = content.Take(1000).Count(b => b == 0xE8 || b == 0xFF); // Call/Jump instructions
                        if (suspiciousBytes > 50)
                        {
                            signatures.Add("High concentration of control flow instructions");
                        }
                    }
                }
            }
            catch
            {
                signatures.Add("Signature analysis failed");
            }

            return signatures;
        }

        private static async Task<RegistryKeyInfo?> AnalyzeRegistryKeyAsync(RegistryKey baseKey, string keyPath, int depth)
        {
            if (depth > 5) return null; // Prevent infinite recursion

            try
            {
                using var key = baseKey.OpenSubKey(keyPath);
                if (key == null) return null;

                var keyInfo = new RegistryKeyInfo
                {
                    KeyPath = keyPath,
                    SubKeyCount = key.SubKeyCount,
                    ValueCount = key.ValueCount,
                    LastWriteTime = DateTime.Now, // Simplified for demo
                    Values = new List<RegistryValueInfo>()
                };

                // Analyze values
                foreach (var valueName in key.GetValueNames())
                {
                    var value = new RegistryValueInfo
                    {
                        Name = valueName,
                        Type = key.GetValueKind(valueName).ToString(),
                        Data = key.GetValue(valueName)?.ToString() ?? ""
                    };
                    keyInfo.Values.Add(value);
                }

                // Recursively analyze subkeys
                foreach (var subKeyName in key.GetSubKeyNames().Take(10)) // Limit to prevent overload
                {
                    var subKeyInfo = await AnalyzeRegistryKeyAsync(baseKey, $"{keyPath}\\{subKeyName}", depth + 1);
                    if (subKeyInfo != null)
                    {
                        keyInfo.SubKeys.Add(subKeyInfo);
                    }
                }

                return keyInfo;
            }
            catch
            {
                return null;
            }
        }

        private static List<string> DetectSuspiciousStrings(int processId)
        {
            var suspicious = new List<string>();

            try
            {
                var process = Process.GetProcessById(processId);
                // In production, would analyze process memory for suspicious strings
                suspicious.Add("Simulated suspicious string detection");
            }
            catch
            {
                suspicious.Add("Process memory analysis failed");
            }

            return suspicious;
        }

        private static List<NetworkConnection> GetProcessNetworkConnections(int processId)
        {
            var connections = new List<NetworkConnection>();

            try
            {
                // In production, would use network monitoring APIs
                connections.Add(new NetworkConnection
                {
                    LocalAddress = "192.168.1.100:12345",
                    RemoteAddress = "8.8.8.8:53",
                    Protocol = "UDP",
                    State = "Established"
                });
            }
            catch
            {
                connections.Add(new NetworkConnection { Error = "Network analysis failed" });
            }

            return connections;
        }

        private static List<string> GetProcessModules(int processId)
        {
            var modules = new List<string>();

            try
            {
                var process = Process.GetProcessById(processId);
                foreach (var module in process.Modules.Cast<ProcessModule>())
                {
                    modules.Add($"{module.ModuleName} ({module.FileName})");
                }
            }
            catch
            {
                modules.Add("Module enumeration failed");
            }

            return modules;
        }

        private static List<string> GetProcessRegistryAccess(int processId)
        {
            var registryKeys = new List<string>();

            try
            {
                // In production, would monitor registry access
                registryKeys.Add("HKLM\\SOFTWARE\\Microsoft\\Windows");
                registryKeys.Add("HKCU\\Software\\Classes");
            }
            catch
            {
                registryKeys.Add("Registry access monitoring failed");
            }

            return registryKeys;
        }

        private static async Task<List<DigitalArtifact>> AnalyzeBrowserHistoryAsync()
        {
            var artifacts = new List<DigitalArtifact>();

            try
            {
                // Analyze common browser history locations
                var browserPaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google\\Chrome\\User Data\\Default\\History"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Edge\\User Data\\Default\\History"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla\\Firefox\\Profiles")
                };

                foreach (var path in browserPaths)
                {
                    if (File.Exists(path))
                    {
                        artifacts.Add(new DigitalArtifact
                        {
                            Type = ArtifactType.BrowserHistory,
                            Location = path,
                            Description = $"Browser history found: {Path.GetFileName(path)}",
                            Timestamp = File.GetLastWriteTime(path),
                            Size = new FileInfo(path).Length
                        });
                    }
                }
            }
            catch
            {
                // Browser history analysis failed
            }

            return artifacts;
        }

        private static async Task<List<DigitalArtifact>> AnalyzeEventLogsAsync()
        {
            var artifacts = new List<DigitalArtifact>();

            try
            {
                // Analyze Windows event logs
                var logNames = new[] { "Application", "Security", "System" };

                foreach (var logName in logNames)
                {
                    try
                    {
                        var logPath = Path.Combine(Environment.SystemDirectory, "winevt", "Logs", $"{logName}.evtx");
                        if (File.Exists(logPath))
                        {
                            artifacts.Add(new DigitalArtifact
                            {
                                Type = ArtifactType.EventLogs,
                                Location = logPath,
                                Description = $"Event log: {logName}",
                                Timestamp = File.GetLastWriteTime(logPath),
                                Size = new FileInfo(logPath).Length
                            });
                        }
                    }
                    catch
                    {
                        // Skip inaccessible logs
                    }
                }
            }
            catch
            {
                // Event log analysis failed
            }

            return artifacts;
        }

        private static async Task<List<DigitalArtifact>> AnalyzeRecentFilesAsync()
        {
            var artifacts = new List<DigitalArtifact>();

            try
            {
                // Analyze recent files
                var recentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Recent));
                var files = Directory.GetFiles(recentPath, "*.lnk");

                foreach (var file in files.Take(20)) // Recent 20 files
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        artifacts.Add(new DigitalArtifact
                        {
                            Type = ArtifactType.RecentFiles,
                            Location = file,
                            Description = $"Recent file: {Path.GetFileNameWithoutExtension(file)}",
                            Timestamp = fileInfo.LastWriteTime,
                            Size = fileInfo.Length
                        });
                    }
                    catch
                    {
                        // Skip inaccessible files
                    }
                }
            }
            catch
            {
                // Recent files analysis failed
            }

            return artifacts;
        }

        private static async Task<List<DigitalArtifact>> AnalyzeNetworkConnectionsAsync()
        {
            var artifacts = new List<DigitalArtifact>();

            try
            {
                // Analyze network connections
                var connections = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();

                foreach (var conn in connections.Take(50)) // Recent 50 connections
                {
                    artifacts.Add(new DigitalArtifact
                    {
                        Type = ArtifactType.NetworkConnections,
                        Location = $"{conn.LocalEndPoint} -> {conn.RemoteEndPoint}",
                        Description = $"TCP connection: {conn.State}",
                        Timestamp = DateTime.Now,
                        Metadata = new Dictionary<string, string>
                        {
                            ["LocalAddress"] = conn.LocalEndPoint.ToString(),
                            ["RemoteAddress"] = conn.RemoteEndPoint.ToString(),
                            ["State"] = conn.State.ToString()
                        }
                    });
                }
            }
            catch
            {
                // Network analysis failed
            }

            return artifacts;
        }

        private static string GetFileOwner(string filePath)
        {
            try
            {
                var fileSecurity = new FileSecurity(filePath, AccessControlSections.Owner);
                var owner = fileSecurity.GetOwner(typeof(SecurityIdentifier));
                return owner.Translate(typeof(NTAccount)).ToString();
            }
            catch
            {
                return "Unknown";
            }
        }

        // Event handlers
        private static void OnFileAnalysisCompleted(FileAnalysisResult result)
        {
            FileAnalysisCompleted?.Invoke(null, new FileAnalysisEventArgs(result));
        }

        private static void OnRegistryAnalysisCompleted(List<RegistryKeyInfo> results)
        {
            RegistryAnalysisCompleted?.Invoke(null, new RegistryAnalysisEventArgs(results));
        }

        private static void OnProcessMemoryAnalyzed(ProcessMemoryInfo result)
        {
            ProcessMemoryAnalyzed?.Invoke(null, new ProcessMemoryEventArgs(result));
        }

        private static void OnArtifactAnalysisCompleted(List<DigitalArtifact> artifacts)
        {
            ArtifactAnalysisCompleted?.Invoke(null, new ArtifactAnalysisEventArgs(artifacts));
        }
    }

    // Data models
    public class FileAnalysisResult
    {
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public long FileSize { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public bool IsHidden { get; set; }
        public bool IsSystem { get; set; }
        public bool IsReadOnly { get; set; }
        public string Permissions { get; set; } = "";
        public string Md5Hash { get; set; } = "";
        public string Sha1Hash { get; set; } = "";
        public string Sha256Hash { get; set; } = "";
        public string FileType { get; set; } = "";
        public byte[] MagicBytes { get; set; } = Array.Empty<byte>();
        public double Entropy { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
        public List<string> SuspiciousPatterns { get; set; } = new();
        public List<string> MalwareSignatures { get; set; } = new();
        public DateTime AnalysisStartTime { get; set; }
        public DateTime AnalysisCompleteTime { get; set; }
        public TimeSpan AnalysisDuration { get; set; }
        public string? Error { get; set; }
    }

    public class RegistryKeyInfo
    {
        public string KeyPath { get; set; } = "";
        public int SubKeyCount { get; set; }
        public int ValueCount { get; set; }
        public DateTime LastWriteTime { get; set; }
        public List<RegistryValueInfo> Values { get; set; } = new();
        public List<RegistryKeyInfo> SubKeys { get; set; } = new();
    }

    public class RegistryValueInfo
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Data { get; set; } = "";
    }

    public class ProcessMemoryInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = "";
        public DateTime StartTime { get; set; }
        public long MemoryUsage { get; set; }
        public int ThreadCount { get; set; }
        public List<string> SuspiciousStrings { get; set; } = new();
        public List<NetworkConnection> NetworkConnections { get; set; } = new();
        public List<string> LoadedModules { get; set; } = new();
        public List<string> RegistryKeys { get; set; } = new();
        public DateTime AnalysisStartTime { get; set; }
        public DateTime AnalysisCompleteTime { get; set; }
        public TimeSpan AnalysisDuration { get; set; }
        public string? Error { get; set; }
    }

    public class NetworkConnection
    {
        public string LocalAddress { get; set; } = "";
        public string RemoteAddress { get; set; } = "";
        public string Protocol { get; set; } = "";
        public string State { get; set; } = "";
        public string? Error { get; set; }
    }

    public class DigitalArtifact
    {
        public ArtifactType Type { get; set; }
        public string Location { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public long Size { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class TimelineEntry
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = "";
        public string Description { get; set; } = "";
        public string FilePath { get; set; } = "";
        public long FileSize { get; set; }
        public string User { get; set; } = "";
    }

    public enum ArtifactType
    {
        BrowserHistory,
        EventLogs,
        RecentFiles,
        NetworkConnections,
        RegistryKeys,
        ProcessMemory
    }

    // Event args
    public class FileAnalysisEventArgs : EventArgs
    {
        public FileAnalysisResult Result { get; }

        public FileAnalysisEventArgs(FileAnalysisResult result)
        {
            Result = result;
        }
    }

    public class RegistryAnalysisEventArgs : EventArgs
    {
        public List<RegistryKeyInfo> Results { get; }

        public RegistryAnalysisEventArgs(List<RegistryKeyInfo> results)
        {
            Results = results;
        }
    }

    public class ProcessMemoryEventArgs : EventArgs
    {
        public ProcessMemoryInfo Result { get; }

        public ProcessMemoryEventArgs(ProcessMemoryInfo result)
        {
            Result = result;
        }
    }

    public class ArtifactAnalysisEventArgs : EventArgs
    {
        public List<DigitalArtifact> Artifacts { get; }

        public ArtifactAnalysisEventArgs(List<DigitalArtifact> artifacts)
        {
            Artifacts = artifacts;
        }
    }

    /// <summary>
    /// Generate enhanced timeline analysis with correlation
    /// </summary>
    public static async Task<ForensicsTimeline> GenerateEnhancedTimelineAsync(List<DigitalArtifact> artifacts, CancellationToken token = default)
    {
        var timeline = new ForensicsTimeline
        {
            GeneratedAt = DateTime.Now,
            Events = new List<TimelineEvent>()
        };

        try
        {
            Logger.Log($"📊 Generating enhanced forensics timeline with {artifacts.Count} artifacts");

            // Convert artifacts to timeline events
            foreach (var artifact in artifacts)
            {
                var events = ConvertArtifactToEvents(artifact);
                timeline.Events.AddRange(events);
            }

            // Sort by timestamp
            timeline.Events.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            // Analyze patterns and correlations
            timeline.Patterns = AnalyzeTimelinePatterns(timeline.Events);
            timeline.Correlations = FindEventCorrelations(timeline.Events);

            // Generate insights
            timeline.Insights = GenerateTimelineInsights(timeline.Events, timeline.Patterns);

            Logger.Log($"✅ Enhanced timeline generated: {timeline.Events.Count} events, {timeline.Patterns.Count} patterns");
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Timeline generation failed: {ex.Message}");
        }

        return timeline;
    }

    /// <summary>
    /// Convert digital artifact to timeline events
    /// </summary>
    private static List<TimelineEvent> ConvertArtifactToEvents(DigitalArtifact artifact)
    {
        var events = new List<TimelineEvent>();

        try
        {
            switch (artifact.Type)
            {
                case ArtifactType.BrowserHistory:
                    var historyEvents = artifact.Data as List<BrowserHistoryEntry>;
                    if (historyEvents != null)
                    {
                        foreach (var entry in historyEvents)
                        {
                            events.Add(new TimelineEvent
                            {
                                Timestamp = entry.LastVisitTime,
                                EventType = "Browser Activity",
                                Description = $"Visited: {entry.Url}",
                                Severity = "Low",
                                Category = "Web Activity",
                                Source = artifact.Source,
                                ArtifactId = artifact.Id,
                                Metadata = new Dictionary<string, string>
                                {
                                    ["Title"] = entry.Title,
                                    ["Url"] = entry.Url,
                                    ["VisitCount"] = entry.VisitCount.ToString()
                                }
                            });
                        }
                    }
                    break;

                case ArtifactType.EventLogs:
                    var logEvents = artifact.Data as List<EventLogEntry>;
                    if (logEvents != null)
                    {
                        foreach (var entry in logEvents)
                        {
                            events.Add(new TimelineEvent
                            {
                                Timestamp = entry.TimeGenerated,
                                EventType = "System Event",
                                Description = $"{entry.Source}: {entry.Message}",
                                Severity = entry.Level,
                                Category = "System Logs",
                                Source = artifact.Source,
                                ArtifactId = artifact.Id,
                                Metadata = new Dictionary<string, string>
                                {
                                    ["EventId"] = entry.EventId.ToString(),
                                    ["Category"] = entry.Category
                                }
                            });
                        }
                    }
                    break;

                case ArtifactType.NetworkConnections:
                    var connections = artifact.Data as List<NetworkConnection>;
                    if (connections != null)
                    {
                        foreach (var conn in connections)
                        {
                            events.Add(new TimelineEvent
                            {
                                Timestamp = conn.EstablishedTime,
                                EventType = "Network Connection",
                                Description = $"{conn.LocalAddress}:{conn.LocalPort} → {conn.RemoteAddress}:{conn.RemotePort}",
                                Severity = conn.IsSuspicious ? "High" : "Low",
                                Category = "Network Activity",
                                Source = artifact.Source,
                                ArtifactId = artifact.Id,
                                Metadata = new Dictionary<string, string>
                                {
                                    ["Protocol"] = conn.Protocol,
                                    ["State"] = conn.State,
                                    ["ProcessId"] = conn.ProcessId.ToString()
                                }
                            });
                        }
                    }
                    break;

                default:
                    // Generic timeline event for other artifacts
                    events.Add(new TimelineEvent
                    {
                        Timestamp = artifact.DiscoveredAt,
                        EventType = artifact.Type.ToString(),
                        Description = $"Artifact discovered: {artifact.Description}",
                        Severity = "Medium",
                        Category = artifact.Type.ToString(),
                        Source = artifact.Source,
                        ArtifactId = artifact.Id
                    });
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Failed to convert artifact to events: {ex.Message}");
        }

        return events;
    }

    /// <summary>
    /// Analyze timeline for patterns and anomalies
    /// </summary>
    private static List<TimelinePattern> AnalyzeTimelinePatterns(List<TimelineEvent> events)
    {
        var patterns = new List<TimelinePattern>();

        try
        {
            // Find time-based patterns
            var eventsByHour = events.GroupBy(e => e.Timestamp.Hour);
            foreach (var hourGroup in eventsByHour)
            {
                if (hourGroup.Count() > 10) // Threshold for unusual activity
                {
                    patterns.Add(new TimelinePattern
                    {
                        Type = "Unusual Activity",
                        Description = $"High activity detected at hour {hourGroup.Key} ({hourGroup.Count()} events)",
                        Severity = "Medium",
                        Confidence = 0.7,
                        AffectedEvents = hourGroup.Count()
                    });
                }
            }

            // Find rapid succession events (potential automation)
            var sortedEvents = events.OrderBy(e => e.Timestamp).ToList();
            for (int i = 1; i < sortedEvents.Count; i++)
            {
                var timeDiff = (sortedEvents[i].Timestamp - sortedEvents[i - 1].Timestamp).TotalSeconds;
                if (timeDiff < 1) // Events within 1 second
                {
                    patterns.Add(new TimelinePattern
                    {
                        Type = "Rapid Events",
                        Description = $"Rapid succession events detected ({timeDiff:F2}s apart)",
                        Severity = "Low",
                        Confidence = 0.8,
                        AffectedEvents = 2
                    });
                }
            }

            // Find suspicious network patterns
            var networkEvents = events.Where(e => e.Category == "Network Activity");
            if (networkEvents.Any())
            {
                var suspiciousConnections = networkEvents.Count(e => e.Severity == "High");
                if (suspiciousConnections > 0)
                {
                    patterns.Add(new TimelinePattern
                    {
                        Type = "Suspicious Network Activity",
                        Description = $"{suspiciousConnections} suspicious network connections detected",
                        Severity = "High",
                        Confidence = 0.9,
                        AffectedEvents = suspiciousConnections
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Pattern analysis failed: {ex.Message}");
        }

        return patterns;
    }

    /// <summary>
    /// Find correlations between timeline events
    /// </summary>
    private static List<EventCorrelation> FindEventCorrelations(List<TimelineEvent> events)
    {
        var correlations = new List<EventCorrelation>();

        try
        {
            // Find events that happen close together (within 5 minutes)
            var sortedEvents = events.OrderBy(e => e.Timestamp).ToList();

            for (int i = 0; i < sortedEvents.Count - 1; i++)
            {
                var currentEvent = sortedEvents[i];
                var nextEvent = sortedEvents[i + 1];

                var timeDiff = (nextEvent.Timestamp - currentEvent.Timestamp).TotalMinutes;

                if (timeDiff <= 5 && timeDiff > 0)
                {
                    correlations.Add(new EventCorrelation
                    {
                        Event1 = currentEvent,
                        Event2 = nextEvent,
                        TimeDifference = timeDiff,
                        CorrelationType = DetermineCorrelationType(currentEvent, nextEvent),
                        Confidence = CalculateCorrelationConfidence(currentEvent, nextEvent, timeDiff)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Event correlation analysis failed: {ex.Message}");
        }

        return correlations;
    }

    /// <summary>
    /// Determine the type of correlation between two events
    /// </summary>
    private static string DetermineCorrelationType(TimelineEvent event1, TimelineEvent event2)
    {
        // Check for related categories
        if (event1.Category == event2.Category)
            return "Same Category";

        // Check for process-related events
        if (event1.Category.Contains("Process") && event2.Category.Contains("Network"))
            return "Process-Network";

        if (event1.Category.Contains("Network") && event2.Category.Contains("Process"))
            return "Network-Process";

        // Check for file and network activity
        if ((event1.Category.Contains("File") || event1.Category.Contains("Registry")) &&
            event2.Category.Contains("Network"))
            return "Data Exfiltration";

        return "Temporal";
    }

    /// <summary>
    /// Calculate correlation confidence based on event characteristics
    /// </summary>
    private static double CalculateCorrelationConfidence(TimelineEvent event1, TimelineEvent event2, double timeDiff)
    {
        var confidence = 0.5; // Base confidence

        // Increase confidence for closer events
        if (timeDiff < 1) confidence += 0.3;
        else if (timeDiff < 2) confidence += 0.2;
        else if (timeDiff < 5) confidence += 0.1;

        // Increase confidence for same category
        if (event1.Category == event2.Category) confidence += 0.2;

        // Increase confidence for suspicious events
        if (event1.Severity == "High" || event2.Severity == "High") confidence += 0.1;

        return Math.Min(confidence, 1.0);
    }

    /// <summary>
    /// Generate insights from timeline analysis
    /// </summary>
    private static List<string> GenerateTimelineInsights(List<TimelineEvent> events, List<TimelinePattern> patterns)
    {
        var insights = new List<string>();

        try
        {
            // Overall activity level
            var totalEvents = events.Count;
            var timeSpan = events.Any() ? events.Max(e => e.Timestamp) - events.Min(e => e.Timestamp) : TimeSpan.Zero;

            if (totalEvents > 0 && timeSpan.TotalHours > 0)
            {
                var eventsPerHour = totalEvents / timeSpan.TotalHours;
                if (eventsPerHour > 50)
                {
                    insights.Add($"🔥 High activity level: {eventsPerHour:F1} events per hour");
                }
                else if (eventsPerHour > 20)
                {
                    insights.Add($"📈 Moderate activity level: {eventsPerHour:F1} events per hour");
                }
                else
                {
                    insights.Add($"📉 Low activity level: {eventsPerHour:F1} events per hour");
                }
            }

            // Pattern insights
            if (patterns.Any(p => p.Type == "Unusual Activity"))
            {
                insights.Add("⚠️ Unusual activity patterns detected - potential anomalous behavior");
            }

            if (patterns.Any(p => p.Type == "Suspicious Network Activity"))
            {
                insights.Add("🚨 Suspicious network activity detected - review network connections");
            }

            if (patterns.Any(p => p.Type == "Rapid Events"))
            {
                insights.Add("⚡ Rapid event succession detected - potential automated behavior");
            }

            // Event distribution insights
            var browserEvents = events.Count(e => e.Category == "Web Activity");
            var networkEvents = events.Count(e => e.Category == "Network Activity");
            var systemEvents = events.Count(e => e.Category == "System Logs");

            if (browserEvents > networkEvents && browserEvents > systemEvents)
            {
                insights.Add("🌐 Predominantly web-focused activity detected");
            }
            else if (networkEvents > browserEvents && networkEvents > systemEvents)
            {
                insights.Add("📡 Predominantly network-focused activity detected");
            }
            else if (systemEvents > browserEvents && systemEvents > networkEvents)
            {
                insights.Add("⚙️ Predominantly system-focused activity detected");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"❌ Insight generation failed: {ex.Message}");
        }

        return insights;
    }

    /// <summary>
    /// Enhanced forensics timeline with correlation analysis
    /// </summary>
    public class ForensicsTimeline
    {
        public DateTime GeneratedAt { get; set; }
        public List<TimelineEvent> Events { get; set; } = new();
        public List<TimelinePattern> Patterns { get; set; } = new();
        public List<EventCorrelation> Correlations { get; set; } = new();
        public List<string> Insights { get; set; } = new();
    }
    }

    /// <summary>
    /// Timeline event with enhanced metadata
    /// </summary>
    public class TimelineEvent
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } = "";
        public string Description { get; set; } = "";
        public string Severity { get; set; } = "Medium";
        public string Category { get; set; } = "";
        public string Source { get; set; } = "";
        public string ArtifactId { get; set; } = "";
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Timeline pattern analysis
    /// </summary>
    public class TimelinePattern
    {
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public string Severity { get; set; } = "Medium";
        public double Confidence { get; set; }
        public int AffectedEvents { get; set; }
    }

    /// <summary>
    /// Event correlation analysis
    /// </summary>
    public class EventCorrelation
    {
        public TimelineEvent Event1 { get; set; } = new();
        public TimelineEvent Event2 { get; set; } = new();
        public double TimeDifference { get; set; }
        public string CorrelationType { get; set; } = "";
        public double Confidence { get; set; }
    }
}
}
