using Cycloside.Core;
using Cycloside.Bridge;
using Cycloside.Input;
using Cycloside.SSH;
using Cycloside.Rules;
using Cycloside.Utils;

var bus = new EventBus();

// Wire bus logger
bus.Subscribe("*/", m => Console.WriteLine($"BUS {m.Topic}: {m.Payload}"));

// Start MQTT bridge (edit broker/creds as needed)
await using var mqtt = new MqttBridge(bus, broker: "localhost", port: 1883, username: null, password: null);
await mqtt.ConnectAsync(new[] { "#" });

// OSC bridge (listen 9000, send 9001)
using var osc = new OscBridge(bus, 9000, "127.0.0.1", 9001);

// Serial (comment if no serial)
try
{
    using var serial = new SerialBridge(bus, "COM3", 115200);
    bus.Subscribe("serial/out/*", m => serial.Send(m.Payload.GetProperty("text").GetString() ?? ""));
}
catch { Console.WriteLine("No COM3, skipping serial."); }

// MIDI + Gamepad
using var midi = new MidiRouter(bus);
midi.OpenAll();
using var gp = new GamepadRouter(bus);

// SSH
var sshProf = new SshProfile();
using var ssh = new SshClientManager(bus, sshProf);
try { ssh.Connect(); } catch { Console.WriteLine("SSH connect failed; edit profile in Program.cs"); }

// Rules
var rules = new List<Rule>
{
    new Rule{ Name="Toast on MIDI", Trigger=TriggerType.BusTopic, TriggerExpr="midi/*", Action=ActionType.ShowToast, ActionExpr="MIDI!" },
    new Rule{ Name="Heartbeat", Trigger=TriggerType.Timer, TriggerExpr="5s", Action=ActionType.PublishBus, ActionExpr="{\"topic\":\"heartbeat\",\"payload\":{\"now\":\"tick\"}}"},
};
using var engine = new RuleEngine(bus, rules);

// Utils
var screenshot = new ScreenshotAnnotator(bus);
var notes = new StickyNotesManager(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CyclosideNotes"));
var color = new ColorPickerTool(bus);
var ruler = new PixelRuler();
var host = new HtmlMarkdownHost();
var py = new PythonRunner(bus);
using var qs = new QuickShareServer(bus, 0);
qs.Start();

Console.WriteLine("Cycloside SampleHost running. Commands:");
Console.WriteLine(" s = screenshot+annotate; n = new note; N = load notes; c = pick color; r = ruler; h = open markdown (README.md); p = run python; q = quit");

bool running = true;
while (running)
{
    var key = Console.ReadKey(true).KeyChar;
    switch (key)
    {
        case 's':
            var img = screenshot.CaptureRegionAndAnnotate();
            Console.WriteLine("Screenshot captured + annotated.");
            break;
        case 'n':
            notes.NewNote();
            break;
        case 'N':
            notes.LoadAll();
            break;
        case 'c':
            var picked = color.PickOnce();
            Console.WriteLine($"Picked #{picked.R:X2}{picked.G:X2}{picked.B:X2}");
            break;
        case 'r':
            ruler.ShowRuler();
            break;
        case 'h':
            var md = Path.Combine(AppContext.BaseDirectory, "README.md");
            if (!File.Exists(md)) await File.WriteAllTextAsync(md, "# Hello from Cycloside\\n\\n**Markdown works.**");
            await host.ShowMarkdownAsync(md);
            break;
        case 'p':
            var outp = py.Run("print('hello from ironpython')\\nfor i in range(3): print(i)");
            Console.WriteLine(outp);
            break;
        case 'q':
            running = false;
            break;
    }
}
