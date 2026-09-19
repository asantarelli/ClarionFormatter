using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClarionWindowFormatter.Services;

namespace ClarionWindowFormatter.Dialogs
{
    public class WindowFormatterSettingsDialog : Form
    {
        private static readonly string[] ControlTypes =
        {
            "WINDOW", "PROMPT", "ENTRY",  "TEXT",  "CHECK", "OPTION",
            "LIST",   "COMBO",  "BUTTON", "STRING", "IMAGE",
            "GROUP",  "PANEL",  "SHEET",  "TAB",    "SPIN"
        };

        private WindowFormatterSettings _settings;
        private WindowFormatterProfile  _current;
        private ControlTypeRule         _currentRule;
        private bool _initializing = true;

        // Profile bar
        private ComboBox _cboProfiles;
        private Button   _btnNewProfile, _btnDelProfile, _btnRenameProfile;

        // Control type list
        private ListBox _lstControls;

        // Per-control fields
        private TextBox  _txtYBase, _txtYIncr, _txtXLabel, _txtXControl;
        private TextBox  _txtHeight, _txtMinWidth, _txtMaxWidth;
        private TextBox  _txtColorAttr;
        private CheckBox _chkGenerateTip;
        private TextBox  _txtTipTemplate;
        private TextBox  _txtExtraRules;

        // Global fields
        private TextBox _txtApiKey, _txtAiModel;
        private TextBox _txtAiExtra;

        public WindowFormatterSettingsDialog()
        {
            _settings = WindowFormatterProfileService.Load();
            _current  = WindowFormatterProfileService.GetActiveProfile();
            BuildUI();
            _initializing = true;
            RefreshProfileList();
            _initializing = false;
        }

        private void BuildUI()
        {
            Text            = "Window Formatter - Configuracion";
            Size            = new Size(700, 660);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;

            // ── Profile bar (top) ──────────────────────────────────────────────
            var profilePanel = new Panel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(4) };
            profilePanel.Controls.Add(new Label { Text = "Perfil:", Location = new Point(6, 10), AutoSize = true });
            _cboProfiles = new ComboBox { Location = new Point(50, 6), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboProfiles.SelectedIndexChanged += (s, e) => OnProfileSelected();
            _btnNewProfile    = MakeSmallBtn("Nuevo",     new Point(268, 6)); _btnNewProfile.Click    += OnNewProfile;
            _btnRenameProfile = MakeSmallBtn("Renombrar", new Point(350, 6)); _btnRenameProfile.Click += OnRenameProfile;
            _btnDelProfile    = MakeSmallBtn("Eliminar",  new Point(436, 6)); _btnDelProfile.Click    += OnDeleteProfile;
            profilePanel.Controls.AddRange(new Control[] { _cboProfiles, _btnNewProfile, _btnRenameProfile, _btnDelProfile });

            // ── Button bar (bottom) ────────────────────────────────────────────
            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            var btnOk = new Button { Text = "Guardar", DialogResult = DialogResult.OK, Size = new Size(90, 26), Location = new Point(498, 7) };
            btnOk.Click += (s, e) => SaveValues();
            var btnCancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Size = new Size(90, 26), Location = new Point(594, 7) };
            btnPanel.Controls.AddRange(new Control[] { btnOk, btnCancel });

            // ── Path label (bottom) ────────────────────────────────────────────
            var pathLabel = new Label
            {
                Text         = "Config: " + WindowFormatterProfileService.SettingsPath,
                Dock         = DockStyle.Bottom,
                Height       = 16,
                Font         = new Font("Courier New", 7f),
                ForeColor    = Color.Gray,
                AutoEllipsis = true
            };

