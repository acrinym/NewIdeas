using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Cycloside.Core;

namespace Cycloside.Utils;

public sealed class ColorPickerTool
{
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("gdi32.dll")] static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    private readonly EventBus _bus;
    public ColorPickerTool(EventBus bus) { _bus = bus; }

    public Color PickOnce()
    {
        using var f = new Form { Opacity = 0, ShowInTaskbar = false, WindowState = FormWindowState.Maximized, TopMost = true };
        f.Show();
        System.Windows.Forms.Cursor.Current = Cursors.Cross;
        var pos = System.Windows.Forms.Cursor.Position;
        var dc = GetDC(IntPtr.Zero);
        var pixel = GetPixel(dc, pos.X, pos.Y);
        ReleaseDC(IntPtr.Zero, dc);
        System.Windows.Forms.Cursor.Current = Cursors.Default;
        var color = Color.FromArgb((int)(pixel & 0x000000FF), (int)((pixel & 0x0000FF00) >> 8), (int)((pixel & 0x00FF0000) >> 16));
        _bus.Publish("color/selected", new { r = color.R, g = color.G, b = color.B, hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}" });
        return color;
    }
}
