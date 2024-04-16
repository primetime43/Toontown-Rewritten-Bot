using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;

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

        private Dictionary<string, string> plantComboDictionary = new Dictionary<string, string>();
        private void createMap()
        {
            //1 bean
            plantComboDictionary.Add("Laff-o-dil", "g");
            plantComboDictionary.Add("Dandy Pansy", "o");
            plantComboDictionary.Add("What-in Carnation", "i");
            plantComboDictionary.Add("School Daisy", "y");
            plantComboDictionary.Add("Lily-of-the-Alley", "c");
            //2 bean
            plantComboDictionary.Add("Daffy Dill", "gc");
            plantComboDictionary.Add("Chim Pansy", "oc");
            plantComboDictionary.Add("Instant Carnation", "iy");
            plantComboDictionary.Add("Lazy Daisy", "yr");
            plantComboDictionary.Add("Lily Pad", "cg");
            //3 bean
            plantComboDictionary.Add("Summer's Last Rose", "rrr");
            plantComboDictionary.Add("Potsen Pansy", "orr");
            plantComboDictionary.Add("Hybrid Carnation", "irr");
            plantComboDictionary.Add("Midsummer Daisy", "yrg");
            plantComboDictionary.Add("Tiger Lily", "coo");
            //4 bean
            plantComboDictionary.Add("Corn Rose", "ryoy");
            plantComboDictionary.Add("Giraff-o-dil", "giyy");
            plantComboDictionary.Add("Marzi Pansy", "oyyr");
            plantComboDictionary.Add("Freshasa Daisy", "yrco");
            plantComboDictionary.Add("Livered Lily", "cooi");
            //5 bean
            plantComboDictionary.Add("Time and a half-o-dil", "gibii");
            plantComboDictionary.Add("Onelip", "urbuu");
            plantComboDictionary.Add("Side Carnation", "irgbr");
            plantComboDictionary.Add("Whoopsie Daisy", "yrooo");
            plantComboDictionary.Add("Chili Lily", "crrrr");
            //6 bean
            plantComboDictionary.Add("Tinted Rose", "rioroi");
            plantComboDictionary.Add("Smarty Pansy", "oiiobi");
            plantComboDictionary.Add("Twolip", "urrruu");
            plantComboDictionary.Add("Upsy Daisy", "ybcubb");
            plantComboDictionary.Add("Silly Lily", "cruuuu");
            //7 bean
            plantComboDictionary.Add("Stinking Rose", "rcoiucc");
            plantComboDictionary.Add("Car Petunia", "bubucbb");
            plantComboDictionary.Add("Model Carnation", "iggggyg");
            plantComboDictionary.Add("Crazy Daisy", "ygroggg");
            plantComboDictionary.Add("Indubitab Lily", "cucbcbb");
            //8 bean
            plantComboDictionary.Add("Istilla Rose", "rbuubbib");
            plantComboDictionary.Add("Threelip", "uyyuyouy");
            plantComboDictionary.Add("Platoonia", "biibroyy");
            plantComboDictionary.Add("Hazy Dazy", "ybucurou");
            plantComboDictionary.Add("Dilly Lilly", "cbyycbyy");
        }

        /// <summary>
        /// Populates the flower selection combo box based on the specified number of beans.
        /// </summary>
        /// <param name="beans">The bean count category which determines the list of available flowers.</param>
        /// <remarks>
        /// This method updates the UI elements to reflect the available flower options based on the bean count.
        /// The bean count is represented as a string that includes the number of beans followed by the text 'Bean Plant'.
        /// Depending on the bean count, different flower options are added to the combo box for user selection.
        /// </remarks>
        public void PopulateFlowerOptionsBasedOnBeanCount(String beans)
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
            loadBeans(plantComboDictionary[selected], 0);
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

        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private async void button1_Click(object sender, EventArgs e)
        {
            DialogResult confirmation = MessageBox.Show("Make sure you're at the flower bed before pressing OK!", "", MessageBoxButtons.OKCancel);
            if (confirmation.Equals(DialogResult.Cancel))
                return;

            string selected = (string)comboBox1.SelectedItem;

            try
            {
                // Place the task on a background thread to avoid blocking the UI
                await Task.Run(() => Gardening.PlantFlowerAsync(plantComboDictionary[selected], _cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Planting was canceled.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Check if the operation is already canceled or not started
            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                MessageBox.Show("Planting is not currently in progress.");
                return;
            }

            _cancellationTokenSource.Cancel();
        }
    }
}
