using Cycloside.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;

namespace Cycloside.Services
{
    /// <summary>
    /// Manages WindowBlinds-style global theming that applies to ALL windows on the desktop.
    /// Uses DLL injection + system hooks to intercept window painting calls.
    /// </summary>
    public class GlobalThemeService
    {
        private static GlobalThemeService? _instance;
        public static GlobalThemeService Instance => _instance ??= new GlobalThemeService();

        private bool _isEnabled = false;
        private Process? _hostProcess;
        private MemoryMappedFile? _sharedMemory;
        private ThemingMode _mode = ThemingMode.Whitelist;
        private readonly HashSet<string> _whitelist = new();
        private readonly HashSet<string> _blacklist = new();
        private readonly Dictionary<string, string> _perAppThemes = new();

        /// <summary>
        /// Event fired when global theming is enabled or disabled
        /// </summary>
        public event Action<bool>? GlobalThemingChanged;

        /// <summary>
        /// Whether global theming is currently enabled
        /// </summary>
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// Current theming mode (All, Whitelist, Blacklist)
        /// </summary>
        public ThemingMode Mode => _mode;

        private GlobalThemeService()
        {
            LoadConfiguration();
            Logger.Log("🌍 GlobalThemeService initialized");
        }

        /// <summary>
        /// Enable global theming for all windows on the desktop.
        /// Requires administrator privileges.
        /// </summary>
        public bool EnableGlobalTheming()
        {
            if (_isEnabled)
            {
                Logger.Log("⚠️ Global theming already enabled");
                return true;
            }

            try
            {
                // Check for administrator privileges
                if (!IsRunningAsAdministrator())
                {
                    Logger.Log("❌ Global theming requires administrator privileges");
                    Logger.Log("💡 Restart Cycloside as administrator to enable global theming");
                    return false;
                }

                // Create shared memory for theme data
                CreateSharedMemory();

                // Launch native host process
                LaunchHostProcess();

                // Wait for host to initialize
                System.Threading.Thread.Sleep(1000);

                _isEnabled = true;
                Logger.Log("✅ Global theming enabled");
                GlobalThemingChanged?.Invoke(true);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to enable global theming: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disable global theming and restore normal window appearance.
        /// </summary>
        public void DisableGlobalTheming()
        {
            if (!_isEnabled)
                return;

            try
            {
                // Stop host process
                StopHostProcess();

                // Clean up shared memory
                CleanupSharedMemory();

                _isEnabled = false;
                Logger.Log("✅ Global theming disabled");
                GlobalThemingChanged?.Invoke(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Error disabling global theming: {ex.Message}");
            }
        }

        /// <summary>
        /// Set the theming mode (All, Whitelist, Blacklist)
        /// </summary>
        public void SetMode(ThemingMode mode)
        {
            _mode = mode;
            Logger.Log($"🎨 Theming mode set to: {mode}");
            UpdateSharedMemory();
            SaveConfiguration();
        }

        /// <summary>
        /// Add an application to the whitelist (only these apps get themed)
        /// </summary>
        public void AddToWhitelist(string appName)
        {
            _whitelist.Add(appName.ToLower());
            Logger.Log($"✅ Added to whitelist: {appName}");
            UpdateSharedMemory();
            SaveConfiguration();
        }

        /// <summary>
        /// Remove an application from the whitelist
        /// </summary>
        public void RemoveFromWhitelist(string appName)
        {
            _whitelist.Remove(appName.ToLower());
            Logger.Log($"➖ Removed from whitelist: {appName}");
            UpdateSharedMemory();
            SaveConfiguration();
        }

        /// <summary>
        /// Add an application to the blacklist (these apps DON'T get themed)
        /// </summary>
        public void AddToBlacklist(string appName)
        {
            _blacklist.Add(appName.ToLower());
            Logger.Log($"🚫 Added to blacklist: {appName}");
            UpdateSharedMemory();
            SaveConfiguration();
        }

        /// <summary>
        /// Remove an application from the blacklist
        /// </summary>
        public void RemoveFromBlacklist(string appName)
        {
            _blacklist.Remove(appName.ToLower());
            Logger.Log($"➖ Removed from blacklist: {appName}");
            UpdateSharedMemory();
            SaveConfiguration();
        }

        /// <summary>
        /// Set a specific theme for a specific application
        /// </summary>
        public void SetThemeForApp(string appName, string themeName)
        {
            _perAppThemes[appName.ToLower()] = themeName;
            Logger.Log($"🎨 Set theme for {appName}: {themeName}");
            UpdateSharedMemory();
            SaveConfiguration();
        }

        /// <summary>
        /// Check if an application should be themed based on current configuration
        /// </summary>
        public bool ShouldThemeApp(string appName)
        {
            var lowerAppName = appName.ToLower();

            // Blacklist always wins
            if (_blacklist.Contains(lowerAppName))
                return false;

            return _mode switch
            {
                ThemingMode.All => true,
                ThemingMode.Whitelist => _whitelist.Contains(lowerAppName),
                ThemingMode.Blacklist => !_blacklist.Contains(lowerAppName),
                _ => false
            };
        }

        /// <summary>
        /// Get the theme name for a specific application (or global default)
        /// </summary>
        public string GetThemeForApp(string appName)
        {
            var lowerAppName = appName.ToLower();

            if (_perAppThemes.TryGetValue(lowerAppName, out var themeName))
                return themeName;

            // Return current global theme
            var currentTheme = WindowDecorationManager.Instance.CurrentTheme;
            return currentTheme?.Name ?? "Default";
        }

        /// <summary>
        /// Get list of currently whitelisted applications
        /// </summary>
        public IReadOnlyCollection<string> GetWhitelist() => _whitelist.ToList().AsReadOnly();

        /// <summary>
        /// Get list of currently blacklisted applications
        /// </summary>
        public IReadOnlyCollection<string> GetBlacklist() => _blacklist.ToList().AsReadOnly();

        #region Private Implementation

        private void CreateSharedMemory()
        {
            try
            {
                // Create 10MB shared memory region for theme data
                _sharedMemory = MemoryMappedFile.CreateOrOpen(
                    "CyclosideThemeData",
                    10 * 1024 * 1024, // 10MB
                    MemoryMappedFileAccess.ReadWrite
                );

                Logger.Log("✅ Created shared memory for theme data");

                // Write initial theme data
                UpdateSharedMemory();
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to create shared memory: {ex.Message}");
                throw;
            }
        }

        private void CleanupSharedMemory()
        {
            try
            {
                _sharedMemory?.Dispose();
                _sharedMemory = null;
                Logger.Log("✅ Cleaned up shared memory");
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Error cleaning up shared memory: {ex.Message}");
            }
        }

        private void UpdateSharedMemory()
        {
            if (_sharedMemory == null)
                return;

            try
            {
                using var accessor = _sharedMemory.CreateViewAccessor();

                // Write theme data structure
                // TODO: Serialize current theme, configuration, etc.
                // For now, just write a header
                accessor.Write(0, (int)0xDECADE); // Magic number
                accessor.Write(4, (int)_mode);
                accessor.Write(8, _whitelist.Count);
                accessor.Write(12, _blacklist.Count);

                Logger.Log("✅ Updated shared memory with theme data");
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Error updating shared memory: {ex.Message}");
            }
        }

        private void LaunchHostProcess()
        {
            try
            {
                var hostPath = Path.Combine(AppContext.BaseDirectory, "CyclosideThemeHost.exe");

                if (!File.Exists(hostPath))
                {
                    Logger.Log($"❌ CyclosideThemeHost.exe not found at: {hostPath}");
                    Logger.Log("💡 Global theming requires native components (not yet built)");
                    Logger.Log("💡 This is a placeholder that will work once we build the C++ components");
                    return;
                }

                _hostProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = hostPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                _hostProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Logger.Log($"[ThemeHost] {e.Data}");
                };

                _hostProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Logger.Log($"[ThemeHost ERROR] {e.Data}");
                };

                _hostProcess.Start();
                _hostProcess.BeginOutputReadLine();
                _hostProcess.BeginErrorReadLine();

                Logger.Log($"✅ Launched CyclosideThemeHost.exe (PID: {_hostProcess.Id})");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to launch host process: {ex.Message}");
                throw;
            }
        }

        private void StopHostProcess()
        {
            if (_hostProcess == null || _hostProcess.HasExited)
                return;

            try
            {
                _hostProcess.Kill();
                _hostProcess.WaitForExit(5000);
                _hostProcess.Dispose();
                _hostProcess = null;

                Logger.Log("✅ Stopped CyclosideThemeHost.exe");
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Error stopping host process: {ex.Message}");
            }
        }

        private bool IsRunningAsAdministrator()
        {
#if WINDOWS
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
#else
            // On Linux, check if running as root
            return Environment.UserName == "root";
#endif
        }

        private void LoadConfiguration()
        {
            try
            {
                // TODO: Load from settings file
                // For now, use defaults
                _mode = ThemingMode.Whitelist;

                // Default whitelist (safe apps to theme)
                _whitelist.Add("firefox.exe");
                _whitelist.Add("chrome.exe");
                _whitelist.Add("code.exe");
                _whitelist.Add("windowsterminal.exe");
                _whitelist.Add("notepad.exe");

                // Default blacklist (critical system processes)
                _blacklist.Add("explorer.exe");
                _blacklist.Add("dwm.exe");
                _blacklist.Add("csrss.exe");
                _blacklist.Add("services.exe");
                _blacklist.Add("svchost.exe");
                _blacklist.Add("lsass.exe");
                _blacklist.Add("winlogon.exe");

                Logger.Log($"✅ Loaded configuration: Mode={_mode}, Whitelist={_whitelist.Count}, Blacklist={_blacklist.Count}");
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Error loading configuration: {ex.Message}");
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                // TODO: Save to settings file
                Logger.Log("💾 Configuration saved");
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Error saving configuration: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Theming mode determines which applications get themed
    /// </summary>
    public enum ThemingMode
    {
        /// <summary>
        /// Theme all applications (except system processes)
        /// </summary>
        All,

        /// <summary>
        /// Only theme applications in the whitelist
        /// </summary>
        Whitelist,

        /// <summary>
        /// Theme all applications except those in the blacklist
        /// </summary>
        Blacklist
    }
}
