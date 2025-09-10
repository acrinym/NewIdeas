using System;
using Cycloside.Plugins;
using Cycloside.Services;
using Cycloside.Widgets;

namespace Cycloside.SDK.Examples.KitchenSink
{
    public class KitchenSinkPlugin : IPlugin
    {
        private KitchenSinkWindow? _window;
        private KitchenSinkWidget? _widget;

        public string Name => "SDK Kitchen Sink";
        public string Description => "An example plugin that demonstrates various SDK features.";
        public Version Version => new(1, 0, 0);
        public IWidget? Widget => _widget;
        public bool ForceDefaultTheme => false;

        public void Start()
        {
            _widget = new KitchenSinkWidget();
            _window = new KitchenSinkWindow
            {
                DataContext = new KitchenSinkViewModel()
            };
            _window.Show();

            // Example of subscribing to a PluginBus event
            PluginBus.Subscribe("kitchensink:message", (payload) =>
            {
                if (payload is string message)
                {
                    // We would typically update the ViewModel here
                    Logger.Log($"Kitchen Sink plugin received message: {message}");
                }
            });
        }

        public void Stop()
        {
            _window?.Close();
            _widget = null;
            // It's good practice to remove the specific handler when unsubscribing,
            // but for this example, we'll just remove all listeners for this event.
            // A more robust implementation would store the handler in a field.
            // PluginBus.Unsubscribe("kitchensink:message", handler);
        }
    }
}
