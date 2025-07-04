using System;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace Cycloside.Services
{
    public static class DialogHelper
    {
        public static async Task<IStorageFolder?> GetDefaultStartLocationAsync(IStorageProvider provider)
        {
            string path = OperatingSystem.IsWindows()
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return await provider.TryGetFolderFromPathAsync(path);
        }
    }
}
