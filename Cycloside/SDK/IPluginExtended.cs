using System;

namespace SiloCide.SDK;

public interface IPluginExtended : IPlugin
{
    void OnSettingsSaved();
    void OnCrash(Exception ex);
}
