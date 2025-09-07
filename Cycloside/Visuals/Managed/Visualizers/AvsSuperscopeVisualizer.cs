using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Cycloside.Plugins.BuiltIn; // AudioData

namespace Cycloside.Visuals.Managed.Visualizers;

/// <summary>
/// A self-contained AVS Superscope-style visualizer implemented in C#.
/// - Parses simple "Init/Frame/Beat/Point" blocks similar to Winamp AVS Superscope.
/// - Evaluates expressions (sin, cos, abs, sqrt, pow, floor, frac, rand, above, below, equal, if).
/// - Uses Avalonia DrawingContext for rendering, integrated with ManagedVisStyle for colors.
/// - Fully managed: no native DLLs, no Winamp dependencies.
/// </summary>
public sealed class AvsSuperscopeVisualizer : IManagedVisualizer, IManagedVisualizerConfigurable
{
    private readonly Random _rng = new();
    private DateTime _start;
    private AudioData _latest = new(new byte[1152], new byte[1152]);
    private AvsProgram _program = AvsProgram.CreateDefault();
    private ExpressionEnv _env = new();
    private int _points = 256;
    private double _time; // seconds
    private double _lastEnergy;
    private string _presetKey = "ManagedAvs.Preset";

    public string Name => "AVS Superscope";
    public string Description => "Executes Superscope-like AVS presets (managed)";

    public void Init()
    {
        _start = DateTime.UtcNow;
        _env = new ExpressionEnv(_rng);
        var saved = StateManager.Get(_presetKey);
        if (!string.IsNullOrWhiteSpace(saved))
        {
            TryLoadPreset(saved);
        }
        else
        {
            // Load a simple, working default preset so first render is visible
            TryLoadPreset(SamplePresets.Spiral);
        }
    }

    public void UpdateAudioData(AudioData data)
    {
        _latest = data;
        // Compute a very lightweight energy metric for beat/scale
        double sum = 0;
        var wf = data.Waveform;
        for (int i = 0; i < 512 && i < wf.Length; i++)
        {
            var v = (wf[i] - 127.5) / 127.5; // -1..1
            sum += v * v;
        }
        _lastEnergy = Math.Sqrt(sum / Math.Max(1, Math.Min(512, wf.Length)));
    }

    public void Render(DrawingContext context, Size size, TimeSpan elapsed)
    {
        // Clear background using managed style
        context.FillRectangle(ManagedVisStyle.Background(), new Rect(size));

        // Update time and environment base variables
        _time = (DateTime.UtcNow - _start).TotalSeconds;
        _env.SetConst("pi", Math.PI);
        _env.SetConst("$PI", Math.PI);
        _env.SetVar("t", _time);
        _env.SetVar("time", _time);

        // Derive v from current energy (0..1)
        var v = Math.Clamp(_lastEnergy * ManagedVisStyle.Sensitivity, 0.0, 1.0);
        _env.SetVar("v", v);
        _env.SetVar("energy", v);

        // Frame code (per-frame variables)
        ExecBlock(_program.Frame);

        // Optional beat hook: naive strobe based on energy spike
        if (v > 0.65 && _rng.NextDouble() < 0.04)
        {
            ExecBlock(_program.Beat);
        }

        // Determine number of points (n)
        _points = 256;
        if (_program.Init.Length > 0)
        {
            ExecBlock(_program.Init);
            _points = (int)Math.Clamp(_env.GetVarOr("n", 256), 16, 4096);
        }

        // Prepare output points
        var pts = new Point[_points];
        var width = size.Width;
        var height = size.Height;

        // Provide some common AVS-like variables
        _env.SetVar("w", width);
        _env.SetVar("h", height);

        // Precompute spectrum as floats 0..1
        var spec = _latest.Spectrum;
        var specF = new double[Math.Min(576, spec.Length)];
        for (int i = 0; i < specF.Length; i++) specF[i] = spec[i] / 255.0;

        // Generate point positions using the per-point block
        for (int i = 0; i < _points; i++)
        {
            var ni = _points > 1 ? (double)i / (_points - 1) : 0.0; // 0..1
            _env.SetVar("i", ni);

            // Provide a crude band amplitude 'b' for convenience
            var band = (int)(ni * (specF.Length - 1));
            _env.SetVar("b", band);
            _env.SetVar("sb", band < specF.Length && band >= 0 ? specF[band] : 0.0);

            // Reset x,y defaults before executing
            _env.SetVar("x", 0.0);
            _env.SetVar("y", 0.0);

            ExecBlock(_program.Point);

            var x = _env.GetVarOr("x", 0.0);
            var y = _env.GetVarOr("y", 0.0);

            // If user produced normalized -1..1, scale to pixels; otherwise treat as pixels if out of range
            double px = Math.Abs(x) <= 1.5 ? ((x + 1.0) * 0.5) * (width - 2) + 1 : x;
            double py = Math.Abs(y) <= 1.5 ? ((-y + 1.0) * 0.5) * (height - 2) + 1 : y;
            pts[i] = new Point(px, py);
        }

        // Render joined polyline
        var lineSize = Math.Clamp(_env.GetVarOr("linesize", 2.0), 0.5, 10.0);
        // Map optional RGB [0..1] to color if provided
        var hasR = _env.GetVarOr("red", double.NaN);
        var hasG = _env.GetVarOr("green", double.NaN);
        var hasB = _env.GetVarOr("blue", double.NaN);
        Pen stroke;
        if (!double.IsNaN(hasR) && !double.IsNaN(hasG) && !double.IsNaN(hasB))
        {
            byte r = (byte)Math.Clamp((int)Math.Round(hasR * 255.0), 0, 255);
            byte g = (byte)Math.Clamp((int)Math.Round(hasG * 255.0), 0, 255);
            byte b = (byte)Math.Clamp((int)Math.Round(hasB * 255.0), 0, 255);
            stroke = new Pen(new SolidColorBrush(Color.FromRgb(r, g, b)), lineSize);
        }
        else
        {
            stroke = new Pen(ManagedVisStyle.Accent(), lineSize);
        }
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            if (pts.Length > 0)
            {
                ctx.BeginFigure(pts[0], false);
                for (int i = 1; i < pts.Length; i++) ctx.LineTo(pts[i]);
                ctx.EndFigure(false);
            }
        }
        context.DrawGeometry(null, stroke, geo);

