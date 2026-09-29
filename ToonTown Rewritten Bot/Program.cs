using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                _ = Utilities.GameProfile.Current;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not read game_selection.json. Fix or remove that file beside the bot, then restart.\n\n{ex.GetBaseException().Message}",
                    "Game selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Application.Run(new MainForm());
        }
    }
}
