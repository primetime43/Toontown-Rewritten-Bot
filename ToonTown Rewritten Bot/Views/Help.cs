using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    public partial class Help : Form
    {
        public Help()
        {
            InitializeComponent();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = linkLabel1.Text,
                UseShellExecute = true // Allows the process to open the link in the default browser
            };

            System.Diagnostics.Process.Start(psi);
        }
    }
}
