using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cycloside;

public class MarketplacePlugin
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}

public static class PluginMarketplace
{
    public static async Task<List<MarketplacePlugin>> FetchAsync(string url)
    {
        try
        {
            using var client = new HttpClient();
            var json = await client.GetStringAsync(url);
            var list = JsonSerializer.Deserialize<List<MarketplacePlugin>>(json);
            return list ?? new();
        }
        catch (Exception ex)
        {
            Logger.Log($"Marketplace fetch failed: {ex.Message}");
            return new();
        }
    }

    public static async Task<bool> InstallAsync(MarketplacePlugin plugin, string pluginDirectory)
    {
        try
        {
            using var client = new HttpClient();
            var data = await client.GetByteArrayAsync(plugin.Url);
            var hash = Convert.ToHexString(SHA256.HashData(data));
            if (!hash.Equals(plugin.Hash, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!Directory.Exists(pluginDirectory))
                Directory.CreateDirectory(pluginDirectory);

            var fileName = Path.GetFileName(new Uri(plugin.Url).LocalPath);
            var path = Path.Combine(pluginDirectory, fileName);
            await File.WriteAllBytesAsync(path, data);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"Plugin install failed for {plugin.Name}: {ex.Message}");
            return false;
        }
    }
}
