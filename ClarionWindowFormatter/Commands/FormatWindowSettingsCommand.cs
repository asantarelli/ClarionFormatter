using System;
using System.Windows.Forms;
using ICSharpCode.Core;
using ClarionWindowFormatter.Dialogs;

namespace ClarionWindowFormatter.Commands
{
    public class FormatWindowSettingsCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            try
            {
                using (var dlg = new WindowFormatterSettingsDialog())
                    dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al abrir configuracion: " + ex.Message,
                    "Window Formatter", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
