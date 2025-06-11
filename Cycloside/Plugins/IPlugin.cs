using System;

namespace Cycloside.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        Version Version { get; }

        /// <summary>
        /// Optional widget surface for this plugin. Returning null means the
        /// plugin does not expose a widget.
        /// </summary>
        Widgets.IWidget? Widget { get; }

        void Start();
        void Stop();
    }
}
