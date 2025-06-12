using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Cycloside.Plugins;

namespace Cycloside;

public class RemoteApiServer
{
    private readonly PluginManager _manager;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

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
        _cts = new CancellationTokenSource();
        _ = Listen(_cts.Token);
    }

    private async Task Listen(CancellationToken token)
    {
        while (_listener?.IsListening == true && !token.IsCancellationRequested)
        {
            HttpListenerContext? ctx = null;
            try
            {
                var contextTask = _listener.GetContextAsync();
                var completed = await Task.WhenAny(contextTask, Task.Delay(Timeout.Infinite, token));
                if (completed != contextTask)
                    break;
                ctx = await contextTask;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            if (ctx != null)
            {
                _ = Task.Run(() => Handle(ctx), token);
            }
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
        _cts?.Cancel();
        _listener?.Close();
        _listener = null;
        _cts?.Dispose();
        _cts = null;
    }
}
