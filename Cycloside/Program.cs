using Avalonia;
using System;
using System.IO;

namespace Cycloside;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--newplugin")
        {
            GeneratePluginTemplate(args[1]);
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void GeneratePluginTemplate(string name)
    {
        var dir = Path.Combine("Plugins", name);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{name}.cs");
        if (File.Exists(path))
            return;
        var content =
            $"using Cycloside.Plugins;\n\npublic class {name} : IPlugin\n{{\n    public string Name => \"{name}\";\n    public string Description => \"Describe your plugin\";\n    public Version Version => new(1,0,0);\n    public void Start(){{}}\n    public void Stop(){{}}\n}}\n";
        File.WriteAllText(path, content);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
