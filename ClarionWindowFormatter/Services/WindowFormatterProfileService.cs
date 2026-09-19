using System;
using System.IO;
using System.Text;

namespace ClarionWindowFormatter.Services
{
    public static class WindowFormatterProfileService
    {
        public static readonly string SettingsPath = ResolveSettingsPath();

        private static string ResolveSettingsPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrEmpty(appData))
                return Path.Combine(appData, "ClarionAssistant", "window-formatter.json");

            string asmDir = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);
            return Path.Combine(asmDir ?? ".", "window-formatter.json");
        }

        private static WindowFormatterSettings _cached;

        public static WindowFormatterSettings Load()
        {
            if (_cached != null) return _cached;
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath, Encoding.UTF8);
                    var parsed = SimpleJsonParser.ParseSettings(json);
                    if (parsed != null) { _cached = parsed; return _cached; }
                }
            }
            catch { }
            _cached = new WindowFormatterSettings();
            Save(_cached);
            return _cached;
        }

        public static void Save(WindowFormatterSettings settings)
        {
            try
            {
                _cached = settings;
                string dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(SettingsPath, SimpleJsonParser.SerializeSettings(settings), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Window Formatter: no se pudo guardar la configuracion.\n" + ex.Message,
                    "Window Formatter", System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }

        public static WindowFormatterProfile GetActiveProfile()
        {
            var s = Load();
            foreach (var p in s.Profiles)
                if (p.ProfileName == s.ActiveProfile) return p;
            return s.Profiles.Count > 0 ? s.Profiles[0] : new WindowFormatterProfile();
        }

        public static void Invalidate() => _cached = null;
    }

    internal static class SimpleJsonParser
    {
        public static string SerializeSettings(WindowFormatterSettings s)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"ActiveProfile\": " + Q(s.ActiveProfile) + ",");
            sb.AppendLine("  \"AnthropicApiKey\": " + Q(s.AnthropicApiKey) + ",");
            sb.AppendLine("  \"AiModel\": " + Q(s.AiModel) + ",");
            sb.AppendLine("  \"Profiles\": [");
            for (int i = 0; i < s.Profiles.Count; i++)
            {
                sb.Append(SerializeProfile(s.Profiles[i], "    "));
                if (i < s.Profiles.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ]");
            sb.Append("}");
            return sb.ToString();
        }

        private static string SerializeProfile(WindowFormatterProfile p, string indent)
        {
            var sb = new StringBuilder();
            string i2 = indent + "  ";
            sb.AppendLine(indent + "{");
            sb.AppendLine(i2 + Q("ProfileName") + ": " + Q(p.ProfileName) + ",");
            sb.AppendLine(i2 + Q("AiExtraInstructions") + ": " + Q(p.AiExtraInstructions) + ",");
            sb.AppendLine(i2 + Q("ControlRules") + ": [");
            string i3 = i2 + "  ";
            for (int i = 0; i < p.ControlRules.Count; i++)
            {
                sb.Append(SerializeControlRule(p.ControlRules[i], i3));
                if (i < p.ControlRules.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine(i2 + "]");
            sb.Append(indent + "}");
            return sb.ToString();
        }

        private static string SerializeControlRule(ControlTypeRule r, string indent)
        {
            var sb = new StringBuilder();
            string i2 = indent + "  ";
            sb.AppendLine(indent + "{");
            sb.AppendLine(i2 + Q("ControlType") + ": " + Q(r.ControlType) + ",");
            sb.AppendLine(i2 + Q("YBase") + ": " + Q(r.YBase) + ",");
            sb.AppendLine(i2 + Q("YIncrement") + ": " + Q(r.YIncrement) + ",");
            sb.AppendLine(i2 + Q("XLabel") + ": " + Q(r.XLabel) + ",");
            sb.AppendLine(i2 + Q("XControl") + ": " + Q(r.XControl) + ",");
            sb.AppendLine(i2 + Q("Height") + ": " + Q(r.Height) + ",");
            sb.AppendLine(i2 + Q("MinWidth") + ": " + Q(r.MinWidth) + ",");
            sb.AppendLine(i2 + Q("MaxWidth") + ": " + Q(r.MaxWidth) + ",");
            sb.AppendLine(i2 + Q("ColorAttr") + ": " + Q(r.ColorAttr) + ",");
            sb.AppendLine(i2 + Q("GenerateTip") + ": " + (r.GenerateTip ? "true" : "false") + ",");
            sb.AppendLine(i2 + Q("TipTemplate") + ": " + Q(r.TipTemplate) + ",");
            sb.AppendLine(i2 + Q("ExtraRules") + ": " + Q(r.ExtraRules));
            sb.Append(indent + "}");
            return sb.ToString();
        }

        public static WindowFormatterSettings ParseSettings(string json)
        {
            try
            {
                var s = new WindowFormatterSettings();
                s.ActiveProfile   = ReadString(json, "ActiveProfile")   ?? s.ActiveProfile;
                s.AnthropicApiKey = ReadString(json, "AnthropicApiKey") ?? s.AnthropicApiKey;
                s.AiModel         = ReadString(json, "AiModel")         ?? s.AiModel;
                s.Profiles.Clear();

                int profilesStart = json.IndexOf("\"Profiles\"", StringComparison.Ordinal);
                if (profilesStart < 0) return s;
                int arrayStart = json.IndexOf('[', profilesStart);
                if (arrayStart < 0) return s;

                int pos = arrayStart + 1;
                while (pos < json.Length)
                {
                    int objStart = json.IndexOf('{', pos);
                    if (objStart < 0) break;
                    int objEnd = FindMatchingBrace(json, objStart);
                    if (objEnd < 0) break;
                    string block = json.Substring(objStart, objEnd - objStart + 1);
                    var p = ParseProfile(block);
                    if (p != null) s.Profiles.Add(p);
                    pos = objEnd + 1;
                }

                if (s.Profiles.Count == 0) s.Profiles.Add(new WindowFormatterProfile());
                return s;
            }
            catch { return null; }
        }

        private static WindowFormatterProfile ParseProfile(string block)
        {
            var p = new WindowFormatterProfile();
            p.ProfileName         = ReadString(block, "ProfileName")         ?? p.ProfileName;
            p.AiExtraInstructions = ReadString(block, "AiExtraInstructions") ?? p.AiExtraInstructions;

            int rulesStart = block.IndexOf("\"ControlRules\"", StringComparison.Ordinal);
            if (rulesStart >= 0)
            {
                int arrayStart = block.IndexOf('[', rulesStart);
                if (arrayStart >= 0)
                {
                    int pos = arrayStart + 1;
                    while (pos < block.Length)
                    {
                        int objStart = block.IndexOf('{', pos);
                        if (objStart < 0) break;
                        int objEnd = FindMatchingBrace(block, objStart);
                        if (objEnd < 0) break;
                        string ruleBlock = block.Substring(objStart, objEnd - objStart + 1);
                        var rule = ParseControlRule(ruleBlock);
                        if (rule != null) p.ControlRules.Add(rule);
                        pos = objEnd + 1;
                    }
                }
            }

            return p;
        }

        private static ControlTypeRule ParseControlRule(string block)
        {
            var r = new ControlTypeRule();
            r.ControlType = ReadString(block, "ControlType") ?? "";
            r.YBase       = ReadString(block, "YBase")       ?? "";
            r.YIncrement  = ReadString(block, "YIncrement")  ?? "";
            r.XLabel      = ReadString(block, "XLabel")      ?? "";
            r.XControl    = ReadString(block, "XControl")    ?? "";
            r.Height      = ReadString(block, "Height")      ?? "";
            r.MinWidth    = ReadString(block, "MinWidth")    ?? "";
            r.MaxWidth    = ReadString(block, "MaxWidth")    ?? "";
            r.ColorAttr   = ReadString(block, "ColorAttr")   ?? "";
            r.GenerateTip = ReadBool(block, "GenerateTip");
            r.TipTemplate = ReadString(block, "TipTemplate") ?? "";
            r.ExtraRules  = ReadString(block, "ExtraRules")  ?? "";
            return r;
        }

        private static string ReadString(string json, string key)
        {
            string pat = "\"" + key + "\"";
            int k = json.IndexOf(pat, StringComparison.Ordinal);
            if (k < 0) return null;
            int colon = json.IndexOf(':', k + pat.Length);
            if (colon < 0) return null;
            int q1 = json.IndexOf('"', colon + 1);
            if (q1 < 0) return null;
            int q2 = q1 + 1;
            while (q2 < json.Length)
            {
                if (json[q2] == '\\') { q2 += 2; continue; }
                if (json[q2] == '"') break;
                q2++;
            }
            if (q2 >= json.Length) return null;
            return json.Substring(q1 + 1, q2 - q1 - 1)
                       .Replace("\\\"", "\"").Replace("\\\\", "\\")
                       .Replace("\\n", "\n").Replace("\\r", "\r");
        }

        private static bool ReadBool(string json, string key)
        {
            string pat = "\"" + key + "\"";
            int k = json.IndexOf(pat, StringComparison.Ordinal);
            if (k < 0) return false;
            int colon = json.IndexOf(':', k + pat.Length);
            if (colon < 0) return false;
            int pos = colon + 1;
            while (pos < json.Length && json[pos] == ' ') pos++;
            return pos + 4 <= json.Length && json.Substring(pos, 4) == "true";
        }

        private static string Q(string s)
            => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"")
                               .Replace("\n", "\\n").Replace("\r", "\\r") + "\"";

        private static int FindMatchingBrace(string s, int open)
        {
            int depth = 0;
            bool inStr = false;
            for (int i = open; i < s.Length; i++)
            {
                if (inStr) { if (s[i] == '\\') i++; else if (s[i] == '"') inStr = false; continue; }
                if (s[i] == '"') { inStr = true; continue; }
                if (s[i] == '{') depth++;
                else if (s[i] == '}') { depth--; if (depth == 0) return i; }
            }
            return -1;
        }
    }
}
