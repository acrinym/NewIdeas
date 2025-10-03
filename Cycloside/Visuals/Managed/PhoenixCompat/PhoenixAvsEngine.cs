using System;
using System.Collections.Generic;
using System.Linq;

namespace Cycloside.Visuals.Managed.PhoenixCompat;

/// <summary>
/// Portable Phoenix AVS engine and simple Superscope evaluator adapted for Cycloside.
/// This reuses logic from the PhoenixVisualizer project but is self-contained to avoid external deps.
/// </summary>
public interface IAvsEngine
{
    void Initialize(int width, int height);
    void LoadPreset(string presetText);
    void Resize(int width, int height);
    void RenderFrame(IAudioFeatures features, ISimpleCanvas canvas);
}

public sealed class AvsEngine : IAvsEngine
{
    private int _width;
    private int _height;
    private Preset _preset = Preset.CreateDefault();

    public void Initialize(int width, int height)
    {
        _width = width; _height = height;
    }

    public void LoadPreset(string presetText)
    {
        try
        {
            var p = new Preset();
            if (presetText.IndexOf("init:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                presetText.IndexOf("per_frame:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                presetText.IndexOf("per_point:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                presetText.IndexOf("beat:", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ParseWinampPreset(presetText, p);
            }
            else
            {
                foreach (var seg in presetText.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = seg.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (kv.Length != 2) continue;
                    var key = kv[0].Trim().ToLowerInvariant();
                    var val = kv[1].Trim().ToLowerInvariant();
                    switch (key)
                    {
                        case "points":
                            if (int.TryParse(val, out var n)) p.Points = Math.Clamp(n, 16, 4096);
                            break;
                        case "mode":
                            p.Mode = val == "bars" ? RenderMode.Bars : RenderMode.Line;
                            break;
                    }
                }
            }
            _preset = p;
        }
        catch
        {
            _preset = Preset.CreateDefault();
        }
    }

    private static void ParseWinampPreset(string presetText, Preset preset)
    {
        // Accept multiple label variants: init/frame/point/beat and per_frame/per_point (case-insensitive)
        var lines = presetText.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var s = line.Trim();
            var low = s.ToLowerInvariant();
            if (low.StartsWith("init:")) preset.InitCode = s[5..].Trim();
            else if (low.StartsWith("per_frame:")) preset.PerFrameCode = s[10..].Trim();
            else if (low.StartsWith("frame:")) preset.PerFrameCode = s[6..].Trim();
            else if (low.StartsWith("per_point:")) preset.PerPointCode = s[10..].Trim();
            else if (low.StartsWith("point:")) preset.PerPointCode = s[6..].Trim();
            else if (low.StartsWith("beat:")) preset.BeatCode = s[5..].Trim();
        }
    }

    public void Resize(int width, int height)
    {
        _width = width; _height = height;
    }

    public void RenderFrame(IAudioFeatures f, ISimpleCanvas canvas)
    {
        // Background clear
        canvas.Clear(0x000000);

        var npts = Math.Clamp(_preset.Points, 16, 2048);
        Span<(float x, float y)> pts = npts <= 1024
            ? stackalloc (float x, float y)[npts]
            : new (float x, float y)[npts];

        // evaluator variables
        var eval = new SuperscopeEvaluator();
        eval.SetVariable("pi", Math.PI);
        eval.SetVariable("$PI", Math.PI);
        eval.SetVariable("time", f.TimeSeconds);
        eval.SetVariable("t", f.TimeSeconds);
        eval.SetVariable("v", f.Energy);
        eval.SetVariable("n", npts);

        // init block (may update n)
        if (!string.IsNullOrWhiteSpace(_preset.InitCode))
        {
            RunBlock(eval, _preset.InitCode);
            npts = (int)Math.Clamp(eval.GetVariable("n"), 16, 4096);
            if (npts != pts.Length)
            {
                pts = new (float x, float y)[npts];
            }
        }

        // beat hook
        if (f.Beat && !string.IsNullOrWhiteSpace(_preset.BeatCode))
            RunBlock(eval, _preset.BeatCode);

        // per-frame
        if (!string.IsNullOrWhiteSpace(_preset.PerFrameCode))
            RunBlock(eval, _preset.PerFrameCode);

        // generate points
        for (int i = 0; i < npts; i++)
        {
            var ni = npts > 1 ? i / (double)(npts - 1) : 0.0;
            eval.SetVariable("i", ni);
            eval.SetVariable("x", 0);
            eval.SetVariable("y", 0);
            if (!string.IsNullOrWhiteSpace(_preset.PerPointCode))
                RunBlock(eval, _preset.PerPointCode);
            var x = eval.GetVariable("x");
            var y = eval.GetVariable("y");
            float px = (float)(Math.Abs(x) <= 1.5 ? ((x + 1.0) * 0.5) * (_width - 2) + 1 : x);
            float py = (float)(Math.Abs(y) <= 1.5 ? ((-y + 1.0) * 0.5) * (_height - 2) + 1 : y);
            pts[i] = (px, py);
        }

        // draw line
        canvas.DrawLines(pts, 2.0f, 0xFFAA44);
    }

    private static void RunBlock(SuperscopeEvaluator eval, string code)
    {
        var parts = code.Split(new[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in parts)
        {
            var line = raw;
            var c = line.IndexOf("//", StringComparison.Ordinal);
            if (c >= 0) line = line[..c];
            line = line.Trim();
            if (line.Length == 0) continue;
            var eq = line.IndexOf('=');
            if (eq > 0)
            {
                var lhs = line[..eq].Trim();
                var rhs = line[(eq + 1)..].Trim();
                var v = eval.EvaluateExpression(rhs);
                eval.SetVariable(lhs, v);
            }
            else
            {
                _ = eval.EvaluateExpression(line);
            }
        }
    }
}

internal sealed class Preset
{
    public int Points { get; set; } = 256;
    public RenderMode Mode { get; set; } = RenderMode.Line;
    public string InitCode { get; set; } = string.Empty;
    public string PerFrameCode { get; set; } = string.Empty;
    public string PerPointCode { get; set; } = string.Empty;
    public string BeatCode { get; set; } = string.Empty;
    public static Preset CreateDefault() => new();
}

internal enum RenderMode { Line, Bars }

/// <summary>
/// Minimal superscope expression evaluator (functions, variables, + - * / % ^, conditionals).
/// </summary>
internal sealed class SuperscopeEvaluator
{
    private readonly Dictionary<string, double> _vars = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Func<double[], double>> _func = new();

    public SuperscopeEvaluator()
    {
        // math
        _func["sin"] = a => Math.Sin(a[0]);
        _func["cos"] = a => Math.Cos(a[0]);
        _func["tan"] = a => Math.Tan(a[0]);
        _func["asin"] = a => Math.Asin(Math.Clamp(a[0], -1, 1));
        _func["acos"] = a => Math.Acos(Math.Clamp(a[0], -1, 1));
        _func["atan"] = a => Math.Atan(a[0]);
        _func["atan2"] = a => Math.Atan2(a[0], a[1]);
        _func["sqrt"] = a => Math.Sqrt(Math.Abs(a[0]));
        _func["abs"] = a => Math.Abs(a[0]);
        _func["pow"] = a => Math.Pow(a[0], a[1]);
        _func["floor"] = a => Math.Floor(a[0]);
        _func["frac"] = a => a[0] - Math.Floor(a[0]);
        _func["sqr"] = a => a[0] * a[0];
        _func["min"] = a => Math.Min(a[0], a[1]);
        _func["max"] = a => Math.Max(a[0], a[1]);
        _func["clamp"] = a => Math.Min(Math.Max(a[0], a[1]), a[2]);
        _func["rand"] = a => new Random().NextDouble() * (a.Length > 0 ? a[0] : 1.0);
        _func["above"] = a => a[0] > a[1] ? 1.0 : 0.0;
        _func["below"] = a => a[0] < a[1] ? 1.0 : 0.0;
        _func["equal"] = a => Math.Abs(a[0] - a[1]) < 1e-6 ? 1.0 : 0.0;
        _func["if"] = a => a[0] != 0.0 ? a[1] : a[2];
    }

    public void SetVariable(string name, double value) => _vars[name] = value;
    public double GetVariable(string name) => _vars.TryGetValue(name, out var v) ? v : 0.0;

    public double EvaluateExpression(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr)) return 0;
        return EvalRpn(ToRpn(Tokenize(expr)));
    }

