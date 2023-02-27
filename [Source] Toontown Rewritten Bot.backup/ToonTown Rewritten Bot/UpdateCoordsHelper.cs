using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    public partial class UpdateCoordsHelper : Form
    {
        private int timeLeft = 6;
        public UpdateCoordsHelper()
        {
            InitializeComponent();
            this.TopMost = true;
            bool showMessageOnce = true;
            if(showMessageOnce)
            {
                MessageBox.Show("This will help you update your coordinates to work with\nwith your screen. Please move your cursor onto the\nlocation that is says it is updating.");
                showMessageOnce = false;
            }
        }

        public void startCountDown(String nameOfItem)
        {
            label1.Text = "Updating " + nameOfItem;
            timer2.Start();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            label1.Visible = true;
            label2.Visible = true;
            if (timeLeft > 0)
            {
                timeLeft = timeLeft - 1;
                label2.Text = timeLeft + " seconds";
            }
            else
            {
                timer2.Stop();
                this.Close();
            }
        }
    }
}
