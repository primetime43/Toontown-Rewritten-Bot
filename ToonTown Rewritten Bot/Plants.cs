using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    public partial class Plants : Form
    {
        public Plants()
        {
            InitializeComponent();
            this.TopMost = true;
            createMap();
        }

        private Dictionary<string, string> dictionary = new Dictionary<string, string>();
        private void createMap()
        {
            //1 bean
            dictionary.Add("Laff-o-dil", "g");
            dictionary.Add("Dandy Pansy", "o");
            dictionary.Add("What-in Carnation", "i");
            dictionary.Add("School Daisy", "y");
            dictionary.Add("Lily-of-the-Alley", "c");
            //2 bean
            dictionary.Add("Daffy Dill", "gc");
            dictionary.Add("Chim Pansy", "oc");
            dictionary.Add("Instant Carnation", "iy");
            dictionary.Add("Lazy Daisy", "yr");
            dictionary.Add("Lily Pad", "cg");
            //3 bean
            dictionary.Add("Summer's Last Rose", "rrr");
            dictionary.Add("Potsen Pansy", "orr");
            dictionary.Add("Hybrid Carnation", "irr");
            dictionary.Add("Midsummer Daisy", "yrg");
            dictionary.Add("Tiger Lily", "coo");
            //4 bean
            dictionary.Add("Corn Rose", "ryoy");
            dictionary.Add("Giraff-o-dil", "giyy");
            dictionary.Add("Marzi Pansy", "oyyr");
            dictionary.Add("Freshasa Daisy", "yrco");
            dictionary.Add("Livered Lily", "cooi");
            //5 bean
            dictionary.Add("Time and a half-o-dil", "gibii");
            dictionary.Add("Onelip", "urbuu");
            dictionary.Add("Side Carnation", "irgbr");
            dictionary.Add("Whoopsie Daisy", "yrooo");
            dictionary.Add("Chili Lily", "crrrr");
            //6 bean
            dictionary.Add("Tinted Rose", "rioroi");
            dictionary.Add("Smarty Pansy", "oiiobi");
            dictionary.Add("Twolip", "urrruu");
            dictionary.Add("Upsy Daisy", "ybcubb");
            dictionary.Add("Silly Lily", "cruuuu");
            //7 bean
            dictionary.Add("Stinking Rose", "rcoiucc");
            dictionary.Add("Car Petunia", "bubucbb");
            dictionary.Add("Model Carnation", "iggggyg");
            dictionary.Add("Crazy Daisy", "ygroggg");
            dictionary.Add("Indubitab Lily", "cucbcbb");
            //8 bean
            dictionary.Add("Istilla Rose", "rbuubbib");
            dictionary.Add("Threelip", "uyyuyouy");
            dictionary.Add("Platoonia", "biibroyy");
            dictionary.Add("Hazy Dazy", "ybucurou");
            dictionary.Add("Dilly Lilly", "cbyycbyy");
        }
        public void loadFlowers(String beans)
        {
            label1.Text = beans;
            switch (beans)
            {
                case "1 Bean Plant":
                    comboBox1.Items.AddRange(new object[] { "Laff-o-dil", "Dandy Pansy", "What-in Carnation", "School Daisy", "Lily-of-the-Alley" });
                    break;
                case "2 Bean Plant":
                    comboBox1.Items.AddRange(new object[] { "Daffy Dill", "Chim Pansy", "Instant Carnation", "Lazy Daisy", "Lily Pad" });
                    break;
                case "3 Bean Plant":
                    comboBox1.Items.AddRange(new object[] { "Summer's Last Rose", "Potsen Pansy", "Hybrid Carnation", "Midsummer Daisy", "Tiger Lily" });
                    break;
                case "4 Bean Plant":
                    comboBox1.Items.AddRange(new object[] { "Corn Rose", "Giraff-o-dil", "Marzi Pansy", "Freshasa Daisy", "Livered Lily" });
                    break;
                case "5 Bean Plant":
                    comboBox1.Items.AddRange(new object[] { "Time and a half-o-dil", "Onelip", "Side Carnation", "Whoopsie Daisy", "Chili Lily" });
                    break;
                case "6 Bean Plant":
                    comboBox1.Items.AddRange(new object[] { "Tinted Rose", "Smarty Pansy", "Twolip", "Upsy Daisy", "Silly Lily" });
                    break;
                case "7 Bean Plant":
                    comboBox1.Items.AddRange(new object[] { "Stinking Rose", "Car Petunia", "Model Carnation", "Crazy Daisy", "Indubitab Lily" });
                    break;
                case "8 Bean Plant":
                    comboBox1.Items.AddRange(new object[] { "Istilla Rose", "Threelip", "Platoonia", "Hazy Dazy", "Dilly Lilly" });
                    break;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = (string)comboBox1.SelectedItem;
            loadBeans(dictionary[selected], 0);
        }

        private void loadBeans(String beansCombo, int index)
        {
  
            if (index != beansCombo.Length)
            {
                char letter = beansCombo[index];
                Image temp = null;
                switch (letter)
                {
                    case 'r':
                        temp = Properties.Resources.Redjellybean;
                        break;
                    case 'g':
                        temp = Properties.Resources.Green_0;
                        break;
                    case 'o':
                        temp = Properties.Resources.Orangejellybean;
                        break;
                    case 'u':
                        temp = Properties.Resources.Purplejellybean;
                        break;
                    case 'b':
                        temp = Properties.Resources.Bluejellybean;
                        break;
                    case 'i':
                        temp = Properties.Resources.Pinkjellybean;
                        break;
                    case 'y':
                        temp = Properties.Resources.Yellowjellybean;
                        break;
                    case 'c':
                        temp = Properties.Resources.Cyan;
                        break;
                    case 's':
                        temp = Properties.Resources.Silver;
                        break;
                }

                switch (index)
                {
                    case 0:
                        pictureBox1.Image = temp;
                        break;
                    case 1:
                        pictureBox2.Image = temp;
                        break;
                    case 2:
                        pictureBox3.Image = temp;
                        break;
                    case 3:
                        pictureBox4.Image = temp;
                        break;
                    case 4:
                        pictureBox5.Image = temp;
                        break;
                    case 5:
                        pictureBox6.Image = temp;
                        break;
                    case 6:
                        pictureBox7.Image = temp;
                        break;
                    case 7:
                        pictureBox8.Image = temp;
                        break;
                }
                loadBeans(beansCombo, index + 1);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult confirmation;
            confirmation = MessageBox.Show("Make sure you're at the flower bed before pressing OK!", "", MessageBoxButtons.OKCancel);
            if (confirmation.Equals(DialogResult.Cancel))
                return;
            string selected = (string)comboBox1.SelectedItem;
            Gardening.plantFlower(dictionary[selected]);
        }
    }
}
