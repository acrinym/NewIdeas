using System.Text;
using System.Windows.Forms;
using Cycloside.Core;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace Cycloside.Utils;

public sealed class PythonRunner
{
    private readonly EventBus _bus;
    private readonly ScriptEngine _engine;

    public PythonRunner(EventBus bus)
    {
        _bus = bus;
        _engine = Python.CreateEngine();
        // basic 'policy': remove socket/http imports by overriding import hook
        var script = @"
import builtins
_real_import = builtins.__import__
def _guarded_import(name, globals=None, locals=None, fromlist=(), level=0):
    forbidden = ['socket','urllib','http','ftplib','telnetlib','ssl']
    if name in forbidden or any(x in name for x in forbidden):
        raise ImportError('Network modules are disabled')
    return _real_import(name, globals, locals, fromlist, level)
builtins.__import__ = _guarded_import
";
        _engine.Execute(script);
    }

    public string Run(string code, Dictionary<string, object>? vars = null)
    {
        var scope = _engine.CreateScope();
        if (vars != null) foreach (var kv in vars) scope.SetVariable(kv.Key, kv.Value);
        var sb = new StringBuilder();
        _engine.Runtime.IO.SetOutput(new MemoryStreamWriter(sb), Encoding.UTF8);
        _engine.Runtime.IO.SetErrorOutput(new MemoryStreamWriter(sb), Encoding.UTF8);
        try
        {
            _engine.Execute(code, scope);
        }
        catch (Exception ex)
        {
            sb.AppendLine(ex.ToString());
        }
        var output = sb.ToString();
        _bus.Publish("python/run", new { ok = true, len = output.Length });
        return output;
    }

    private sealed class MemoryStreamWriter : Stream
    {
        private readonly StringBuilder _sb;
        public MemoryStreamWriter(StringBuilder sb) => _sb = sb;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get => 0; set { } }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => 0;
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count)
        {
            _sb.Append(Encoding.UTF8.GetString(buffer, offset, count));
        }
    }
}
