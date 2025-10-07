using System.Drawing;
using System.Windows.Forms;

namespace Cycloside.Utils;

public sealed class PixelRuler
{
    public void ShowRuler()
    {
        var f = new Form { FormBorderStyle = FormBorderStyle.SizableToolWindow, Width = 800, Height = 100, TopMost = true, Text = "Pixel Ruler" };
        f.Paint += (s, e) =>
        {
            for (int x = 0; x < f.ClientSize.Width; x += 10)
            {
                e.Graphics.DrawLine(Pens.Black, x, 0, x, x % 50 == 0 ? 20 : 10);
                if (x % 50 == 0) e.Graphics.DrawString(x.ToString(), SystemFonts.DefaultFont, Brushes.Black, x + 2, 22);
            }
        };
        f.Show();
    }
}
