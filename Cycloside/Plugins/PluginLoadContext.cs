using System.Reflection;
using System.Runtime.Loader;

namespace Cycloside.Plugins;

internal class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string path) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(path);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var asmPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (asmPath != null)
            return LoadFromAssemblyPath(asmPath);
        return null;
    }

    public Assembly LoadPlugin(string path) => LoadFromAssemblyPath(path);
}