            // ── TabControl (fill) ─────────────────────────────────────────────
            var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Regular) };

            tabs.TabPages.Add(BuildTabControles());
            tabs.TabPages.Add(BuildTabNotas());
            tabs.TabPages.Add(BuildTabApi());

            Controls.Add(tabs);
            Controls.Add(pathLabel);
            Controls.Add(btnPanel);
            Controls.Add(profilePanel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        // ── Tab: Controles ────────────────────────────────────────────────────

        private TabPage BuildTabControles()
        {
            var page = new TabPage("Controles");

            // Import button at bottom
            var btnImport = new Button
            {
                Text     = "Importar protocolo v2.5 como valores por defecto",
                Dock     = DockStyle.Bottom,
                Height   = 28,
                Font     = new Font(Font, FontStyle.Regular),
                ForeColor = Color.DarkGreen
            };
            btnImport.Click += OnImportProtocol;

            // Main split
            var mainPanel = new Panel { Dock = DockStyle.Fill };

            _lstControls = new ListBox
            {
                Dock        = DockStyle.Left,
                Width       = 110,
                Font        = new Font("Courier New", 9f),
                BorderStyle = BorderStyle.None,
                BackColor   = SystemColors.Control
            };
            foreach (var ct in ControlTypes) _lstControls.Items.Add(ct);
            _lstControls.SelectedIndexChanged += OnControlTypeSelected;

            var splitter = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = SystemColors.ControlDark };

            var rightPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10, 6, 8, 6) };
            BuildRightPanel(rightPanel);

            mainPanel.Controls.Add(rightPanel);
            mainPanel.Controls.Add(splitter);
            mainPanel.Controls.Add(_lstControls);

            page.Controls.Add(mainPanel);
            page.Controls.Add(btnImport);
            return page;
        }

        private void BuildRightPanel(Panel p)
        {
            int y = 4;
            int w = 540;

            var tt = new ToolTip { AutoPopDelay = 9000, InitialDelay = 400, ReshowDelay = 200 };

            // ── Coordenadas ──────────────────────────────────────────────────
            AddFieldSectionLabel(p, "Coordenadas", ref y);

            _txtYBase = AddNumField(p, "Y base:",  8, y, 56, 60);
            _txtYIncr = AddNumField(p, "Incr. Y:", 200, y, 56, 60);
            tt.SetToolTip(_txtYBase, "Coordenada Y de la primera fila de controles. Segun protocolo v2.5: Y=23 para PROMPT");
            tt.SetToolTip(_txtYIncr, "Incremento vertical entre filas. Segun protocolo v2.5: 14px (secuencia 23,37,51,65...)");
            y += 28;

            _txtXLabel   = AddNumField(p, "X etiqueta:", 8,   y, 76, 60);
            _txtXControl = AddNumField(p, "X control:",  200, y, 68, 60);
            tt.SetToolTip(_txtXLabel,   "Coordenada X de los PROMPT/labels. Segun protocolo v2.5: X=11 (estandar)");
            tt.SetToolTip(_txtXControl, "Coordenada X de los controles (ENTRY, etc.). Se calcula: X_PROMPT + W_PROMPT_largo + 5");
            y += 30;

            // ── Dimensiones ───────────────────────────────────────────────────
            AddFieldSectionLabel(p, "Dimensiones", ref y);

            _txtHeight   = AddNumField(p, "Altura:",      8,   y, 50, 60);
            _txtMinWidth = AddNumField(p, "Ancho min.:", 200,  y, 72, 60);
            _txtMaxWidth = AddNumField(p, "Ancho max.:", 380,  y, 72, 60);
            tt.SetToolTip(_txtHeight,   "Altura del control. v2.5: PROMPT=10, ENTRY/CHECK/LIST/SPIN=9, BUTTON principal=14");
            tt.SetToolTip(_txtMinWidth, "Ancho minimo. Dejar vacio para no restringir");
            tt.SetToolTip(_txtMaxWidth, "Ancho maximo. Dejar vacio para no restringir");
            y += 30;

            // ── Apariencia ───────────────────────────────────────────────────
            AddFieldSectionLabel(p, "Apariencia", ref y);

            p.Controls.Add(new Label { Text = "COLOR:", Location = new Point(8, y + 3), AutoSize = true });
            _txtColorAttr = new TextBox { Location = new Point(56, y), Width = 300, Font = new Font("Courier New", 8.5f) };
            tt.SetToolTip(_txtColorAttr,
                "Atributo COLOR de Clarion. Ejemplos del protocolo v2.5:\n" +
                "  COLOR(00E0F0FFh)  -> Azul claro (campos obligatorios REQ)\n" +
                "  COLOR(00F0F0F0h)  -> Gris claro (campos READONLY)\n" +
                "  COLOR(00E8FFE8h)  -> Verde claro (campos monetarios)\n" +
                "  COLOR(00F8FFFFh)  -> Amarillo suave (areas TEXT)\n" +
                "  COLOR(00F8F8F8h)  -> Gris muy claro (listas)\n" +
                "Dejar vacio para no modificar el color.");
            p.Controls.Add(_txtColorAttr);
            y += 30;

            // ── Tooltip ───────────────────────────────────────────────────────
            AddFieldSectionLabel(p, "Tooltip (TIP)", ref y);

            _chkGenerateTip = new CheckBox { Text = "Generar TIP automaticamente", Location = new Point(8, y), AutoSize = true };
            _chkGenerateTip.CheckedChanged += (s, e) => _txtTipTemplate.Enabled = _chkGenerateTip.Checked;
            tt.SetToolTip(_chkGenerateTip,
                "Si esta activo, Claude generara el atributo TIP() para controles de este tipo.\n" +
                "Segun protocolo v2.5: agregar TIP a todos los controles excepto SHEET, PROMPT y STRING.");
            p.Controls.Add(_chkGenerateTip);
            y += 24;

            p.Controls.Add(new Label { Text = "Plantilla:", Location = new Point(8, y + 3), AutoSize = true });
            _txtTipTemplate = new TextBox { Location = new Point(72, y), Width = 340, Enabled = false };
            tt.SetToolTip(_txtTipTemplate,
                "Texto base del TIP. Usa {LABEL} como variable con el texto del PROMPT.\n" +
                "Ejemplos del protocolo v2.5:\n" +
                "  Campos REQ:      'REQUERIDO - {LABEL} (Enter para validar)'\n" +
                "  Campos lookup:   '{LABEL} - F2 para buscar, Enter para validar'\n" +
                "  Campos calc:     '{LABEL} - Se calcula: Cantidad x Precio unitario'");
            p.Controls.Add(_txtTipTemplate);
            y += 30;

            // ── Reglas adicionales ────────────────────────────────────────────
            AddFieldSectionLabel(p, "Reglas adicionales para este control", ref y);

            _txtExtraRules = new TextBox
            {
                Location      = new Point(8, y),
                Size          = new Size(w, 72),
                Multiline     = true,
                ScrollBars    = ScrollBars.Vertical,
                WordWrap      = true,
                AcceptsReturn = true
            };
            tt.SetToolTip(_txtExtraRules,
                "Instrucciones en texto libre solo para este tipo de control.\n" +
                "Ej para ENTRY: 'Si el campo es READONLY agregar COLOR(00F0F0F0h)'\n" +
                "Ej para BUTTON: 'Posicionar de derecha a izquierda con gap de 4px'");
            p.Controls.Add(_txtExtraRules);
        }

        // ── Tab: Notas ────────────────────────────────────────────────────────

        private TabPage BuildTabNotas()
        {
            var page = new TabPage("Notas adicionales");

            var lbl = new Label
            {
                Text     = "Instrucciones adicionales para Claude (aplican a todos los formateos de este perfil):",
                Dock     = DockStyle.Top,
                Height   = 22,
                Padding  = new Padding(6, 4, 0, 0),
                Font     = new Font(Font, FontStyle.Regular)
            };

            _txtAiExtra = new TextBox
            {
                Dock          = DockStyle.Fill,
                Multiline     = true,
                ScrollBars    = ScrollBars.Vertical,
                WordWrap      = true,
                AcceptsReturn = true,
                Font          = new Font("Courier New", 8.5f)
            };

            page.Controls.Add(_txtAiExtra);
            page.Controls.Add(lbl);
            return page;
        }

        // ── Tab: API Claude ───────────────────────────────────────────────────

        private TabPage BuildTabApi()
        {
            var page = new TabPage("API Claude");
            int y = 20;

            page.Controls.Add(new Label { Text = "Configuracion global de la API de Anthropic.", Location = new Point(14, y), AutoSize = true, ForeColor = Color.Gray });
            y += 28;

            page.Controls.Add(new Label { Text = "API Key (sk-ant-...):", Location = new Point(14, y + 2), AutoSize = true });
            _txtApiKey = new TextBox { Location = new Point(170, y), Width = 470, PasswordChar = '*' };
            _txtApiKey.Text = _settings.AnthropicApiKey ?? "";
            page.Controls.Add(_txtApiKey);
            y += 30;

            page.Controls.Add(new Label { Text = "Modelo:", Location = new Point(14, y + 2), AutoSize = true });
            _txtAiModel = new TextBox { Location = new Point(170, y), Width = 300 };
            _txtAiModel.Text = _settings.AiModel ?? "claude-sonnet-4-6";
            page.Controls.Add(_txtAiModel);
            y += 22;

            page.Controls.Add(new Label
            {
                Text      = "Recomendado: claude-sonnet-4-6   Rapido/economico: claude-haiku-4-5-20251001",
                Location  = new Point(170, y), Width = 470, Height = 16,
                Font      = new Font(Font, FontStyle.Italic), ForeColor = Color.Gray
            });
            y += 32;

            var divider = new Panel { Location = new Point(14, y), Size = new Size(628, 1), BackColor = Color.LightGray };
            page.Controls.Add(divider);
            y += 14;

            page.Controls.Add(new Label
            {
                Text     = "La API Key se almacena en:\n" + WindowFormatterProfileService.SettingsPath + "\nNo se transmite a ningun servicio que no sea api.anthropic.com.",
                Location = new Point(14, y), Width = 628, Height = 48,
                Font     = new Font("Courier New", 7.5f), ForeColor = Color.Gray
            });

            return page;
        }

        // ── Import protocol v2.5 ──────────────────────────────────────────────

        private void OnImportProtocol(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Esto va a cargar los valores del Protocolo Clarion v2.5 como configuracion por defecto.\n\n" +
                "Los campos que ya tengas configurados van a ser reemplazados.\n\n" +
                "¿Continuar?",
                "Importar protocolo v2.5", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            // Guardar regla actual antes de sobreescribir
            if (_currentRule != null) ReadRuleFromForm(_currentRule);

            ApplyProtocolDefaults(_current);

            // Recargar la regla visible
            if (_currentRule != null)
            {
                var updated = _current.ControlRules.FirstOrDefault(r => r.ControlType == _currentRule.ControlType);
                if (updated != null) { _currentRule = updated; LoadRuleToForm(_currentRule); }
            }

            // Cargar notas generales
            _txtAiExtra.Text = BuildDefaultNotes();

            MessageBox.Show(
                "Protocolo v2.5 importado.\n\n" +
                "Revisa cada tipo de control y ajusta los valores a tu proyecto.\n" +
                "Las notas adicionales tambien fueron actualizadas (solapa 'Notas adicionales').",
                "Importar protocolo v2.5", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void ApplyProtocolDefaults(WindowFormatterProfile p)
        {
            SetRule(p, "WINDOW",  yBase: "",   yIncr: "",   xLabel: "",  xCtrl: "",  h: "",
                color: "", tip: false, tipTmpl: "",
                extra: "Calcular dimensiones minimas necesarias con margen de 5px en cada direccion.\n" +
                       "Botones inferiores: Y = Alto_ventana - 19. H_botones = 14. Gap entre botones: 4px.\n" +
                       "Posicionar botones de derecha a izquierda: X_anterior = X_siguiente - W_anterior - 4.\n" +
                       "Eliminar atributo JOIN en controles SHEET (causa problemas de renderizado).");

            SetRule(p, "PROMPT",  yBase: "23", yIncr: "14", xLabel: "11", xCtrl: "",  h: "10",
                color: "", tip: false, tipTmpl: "",
                extra: "Alinear todos los ENTRYs de una seccion al mismo X, determinado por el PROMPT mas largo + 5px.\n" +
                       "Si hay un PROMPT excepcionalmente largo, excluirlo del calculo de columna comun.\n" +
                       "Secuencia Y: 23, 37, 51, 65, 79, 93, 107...");

            SetRule(p, "ENTRY",   yBase: "",   yIncr: "14", xLabel: "",   xCtrl: "",  h: "9",
                color: "COLOR(00E0F0FFh)", tip: true,
                tipTmpl: "REQUERIDO - {LABEL} (Enter para validar)",
                extra: "Y = PROMPT Y + 1 (bottom-aligned: ENTRY comparte borde inferior con su PROMPT).\n" +
                       "Campos READONLY: usar COLOR(00F0F0F0h) en lugar del color por defecto.\n" +
                       "Campos monetarios (picture con $): usar COLOR(00E8FFE8h).\n" +
                       "Campos REQ: usar COLOR(00E0F0FFh) y TIP que empiece con 'REQUERIDO - '.\n" +
                       "Campos con lookup: TIP = '{LABEL} - F2 para buscar, Enter para validar'.");

            SetRule(p, "TEXT",    yBase: "",   yIncr: "",   xLabel: "",   xCtrl: "",  h: "",
                color: "COLOR(00F8FFFFh)", tip: true,
                tipTmpl: "{LABEL}",
                extra: "Mantener altura original (minimo 20px). No reducir alto del TEXT.\n" +
                       "Si esta dentro de un GROUP con un solo TEXT: margen izq=9, margen sup=15, ancho=GROUP_W-18, alto=GROUP_H-25.");

            SetRule(p, "CHECK",   yBase: "",   yIncr: "14", xLabel: "",   xCtrl: "",  h: "9",
                color: "", tip: true, tipTmpl: "{LABEL}",
                extra: "");

            SetRule(p, "OPTION",  yBase: "",   yIncr: "14", xLabel: "",   xCtrl: "",  h: "9",
                color: "", tip: true, tipTmpl: "{LABEL}",
                extra: "");

            SetRule(p, "LIST",    yBase: "",   yIncr: "",   xLabel: "",   xCtrl: "",  h: "9",
                color: "COLOR(00F8F8F8h)", tip: true, tipTmpl: "{LABEL}",
                extra: "COMBO en fila de filtro de browse: mantener H=10 (no bajar a 9).\n" +
                       "En ventana browse: X=11, Y= Y_filtro + H_filtro + 6 (tipicamente Y=38).\n" +
                       "H_LIST = Y_CRUD - Y_LIST - 4.");

            SetRule(p, "COMBO",   yBase: "",   yIncr: "",   xLabel: "",   xCtrl: "",  h: "10",
                color: "", tip: true, tipTmpl: "{LABEL} - F2 para buscar, Enter para validar",
                extra: "En fila de filtro de browse: mantener H=10. X = CHECK_right + 4.");

            SetRule(p, "BUTTON",  yBase: "",   yIncr: "",   xLabel: "",   xCtrl: "",  h: "14",
                color: "", tip: false, tipTmpl: "",
                extra: "Botones principales (OK, Cancelar, Guardar): H=14.\n" +
                       "Botones lookup/F2/Calendar inline (a la derecha de un ENTRY): H=10, Y=PROMPT_Y.\n" +
                       "Posicionar de derecha a izquierda: X_anterior = X_siguiente - W_anterior - 4.\n" +
                       "Verificar que gap entre botones adyacentes sea exactamente 4px.");

            SetRule(p, "STRING",  yBase: "",   yIncr: "",   xLabel: "",   xCtrl: "",  h: "10",
                color: "", tip: false, tipTmpl: "",
                extra: "STRING inline (misma fila que un ENTRY): H=10, Y = PROMPT Y (NO Y_ENTRY).\n" +
                       "X = ENTRY_right + 5 o BUTTON_right + gap.\n" +
                       "No agregar TIP a STRING.");

            SetRule(p, "IMAGE",   yBase: "",   yIncr: "",   xLabel: "",   xCtrl: "",  h: "",
                color: "", tip: false, tipTmpl: "", extra: "");

            SetRule(p, "GROUP",   yBase: "",   yIncr: "",   xLabel: "",   xCtrl: "",  h: "",
                color: "", tip: false, tipTmpl: "",
                extra: "Margenes estandar (multiples controles): superior=6, izquierdo=5, inferior=5.\n" +
                       "Caso especial GROUP con un unico TEXT: margen_izq=9, margen_sup=15, ancho_TEXT=GROUP_W-18, alto_TEXT=GROUP_H-25.\n" +
                       "Solo ajustar el contenido interno, no el GROUP en si.");

            SetRule(p, "PANEL",   yBase: "",   yIncr: "",   xLabel: "",   xCtrl: "",  h: "",
                color: "", tip: false, tipTmpl: "", extra: "");

            SetRule(p, "SHEET",   yBase: "",   yIncr: "",   xLabel: "",   xCtrl: "",  h: "",
                color: "", tip: false, tipTmpl: "",
                extra: "Formularios: SHEET_bottom = Y_botones - 3. H_ventana = Y_botones + 14 + 3.\n" +
                       "Browse:      SHEET_bottom = Y_botones - 4. H_ventana = Y_botones + 18.\n" +
                       "No agregar TIP a SHEET.");

            SetRule(p, "TAB",     yBase: "",   yIncr: "",   xLabel: "",   xCtrl: "",  h: "",
                color: "", tip: false, tipTmpl: "",
                extra: "Primer control visible dentro del TAB: siempre X=11, Y=23 (igual que formulario).");

            SetRule(p, "SPIN",    yBase: "",   yIncr: "14", xLabel: "",   xCtrl: "",  h: "9",
                color: "", tip: true, tipTmpl: "{LABEL}",
                extra: "");
        }

        private static void SetRule(WindowFormatterProfile p, string ctrlType,
            string yBase, string yIncr, string xLabel, string xCtrl, string h,
            string color, bool tip, string tipTmpl, string extra)
        {
            var r = p.ControlRules.FirstOrDefault(x => x.ControlType == ctrlType);
            if (r == null) { r = new ControlTypeRule { ControlType = ctrlType }; p.ControlRules.Add(r); }
            r.YBase       = yBase;
            r.YIncrement  = yIncr;
            r.XLabel      = xLabel;
            r.XControl    = xCtrl;
            r.Height      = h;
            r.ColorAttr   = color;
            r.GenerateTip = tip;
            r.TipTemplate = tipTmpl;
            r.ExtraRules  = extra;
        }

        private static string BuildDefaultNotes()
        {
            return
                "CONSERVAR SIN NINGUN CAMBIO (critico para templates Clarion):\n" +
                "  #SEQ(), #ORIG(), #ORDINAL(), #LINK(), #FIELDS()\n" +
                "  USE(), FORMAT(), MSG(), ICON()\n" +
                "  Texto de labels, titulos y strings\n" +
                "  No agregar ni quitar controles\n" +
                "\n" +
                "COLORES SEMANTICOS (usar COLOR, no BCOLOR):\n" +
                "  COLOR(00E0F0FFh) -> Azul claro para campos REQ (obligatorios)\n" +
                "  COLOR(00F0F0F0h) -> Gris claro para campos READONLY\n" +
                "  COLOR(00E8FFE8h) -> Verde claro para campos monetarios ($ en picture)\n" +
                "  COLOR(00F8FFFFh) -> Amarillo suave para areas TEXT\n" +
                "  COLOR(00F8F8F8h) -> Gris muy claro para listas\n" +
                "  Jerarquia: Obligatorio > ReadOnly > Monetario > tipo de control\n" +
                "\n" +
                "TOOLTIPS:\n" +
                "  Agregar TIP a todos los controles EXCEPTO: SHEET, PROMPT, STRING\n" +
                "  Campos REQ: iniciar con 'REQUERIDO - '\n" +
                "  Campos con lookup: mencionar 'F2 para buscar'\n" +
                "  Campos calculados: explicar formula brevemente\n" +
                "\n" +
                "ELIMINAR si existe:\n" +
                "  Atributo JOIN en controles SHEET (causa problemas en versiones modernas)\n" +
                "\n" +
                "ADAPTABILIDAD:\n" +
                "  El protocolo es una guia, no una regla ciega.\n" +
                "  Permitir ajustes de +/-1px para perfeccion visual.\n" +
                "  Priorizar legibilidad real sobre cumplimiento exacto de formula.\n" +
                "  Contenido especial puede requerir excepciones justificadas.";
        }

        // ── Control type selection ────────────────────────────────────────────

        private void OnControlTypeSelected(object sender, EventArgs e)
        {
            if (_initializing) return;
            if (_currentRule != null) ReadRuleFromForm(_currentRule);
            string ctrlType = _lstControls.SelectedItem?.ToString();
            if (ctrlType == null) return;
            _currentRule = GetOrCreateRule(ctrlType);
            LoadRuleToForm(_currentRule);
        }

        private ControlTypeRule GetOrCreateRule(string ctrlType)
        {
            var existing = _current.ControlRules.FirstOrDefault(r => r.ControlType == ctrlType);
            if (existing != null) return existing;
            var newRule = new ControlTypeRule { ControlType = ctrlType };
            _current.ControlRules.Add(newRule);
            return newRule;
        }

        private void ReadRuleFromForm(ControlTypeRule r)
        {
            r.YBase       = _txtYBase.Text.Trim();
            r.YIncrement  = _txtYIncr.Text.Trim();
            r.XLabel      = _txtXLabel.Text.Trim();
            r.XControl    = _txtXControl.Text.Trim();
            r.Height      = _txtHeight.Text.Trim();
            r.MinWidth    = _txtMinWidth.Text.Trim();
            r.MaxWidth    = _txtMaxWidth.Text.Trim();
            r.ColorAttr   = _txtColorAttr.Text.Trim();
            r.GenerateTip = _chkGenerateTip.Checked;
            r.TipTemplate = _txtTipTemplate.Text.Trim();
            r.ExtraRules  = _txtExtraRules.Text;
        }

        private void LoadRuleToForm(ControlTypeRule r)
        {
            _txtYBase.Text          = r.YBase;
            _txtYIncr.Text          = r.YIncrement;
            _txtXLabel.Text         = r.XLabel;
            _txtXControl.Text       = r.XControl;
            _txtHeight.Text         = r.Height;
            _txtMinWidth.Text       = r.MinWidth;
            _txtMaxWidth.Text       = r.MaxWidth;
            _txtColorAttr.Text      = r.ColorAttr;
            _chkGenerateTip.Checked = r.GenerateTip;
            _txtTipTemplate.Text    = r.TipTemplate;
            _txtTipTemplate.Enabled = r.GenerateTip;
            _txtExtraRules.Text     = r.ExtraRules;
        }

        private void ClearRuleForm()
        {
            _txtYBase.Text          = "";
            _txtYIncr.Text          = "";
            _txtXLabel.Text         = "";
            _txtXControl.Text       = "";
            _txtHeight.Text         = "";
            _txtMinWidth.Text       = "";
            _txtMaxWidth.Text       = "";
            _txtColorAttr.Text      = "";
            _chkGenerateTip.Checked = false;
            _txtTipTemplate.Text    = "";
            _txtTipTemplate.Enabled = false;
            _txtExtraRules.Text     = "";
        }

        // ── Profile management ────────────────────────────────────────────────

        private void RefreshProfileList()
        {
            _initializing = true;
            _cboProfiles.Items.Clear();
            foreach (var p in _settings.Profiles)
                _cboProfiles.Items.Add(p.ProfileName);
            int idx = _cboProfiles.Items.IndexOf(_settings.ActiveProfile);
            _cboProfiles.SelectedIndex = idx >= 0 ? idx : 0;
            _initializing = false;

            string name = _cboProfiles.SelectedItem?.ToString();
            _current = _settings.Profiles.FirstOrDefault(p => p.ProfileName == name) ?? _settings.Profiles[0];
            _txtAiExtra.Text = _current.AiExtraInstructions ?? "";
            _currentRule = null;
            ClearRuleForm();
            _lstControls.SelectedIndex = 0;
        }

        private void OnProfileSelected()
        {
            if (_initializing || _cboProfiles.SelectedIndex < 0) return;
            ReadFormInto(_current);
            string name = _cboProfiles.SelectedItem.ToString();
            _current = _settings.Profiles.FirstOrDefault(p => p.ProfileName == name) ?? _settings.Profiles[0];
            _settings.ActiveProfile = _current.ProfileName;
            _txtAiExtra.Text = _current.AiExtraInstructions ?? "";
            _currentRule = null;
            ClearRuleForm();
            _lstControls.SelectedIndex = 0;
        }

        private void OnNewProfile(object s, EventArgs e)
        {
            string name = PromptString("Nombre del nuevo perfil:", "Nuevo perfil");
            if (string.IsNullOrWhiteSpace(name)) return;
            if (_settings.Profiles.Any(p => p.ProfileName == name))
            { MessageBox.Show("Ya existe un perfil con ese nombre."); return; }
            var newP = new WindowFormatterProfile { ProfileName = name };
            _settings.Profiles.Add(newP);
            _settings.ActiveProfile = name;
            _current = newP;
            RefreshProfileList();
        }

        private void OnRenameProfile(object s, EventArgs e)
        {
            string name = PromptString("Nuevo nombre:", "Renombrar perfil", _current.ProfileName);
            if (string.IsNullOrWhiteSpace(name) || name == _current.ProfileName) return;
            if (_settings.Profiles.Any(p => p.ProfileName == name))
            { MessageBox.Show("Ya existe un perfil con ese nombre."); return; }
            _current.ProfileName = name;
            _settings.ActiveProfile = name;
            RefreshProfileList();
        }

        private void OnDeleteProfile(object s, EventArgs e)
        {
            if (_settings.Profiles.Count <= 1)
            { MessageBox.Show("No se puede eliminar el unico perfil."); return; }
            if (MessageBox.Show("Eliminar perfil \"" + _current.ProfileName + "\"?",
                "Confirmar", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            _settings.Profiles.Remove(_current);
            _settings.ActiveProfile = _settings.Profiles[0].ProfileName;
            _current = _settings.Profiles[0];
            RefreshProfileList();
        }

        // ── Load / Save ───────────────────────────────────────────────────────

        private void ReadFormInto(WindowFormatterProfile p)
        {
            _settings.AnthropicApiKey = _txtApiKey.Text.Trim();
            _settings.AiModel         = _txtAiModel.Text.Trim();
            p.AiExtraInstructions     = _txtAiExtra.Text;
            if (_currentRule != null) ReadRuleFromForm(_currentRule);
        }

        private void SaveValues()
        {
            ReadFormInto(_current);
            WindowFormatterProfileService.Save(_settings);
            WindowFormatterProfileService.Invalidate();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Button MakeSmallBtn(string text, Point loc)
            => new Button { Text = text, Size = new Size(80, 26), Location = loc };

        private static TextBox AddNumField(Panel p, string label, int x, int y, int labelW, int boxW)
        {
            p.Controls.Add(new Label { Text = label, Location = new Point(x, y + 3), Width = labelW, Height = 16 });
            var tb = new TextBox { Location = new Point(x + labelW + 2, y), Width = boxW, MaxLength = 12 };
            p.Controls.Add(tb);
            return tb;
        }

        private static void AddFieldSectionLabel(Panel p, string text, ref int y)
        {
            var lbl = new Label
            {
                Text      = "-- " + text + " --",
                Location  = new Point(8, y),
                Width     = 560,
                Height    = 16,
                Font      = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            p.Controls.Add(lbl);
            y += 22;
        }

        private static string PromptString(string prompt, string title, string defaultVal = "")
        {
            var f = new Form
            {
                Text = title, Size = new Size(340, 120),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent, MaximizeBox = false
            };
            f.Controls.Add(new Label { Text = prompt, Location = new Point(10, 10), AutoSize = true });
            var tb = new TextBox { Location = new Point(10, 30), Width = 300, Text = defaultVal };
            var ok = new Button { Text = "OK",       Location = new Point(140, 58), DialogResult = DialogResult.OK };
            var ca = new Button { Text = "Cancelar", Location = new Point(228, 58), DialogResult = DialogResult.Cancel };
            f.Controls.AddRange(new Control[] { tb, ok, ca });
            f.AcceptButton = ok; f.CancelButton = ca;
            return f.ShowDialog() == DialogResult.OK ? tb.Text.Trim() : null;
        }
    }
}
