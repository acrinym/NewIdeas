using System.Drawing;
using System.Windows.Forms;
using Cycloside.Core;

namespace Cycloside.Utils;

public sealed class ScreenshotAnnotator
{
    private readonly EventBus _bus;
    public ScreenshotAnnotator(EventBus bus) { _bus = bus; }

    public Image CaptureRegionAndAnnotate()
    {
        Rectangle rect = SelectRegion();
        if (rect.Width <= 0 || rect.Height <= 0) return new Bitmap(1, 1);

        using var bmp = new Bitmap(rect.Width, rect.Height);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(rect.Location, Point.Empty, rect.Size);
        }

        var img = (Image)bmp.Clone();
        ShowAnnotator(img);
        return img;
    }

    private static Rectangle SelectRegion()
    {
        Rectangle selected = Rectangle.Empty;
        using var overlay = new Form { FormBorderStyle = FormBorderStyle.None, Opacity = 0.25, BackColor = Color.Black, WindowState = FormWindowState.Maximized, TopMost = true };
        Point start = Point.Empty, end = Point.Empty;
        bool dragging = false;
        overlay.Cursor = Cursors.Cross;
        overlay.MouseDown += (_, e) => { dragging = true; start = e.Location; };
        overlay.MouseMove += (_, e) => { if (dragging) { end = e.Location; overlay.Invalidate(); } };
        overlay.MouseUp += (_, e) => { dragging = false; end = e.Location; overlay.Close(); };
        overlay.Paint += (_, e) =>
        {
            if (dragging)
            {
                var r = new Rectangle(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y),
                                      Math.Abs(start.X - end.X), Math.Abs(start.Y - end.Y));
                using var p = new Pen(Color.Red, 2);
                e.Graphics.DrawRectangle(p, r);
            }
        };
        overlay.ShowDialog();
        selected = new Rectangle(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y),
                                 Math.Abs(start.X - end.X), Math.Abs(start.Y - end.Y));
        return selected;
    }

    private void ShowAnnotator(Image image)
    {
        var form = new Form { Text = "Annotate Screenshot", Width = image.Width + 200, Height = image.Height + 100, TopMost = true };
        var pb = new PictureBox { Image = image, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill };
        var tools = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 180 };
        var color = Color.Red; int size = 3;
        var btnSave = new Button { Text = "Save" };
        var btnPen = new Button { Text = "Pen" };
        var btnRect = new Button { Text = "Box" };
        var btnText = new Button { Text = "Text" };
        var btnCopy = new Button { Text = "Copy" };

        tools.Controls.AddRange(new Control[] { btnSave, btnCopy, btnPen, btnRect, btnText });
        form.Controls.Add(pb);
        form.Controls.Add(tools);

        bool drawing = false; Point last = Point.Empty;
        string mode = "pen";
        btnPen.Click += (_, __) => mode = "pen";
        btnRect.Click += (_, __) => mode = "rect";
        btnText.Click += (_, __) => mode = "text";
        btnCopy.Click += (_, __) => { Clipboard.SetImage(pb.Image); _bus.Publish("screenshot/capture", new { where = "clipboard" }); };
        btnSave.Click += (_, __) =>
        {
            using var sfd = new SaveFileDialog() { Filter = "PNG|*.png" };
            if (sfd.ShowDialog() == DialogResult.OK) { pb.Image.Save(sfd.FileName); _bus.Publish("screenshot/capture", new { path = sfd.FileName }); }
        };

        pb.MouseDown += (_, e) => { drawing = true; last = e.Location; };
        pb.MouseMove += (_, e) =>
        {
            if (!drawing) return;
            using var g = Graphics.FromImage(pb.Image!);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if (mode == "pen")
            {
                g.DrawLine(new Pen(color, size), last, e.Location);
                last = e.Location;
            }
            pb.Invalidate();
        };
        pb.MouseUp += (_, e) =>
        {
            if (!drawing) return;
            drawing = false;
            if (mode == "rect")
            {
                var rect = new Rectangle(Math.Min(last.X, e.X), Math.Min(last.Y, e.Y), Math.Abs(last.X - e.X), Math.Abs(last.Y - e.Y));
                using var g = Graphics.FromImage(pb.Image!);
                using var p = new Pen(color, size);
                g.DrawRectangle(p, rect);
                pb.Invalidate();
            }
            if (mode == "text")
            {
                var text = Microsoft.VisualBasic.Interaction.InputBox("Text:", "Annotate", "");
                if (!string.IsNullOrEmpty(text))
                {
                    using var g = Graphics.FromImage(pb.Image!);
                    g.DrawString(text, SystemFonts.DefaultFont, Brushes.Yellow, e.Location);
                    pb.Invalidate();
                }
            }
        };
        form.ShowDialog();
    }
}
