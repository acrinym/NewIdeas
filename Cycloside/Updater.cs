using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Cycloside;

public static class Updater
{
    public static async Task<bool> CheckAndUpdate(string url, string expectedHash)
    {
        try
        {
            using var client = new HttpClient();
            var data = await client.GetByteArrayAsync(url);
            var hash = Convert.ToHexString(SHA256.HashData(data));
            if (!hash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                return false;

            var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exe)) return false;
            var temp = exe + ".new";
            await File.WriteAllBytesAsync(temp, data);
            File.Replace(temp, exe, null);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"Update failed: {ex.Message}");
            return false;
        }
    }
}
