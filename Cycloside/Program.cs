using Avalonia;
using System;
using System.IO;

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
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Logger.Log($"Unobserved: {e.Exception}");
        };

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
