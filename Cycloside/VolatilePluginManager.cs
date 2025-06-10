using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MoonSharp.Interpreter;

namespace Cycloside;

public class VolatilePluginManager
{
    private readonly List<object> _running = new();

    public IReadOnlyList<object> Running => _running.AsReadOnly();

    public void RunLua(string luaCode)
    {
        try
        {
            var script = new Script();
            var result = script.DoString(luaCode);
            _running.Add(script);
            Logger.Log($"Lua result: {result}");
        }
        catch (Exception ex)
        {
            Logger.Log($"Lua error: {ex.Message}");
        }
    }

    public void RunCSharp(string csharpCode)
    {
        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location));
            var compilation = CSharpCompilation.Create(
                "InMemoryAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);
            if (!emitResult.Success)
            {
                foreach (var d in emitResult.Diagnostics)
                    Logger.Log(d.ToString());
                return;
            }
            ms.Seek(0, SeekOrigin.Begin);
            var asm = Assembly.Load(ms.ToArray());
            _running.Add(asm);
            var type = asm.GetType("Script.Main");
            var method = type?.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
            var result = method?.Invoke(null, null);
            Logger.Log($"C# result: {result}");
        }
        catch (Exception ex)
        {
            Logger.Log($"C# error: {ex.Message}");
        }
    }
}

