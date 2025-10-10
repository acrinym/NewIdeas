using Avalonia;
using System;
using System.IO;
using System.Runtime.ExceptionServices;

namespace Cycloside;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length >= 2 && args[0] == "--newplugin")
        {
            var name = args[1];
            var withTests = args.Length >= 3 && args[2] == "--with-tests";
            GeneratePluginTemplate(name, withTests);
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Logger.Log($"Unhandled: {ex}");
        };
        AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
        {
            var ex = e.Exception;
            // Early exception tracing to catch type-load and static ctor issues
            try
            {
                var typeName = ex.GetType().FullName ?? ex.GetType().Name;
                var addl = ex is NullReferenceException ? " [NullRef]" : string.Empty;

                // Build log message defensively to avoid property access exceptions
                var msg = $"FirstChance{addl}: {typeName}: {ex.Message}";

                // Source (safe)
                try
                {
                    var src = ex.Source;
                    if (!string.IsNullOrWhiteSpace(src))
                        msg += $"\nSource: {src}";
                }
                catch { /* ignore */ }

                // TargetSite (safe) — method and declaring type
                try
                {
                    var site = ex.TargetSite;
                    if (site != null)
                    {
                        string decl = "<unknown>";
                        string name = "<unknown>";
                        try { decl = site.DeclaringType?.FullName ?? "<unknown>"; } catch { }
                        try { name = site.Name ?? "<unknown>"; } catch { }
                        msg += $"\nTargetSite: {decl}.{name}";
                    }
                }
                catch { /* ignore */ }

                // Stack trace (safe)
                try
                {
                    var st = ex.StackTrace;
                    if (!string.IsNullOrWhiteSpace(st))
                        msg += $"\n{st}";
                }
                catch { /* ignore */ }

                Logger.Log(msg);
            }
            catch
            {
                // Fallback to original logging if enhanced details fail
                Logger.Log($"FirstChance: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        };
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Logger.Log($"Unobserved: {e.Exception}");
        };
        // Prevent crash when no GUI environment is present (e.g. CI or headless servers)
        if (OperatingSystem.IsLinux() && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
        {
            Console.Error.WriteLine("X11 display not found. Exiting...");
            return; // gracefully bail out
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    internal static void GeneratePluginTemplate(string name, bool withTests)
    {
        var baseDir = Path.Combine("Plugins", name);
        Directory.CreateDirectory(baseDir);

        var srcDir = Path.Combine(baseDir, "src");
        Directory.CreateDirectory(srcDir);

        var path = Path.Combine(srcDir, $"{name}.cs");
        if (!File.Exists(path))
        {
            var content = $@"using Cycloside.Plugins;

public class {name} : IPlugin
{{
    public string Name => ""{name}"";
    public string Description => ""Describe your plugin."";
    public Version Version => new(1, 0, 0);
    public Cycloside.Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;

    public void Start()
    {{
        // Plugin startup logic here
    }}

    public void Stop()
    {{
        // Plugin shutdown logic here
    }}
}}";
            File.WriteAllText(path, content);
        }

        var csprojPath = Path.Combine(srcDir, $"{name}.csproj");
        if (!File.Exists(csprojPath))
        {
            var csproj = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""..\..\Cycloside.csproj"" />
  </ItemGroup>
</Project>";
            File.WriteAllText(csprojPath, csproj);
        }

        if (withTests)
        {
            var testsDir = Path.Combine(baseDir, "tests");
            Directory.CreateDirectory(testsDir);

            var testProjPath = Path.Combine(testsDir, $"{name}.Tests.csproj");
            if (!File.Exists(testProjPath))
            {
                var testProj = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""xunit"" Version=""2.5.0"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.5.0"" />
    <ProjectReference Include=""..\src\{name}.csproj"" />
  </ItemGroup>
</Project>";
                File.WriteAllText(testProjPath, testProj);
            }

            var testFile = Path.Combine(testsDir, "BasicTests.cs");
            if (!File.Exists(testFile))
            {
                var testContent = $@"using Xunit;
using {name};

public class BasicTests
{{
    [Fact]
    public void PluginLoads()
    {{
        var plugin = new {name}();
        Assert.Equal(""{name}"", plugin.Name);
    }}
}}";
                File.WriteAllText(testFile, testContent);
            }
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