    private enum T { Num, Id, Op, Lp, Rp, Comma }
    private readonly record struct Tok(T K, string S);

    private IEnumerable<Tok> Tokenize(string s)
    {
        int i = 0;
        while (i < s.Length)
        {
            var c = s[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (char.IsDigit(c) || (c == '.' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
            {
                int j = i + 1; while (j < s.Length && (char.IsDigit(s[j]) || s[j] == '.')) j++;
                yield return new Tok(T.Num, s[i..j]); i = j; continue;
            }
            if (char.IsLetter(c) || c == '_' || c == '$')
            {
                int j = i + 1; while (j < s.Length && (char.IsLetterOrDigit(s[j]) || s[j] == '_' || s[j] == '$')) j++;
                yield return new Tok(T.Id, s[i..j]); i = j; continue;
            }
            if (c == '(') { yield return new Tok(T.Lp, "("); i++; continue; }
            if (c == ')') { yield return new Tok(T.Rp, ")"); i++; continue; }
            if (c == ',') { yield return new Tok(T.Comma, ","); i++; continue; }
            if ("+-*/%^".Contains(c)) { yield return new Tok(T.Op, c.ToString()); i++; continue; }
            i++;
        }
    }

    private static int Prec(string op) => op switch { "^" => 4, "*" or "/" or "%" => 3, "+" or "-" => 2, _ => 1 };
    private static bool Right(string op) => op == "^";

    private IEnumerable<Tok> ToRpn(IEnumerable<Tok> toks)
    {
        var o = new List<Tok>(); var st = new Stack<Tok>();
        foreach (var t in toks)
        {
            switch (t.K)
            {
                case T.Num: o.Add(t); break;
                case T.Id: st.Push(t); break;
                case T.Op:
                    while (st.Count > 0 && st.Peek().K == T.Op && (Prec(st.Peek().S) > Prec(t.S) || (Prec(st.Peek().S) == Prec(t.S) && !Right(t.S)))) o.Add(st.Pop());
                    o.Add(t); break;
                case T.Lp: st.Push(t); break;
                case T.Rp:
                    while (st.Count > 0 && st.Peek().K != T.Lp) o.Add(st.Pop());
                    if (st.Count > 0 && st.Peek().K == T.Lp) st.Pop();
                    if (st.Count > 0 && st.Peek().K == T.Id) o.Add(st.Pop());
                    break;
                case T.Comma:
                    while (st.Count > 0 && st.Peek().K != T.Lp) o.Add(st.Pop());
                    break;
            }
        }
        while (st.Count > 0) o.Add(st.Pop());
        return o;
    }

    private double EvalRpn(IEnumerable<Tok> rpn)
    {
        var st = new Stack<double>();
        foreach (var t in rpn)
        {
            switch (t.K)
            {
                case T.Num:
                    st.Push(double.Parse(t.S, System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case T.Id:
                    var id = t.S.ToLowerInvariant();
                    if (_func.TryGetValue(id, out var fn))
                    {
                        var ar = Arity(id);
                        var argv = new double[ar];
                        for (int i = ar - 1; i >= 0; i--) argv[i] = st.Count > 0 ? st.Pop() : 0.0;
                        st.Push(fn(argv));
                    }
                    else
                    {
                        st.Push(_vars.TryGetValue(t.S, out var v) ? v : 0.0);
                    }
                    break;
                case T.Op:
                    var b = st.Count > 0 ? st.Pop() : 0.0; var a = st.Count > 0 ? st.Pop() : 0.0;
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

    private static int Arity(string id) => id switch
    {
        "pow" => 2,
        "atan2" => 2,
        "min" => 2,
        "max" => 2,
        "clamp" => 3,
        "if" => 3,
        _ => 1
    };
}
