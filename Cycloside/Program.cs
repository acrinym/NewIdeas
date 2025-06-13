using Avalonia;
using System;
using System.IO;
using Cycloside.Plugins.BuiltIn;

namespace Cycloside;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length >= 2 && args[0] == "--qbasic")
        {
            QBasicRetroIDEPlugin.RunCli(args[1]).GetAwaiter().GetResult();
            return;
        }
        if (args.Length == 2 && args[0] == "--newplugin")
        {
            GeneratePluginTemplate(args[1]);
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

    internal static void GeneratePluginTemplate(string name)
    {
        var dir = Path.Combine("Plugins", name);
        Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, $"{name}.cs");
        if (File.Exists(path))
            return;

        var content = $@"using Cycloside.Plugins;

public class {name} : IPlugin
{{
    public string Name => ""{name}"";
    public string Description => ""Describe your plugin."";
    public Version Version => new(1, 0, 0);

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

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
