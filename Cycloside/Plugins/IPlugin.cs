namespace Cycloside.Plugins;

public interface IPlugin
{
    string Name { get; }
    void Start();
    void Stop();
}
