using Cycloside.Core;
using NAudio.Midi;

namespace Cycloside.Input;

public sealed class MidiRouter : IDisposable
{
    private readonly EventBus _bus;
    private readonly List<MidiIn> _ins = new();
    private readonly string _topic;

    public MidiRouter(EventBus bus, string topicPrefix = "midi")
    {
        _bus = bus;
        _topic = $"{topicPrefix}/in";
    }

    public void OpenAll()
    {
        for (int i = 0; i < MidiIn.NumberOfDevices; i++)
        {
            var mi = new MidiIn(i);
            mi.MessageReceived += (s, e) =>
            {
                _bus.Publish(_topic, new { device = MidiIn.DeviceInfo(i).ProductName, cmd = e.MidiEvent.CommandCode.ToString(), data = e.RawMessage });
            };
            mi.ErrorReceived += (s, e) => { };
            mi.Start();
            _ins.Add(mi);
        }
    }

    public void Dispose()
    {
        foreach (var i in _ins)
        {
            try { i.Stop(); i.Close(); i.Dispose(); } catch { }
        }
        _ins.Clear();
    }
}
