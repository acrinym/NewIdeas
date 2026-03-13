using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cycloside.Services
{
    /// <summary>
    /// Theme pack manifest (theme.json). Enables themes as complete experience packs.
    /// </summary>
    public class ThemeManifest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("author")]
        public string Author { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("screenshots")]
        public List<string> Screenshots { get; set; } = new();

        [JsonPropertyName("styles")]
        public List<string> Styles { get; set; } = new();

        [JsonPropertyName("assets")]
        public ThemeAssets Assets { get; set; } = new();

        [JsonPropertyName("scripts")]
        public ThemeScripts Scripts { get; set; } = new();

        [JsonPropertyName("dependencies")]
        public ThemeDependencies Dependencies { get; set; } = new();

        [JsonPropertyName("settings")]
        public Dictionary<string, JsonElement> Settings { get; set; } = new();

        /// <summary>
        /// Load manifest from theme directory. Looks for theme.json.
        /// </summary>
        public static ThemeManifest? Load(string themeDir)
        {
            if (string.IsNullOrWhiteSpace(themeDir) || !Directory.Exists(themeDir))
                return null;

            var path = Path.Combine(themeDir, "theme.json");
            if (!File.Exists(path))
                return null;

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<ThemeManifest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load theme manifest from {path}: {ex.Message}");
                return null;
            }
        }
    }

    public class ThemeAssets
    {
        [JsonPropertyName("images")]
        public List<string> Images { get; set; } = new();

        [JsonPropertyName("cursors")]
        public List<string> Cursors { get; set; } = new();

        [JsonPropertyName("icons")]
        public List<string> Icons { get; set; } = new();

        [JsonPropertyName("sounds")]
        public List<string> Sounds { get; set; } = new();
    }

    public class ThemeScripts
    {
        [JsonPropertyName("lua")]
        public List<string> Lua { get; set; } = new();
    }

    public class ThemeDependencies
    {
        [JsonPropertyName("requiredThemes")]
        public List<string> RequiredThemes { get; set; } = new();

        [JsonPropertyName("requiredPlugins")]
        public List<string> RequiredPlugins { get; set; } = new();
    }
}
