using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Cycloside.Plugins;

namespace Cycloside;

public class RemoteApiServer
{
    private readonly PluginManager _manager;
    private HttpListener? _listener;

    public RemoteApiServer(PluginManager manager)
    {
        _manager = manager;
    }

    public void Start(int port = 4123)
    {
        if (!HttpListener.IsSupported) return;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        _ = Listen();
    }

    private async Task Listen()
    {
        while (_listener?.IsListening == true)
        {
            var ctx = await _listener.GetContextAsync();
            _ = Task.Run(() => Handle(ctx));
        }
    }

    private void Handle(HttpListenerContext ctx)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath?.Trim('/');
            if (path == "trigger" && ctx.Request.HttpMethod == "POST")
            {
                using var sr = new StreamReader(ctx.Request.InputStream);
                var body = sr.ReadToEnd();
                PluginBus.Publish(body);
                ctx.Response.StatusCode = 200;
            }
            else
            {
                ctx.Response.StatusCode = 404;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Remote API error: {ex.Message}");
            ctx.Response.StatusCode = 500;
        }
        finally
        {
            try { ctx.Response.Close(); } catch { }
        }
    }

    public void Stop()
    {
        _listener?.Stop();
        _listener = null;
    }
}
