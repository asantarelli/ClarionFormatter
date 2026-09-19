using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.Core;
using ClarionWindowFormatter.Dialogs;
using ClarionWindowFormatter.Services;

namespace ClarionWindowFormatter.Commands
{
    public class FormatWindowWithAiCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            try
            {
                var settings = WindowFormatterProfileService.Load();

                string apiKey = settings.AnthropicApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                    apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show(
                        "No hay API key disponible.\n\n" +
                        "Ve a: Tools > Formatear ventana - Configuracion\n" +
                        "y pega tu clave de Anthropic (sk-ant-...).",
                        "Formatear con IA", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                settings.AnthropicApiKey = apiKey;

                var editorSvc = new EditorService();
                string source = editorSvc.GetActiveDocumentContent();
                if (string.IsNullOrEmpty(source))
                {
                    MessageBox.Show("No se pudo leer el editor activo. Abri un archivo .clw primero.",
                        "Formatear con IA", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string windowBlock = ExtractWindowBlock(source);
                if (windowBlock == null)
                {
                    MessageBox.Show("No se encontro un bloque WINDOW en el archivo activo.",
                        "Formatear con IA", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var profile = WindowFormatterProfileService.GetActiveProfile();
                string prompt1 = BuildFormatPrompt(windowBlock, profile);
                string prompt2Template = BuildValidationPromptTemplate(profile);

                string result1 = null, result2 = null;
                Exception error = null;

                using (var dlg = new ProgressDialog("Paso 1/2 — Reformateando ventana..."))
                {
                    var ct = dlg.CancellationToken;

                    var thread = new Thread(() =>
                    {
                        try
                        {
                            // Paso 1: formatear
                            var task1 = AnthropicApiClient.SendMessageAsync(apiKey, settings.AiModel, prompt1, ct);
                            result1 = task1.GetAwaiter().GetResult();

                            if (ct.IsCancellationRequested || result1 == null) return;

                            string block1 = ExtractWindowBlock(result1);
                            if (block1 == null) return;

                            // Paso 2: validar y corregir
                            dlg.SetStatus("Paso 2/2 — Verificando resultado...");
                            string prompt2 = prompt2Template + "\n\n--- BLOQUE A REVISAR ---\n\n" + block1;
                            var task2 = AnthropicApiClient.SendMessageAsync(apiKey, settings.AiModel, prompt2, ct);
                            result2 = task2.GetAwaiter().GetResult();
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex) { error = ex; }
                        finally
                        {
                            if (!ct.IsCancellationRequested)
                                dlg.Invoke(new Action(() => dlg.DialogResult = DialogResult.OK));
                        }
                    });
                    thread.IsBackground = true;
                    thread.Start();
                    dlg.ShowDialog();
                }

                if (error != null)
                {
                    string msg = error.Message;
                    if (error.InnerException != null) msg += "\n\nDetalle: " + error.InnerException.Message;
                    MessageBox.Show("Error al llamar la API de Claude:\n\n" + msg,
                        "Formatear con IA", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (result1 == null) return; // cancelado en paso 1

                // Usar resultado del paso 2 si está disponible, sino paso 1
                string finalResult = result2 ?? result1;
                string newBlock = ExtractWindowBlock(finalResult);

                if (newBlock == null)
                {
                    // Intentar con resultado del paso 1
                    newBlock = ExtractWindowBlock(result1);
                    if (newBlock == null)
                    {
                        MessageBox.Show(
                            "Claude respondio pero no se encontro un bloque WINDOW...END en la respuesta.\n\n" +
                            "Respuesta recibida:\n" + (result1.Length > 500 ? result1.Substring(0, 500) + "..." : result1),
                            "Formatear con IA", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                var parser = new ClarionWindowParser();
                var parsed = parser.Parse(source);
                if (parsed == null)
                {
                    MessageBox.Show("No se pudo localizar el bloque WINDOW en el editor para reemplazarlo.",
                        "Formatear con IA", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var lines = source.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
                int lastLineIdx = parsed.EndLine - 1;
                int lastColLen  = lastLineIdx < lines.Length ? lines[lastLineIdx].Length + 1 : 1;

                var replResult = editorSvc.ReplaceRange(
                    parsed.StartLine, 1, parsed.EndLine, lastColLen, newBlock);

                if (!replResult.Success)
                {
                    MessageBox.Show("Claude respondio correctamente pero no se pudo aplicar en el editor:\n" +
                        replResult.ErrorMessage, "Formatear con IA", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show(
                    "Ventana reformateada con exito.\n\nRevisa el resultado y guarda si estas conforme (Ctrl+S).",
                    "Formatear con IA", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inesperado: " + ex.Message,
                    "Formatear con IA", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Prompt building ───────────────────────────────────────────────────

        private static string BuildFormatPrompt(string windowBlock, WindowFormatterProfile p)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Reformatea este bloque WINDOW de Clarion aplicando las reglas indicadas.");
            sb.AppendLine("Devuelve UNICAMENTE el bloque WINDOW...END reformateado, sin explicaciones ni markdown.");
            sb.AppendLine("CONSERVAR SIN CAMBIOS: USE(), FORMAT(), MSG(), #SEQ(), #ORIG(), #ORDINAL(), #LINK(), #FIELDS(), texto de labels, titulos y strings. No agregar ni quitar controles.");

            AppendProtocolSection(sb, p);

            sb.AppendLine();
            sb.AppendLine("--- BLOQUE A REFORMATEAR ---");
            sb.AppendLine();
            sb.Append(windowBlock);
            return sb.ToString();
        }

        private static string BuildValidationPromptTemplate(WindowFormatterProfile p)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Revisa el siguiente bloque WINDOW de Clarion y verifica que cumple EXACTAMENTE con las reglas del protocolo.");
            sb.AppendLine("Si encuentras incumplimientos, corrigelos. Si todo esta correcto, devuelve el bloque sin cambios.");
            sb.AppendLine("Devuelve UNICAMENTE el bloque WINDOW...END, sin explicaciones ni markdown.");
            sb.AppendLine("CONSERVAR SIN CAMBIOS: USE(), FORMAT(), MSG(), #SEQ(), #ORIG(), #ORDINAL(), #LINK(), #FIELDS(), texto de labels, titulos y strings. No agregar ni quitar controles.");

            AppendProtocolSection(sb, p);
            return sb.ToString();
        }

        private static void AppendProtocolSection(StringBuilder sb, WindowFormatterProfile p)
        {
            string protocol = BuildProtocolFromRules(p);
            bool hasRules = p.ControlRules.Any(r => HasAnyValue(r));

            if (hasRules)
            {
                sb.AppendLine();
                sb.AppendLine("=== PROTOCOLO DE FORMATEO ===");
                sb.AppendLine();
                sb.Append(protocol);
                sb.AppendLine("=== FIN DEL PROTOCOLO ===");
            }

            if (!string.IsNullOrWhiteSpace(p.AiExtraInstructions))
            {
                sb.AppendLine();
                sb.AppendLine("INSTRUCCIONES ADICIONALES:");
                foreach (var line in p.AiExtraInstructions.Replace("\r\n", "\n").Split('\n'))
                {
                    string t = line.Trim();
                    if (!string.IsNullOrEmpty(t)) sb.AppendLine("   - " + t);
                }
            }

            if (!hasRules && string.IsNullOrWhiteSpace(p.AiExtraInstructions))
            {
                sb.AppendLine();
                sb.AppendLine("Aplica las convenciones estandar de Clarion para coordenadas, alturas y colores.");
            }
        }

        private static string BuildProtocolFromRules(WindowFormatterProfile p)
        {
            var sb = new StringBuilder();

            foreach (var rule in p.ControlRules)
            {
                if (!HasAnyValue(rule)) continue;

                sb.AppendLine("CONTROL: " + rule.ControlType);

                var coords = new List<string>();
                if (!string.IsNullOrEmpty(rule.YBase))      coords.Add("Y de la primera fila: " + rule.YBase);
                if (!string.IsNullOrEmpty(rule.YIncrement)) coords.Add("Incremento Y entre filas: " + rule.YIncrement);
                if (!string.IsNullOrEmpty(rule.XLabel))     coords.Add("X de las etiquetas: " + rule.XLabel);
                if (!string.IsNullOrEmpty(rule.XControl))   coords.Add("X de los controles: " + rule.XControl);
                if (coords.Count > 0)
                {
                    sb.AppendLine("  Coordenadas:");
                    foreach (var l in coords) sb.AppendLine("    - " + l);
                }

                var dims = new List<string>();
                if (!string.IsNullOrEmpty(rule.Height))   dims.Add("Altura: " + rule.Height);
                if (!string.IsNullOrEmpty(rule.MinWidth)) dims.Add("Ancho minimo: " + rule.MinWidth);
                if (!string.IsNullOrEmpty(rule.MaxWidth)) dims.Add("Ancho maximo: " + rule.MaxWidth);
                if (dims.Count > 0)
                {
                    sb.AppendLine("  Dimensiones:");
                    foreach (var l in dims) sb.AppendLine("    - " + l);
                }

                if (!string.IsNullOrEmpty(rule.ColorAttr))
                    sb.AppendLine("  Color: " + rule.ColorAttr);

                if (rule.GenerateTip)
                {
                    sb.Append("  Generar TIP automaticamente");
                    if (!string.IsNullOrEmpty(rule.TipTemplate))
                        sb.Append(" con plantilla: \"" + rule.TipTemplate + "\"");
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine("  No generar TIP en controles de este tipo.");
                }

                if (!string.IsNullOrWhiteSpace(rule.ExtraRules))
                {
                    sb.AppendLine("  Reglas adicionales:");
                    foreach (var line in rule.ExtraRules.Replace("\r\n", "\n").Split('\n'))
                    {
                        string t = line.Trim();
                        if (!string.IsNullOrEmpty(t)) sb.AppendLine("    - " + t);
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static bool HasAnyValue(ControlTypeRule r)
        {
            return !string.IsNullOrEmpty(r.YBase)      || !string.IsNullOrEmpty(r.YIncrement) ||
                   !string.IsNullOrEmpty(r.XLabel)     || !string.IsNullOrEmpty(r.XControl)   ||
                   !string.IsNullOrEmpty(r.Height)     || !string.IsNullOrEmpty(r.MinWidth)   ||
                   !string.IsNullOrEmpty(r.MaxWidth)   || !string.IsNullOrEmpty(r.ColorAttr)  ||
                   r.GenerateTip                       || !string.IsNullOrEmpty(r.TipTemplate) ||
                   !string.IsNullOrEmpty(r.ExtraRules);
        }

        // ── Window block extraction ───────────────────────────────────────────

        private static string ExtractWindowBlock(string text)
        {
            text = Regex.Replace(text, @"```[^\n]*\n?", "", RegexOptions.IgnoreCase).Replace("```", "");

            var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            int start = -1, depth = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].TrimStart();
                if (start < 0)
                {
                    if (Regex.IsMatch(t, @"^\w[\w\d]*\s+WINDOW\b", RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(t, @"^WINDOW\b", RegexOptions.IgnoreCase))
                    { start = i; depth = 1; }
                    continue;
                }
                if (Regex.IsMatch(t, @"^(SHEET|TAB|GROUP|OPTION|MENUBAR|MENU|TOOLBAR|ITEM)\b", RegexOptions.IgnoreCase))
                    depth++;
                else if (Regex.IsMatch(t, @"^END\b", RegexOptions.IgnoreCase) && --depth == 0)
                {
                    var sb = new StringBuilder();
                    for (int j = start; j <= i; j++) { sb.Append(lines[j]); if (j < i) sb.AppendLine(); }
                    return sb.ToString();
                }
            }
            return null;
        }
    }
}
