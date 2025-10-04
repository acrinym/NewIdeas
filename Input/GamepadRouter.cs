using Cycloside.Core;
using SharpDX.XInput;

namespace Cycloside.Input;

public sealed class GamepadRouter : IDisposable
{
    private readonly EventBus _bus;
    private readonly Controller[] _controllers;
    private readonly Timer _timer;
    private readonly string _topicPrefix;

    public GamepadRouter(EventBus bus, string topicPrefix = "gamepad", int pollMs = 30)
    {
        _bus = bus;
        _topicPrefix = topicPrefix;
        _controllers = new[] { new Controller(UserIndex.One), new Controller(UserIndex.Two), new Controller(UserIndex.Three), new Controller(UserIndex.Four) };
        _timer = new Timer(Poll, null, pollMs, pollMs);
    }

    private void Poll(object? _)
    {
        for (int i = 0; i < _controllers.Length; i++)
        {
            var c = _controllers[i];
            if (!c.IsConnected) continue;
            var state = c.GetState();
            _bus.Publish($"{_topicPrefix}/state", new
            {
                index = i + 1,
                buttons = state.Gamepad.Buttons.ToString(),
                lx = state.Gamepad.LeftThumbX,
                ly = state.Gamepad.LeftThumbY,
                rx = state.Gamepad.RightThumbX,
                ry = state.Gamepad.RightThumbY,
                lt = state.Gamepad.LeftTrigger,
                rt = state.Gamepad.RightTrigger
            });
        }
    }

    public void Dispose() => _timer.Dispose();
}
