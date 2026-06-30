using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.Core;
using ClarionWindowFormatter.Dialogs;
using ClarionWindowFormatter.Services;

namespace ClarionWindowFormatter.Commands
{
    /// <summary>
    /// Extrae el bloque WINDOW del editor, llama a la API de Claude con las
    /// reglas del perfil activo, y aplica el resultado directo en el editor.
    /// Sin clipboard, sin copy-paste manual.
    /// </summary>
    public class FormatWindowWithAiCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            try
            {
                var settings = WindowFormatterProfileService.Load();

                // Prioridad: campo configurado > variable de entorno (Claude Code / Max plan)
                string apiKey = settings.AnthropicApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                    apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show(
                        "No hay API key disponible.\n\n" +
                        "Opciones:\n" +
                        "1. Ve a: Tools > Formatear ventana - Configuracion > pestana IA\n" +
                        "   y pega tu clave de Anthropic (sk-ant-...).\n\n" +
                        "2. O asegurate de tener Claude Code instalado y configurado\n" +
                        "   (usa la variable de entorno ANTHROPIC_API_KEY automaticamente).",
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
                string resolvedProtocol = ResolveProtocolFile(profile.AiProtocolFile);
                string prompt = BuildPrompt(windowBlock, profile, resolvedProtocol);

                // Mostrar dialogo de progreso y llamar la API en background
                string result = null;
                Exception error = null;

                using (var dlg = new ProgressDialog("Consultando a Claude... (puede tardar unos segundos)"))
                {
                    var ct = dlg.CancellationToken;

                    // Llamar API en thread separado para no bloquear el UI
                    var thread = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            var task = AnthropicApiClient.SendMessageAsync(
                                apiKey, settings.AiModel, prompt, ct);
                            result = task.GetAwaiter().GetResult();
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
                    if (error.InnerException != null)
                        msg += "\n\nDetalle: " + error.InnerException.Message;
                    MessageBox.Show("Error al llamar la API de Claude:\n\n" + msg,
                        "Formatear con IA", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (result == null) return; // cancelado

                // Extraer WINDOW...END de la respuesta
                string newBlock = ExtractWindowBlock(result);
                if (newBlock == null)
                {
                    MessageBox.Show(
                        "Claude respondio pero no se encontro un bloque WINDOW...END en la respuesta.\n\n" +
                        "Respuesta recibida:\n" + (result.Length > 500 ? result.Substring(0, 500) + "..." : result),
                        "Formatear con IA", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Ubicar el bloque original en el editor
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

        private static string ExtractWindowBlock(string text)
        {
            // Quitar fences de markdown si Claude los agrego
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

        private static string ResolveProtocolFile(string configured)
        {
            // 1. Archivo configurado en el perfil
            if (!string.IsNullOrWhiteSpace(configured) && System.IO.File.Exists(configured))
                return configured;

            // 2. Predeterminado en %APPDATA%\ClarionAssistant\
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string defaultPath = System.IO.Path.Combine(appData, "ClarionAssistant", "Protocolo_WindowFormatter.md");
            if (System.IO.File.Exists(defaultPath))
                return defaultPath;

            // 3. Sin protocolo — se usarán las reglas hardcodeadas del perfil
            return null;
        }

        private static string BuildPrompt(string windowBlock, WindowFormatterProfile p, string protocolFile = null)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(protocolFile) && System.IO.File.Exists(protocolFile))
            {
                string protocol = System.IO.File.ReadAllText(protocolFile, Encoding.UTF8);
                sb.AppendLine("Reformatea este bloque WINDOW de Clarion aplicando el protocolo que se indica a continuacion.");
                sb.AppendLine("Devuelve UNICAMENTE el bloque WINDOW...END, sin explicaciones ni bloques de codigo markdown.");
                sb.AppendLine();
                sb.AppendLine("=== PROTOCOLO DE FORMATEO ===");
                sb.AppendLine();
                sb.AppendLine(protocol);
                sb.AppendLine();
                sb.AppendLine("=== FIN DEL PROTOCOLO ===");
            }
            else
            {
                sb.AppendLine("Reformatea este bloque WINDOW de Clarion aplicando las convenciones estandar de Clarion.");
                sb.AppendLine("Devuelve UNICAMENTE el bloque WINDOW...END, sin explicaciones ni bloques de codigo markdown.");
                sb.AppendLine("CONSERVAR SIN NINGUN CAMBIO: USE(), FORMAT(), MSG(), TIP(), ICON(), #SEQ(), #ORIG(),");
                sb.AppendLine("#ORDINAL(), #LINK(), #FIELDS(), texto de labels, titulos y strings. No agregar ni quitar controles.");
            }

            if (!string.IsNullOrWhiteSpace(p.AiExtraInstructions))
            {
                sb.AppendLine();
                sb.AppendLine("INSTRUCCIONES ADICIONALES DEL PERFIL:");
                foreach (var line in p.AiExtraInstructions.Replace("\r\n", "\n").Split('\n'))
                {
                    string t = line.Trim();
                    if (!string.IsNullOrEmpty(t)) sb.AppendLine("   - " + t);
                }
            }

            sb.AppendLine();
            sb.AppendLine("--- BLOQUE A REFORMATEAR ---");
            sb.AppendLine();
            sb.Append(windowBlock);

            return sb.ToString();
        }
    }
}
