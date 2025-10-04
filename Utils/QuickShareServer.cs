using System.Net;
using System.Text;
using System.Web;
using System.Windows.Forms;
using Cycloside.Core;
using QRCoder;

namespace Cycloside.Utils;

public sealed class QuickShareServer : IDisposable
{
    private readonly EventBus _bus;
    private readonly HttpListener _listener = new();
    private CancellationTokenSource? _cts;
    private readonly string _root;

    public int Port { get; }
    public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

    public QuickShareServer(EventBus bus, int port = 0, string? root = null)
    {
        _bus = bus;
        _root = root ?? Path.Combine(Path.GetTempPath(), "CyclosideQuickShare");
        Directory.CreateDirectory(_root);
        Port = port == 0 ? GetFreePort() : port;
        _listener.Prefixes.Add($"http://+:{Port}/");
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener.Start();
        Task.Run(() => Loop(_cts.Token));
        ShowQr();
        _bus.Publish("quickshare/start", new { port = Port, root = _root });
    }

    private async Task Loop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx = await _listener.GetContextAsync();
            _ = Task.Run(() => Handle(ctx), ct);
        }
    }

    private async Task Handle(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        if (req.HttpMethod == "GET")
        {
            var path = HttpUtility.UrlDecode(req.Url!.LocalPath.Trim('/'));
            if (string.IsNullOrEmpty(path))
            {
                // list files + upload form
                var files = Directory.GetFiles(_root).Select(Path.GetFileName).OrderBy(x => x).ToArray();
                var html = new StringBuilder();
                html.Append("<html><body><h1>QuickShare</h1><form method='POST' enctype='multipart/form-data'>");
                html.Append("<input type='file' name='file'/><input type='submit' value='Upload'></form><ul>");
                foreach (var f in files) html.Append($"<li><a href='/{HttpUtility.UrlEncode(f)}'>{f}</a></li>");
                html.Append("</ul></body></html>");
                await WriteHtml(res, html.ToString());
            }
            else
            {
                var full = Path.Combine(_root, path);
                if (!File.Exists(full)) { res.StatusCode = 404; res.Close(); return; }
                await using var fs = File.OpenRead(full);
                res.ContentType = "application/octet-stream";
                res.AddHeader("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(full)}\"");
                await fs.CopyToAsync(res.OutputStream);
                res.Close();
            }
        }
        else if (req.HttpMethod == "POST")
        {
            var boundary = req.ContentType!.Split("boundary=")[1];
            var ms = new MemoryStream();
            await req.InputStream.CopyToAsync(ms);
            ms.Position = 0;
            var bytes = ms.ToArray();
            // naive multipart parser (single file)
            var marker = Encoding.UTF8.GetBytes("--" + boundary);
            int start = IndexOf(bytes, marker) + marker.Length + 2;
            int headerEnd = IndexOf(bytes, Encoding.UTF8.GetBytes("\r\n\r\n"), start) + 4;
            int end = IndexOf(bytes, marker, headerEnd) - 2;
            var headers = Encoding.UTF8.GetString(bytes, start, headerEnd - start - 4);
            var filename = "upload.bin";
            var fnKey = "filename=\"";
            var idx = headers.IndexOf(fnKey);
            if (idx >= 0)
            {
                var idx2 = headers.IndexOf('"', idx + fnKey.Length);
                filename = headers.Substring(idx + fnKey.Length, idx2 - (idx + fnKey.Length));
            }
            var fileBytes = bytes.Skip(headerEnd).Take(end - headerEnd).ToArray();
            var path = Path.Combine(_root, filename);
            await File.WriteAllBytesAsync(path, fileBytes);
            _bus.Publish("quickshare/upload", new { path });
            res.Redirect("/");
            res.Close();
        }
    }

    private static int IndexOf(byte[] haystack, byte[] needle, int start = 0)
    {
        for (int i = start; i <= haystack.Length - needle.Length; i++)
        {
            int j = 0;
            for (; j < needle.Length; j++) if (haystack[i + j] != needle[j]) break;
            if (j == needle.Length) return i;
        }
        return -1;
    }

    private void ShowQr()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ips = host.AddressList.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToArray();
            if (ips.Length == 0) return;
            var url = $"http://{ips[0]}:{Port}/";
            using var gen = new QRCodeGenerator();
            using var data = gen.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            using var qr = new QRCode(data);
            using var img = qr.GetGraphic(6);
            var f = new Form { Text = "QuickShare URL (scan me)", Width = 400, Height = 450, TopMost = true };
            var pb = new PictureBox { Dock = DockStyle.Fill, Image = img };
            var tb = new TextBox { Dock = DockStyle.Bottom, ReadOnly = true, Text = url };
            f.Controls.Add(pb); f.Controls.Add(tb);
            f.Show();
        }
        catch { }
    }

    private static int GetFreePort()
    {
        var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var p = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return p;
    }

    private static async Task WriteHtml(HttpListenerResponse res, string html)
    {
        var bytes = Encoding.UTF8.GetBytes(html);
        res.ContentType = "text/html; charset=utf-8";
        await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        res.Close();
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener.Stop(); } catch { }
        _listener.Close();
    }
}
