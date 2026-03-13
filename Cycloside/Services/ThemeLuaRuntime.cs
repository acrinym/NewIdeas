using System;
using System.Collections.Generic;
using System.IO;
using MoonSharp.Interpreter;

namespace Cycloside.Services
{
    /// <summary>
    /// Sandboxed Lua runtime for theme scripts. Exposes theme and system tables only.
    /// </summary>
    public class ThemeLuaRuntime
    {
        private readonly string _themeDir;
        private readonly Dictionary<string, object> _themeSettings = new();
        private readonly Dictionary<string, string> _themeColors = new();
        private Script? _script;

        public ThemeLuaRuntime(string themeDir)
        {
            _themeDir = themeDir ?? "";
        }

        /// <summary>
        /// Run a Lua script from the theme directory. Path is relative to theme dir.
        /// </summary>
        public void RunScript(string relativePath)
        {
            var fullPath = Path.Combine(_themeDir, relativePath);
            if (!File.Exists(fullPath))
            {
                Logger.Log($"Theme script not found: {fullPath}");
                return;
            }

            var baseDir = Path.GetDirectoryName(fullPath) ?? _themeDir;
            if (!Path.GetFullPath(baseDir).StartsWith(Path.GetFullPath(_themeDir), StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"Theme script path outside theme directory: {relativePath}");
                return;
            }

            var code = File.ReadAllText(fullPath);
            RunCode(code, baseDir);
        }

        /// <summary>
        /// Run Lua code string. BaseDir used for any relative paths in script.
        /// </summary>
        public void RunCode(string code, string? baseDir = null)
        {
            baseDir ??= _themeDir;

            try
            {
                _script = new Script(CoreModules.Preset_SoftSandbox);

                var themeTable = new Table(_script);
                var colorsTable = new Table(_script);
                foreach (var kv in _themeColors)
                    colorsTable[kv.Key] = kv.Value;
                var settingsTable = new Table(_script);
                foreach (var kv in _themeSettings)
                    settingsTable[kv.Key] = kv.Value?.ToString() ?? "";
                themeTable["colors"] = colorsTable;
                themeTable["settings"] = settingsTable;
                themeTable["getSetting"] = (Func<string, object?>)GetSetting;
                themeTable["setSetting"] = (Action<string, object>)SetSetting;
                _script.Globals["theme"] = themeTable;

                var systemTable = new Table(_script);
                systemTable["time"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                systemTable["platform"] = Environment.OSVersion.Platform.ToString();
                systemTable["user"] = Environment.UserName;
                _script.Globals["system"] = systemTable;

                _script.Globals["OnLoad"] = DynValue.Nil;
                _script.Globals["OnApply"] = DynValue.Nil;
                _script.Globals["OnSettingChange"] = DynValue.Nil;

                _script.DoString(code);

                var onLoad = _script.Globals.Get("OnLoad");
                if (onLoad.Type == DataType.Function)
                    _script.Call(onLoad);
            }
            catch (Exception ex)
            {
                Logger.Log($"Theme Lua error: {ex.Message}");
            }
        }

        /// <summary>
        /// Call OnApply hook if defined. Call after theme styles are applied.
        /// </summary>
        public void CallOnApply()
        {
            if (_script == null) return;
            try
            {
                var onApply = _script.Globals.Get("OnApply");
                if (onApply.Type == DataType.Function)
                    _script.Call(onApply);
            }
            catch (Exception ex)
            {
                Logger.Log($"Theme OnApply error: {ex.Message}");
            }
        }

        public object? GetSetting(string key)
        {
            return _themeSettings.TryGetValue(key, out var v) ? v : null;
        }

        public void SetSetting(string key, object value)
        {
            _themeSettings[key] = value;
        }

        public void SetInitialSettings(Dictionary<string, object>? settings)
        {
            if (settings == null) return;
            foreach (var kv in settings)
                _themeSettings[kv.Key] = kv.Value;
        }
    }
}
