using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cycloside.Plugins.BuiltIn.ScreenSaverModules
{
    internal static class ScreenSaverModuleRegistry
    {
        private static readonly Lazy<Dictionary<string, Type>> _modules = new(() =>
            Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IScreenSaverModule).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .Select(t => (IScreenSaverModule)Activator.CreateInstance(t)!)
                .ToDictionary(m => m.Name, m => m.GetType(), StringComparer.OrdinalIgnoreCase));

        public static IReadOnlyCollection<string> ModuleNames => _modules.Value.Keys;

        public static IScreenSaverModule Create(string name)
        {
            if (!_modules.Value.TryGetValue(name, out var type))
            {
                type = _modules.Value.Values.First();
            }
            return (IScreenSaverModule)Activator.CreateInstance(type)!;
        }
    }
}
