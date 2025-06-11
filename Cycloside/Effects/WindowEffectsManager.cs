using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;

namespace Cycloside.Effects;

public class WindowEffectsManager
{
    private readonly Dictionary<string, IWindowEffect> _registered = new();
    private readonly Dictionary<Window, List<IWindowEffect>> _active = new();
    private readonly Dictionary<string, List<string>> _config;

    public static WindowEffectsManager Instance { get; } = new();

    private WindowEffectsManager()
    {
        _config = SettingsManager.Settings.WindowEffects;

        RegisterEffect(new RollUpEffect());
        RegisterEffect(new WobblyWindowEffect());

        var path = Path.Combine(AppContext.BaseDirectory, "Effects");
        LoadEffectPlugins(path);
    }

    public void RegisterEffect(IWindowEffect effect)
    {
        _registered[effect.Name] = effect;
    }

    public void LoadEffectPlugins(string directory)
    {
        if (!Directory.Exists(directory))
            return;

        foreach (var dll in Directory.GetFiles(directory, "*.dll"))
        {
            try
            {
                var asm = Assembly.LoadFrom(dll);
                var types = asm.GetTypes().Where(t => typeof(IWindowEffect).IsAssignableFrom(t) && !t.IsAbstract);
                foreach (var t in types)
                {
                    if (Activator.CreateInstance(t) is IWindowEffect effect)
                        RegisterEffect(effect);
                }
            }
            catch { }
        }
    }

    public void EnableEffectFor(string key, string effectName)
    {
        if (!_config.TryGetValue(key, out var list))
        {
            list = new List<string>();
            _config[key] = list;
        }

        if (!list.Contains(effectName))
            list.Add(effectName);

        SettingsManager.Save();
    }

    public void DisableEffectFor(string key, string effectName)
    {
        if (_config.TryGetValue(key, out var list))
        {
            list.Remove(effectName);
            if (list.Count == 0)
                _config.Remove(key);
            SettingsManager.Save();
        }
    }

    public void ApplyConfiguredEffects(Window window, string key)
    {
        if (_config.TryGetValue("*", out var global))
        {
            foreach (var name in global)
                AttachEffect(window, name);
        }

        if (_config.TryGetValue(key, out var specific))
        {
            foreach (var name in specific)
                AttachEffect(window, name);
        }
    }

    public void AttachEffect(Window window, string effectName)
    {
        if (!_registered.TryGetValue(effectName, out var effect))
            return;

        if (!_active.TryGetValue(window, out var list))
        {
            list = new List<IWindowEffect>();
            _active[window] = list;
            window.Closed += (_, _) => DetachAll(window);
        }

        if (list.Contains(effect))
            return;

        effect.Attach(window);
        list.Add(effect);
    }

    public void DetachAll(Window window)
    {
        if (!_active.TryGetValue(window, out var list))
            return;

        foreach (var effect in list)
            effect.Detach(window);

        _active.Remove(window);
    }
}
