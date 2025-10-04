using System.Drawing;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace Cycloside.Utils;

public sealed class StickyNotesManager
{
    private readonly string _storeDir;
    public StickyNotesManager(string storeDir)
    {
        _storeDir = storeDir;
        Directory.CreateDirectory(_storeDir);
    }

    public void NewNote(Point? location = null, Size? size = null)
    {
        var id = Guid.NewGuid().ToString("N");
        var note = new NoteForm(_storeDir, id) { StartPosition = FormStartPosition.Manual };
        if (location != null) note.Location = location.Value;
        if (size != null) note.Size = size.Value;
        note.Show();
    }

    public void LoadAll()
    {
        foreach (var file in Directory.GetFiles(_storeDir, "*.json"))
        {
            var id = Path.GetFileNameWithoutExtension(file);
            var nf = new NoteForm(_storeDir, id);
            nf.Show();
        }
    }

    private sealed class NoteForm : Form
    {
        private readonly string _storeDir;
        private readonly string _id;
        private readonly TextBox _tb;
        private readonly Timer _saveTimer;

        public NoteForm(string storeDir, string id)
        {
            _storeDir = storeDir; _id = id;
            Text = "Sticky Note";
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            TopMost = true;
            BackColor = Color.LemonChiffon;
            _tb = new TextBox { Multiline = true, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Color.LemonChiffon, ScrollBars = ScrollBars.Vertical };
            Controls.Add(_tb);
            _saveTimer = new Timer { Interval = 1000 };
            _saveTimer.Tick += (_, __) => Save();
            _saveTimer.Start();
            LoadState();
        }

        private string PathJson => System.IO.Path.Combine(_storeDir, _id + ".json");

        private void LoadState()
        {
            if (!File.Exists(PathJson)) return;
            var json = File.ReadAllText(PathJson);
            var s = JsonSerializer.Deserialize<NoteState>(json)!;
            _tb.Text = s.Text;
            this.Location = new Point(s.X, s.Y);
            this.Size = new Size(s.W, s.H);
        }

        private void Save()
        {
            var s = new NoteState { Text = _tb.Text, X = Location.X, Y = Location.Y, W = Size.Width, H = Size.Height };
            File.WriteAllText(PathJson, JsonSerializer.Serialize(s));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Save();
            base.OnFormClosing(e);
        }

        private sealed class NoteState
        {
            public string Text { get; set; } = "";
            public int X { get; set; }
            public int Y { get; set; }
            public int W { get; set; }
            public int H { get; set; }
        }
    }
}
