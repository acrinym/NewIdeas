using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
// PowerShell automation namespace removed - using Process.Start instead
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Cycloside.Services
{
    /// <summary>
    /// POWEROSSHELL MANAGER - Advanced PowerShell detection, installation, and elevation
    /// Detects newest PowerShell, handles elevation, and provides automated installation
    /// </summary>
    public static class PowerShellManager
    {
        private static string? _powerShellPath;
        private static Version? _powerShellVersion;
        private static bool _isAdminMode;
        private static bool _isInitialized;

        public static event EventHandler<string>? StatusChanged;
        public static event EventHandler<bool>? ElevationStatusChanged;

        public static bool IsInitialized => _isInitialized;
        public static bool IsElevated => _isAdminMode;
        public static Version? Version => _powerShellVersion;
        public static string? PowerShellPath => _powerShellPath;
        public static bool IsPowerShellAvailable => !string.IsNullOrEmpty(_powerShellPath);

        /// <summary>
        /// Initialize PowerShell Manager with detection and elevation checking
        /// </summary>
        public static async Task InitializeAsync()
        {
            Logger.Log("🔧 Initializing PowerShell Manager...");

            try
            {
                // Check admin elevation first
                _isAdminMode = IsRunningAsAdmin();
                OnElevationStatusChanged();

                // Detect PowerShell installations
                await DetectPowerShellAsync();

                // If no PowerShell or outdated, show options
                if (!IsPowerShellAvailable)
                {
                    LogStatus("⚠️ PowerShell not detected - Installation required");
                }
                else if (_powerShellVersion != null && IsPowerShellOutdated())
                {
                    LogStatus($"⚠️ PowerShell {_powerShellVersion} is outdated - Update available");
                }
                else
                {
                    LogStatus($"✅ PowerShell {_powerShellVersion} detected and ready");
                }

                _isInitialized = true;
                Logger.Log("🔧 PowerShell Manager initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ PowerShell Manager initialization failed: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Detect all PowerShell installations and select the newest version
        /// </summary>
        private static async Task DetectPowerShellAsync()
        {
            var powershellPaths = new List<(string path, Version version)>();

            // Common PowerShell installation locations
            var searchPaths = new[]
            {
                @"C:\Program Files\PowerShell\7\pwsh.exe",      // PowerShell 7+
                @"C:\Program Files\PowerShell\6\pwsh.exe",      // PowerShell 6
                @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe", // Windows PowerShell
                @"C:\Users\{0}\AppData\Local\Microsoft\WindowsApps\pwsh.exe", // PowerShell Store version
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\*\Common7\Tools\pwsh.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2022\*\Common7\Tools\pwsh.exe"
            };

            // Replace environment variables
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            searchPaths = searchPaths.Select(path => path.Replace("{0}", Path.GetFileName(userProfile))).ToArray();

            foreach (var path in searchPaths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        var version = await GetPowerShellVersionAsync(path);
                        if (version != null)
                        {
                            powershellPaths.Add((path, version));
                            Logger.Log($"🔍 Found PowerShell: {path} (v{version})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"⚠️ Error checking {path}: {ex.Message}");
                }
            }

            // Select the newest version
            if (powershellPaths.Any())
            {
                var newest = powershellPaths.OrderByDescending(x => x.version).First();
                _powerShellPath = newest.path;
                _powerShellVersion = newest.version;

                LogStatus($"💻 PowerShell {_powerShellVersion} selected: {Path.GetFileName(_powerShellPath)}");
            }
            else
            {
                _powerShellPath = null;
                _powerShellVersion = null;
            }
        }

        /// <summary>
        /// Get PowerShell version from executable
        /// </summary>
        private static async Task<Version?> GetPowerShellVersionAsync(string path)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "-Command \"$PSVersionTable.PSVersion\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var process = Process.Start(startInfo)!;
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    // Parse version from output like: "7.4.0"
                    var versionString = output.Trim().Replace("\"", "");
                    if (Version.TryParse(versionString, out var version))
                    {
                        return version;
                    }
                }

                // Fallback: try to get version from file info
                var fileVer = FileVersionInfo.GetVersionInfo(path);
                if (!string.IsNullOrEmpty(fileVer.FileVersion) &&
                    Version.TryParse(fileVer.FileVersion, out var fileVersion))
                {
                    return fileVersion;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Error getting PowerShell version from {path}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Check if PowerShell is outdated (older than PowerShell 7.0)
        /// </summary>
        private static bool IsPowerShellOutdated()
        {
            return _powerShellVersion != null && _powerShellVersion < new Version(7, 0);
        }

        /// <summary>
        /// Launch PowerShell command with elevation if needed
        /// </summary>
        public static async Task<string?> ExecutePowerShellCommandAsync(string command, bool requireElevation = false)
        {
            if (!IsPowerShellAvailable)
            {
                LogStatus("❌ PowerShell not available - Install required");
                return null;
            }

            if (requireElevation && !_isAdminMode)
            {
                LogStatus("🔒 Elevation required for command");

                var result = await ElevateToAdminAsync();
                if (!result)
                {
                    LogStatus("❌ Failed to elevate - Command cancelled");
                    return null;
                }
            }

            try
            {
                LogStatus($"⚡ Executing PowerShell: {command}");

                var result = await ExecuteCommandAsync(_powerShellPath!, command);

                LogStatus("✅ PowerShell command completed");
                return result;
            }
            catch (Exception ex)
            {
                LogStatus($"❌ PowerShell execution failed: {ex.Message}");
                Logger.Log($"PowerShell error: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Execute command and return output
        /// </summary>
        private static async Task<string> ExecuteCommandAsync(string executable, string command)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = $"-Command \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var process = Process.Start(startInfo)!;
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Command failed with exit code {process.ExitCode}: {error}");
            }

            return output;
        }

        /// <summary>
        /// Check if current process is running with admin privileges
        /// </summary>
        public static bool IsRunningAsAdmin()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Elevate to admin privileges
        /// </summary>
        public static async Task<bool> ElevateToAdminAsync()
        {
            try
            {
                LogStatus("🔒 Requesting elevation to admin...");

                var startInfo = new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath!,
                    Arguments = "--elevate",
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = Environment.CurrentDirectory
                };

                using var process = Process.Start(startInfo)!;
                await process.WaitForExitAsync();

                var success = process.ExitCode == 0;

                if (success)
                {
                    _isAdminMode = true;
                    OnElevationStatusChanged();
                    LogStatus("✅ Successfully elevated to admin");
                }
                else
                {
                    LogStatus("❌ Elevation failed - User denied or error occurred");
                }

                return success;
            }
            catch (Exception ex)
            {
                LogStatus($"❌ Elevation failed: {ex.Message}");
                Logger.Log($"Elevation error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Install PowerShell automatically
        /// </summary>
        public static async Task<bool> InstallPowerShellAsync()
        {
            try
            {
                LogStatus("📥 Installing PowerShell...");

                // Check if we have PowerShell Core MSI available
                var msiPaths = new[]
                {
                    @"C:\temp\PowerShell-7.x.x-win-x64.msi",
                    @"C:\Downloads\PowerShell-7.x.x-win-x64.msi",
                    Path.Combine(Path.GetTempPath(), "PowerShell.msi")
                };

                string? msiPath = null;
                foreach (var path in msiPaths)
                {
                    if (File.Exists(path))
                    {
                        msiPath = path;
                        break;
                    }
                }

                if (msiPath == null)
                {
                    // Download PowerShell
                    LogStatus("🌐 Downloading PowerShell installer...");
                    msiPath = await DownloadPowerShellInstallerAsync();
                }

                if (msiPath != null)
                {
                    // Install using MSI
                    LogStatus("⚙️ Installing PowerShell from MSI...");
                    return await InstallPowerShellFromMsiAsync(msiPath);
                }
                else
                {
                    LogStatus("❌ Failed to obtain PowerShell installer");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogStatus($"❌ PowerShell installation failed: {ex.Message}");
                Logger.Log($"Installation error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Download PowerShell installer
        /// </summary>
        private static Task<string?> DownloadPowerShellInstallerAsync()
        {
            try
            {
                // This would require HttpClient and specific PowerShell download URLs
                // For now, return null to indicate download not implemented
                LogStatus("⚠️ PowerShell download not implemented - Manual installation required");

                // In production, this would:
                // 1. Query PowerShell GitHub releases API
                // 2. Download latest MSI for user's architecture
                // 3. Save to temp directory

                return Task.FromResult<string?>(null);
            }
            catch (Exception ex)
            {
                Logger.Log($"Download error: {ex.Message}");
                return Task.FromResult<string?>(null);
            }
        }

        /// <summary>
        /// Install PowerShell from MSI package
        /// </summary>
        private static async Task<bool> InstallPowerShellFromMsiAsync(string msiPath)
        {
            try
            {
                if (!File.Exists(msiPath))
                {
                    LogStatus($"❌ MSI file not found: {msiPath}");
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "msiexec.exe",
                    Arguments = $"/i \"{msiPath}\" /quiet /norestart",
                    UseShellExecute = true,
                    Verb = "runas", // Requires elevation
                    WorkingDirectory = Path.GetDirectoryName(msiPath)
                };

                LogStatus("⚙️ Installing PowerShell (silent mode)...");

                using var process = Process.Start(startInfo)!;
                await process.WaitForExitAsync();

                var success = process.ExitCode == 0;

                if (success)
                {
                    LogStatus("✅ PowerShell installation completed");
                    // Re-detect PowerShell after installation
                    await DetectPowerShellAsync();
                    return true;
                }
                else
                {
                    LogStatus($"❌ PowerShell installation failed (exit code: {process.ExitCode})");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogStatus($"❌ PowerShell MSI installation error: {ex.Message}");
                LogStatus($"MSI Error details: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Execute PowerShell script file
        /// </summary>
        public static async Task<string?> ExecutePowerShellScriptAsync(string scriptPath, bool requireElevation = false)
        {
            if (!File.Exists(scriptPath))
            {
                LogStatus($"❌ Script file not found: {scriptPath}");
                return null;
            }

            var command = $"-File \"{scriptPath}\"";
            return await ExecutePowerShellCommandAsync(command, requireElevation);
        }

        /// <summary>
        /// Execute PowerShell script content
        /// </summary>
        public static async Task<string?> ExecutePowerShellCodeAsync(string scriptContent, bool requireElevation = false)
        {
            if (string.IsNullOrWhiteSpace(scriptContent))
            {
                LogStatus("❌ Empty script content");
                return null;
            }

            var command = $"-Command \"{scriptContent.Replace("\"", "\\\"").Replace("\n", "; ")}\"";
            return await ExecutePowerShellCommandAsync(command, requireElevation);
        }

        /// <summary>
        /// Get PowerShell modules/snapins
        /// </summary>
        public static async Task<string?> GetPowerShellModulesAsync()
        {
            return await ExecutePowerShellCommandAsync("Get-Module -Available", false);
        }

        /// <summary>
        /// Get PowerShell execution policy
        /// </summary>
        public static async Task<string?> GetExecutionPolicyAsync()
        {
            return await ExecutePowerShellCommandAsync("Get-ExecutionPolicy", false);
        }

        /// <summary>
        /// Set PowerShell execution policy
        /// </summary>
        public static async Task<bool> SetExecutionPolicyAsync(string policy, bool requireElevation = true)
        {
            var command = $"Set-ExecutionPolicy {policy} -Force";
            var result = await ExecutePowerShellCommandAsync(command, requireElevation);
            return result != null;
        }

        // Event handlers
        private static void LogStatus(string message)
        {
            Logger.Log($"🔧 PowerShell Manager: {message}");
            StatusChanged?.Invoke(null, message);
        }

        private static void OnElevationStatusChanged()
        {
            ElevationStatusChanged?.Invoke(null, _isAdminMode);
        }
    }
}