        // Optional peaks overlay for dynamics
        var peaks = ManagedVisStyle.Peak();
        for (int i = 0; i < pts.Length; i += Math.Max(1, pts.Length / 64))
        {
            var r = 1.5 + 3.0 * v;
            context.DrawEllipse(peaks, null, pts[i], r, r);
        }
    }

    public void Dispose() { /* nothing to dispose */ }

    public string ConfigKey => _presetKey;

    public Control BuildOptionsView()
    {
        // Build a compact options UI: preset selector + editable text + apply button
        var root = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var presets = new ComboBox { Width = 160, ItemsSource = new[] { "Spiral", "Rotating Bow", "Vertical Bounce", "Vibrating Worm" } };
        presets.SelectedIndex = 0;
        var applyPreset = new Button { Content = "Load Preset" };
        var tb = new TextBox { AcceptsReturn = true, MinHeight = 120, TextWrapping = TextWrapping.Wrap };
        tb.Text = _program.RawText;
        var apply = new Button { Content = "Apply Text" };
        var open = new Button { Content = "Open .avs..." };

        applyPreset.Click += (_, __) =>
        {
            var name = presets.SelectedItem?.ToString() ?? "Spiral";
            var code = name switch
            {
                "Rotating Bow" => SamplePresets.RotatingBow,
                "Vertical Bounce" => SamplePresets.VerticalBounce,
                "Vibrating Worm" => SamplePresets.VibratingWorm,
                _ => SamplePresets.Spiral
            };
            tb.Text = code;
        };

        apply.Click += (_, __) =>
        {
            TryLoadPreset(tb.Text ?? string.Empty);
            StateManager.Set(_presetKey, tb.Text ?? string.Empty);
        };

        open.Click += async (_, __) =>
        {
            await Task.Yield();
            OnOpenAvsButtonClick(tb);
        };

        row.Children.Add(new TextBlock { Text = "Preset:", VerticalAlignment = VerticalAlignment.Center });
        row.Children.Add(presets);
        row.Children.Add(applyPreset);
        row.Children.Add(open);
        root.Children.Add(row);
        root.Children.Add(tb);
        root.Children.Add(apply);
        return root;
    }

    public void LoadOptions() { /* options are loaded on Init via StateManager */ }

    private void ExecBlock(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return;
        // Split on semicolons and newlines; ignore // comments
        var parts = code.Replace("\r", string.Empty).Split(new[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in parts)
        {
            var line = raw;
            var com = line.IndexOf("//", StringComparison.Ordinal);
            if (com >= 0) line = line.Substring(0, com);
            line = line.Trim();
            if (line.Length == 0) continue;

            var eq = line.IndexOf('=');
            if (eq > 0)
            {
                var lhs = line.Substring(0, eq).Trim();
                var rhs = line.Substring(eq + 1).Trim();
                var val = _env.Eval(rhs);
                _env.SetVar(lhs, val);
            }
            else
            {
                // Expression without assignment: just evaluate for side-effects on RNG/etc
                _ = _env.Eval(line);
            }
        }
    }

    private void TryLoadPreset(string text)
    {
        // Accepts either simple blocks with labels or raw point-only code
        var prog = AvsProgram.Parse(text);
        _program = prog;
        // Run Init once to prime variables and point count
        _env = new ExpressionEnv(_rng);
        ExecBlock(_program.Init);
        _points = (int)Math.Clamp(_env.GetVarOr("n", 256), 16, 4096);
    }

    private static class SamplePresets
    {
        // A simple spiral using time t and index i
        public const string Spiral =
            "Init: n=800\n" +
            "Frame: t=t-0.05\n" +
            "Beat:\n" +
            "Point: d=i+v*0.2; r=t+i*$PI*4; x=cos(r)*d; y=sin(r)*d";

        public const string RotatingBow =
            "Init: n=80;t=0.0;\n" +
            "Frame: t=t+0.01\n" +
            "Beat:\n" +
            "Point: r=i*$PI*2; d=sin(r*3)+v*0.5; x=cos(t+r)*d; y=sin(t-r)*d";

        public const string VerticalBounce =
            "Init: n=100; t=0; tv=0.1;dt=1;\n" +
            "Frame: t=t*0.9+tv*0.1\n" +
            "Beat: tv=((rand(50.0)/50.0))*dt; dt=-dt;\n" +
            "Point: x=t+v*pow(sin(i*$PI),2); y=i*2-1.0;";

        public const string VibratingWorm =
            "Init: n=400; dt=0.01; t=0; sc=1;\n" +
            "Frame: t=t+dt;dt=0.9*dt+0.001; t=if(above(t,$PI*2),t-$PI*2,t);\n" +
            "Beat: dt=sc;sc=-sc;\n" +
            "Point: x=cos(2*i*6.283+t)*0.9*(v*0.5+0.5); y=sin(i*2*6.283+t)*0.9*(v*0.5+0.5);";
    }

    private sealed class AvsProgram
    {
        public string Init { get; private set; } = string.Empty;
        public string Frame { get; private set; } = string.Empty;
        public string Beat { get; private set; } = string.Empty;
        public string Point { get; private set; } = "x=i*2-1;y=0";
        public string RawText { get; private set; } = string.Empty;

        public static AvsProgram CreateDefault() => Parse(SamplePresets.Spiral);

        public static AvsProgram Parse(string text)
        {
            var p = new AvsProgram();
            p.RawText = text;
            var t = text.Replace("\r", string.Empty);
            if (t.IndexOf("init:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.IndexOf("frame:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.IndexOf("beat:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.IndexOf("point:", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Block format
                foreach (var line in t.Split('\n'))
                {
                    var s = line.Trim();
                    if (s.Length == 0) continue;
                    if (s.StartsWith("init:", StringComparison.OrdinalIgnoreCase)) p.Init = s.Substring(5).Trim();
                    else if (s.StartsWith("frame:", StringComparison.OrdinalIgnoreCase)) p.Frame = s.Substring(6).Trim();
                    else if (s.StartsWith("beat:", StringComparison.OrdinalIgnoreCase)) p.Beat = s.Substring(5).Trim();
                    else if (s.StartsWith("point:", StringComparison.OrdinalIgnoreCase)) p.Point = s.Substring(6).Trim();
                }
            }
            else
            {
                // Treat as point-only expression block
                p.Point = t;
            }
            return p;
        }
    }

    private sealed class ExpressionEnv
    {
        private readonly Dictionary<string, double> _vars = new(StringComparer.OrdinalIgnoreCase);
        private readonly Random _rng;

        public ExpressionEnv(Random rng)
        {
            _rng = rng;
            SetConst("pi", Math.PI);
            SetConst("$PI", Math.PI);
        }
        public ExpressionEnv() : this(new Random()) { }

        public void SetConst(string name, double value) => _vars[name] = value;
        public void SetVar(string name, double value) => _vars[name] = value;
        public double GetVarOr(string name, double fallback) => _vars.TryGetValue(name, out var v) ? v : fallback;

        public double Eval(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return 0;
            // Replace common constants
            expr = expr.Replace("$PI", "pi", StringComparison.OrdinalIgnoreCase);
            var parser = new ExprParser(_vars, _rng);
            return parser.Evaluate(expr);
        }
    }

    private sealed class ExprParser
    {
        private readonly Dictionary<string, double> _vars;
        private readonly Random _rng;

        public ExprParser(Dictionary<string, double> vars, Random rng)
        {
            _vars = vars; _rng = rng;
        }

        public double Evaluate(string input)
        {
            // Simple tokenizer + shunting-yard to RPN + evaluator
            var tokens = Tokenize(input);
            var rpn = ToRpn(tokens);
            return EvalRpn(rpn);
        }

        private enum TokType { Num, Id, Op, LParen, RParen, Comma }
        private readonly record struct Tok(TokType T, string S);

        private IEnumerable<Tok> Tokenize(string s)
        {
            int i = 0;
            while (i < s.Length)
            {
                var c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (char.IsDigit(c) || (c == '.' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
                {
                    int j = i + 1;
                    while (j < s.Length && (char.IsDigit(s[j]) || s[j] == '.')) j++;
                    yield return new Tok(TokType.Num, s.Substring(i, j - i)); i = j; continue;
                }
                if (char.IsLetter(c) || c == '_' || c == '$')
                {
                    int j = i + 1;
                    while (j < s.Length && (char.IsLetterOrDigit(s[j]) || s[j] == '_' || s[j] == '$')) j++;
                    yield return new Tok(TokType.Id, s.Substring(i, j - i)); i = j; continue;
                }
                if (c == '(') { yield return new Tok(TokType.LParen, "("); i++; continue; }
                if (c == ')') { yield return new Tok(TokType.RParen, ")"); i++; continue; }
                if (c == ',') { yield return new Tok(TokType.Comma, ","); i++; continue; }
                if ("+-*/%^".Contains(c)) { yield return new Tok(TokType.Op, c.ToString()); i++; continue; }
                // Unknown char: skip
                i++;
            }
        }

        private static int Prec(string op) => op switch
        {
            "^" => 4,
            "*" or "/" or "%" => 3,
            "+" or "-" => 2,
            _ => 1
        };

        private static bool RightAssoc(string op) => op == "^";

        private IEnumerable<Tok> ToRpn(IEnumerable<Tok> toks)
        {
            var outQ = new List<Tok>();
            var stack = new Stack<Tok>();
            var it = toks.GetEnumerator();
            Tok prev = default;
            while (it.MoveNext())
            {
                var t = it.Current;
                switch (t.T)
                {
                    case TokType.Num: outQ.Add(t); break;
                    case TokType.Id:
                        // function or variable: lookahead for '(' to detect function
                        if (stack.Count > 0 && stack.Peek().T == TokType.Id)
                            outQ.Add(stack.Pop());
                        stack.Push(t);
                        break;
                    case TokType.Op:
                        while (stack.Count > 0 && stack.Peek().T == TokType.Op &&
                               (Prec(stack.Peek().S) > Prec(t.S) || (Prec(stack.Peek().S) == Prec(t.S) && !RightAssoc(t.S))))
                        {
                            outQ.Add(stack.Pop());
                        }
                        outQ.Add(t);
                        break;
                    case TokType.LParen:
                        stack.Push(t);
                        break;
                    case TokType.RParen:
                        while (stack.Count > 0 && stack.Peek().T != TokType.LParen) outQ.Add(stack.Pop());
                        if (stack.Count > 0 && stack.Peek().T == TokType.LParen) stack.Pop();
                        // If there is a function id on stack, move it to output
                        if (stack.Count > 0 && stack.Peek().T == TokType.Id) outQ.Add(stack.Pop());
                        break;
                    case TokType.Comma:
                        // drain until LParen
                        while (stack.Count > 0 && stack.Peek().T != TokType.LParen) outQ.Add(stack.Pop());
                        break;
                }
                prev = t;
            }
            while (stack.Count > 0) outQ.Add(stack.Pop());
            return outQ;
        }

        private double EvalRpn(IEnumerable<Tok> rpn)
        {
            var st = new Stack<double>();
            var args = new Stack<List<double>>();
            foreach (var t in rpn)
            {
                switch (t.T)
                {
                    case TokType.Num:
                        st.Push(double.Parse(t.S, CultureInfo.InvariantCulture));
                        break;
                    case TokType.Id:
                        // function call: collect arguments from stack if they were just pushed via commas/LParen handling
                        var fname = t.S.ToLowerInvariant();
                        if (IsFunction(fname))
                        {
                            // functions are called with arguments already on stack separated by markers – but our simple RPN builder
                            // places arguments in order, so here we pop based on arity hints when possible; otherwise try 1-3.
                            // We support variable-arity funcs via stacking rules by commas+parentheses; here, assume arity by name.
                            var arity = FuncArity(fname);
                            var argv = new double[arity];
                            for (int i = arity - 1; i >= 0; i--) argv[i] = st.Count > 0 ? st.Pop() : 0.0;
                            st.Push(CallFunc(fname, argv));
                        }
                        else
                        {
                            st.Push(_vars.TryGetValue(t.S, out var v) ? v : 0.0);
                        }
                        break;
                    case TokType.Op:
                        var b = st.Count > 0 ? st.Pop() : 0.0;
                        var a = st.Count > 0 ? st.Pop() : 0.0;
                        st.Push(t.S switch
                        {
                            "+" => a + b,
                            "-" => a - b,
                            "*" => a * b,
                            "/" => b == 0 ? 0.0 : a / b,
                            "%" => b == 0 ? 0.0 : a % b,
                            "^" => Math.Pow(a, b),
                            _ => 0.0
                        });
                        break;
                }
            }
            return st.Count > 0 ? st.Pop() : 0.0;
        }

        private static bool IsFunction(string id) => id is
            "sin" or "cos" or "tan" or "sqrt" or "abs" or "pow" or "floor" or "frac" or
            "asin" or "acos" or "atan" or "atan2" or "sqr" or "min" or "max" or "clamp" or
            "rand" or "above" or "below" or "equal" or "if";

        private static int FuncArity(string id) => id switch
        {
            "pow" => 2,
            "rand" => 1,
            "above" => 2,
            "below" => 2,
            "equal" => 2,
            "if" => 3,
            "atan2" => 2,
            "sqr" => 1,
            "min" => 2,
            "max" => 2,
            "clamp" => 3,
            _ => 1
        };

        private double CallFunc(string name, IReadOnlyList<double> a)
        {
            switch (name)
            {
                case "sin": return Math.Sin(a[0]);
                case "cos": return Math.Cos(a[0]);
                case "tan": return Math.Tan(a[0]);
                case "sqrt": return Math.Sqrt(Math.Abs(a[0]));
                case "abs": return Math.Abs(a[0]);
                case "pow": return Math.Pow(a[0], a[1]);
                case "floor": return Math.Floor(a[0]);
                case "frac": return a[0] - Math.Floor(a[0]);
                case "asin": return Math.Asin(Math.Clamp(a[0], -1.0, 1.0));
                case "acos": return Math.Acos(Math.Clamp(a[0], -1.0, 1.0));
                case "atan": return Math.Atan(a[0]);
                case "atan2": return Math.Atan2(a[0], a[1]);
                case "sqr": return a[0] * a[0];
                case "min": return Math.Min(a[0], a[1]);
                case "max": return Math.Max(a[0], a[1]);
                case "clamp": return Math.Min(Math.Max(a[0], a[1]), a[2]);
                case "rand": return _rng.NextDouble() * a[0];
                case "above": return a[0] > a[1] ? 1.0 : 0.0;
                case "below": return a[0] < a[1] ? 1.0 : 0.0;
                case "equal": return Math.Abs(a[0] - a[1]) < 1e-6 ? 1.0 : 0.0;
                case "if": return a[0] != 0.0 ? a[1] : a[2];
                default: return 0.0;
            }
        }
    }

    // Wire up the Open .avs... button (declared above) with async file picker and preset load.
    // Implemented as local function to keep all logic in this source file, avoiding glue elsewhere.
    private async void OnOpenAvsButtonClick(TextBox target)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null)
            return;
        try
        {
            var files = await desktop.MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Open AVS Superscope Preset",
                FileTypeFilter = new[] { new FilePickerFileType("AVS/Superscope") { Patterns = new[] { "*.avs", "*.txt" } } }
            });
            var file = files.FirstOrDefault();
            if (file != null)
            {
                await using var s = await file.OpenReadAsync();
                using var sr = new StreamReader(s);
                var text = await sr.ReadToEndAsync();
                target.Text = text;
                TryLoadPreset(text);
                StateManager.Set(_presetKey, text);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to open AVS preset: {ex.Message}");
        }
    }
}
