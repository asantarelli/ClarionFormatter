using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClarionWindowFormatter.Services
{
    // ── Data model ────────────────────────────────────────────────────────────

    public enum ClarionControlKind
    {
        Unknown, Window,
        Sheet, Tab, Group, Toolbar, Option,
        Prompt, Entry, Check, Spin, List, Drop, Combo, Button,
        String, Text, Image, Radio, OleControl, ActiveX
    }

    public class AtCoords
    {
        public int? X, Y, W, H;

        public static AtCoords Parse(string atClause)
        {
            if (string.IsNullOrEmpty(atClause)) return null;
            // AT() can have 1-4 params: AT(x), AT(x,y), AT(x,y,w), AT(x,y,w,h)
            var m = Regex.Match(atClause, @"AT\s*\(([^)]*)\)", RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            var parts = m.Groups[1].Value.Split(',');
            var c = new AtCoords();
            if (parts.Length >= 1) c.X = ParseInt(parts[0]);
            if (parts.Length >= 2) c.Y = ParseInt(parts[1]);
            if (parts.Length >= 3) c.W = ParseInt(parts[2]);
            if (parts.Length >= 4) c.H = ParseInt(parts[3]);
            return c;
        }

        private static int? ParseInt(string s)
        {
            s = s.Trim();
            if (string.IsNullOrEmpty(s) || s == "*") return null;
            int v; return int.TryParse(s, out v) ? (int?)v : null;
        }

        public string Render()
        {
            // Build only as many params as needed (don't add trailing empty commas)
            if (H.HasValue)
                return string.Format("AT({0},{1},{2},{3})",
                    X.HasValue ? X.ToString() : "",
                    Y.HasValue ? Y.ToString() : "",
                    W.HasValue ? W.ToString() : "",
                    H.ToString());
            if (W.HasValue)
                return string.Format("AT({0},{1},{2})",
                    X.HasValue ? X.ToString() : "",
                    Y.HasValue ? Y.ToString() : "",
                    W.ToString());
            if (Y.HasValue)
                return string.Format("AT({0},{1})",
                    X.HasValue ? X.ToString() : "",
                    Y.ToString());
            return string.Format("AT({0})", X.HasValue ? X.ToString() : "");
        }
    }

    /// <summary>
    /// A logical control = one or more physical source lines joined by Clarion's | continuation.
    /// All physical lines are kept verbatim so we can reconstruct the source exactly.
    /// </summary>
    public class LogicalControl
    {
        /// <summary>Physical lines that make up this logical control (verbatim).</summary>
        public List<string> PhysicalLines { get; set; } = new List<string>();

        public ClarionControlKind Kind { get; set; }

        /// <summary>AT coordinates extracted from the first physical line (may be null).</summary>
        public AtCoords At { get; set; }

        /// <summary>Controls nested inside this block (SHEET/TAB/GROUP/etc.).</summary>
        public List<LogicalControl> Children { get; set; } = new List<LogicalControl>();

        /// <summary>Closing END line(s) for block controls.</summary>
        public List<string> ClosingLines { get; set; } = new List<string>();

        public bool IsBlock =>
            Kind == ClarionControlKind.Window ||
            Kind == ClarionControlKind.Sheet  ||
            Kind == ClarionControlKind.Tab    ||
            Kind == ClarionControlKind.Group  ||
            Kind == ClarionControlKind.Toolbar||
            Kind == ClarionControlKind.Option;

        /// <summary>Join physical lines back to source text (preserving original line endings).</summary>
        public string ToSource(string lineEnding = "\r\n")
        {
            return string.Join(lineEnding, PhysicalLines);
        }
    }

    public class ParsedWindow
    {
        public LogicalControl Window { get; set; }
        public int StartLine { get; set; }   // 1-based
        public int EndLine   { get; set; }   // 1-based
    }

    // ── Parser ────────────────────────────────────────────────────────────────

    public class ClarionWindowParser
    {
        private static readonly Regex _windowStart = new Regex(
            @"^\s*Window\s+WINDOW\b|^\s*WINDOW\s*\(", RegexOptions.IgnoreCase);

        private static readonly Regex _controlKw = new Regex(
            @"^\s*(SHEET|TAB|GROUP|TOOLBAR|PROMPT|ENTRY|CHECK|SPIN|LIST|DROP|COMBO|BUTTON|STRING|TEXT|IMAGE|OPTION|RADIO|OLECONTROL|ACTIVEX)\b",
            RegexOptions.IgnoreCase);

        private static readonly Regex _endKw = new Regex(
            @"^\s*END\s*($|!|\r)", RegexOptions.IgnoreCase);

        private static readonly Regex _atPattern = new Regex(
            @"AT\s*\([^)]*\)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Parse the first WINDOW...END block in source.
        /// Returns null if none found.
        /// </summary>
        public ParsedWindow Parse(string source)
        {
            var lines = SplitLines(source);

            // Find WINDOW start
            int startIdx = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (_windowStart.IsMatch(lines[i])) { startIdx = i; break; }
            }
            if (startIdx < 0) return null;

            // Collect lines until matching END (track depth)
            int depth = 1, endIdx = -1;
            for (int i = startIdx + 1; i < lines.Count; i++)
            {
                string code = StripContinuationAndComment(lines[i]);
                if (IsBlockOpener(code)) depth++;
                if (_endKw.IsMatch(lines[i].TrimStart())) { depth--; if (depth == 0) { endIdx = i; break; } }
            }
            if (endIdx < 0) endIdx = lines.Count - 1;

            var blockLines = lines.GetRange(startIdx, endIdx - startIdx + 1);

            var window = ParseBlock(blockLines, out _);
            window.Kind = ClarionControlKind.Window;

            return new ParsedWindow
            {
                Window    = window,
                StartLine = startIdx + 1,
                EndLine   = endIdx + 1
            };
        }

        // ── Recursive block parser ─────────────────────────────────────────────

        private LogicalControl ParseBlock(List<string> lines, out int consumed)
        {
            var ctrl = new LogicalControl();
            ctrl.Kind = DetectKind(lines[0]);
            ctrl.At   = ExtractAt(lines[0]);

            // Collect header physical lines (first + its continuations)
            int i = 0;
            ctrl.PhysicalLines.Add(lines[i]);
            while (i < lines.Count - 1 && EndsWithContinuation(lines[i]))
            {
                i++;
                ctrl.PhysicalLines.Add(lines[i]);
            }
            i++;

            if (!ctrl.IsBlock) { consumed = i; return ctrl; }

            // Parse children until END at this depth
            while (i < lines.Count)
            {
                string trimmed = lines[i].TrimStart();

                if (_endKw.IsMatch(trimmed))
                {
                    ctrl.ClosingLines.Add(lines[i]);
                    i++;
                    break;
                }

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("!"))
                {
                    // blank / comment: create passthrough child
                    var blank = new LogicalControl { Kind = ClarionControlKind.Unknown };
                    blank.PhysicalLines.Add(lines[i]);
                    ctrl.Children.Add(blank);
                    i++;
                    continue;
                }

                if (_controlKw.IsMatch(lines[i]) || (DetectKind(lines[i]) != ClarionControlKind.Unknown))
                {
                    int consumed2;
                    var child = ParseBlock(lines.GetRange(i, lines.Count - i), out consumed2);
                    ctrl.Children.Add(child);
                    i += consumed2;
                }
                else
                {
                    // Attribute-only line or orphan continuation — keep as passthrough
                    var orphan = new LogicalControl { Kind = ClarionControlKind.Unknown };
                    orphan.PhysicalLines.Add(lines[i]);
                    // Collect its continuations too
                    while (i < lines.Count - 1 && EndsWithContinuation(lines[i]))
                    {
                        i++;
                        orphan.PhysicalLines.Add(lines[i]);
                    }
                    ctrl.Children.Add(orphan);
                    i++;
                }
            }

            consumed = i;
            return ctrl;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static List<string> SplitLines(string source)
        {
            return new List<string>(source.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n'));
        }

        private static bool EndsWithContinuation(string line)
        {
            // Clarion continuation: line ends with | (possibly followed by whitespace or comment)
            string s = line.TrimEnd();
            // Remove inline comment after |
            int excl = s.LastIndexOf('!');
            if (excl > 0) s = s.Substring(0, excl).TrimEnd();
            return s.EndsWith("|");
        }

        private static string StripContinuationAndComment(string line)
        {
            string s = line.TrimEnd();
            int ex = s.IndexOf('!'); if (ex >= 0) s = s.Substring(0, ex).TrimEnd();
            if (s.EndsWith("|")) s = s.Substring(0, s.Length - 1).TrimEnd();
            return s;
        }

        private static bool IsBlockOpener(string codeLine)
        {
            return Regex.IsMatch(codeLine.TrimStart(),
                @"^(SHEET|TAB|GROUP|TOOLBAR|OPTION|WINDOW)\b", RegexOptions.IgnoreCase);
        }

        public static ClarionControlKind DetectKind(string line)
        {
            // Handle "Window  WINDOW(...)" label prefix
            var m = Regex.Match(line,
                @"^\s*(?:\w+\s+)?(WINDOW|SHEET|TAB|GROUP|TOOLBAR|PROMPT|ENTRY|CHECK|SPIN|LIST|DROP|COMBO|BUTTON|STRING|TEXT|IMAGE|OPTION|RADIO|OLECONTROL|ACTIVEX)\b",
                RegexOptions.IgnoreCase);
            if (!m.Success) return ClarionControlKind.Unknown;
            switch (m.Groups[1].Value.ToUpper())
            {
                case "WINDOW":   return ClarionControlKind.Window;
                case "SHEET":    return ClarionControlKind.Sheet;
                case "TAB":      return ClarionControlKind.Tab;
                case "GROUP":    return ClarionControlKind.Group;
                case "TOOLBAR":  return ClarionControlKind.Toolbar;
                case "PROMPT":   return ClarionControlKind.Prompt;
                case "ENTRY":    return ClarionControlKind.Entry;
                case "CHECK":    return ClarionControlKind.Check;
                case "SPIN":     return ClarionControlKind.Spin;
                case "LIST":     return ClarionControlKind.List;
                case "DROP":     return ClarionControlKind.Drop;
                case "COMBO":    return ClarionControlKind.Combo;
                case "BUTTON":   return ClarionControlKind.Button;
                case "STRING":   return ClarionControlKind.String;
                case "TEXT":     return ClarionControlKind.Text;
                case "IMAGE":    return ClarionControlKind.Image;
                case "OPTION":   return ClarionControlKind.Option;
                case "RADIO":    return ClarionControlKind.Radio;
                default:         return ClarionControlKind.Unknown;
            }
        }

        public static AtCoords ExtractAt(string firstPhysicalLine)
        {
            var m = _atPattern.Match(firstPhysicalLine);
            return m.Success ? AtCoords.Parse(m.Value) : null;
        }

        /// <summary>
        /// Replace the AT(...) clause in the first physical line of a control with new coords.
        /// Returns the modified line.
        /// </summary>
        public static string ReplaceAt(string firstLine, AtCoords newAt)
        {
            return _atPattern.Replace(firstLine, newAt.Render(), 1);
        }
    }
}
