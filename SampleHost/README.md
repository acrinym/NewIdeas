# CyclosideNextFeatures

A .NET 8 multi-project repo delivering working implementations for:
- Serial/MQTT/OSC Bridge
- MIDI & Gamepad Input Router
- SSH Console (profiles, run commands, tail)
- Rules Engine (topic/file/process/timer → actions)
- Screenshot & Annotate (WinForms overlay)
- Sticky Notes (persisted JSON windows)
- Color Picker & Pixel Ruler
- HTML/Markdown Host (WebView2 + Markdig)
- Python Volatile (IronPython runner, network-disabled imports)
- LAN QuickShare (HttpListener + QR URL)

## Build
```bash
dotnet build
cd SampleHost
dotnet run
```

While SampleHost is running:

* s = screenshot+annotate (emits `screenshot/capture`)
* n = new note / N = load notes
* c = pick color (emits `color/selected`)
* r = show pixel ruler
* h = open markdown (README.md)
* p = run IronPython sample
* q = quit

Notes

* MQTT subscribes to `#`, publishes on `mqtt/out/*` (edit broker in Program.cs).
* OSC listens on 9000, sends to 127.0.0.1:9001.
* Serial targets COM3@115200 by default (edit in Program.cs).
* RulesEngine includes working examples; extend by adding Rule objects or JSON piping from your app.
* Utils project targets `net8.0-windows` with WinForms for speed and reliability; easy to port the services into Avalonia later if you prefer.
