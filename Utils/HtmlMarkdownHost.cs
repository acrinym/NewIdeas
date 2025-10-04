using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Markdig;

namespace Cycloside.Utils;

public sealed class HtmlMarkdownHost
{
    private readonly Form _window;
    private readonly WebView2 _web;

    public HtmlMarkdownHost(string title = "HTML/Markdown Host")
    {
        _window = new Form { Text = title, Width = 900, Height = 700, StartPosition = FormStartPosition.CenterScreen };
        _web = new WebView2 { Dock = DockStyle.Fill };
        _window.Controls.Add(_web);
    }

    public async Task ShowHtmlAsync(string htmlPath)
    {
        await _web.EnsureCoreWebView2Async();
        _web.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
        _window.Show();
    }

    public async Task ShowMarkdownAsync(string mdPath)
    {
        var markdown = await File.ReadAllTextAsync(mdPath);
        var html = Markdown.ToHtml(markdown);
        await _web.EnsureCoreWebView2Async();
        _web.NavigateToString($"<html><head><meta charset='utf-8'><style>body{{font-family:Segoe UI;margin:24px;}}</style></head><body>{html}</body></html>");
        _window.Show();
    }
}
