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
        var projectRoot = ResolvePluginScaffoldRoot();
        var baseDir = Path.Combine(projectRoot, "Plugins", name);
        var targetFramework = OperatingSystem.IsWindows() ? "net8.0-windows" : "net8.0";
        Directory.CreateDirectory(baseDir);

        var srcDir = Path.Combine(baseDir, "src");
        Directory.CreateDirectory(srcDir);

        var path = Path.Combine(srcDir, $"{name}.cs");
        if (!File.Exists(path))
        {
            var content = $@"using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Cycloside.Plugins;

public sealed class {name} : IPlugin
{{
    private PluginWindowBase? _window;

    public string Name => ""{name}"";
    public string Description => ""{name} plugin for the Cycloside shell."";
    public Version Version => new(1, 0, 0);
    public Cycloside.Widgets.IWidget? Widget => null;
    public bool ForceDefaultTheme => false;

    public void Start()
    {{
        if (_window != null)
        {{
            _window.Activate();
            return;
        }}

        var message = new TextBlock
        {{
            Text = ""{name} is running inside Cycloside."",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 16
        }};

        _window = new PluginWindowBase
        {{
            Title = Name,
            Width = 520,
            Height = 320,
            CanResize = true,
            Content = new Border
            {{
                Padding = new Thickness(24),
                Child = message
            }}
        }};

        _window.Plugin = this;
        _window.ApplyPluginThemeAndSkin(this);
        _window.Closed += OnWindowClosed;
        _window.Show();
    }}

    public void Stop()
    {{
        if (_window == null)
        {{
            return;
        }}

        _window.Closed -= OnWindowClosed;
        _window.Close();
        _window = null;
    }}

    private void OnWindowClosed(object? sender, EventArgs e)
    {{
        if (_window != null)
        {{
            _window.Closed -= OnWindowClosed;
            _window = null;
        }}
    }}
}}";
            File.WriteAllText(path, content);
        }

        var csprojPath = Path.Combine(srcDir, $"{name}.csproj");
        if (!File.Exists(csprojPath))
        {
            var csproj = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{targetFramework}</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""..\..\..\Cycloside.csproj"" />
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
    <TargetFramework>{targetFramework}</TargetFramework>
    <Nullable>enable</Nullable>
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

    private static string ResolvePluginScaffoldRoot()
    {
        var candidates = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var candidate in candidates)
        {
            var resolved = TryFindCyclosideProjectRoot(candidate);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    private static string? TryFindCyclosideProjectRoot(string startPath)
    {
        if (string.IsNullOrWhiteSpace(startPath))
        {
            return null;
        }

        var current = new DirectoryInfo(startPath);
        while (current != null)
        {
            var projectFilePath = Path.Combine(current.FullName, "Cycloside.csproj");
            if (File.Exists(projectFilePath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }
}
