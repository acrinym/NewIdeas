// CyclosideBASIC Engine - Full Interpreter
// Save as CyclosideBasicEngine.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace Cycloside.Scripting
{
    public class CyclosideBasicEngine
    {
        public InterpreterContext Context { get; }
        private readonly Dictionary<string, Action<string[]>> _commands;
        private readonly Dictionary<string, object> _vars = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, object> _readonlyVars = new(StringComparer.OrdinalIgnoreCase);
        public bool Exit { get; private set; }

        public CyclosideBasicEngine(InterpreterContext context)
        {
            Context = context;
            // Built-in read-only variables expose core paths and metadata
            _readonlyVars["APP_NAME"] = context.AppName;
            _readonlyVars["APP_VERSION"] = context.AppVersion;
            if (!string.IsNullOrEmpty(context.PluginDirectory))
                _readonlyVars["PLUGIN_DIR"] = context.PluginDirectory;
            if (!string.IsNullOrEmpty(context.SettingsPath))
                _readonlyVars["SETTINGS_PATH"] = context.SettingsPath;
            if (!string.IsNullOrEmpty(context.MarketplaceUrl))
                _readonlyVars["MARKETPLACE_URL"] = context.MarketplaceUrl;
            if (!string.IsNullOrEmpty(context.AppDirectory))
                _readonlyVars["APP_DIR"] = context.AppDirectory;
            if (!string.IsNullOrEmpty(context.LogsDirectory))
                _readonlyVars["LOG_DIR"] = context.LogsDirectory;
            if (!string.IsNullOrEmpty(context.MusicDirectory))
                _readonlyVars["MUSIC_DIR"] = context.MusicDirectory;
            if (!string.IsNullOrEmpty(context.SkinsDirectory))
                _readonlyVars["SKIN_DIR"] = context.SkinsDirectory;
            if (!string.IsNullOrEmpty(context.ThemesDirectory))
                _readonlyVars["THEME_DIR"] = context.ThemesDirectory;
            if (!string.IsNullOrEmpty(context.StatePath))
                _readonlyVars["STATE_PATH"] = context.StatePath;
            if (!string.IsNullOrEmpty(context.ProfilePath))
                _readonlyVars["PROFILE_PATH"] = context.ProfilePath;
            if (!string.IsNullOrEmpty(context.UserHome))
                _readonlyVars["HOME_DIR"] = context.UserHome;
            if (!string.IsNullOrEmpty(context.DesktopPath))
                _readonlyVars["DESKTOP_PATH"] = context.DesktopPath;
            if (!string.IsNullOrEmpty(context.OS))
                _readonlyVars["OS"] = context.OS;

            _commands = new(StringComparer.OrdinalIgnoreCase)
            {
                // Core
                ["PRINT"] = args => Context.Print(ExpandVars(string.Join(" ", args))),
                ["INPUT"] = args => Input(args),
                ["LET"] = args => Let(args),
                ["IF"] = args => IfThen(args),
                ["WAIT"] = args => Task.Delay(ParseInt(args[0])).Wait(),
                ["EXIT"] = args => Exit = true,
                // Cycloside/Manager Integration
                ["THEME"] = args => Context.ThemeManager?.Invoke(args[0]),
                ["SKIN"] = args => Context.SkinManager?.Invoke(args[0]),
                ["SHOWMSG"] = args => Context.ShowMsg?.Invoke(ExpandVars(string.Join(" ", args))),
                ["NOTIFY"] = args => Context.Notify?.Invoke(args.ElementAtOrDefault(0) ?? "", ExpandVars(string.Join(" ", args.Skip(1)))),
                ["SET_CLIPBOARD"] = args => Context.SetClipboard?.Invoke(ExpandVars(string.Join(" ", args))),
                ["GET_CLIPBOARD"] = args => SetVar(args[0], Context.GetClipboard?.Invoke() ?? ""),
                ["RUNPLUGIN"] = args => Context.RunPlugin?.Invoke(args[0]),
                ["LIST_PLUGINS"] = args => Context.Print(Context.ListPlugins != null ? string.Join(", ", Context.ListPlugins()) : "[No plugins]"),
                ["ENABLE_PLUGIN"] = args => Context.EnablePlugin?.Invoke(args[0]),
                ["DISABLE_PLUGIN"] = args => Context.DisablePlugin?.Invoke(args[0]),
                ["SET_WALLPAPER"] = args => Context.SetWallpaper?.Invoke(args[0]),
                ["GET_DISKUSAGE"] = args => SetVar(args[1], Context.GetDiskUsage != null ? Context.GetDiskUsage(args[0]) : "0"),
                ["SHOW_PROCESSLIST"] = args => Context.Print(Context.ListProcesses != null ? string.Join("\n", Context.ListProcesses()) : "[No processes]"),
                ["LOG"] = args => Context.AppendLog?.Invoke(ExpandVars(string.Join(" ", args))),
                ["SET_VOLUME"] = args => Context.SetSystemVolume?.Invoke(ParseInt(args[0])),
                ["GET_VOLUME"] = args => SetVar(args[0], Context.GetSystemVolume != null ? Context.GetSystemVolume() : 0),
                ["MUTE"] = args => Context.SetMuted?.Invoke(true),
                ["UNMUTE"] = args => Context.SetMuted?.Invoke(false),
                ["MEDIA_PLAY"] = _ => Context.MediaPlay?.Invoke(),
                ["MEDIA_PAUSE"] = _ => Context.MediaPause?.Invoke(),
                ["MEDIA_STOP"] = _ => Context.MediaStop?.Invoke(),
                // Extend more as needed!
            };
        }

        /// <summary>
        /// Main script entry. Call with your BASIC code.
        /// </summary>
        public void ExecuteScript(string code)
        {
            Exit = false;
            var lines = ParseLines(code);
            for (int i = 0; i < lines.Count && !Exit; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("'")) continue; // comment
                // Support for GOTO, labels
                if (line.EndsWith(":")) continue; // label line
                var parts = SplitFirstWord(line);
                var cmd = parts.cmd;
                var args = SplitArgs(parts.rest);

                if (string.Equals(cmd, "GOTO", StringComparison.OrdinalIgnoreCase))
                {
                    var label = args[0].TrimEnd(':');
                    var idx = lines.FindIndex(l => l.TrimStart().StartsWith(label + ":", StringComparison.OrdinalIgnoreCase));
                    if (idx != -1) i = idx - 1; // minus 1 because for-loop will add
                    continue;
                }
                else if (string.Equals(cmd, "GOSUB", StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: Add subroutine support if needed
                }
                else if (_commands.TryGetValue(cmd, out var action))
                {
                    action(args);
                }
                else
                {
                    Context.Print($"[Unknown cmd: {cmd}]");
                }
            }
        }

        // Variable system
        public object GetVar(string name)
        {
            name = name.Trim();
            return _vars.TryGetValue(name, out var v)
                ? v
                : _readonlyVars.TryGetValue(name, out var rv) ? rv : "";
        }

        public void SetVar(string name, object val)
        {
            name = name.Trim();
            if (_readonlyVars.ContainsKey(name))
                return; // ignore attempts to overwrite built-in vars
            _vars[name] = val;
        }

        // Utility: expands any $(var) or ${var} in a string
        private string ExpandVars(string input)
            => Regex.Replace(input, @"\$\{?(\w+)\}?", m => GetVar(m.Groups[1].Value)?.ToString() ?? "");

        // ========== BASIC Commands Implementation ==========

        private void Input(string[] args)
        {
            var prompt = ExpandVars(string.Join(" ", args.Take(args.Length - 1)));
            var varName = args.Last();
            string val = Context.GetInput != null ? Context.GetInput(prompt) : "";
            SetVar(varName, val);
        }

        private void Let(string[] args)
        {
            // Example: LET foo = 42+7
            var s = string.Join(" ", args);
            var parts = s.Split('=', 2);
            if (parts.Length != 2) return;
            var name = parts[0].Trim();
            var expr = ExpandVars(parts[1]);
            SetVar(name, expr);
        }

        private void IfThen(string[] args)
        {
            // IF <expr> THEN <cmd> <args...>
            var txt = string.Join(" ", args);
            var thenIdx = txt.IndexOf("THEN", StringComparison.OrdinalIgnoreCase);
            if (thenIdx == -1) return;
            var expr = txt.Substring(0, thenIdx).Trim();
            var thenCmd = txt.Substring(thenIdx + 4).Trim();
            bool result = EvaluateCondition(expr);
            if (result)
            {
                var (cmd, rest) = SplitFirstWord(thenCmd);
                if (_commands.TryGetValue(cmd, out var action))
                {
                    action(SplitArgs(rest));
                }
            }
        }

        private bool EvaluateCondition(string expr)
        {
            // Simple evaluator: foo = bar, foo <> bar, foo > 3, etc
            expr = ExpandVars(expr);
            var m = Regex.Match(expr, @"^(.+?)\s*([=<>!]+)\s*(.+)$");
            if (!m.Success) return false;
            var left = m.Groups[1].Value.Trim();
            var op = m.Groups[2].Value;
            var right = m.Groups[3].Value.Trim();
            if (double.TryParse(left, out var lf) && double.TryParse(right, out var rf))
            {
                return op switch
                {
                    "=" or "==" => lf == rf,
                    "<" => lf < rf,
                    ">" => lf > rf,
                    "<=" => lf <= rf,
                    ">=" => lf >= rf,
                    "<>" or "!=" => lf != rf,
                    _ => false
                };
            }
            else
            {
                return op switch
                {
                    "=" or "==" => string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
                    "<>" or "!=" => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
                    _ => false
                };
            }
        }

        private int ParseInt(string s)
        {
            if (int.TryParse(ExpandVars(s), out var v)) return v;
            return 0;
        }

        // ========== Helpers ==========

        private (string cmd, string rest) SplitFirstWord(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return ("", "");
            var idx = s.IndexOf(' ');
            if (idx == -1) return (s, "");
            return (s.Substring(0, idx), s.Substring(idx + 1));
        }

        private string[] SplitArgs(string s)
        {
            // Handles quoted strings and commas: "hello, world", foo, 3
            var args = new List<string>();
            var curr = "";
            bool inQuote = false;
            foreach (var ch in s)
            {
                if (ch == '"') inQuote = !inQuote;
                else if (ch == ',' && !inQuote)
                {
                    args.Add(curr.Trim());
                    curr = "";
                }
                else curr += ch;
            }
            if (!string.IsNullOrWhiteSpace(curr)) args.Add(curr.Trim());
            return args.ToArray();
        }

        private List<string> ParseLines(string code)
        {
            // Supports multi-line scripts, ignores blank lines and trims.
            var lines = code.Split('\n')
                .Select(x => x.TrimEnd('\r').Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            return lines;
        }
    }

    // === INTERPRETER CONTEXT ===
    // Provide implementations as lambdas or delegates when constructing the engine.

    public class InterpreterContext
    {
        public Action<string> Print { get; set; } = s => Console.WriteLine(s);
        public Func<string, string> GetInput { get; set; } = prompt => Console.ReadLine() ?? "";
        public Action<string> ShowMsg { get; set; }
        public Action<string, string> Notify { get; set; }
        public Action<string> SetClipboard { get; set; }
        public Func<string> GetClipboard { get; set; }
        public Action<string> ThemeManager { get; set; }
        public Action<string> SkinManager { get; set; }
        public Action<string> SetWallpaper { get; set; }
        public Func<string[]> ListPlugins { get; set; }
        public Action<string> RunPlugin { get; set; }
        public Action<string> EnablePlugin { get; set; }
        public Action<string> DisablePlugin { get; set; }
        public Func<string, string> GetDiskUsage { get; set; }
        public Func<string[]> ListProcesses { get; set; }
        public Action<string> AppendLog { get; set; }
        public Action<int> SetSystemVolume { get; set; }
        public Func<int> GetSystemVolume { get; set; }
        public Action<bool> SetMuted { get; set; }
        public Action MediaPlay { get; set; }
        public Action MediaPause { get; set; }
        public Action MediaStop { get; set; }
        // Built-in values that scripts can read but not modify
        public string AppName { get; init; } = "Cycloside";
        public string AppVersion { get; init; } = "1.0.0";
        public string PluginDirectory { get; init; } = string.Empty;
        public string SettingsPath { get; init; } = string.Empty;
        public string MarketplaceUrl { get; init; } = string.Empty;
        public string AppDirectory { get; init; } = string.Empty;
        public string LogsDirectory { get; init; } = string.Empty;
        public string MusicDirectory { get; init; } = string.Empty;
        public string SkinsDirectory { get; init; } = string.Empty;
        public string ThemesDirectory { get; init; } = string.Empty;
        public string StatePath { get; init; } = string.Empty;
        public string ProfilePath { get; init; } = string.Empty;
        public string UserHome { get; init; } = string.Empty;
        public string DesktopPath { get; init; } = string.Empty;
        public string OS { get; init; } = string.Empty;
        // ...extend as needed
    }
}
