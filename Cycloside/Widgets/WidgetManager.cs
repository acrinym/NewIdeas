using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cycloside.Widgets;

public class WidgetManager
{
    private readonly List<IWidget> _widgets = new();
    public IReadOnlyList<IWidget> Widgets => _widgets;

    public void LoadBuiltIn()
    {
        var asm = Assembly.GetExecutingAssembly();
        var types = asm.GetTypes().Where(t => typeof(IWidget).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
        foreach (var t in types)
        {
            if (Activator.CreateInstance(t) is IWidget w)
                _widgets.Add(w);
        }
    }
}
