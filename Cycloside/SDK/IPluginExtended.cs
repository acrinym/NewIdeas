using System;

namespace Cycloside.Plugins;

public interface IPluginExtended : IPlugin
{
    void OnSettingsSaved();
    void OnCrash(Exception ex);
}
