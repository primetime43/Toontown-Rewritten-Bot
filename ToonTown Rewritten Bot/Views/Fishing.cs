using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Utilities;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Views
{
    class Fishing : AdvancedTemp
    {
        //location, num of casts, num of sells

        private static AdvancedSettings imgRec;
        private static void imgRecLocateExitBtn()
        {
        retry:
            imgRec = new AdvancedSettings();
            if (Properties.Settings.Default["exitFishingBtn"].ToString() == "")//no image has been set to the property
            {
                MessageBox.Show("Exit Fishing Button Image Not Set. Set it in Settings.");
                openImageSettingsForm();
            }
            else
                imgRec.callImageRecScript("exitFishingBtn");//run the script to try to find the imate and update/set them

            //eventually make this a function to use less code, just pass through the numerical value of the coordinate
            if (imgRec.message == "")//coordinates were found
            {
                Point coords = new Point(Convert.ToInt16(imgRec.x), Convert.ToInt16(imgRec.y));
                string x = imgRec.x;
                string y = imgRec.y;
                string[] lines = File.ReadAllLines(Path.GetFullPath("Coordinates Data File.txt"));
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("."))
                    {
                        if ("16".Equals(lines[i].Substring(0, lines[i].IndexOf('.'))))//look for the number it cooresponds to
                        {
                            lines[i] = "16" + "." + "(" + x + "," + y + ")";
                            CoreFunctionality.updateTextFile(lines);//changes the coordinate values in the data file
                        }
                    }
                }
            }
            else//coordinates were not found, try to update them manually instead
            {
                DialogResult dialogResult = MessageBox.Show(imgRec.message + ". Select Yes to try again or No to update the coordinate manually", imgRec.message, MessageBoxButtons.YesNoCancel);
                if (dialogResult == DialogResult.Yes)
                {
                    goto retry;
                }
                else if (dialogResult == DialogResult.No)
                {
                    //CommonFunctionality.ManualUpdateCoordinates("16");
                }
                else//cancel
                    return;
            }
        }

        private static void imgRecLocateSellBtn()
        {
        retry:
            imgRec = new AdvancedSettings();
            if (Properties.Settings.Default["sellFishBtn"].ToString() == "")//no image has been set to the property
            {
                MessageBox.Show("Sell Fish Button Image Not Set. Set it in Settings.");
                openImageSettingsForm();
            }
            else
                imgRec.callImageRecScript("sellFishBtn");//run the script to try to find the imate and update/set them

            //eventually make this a function to use less code, just pass through the numerical value of the coordinate
            if (imgRec.message == "")//coordinates were found
            {
                Point coords = new Point(Convert.ToInt16(imgRec.x), Convert.ToInt16(imgRec.y));
                string x = imgRec.x;
                string y = imgRec.y;
                string[] lines = File.ReadAllLines(Path.GetFullPath("Coordinates Data File.txt"));
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("."))
                    {
                        if ("17".Equals(lines[i].Substring(0, lines[i].IndexOf('.'))))//look for the number it cooresponds to
                        {
                            lines[i] = "17" + "." + "(" + x + "," + y + ")";
                            CoreFunctionality.updateTextFile(lines);//changes the coordinate values in the data file
                        }
                    }
                }
            }
            else//coordinates were not found, try to update them manually instead
            {
                DialogResult dialogResult = MessageBox.Show(imgRec.message + ". Select Yes to try again or No to update the coordinate manually", imgRec.message, MessageBoxButtons.YesNoCancel);
                if (dialogResult == DialogResult.Yes)
                {
                    goto retry;
                }
                else if (dialogResult == DialogResult.No)
                {
                    //CommonFunctionality.ManualUpdateCoordinates("17");
                }
                else//cancel
                    return;
            }
        }

        private static void imgRecLocateRedCastBtn()
        {
        retry:
            imgRec = new AdvancedSettings();
            if (Properties.Settings.Default["fishingCastBtn"].ToString() == "")//no image has been set to the property
            {
                MessageBox.Show("Red Fishing Button Image Not Set. Set it in Settings.");
                openImageSettingsForm();
            }
            else
                imgRec.callImageRecScript("fishingCastBtn");//run the script to try to find the imate and update/set them

            //eventually make this a function to use less code, just pass through the numerical value of the coordinate
            if (imgRec.message == "")//coordinates were found
            {
                Point coords = new Point(Convert.ToInt16(imgRec.x), Convert.ToInt16(imgRec.y));
                string x = imgRec.x;
                string y = imgRec.y;
                string[] lines = File.ReadAllLines(Path.GetFullPath("Coordinates Data File.txt"));
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("."))
                    {
                        if ("15".Equals(lines[i].Substring(0, lines[i].IndexOf('.'))))//look for the number it cooresponds to
                        {
                            lines[i] = "15" + "." + "(" + x + "," + y + ")";
                            CoreFunctionality.updateTextFile(lines);//changes the coordinate values in the data file
                        }
                    }
                }
            }
            else//coordinates were not found, try to update them manually instead
            {
                DialogResult dialogResult = MessageBox.Show(imgRec.message + ". Select Yes to try again or No to update the coordinate manually", imgRec.message, MessageBoxButtons.YesNoCancel);
                if (dialogResult == DialogResult.Yes)
                {
                    goto retry;
                }
                else if (dialogResult == DialogResult.No)
                {
                    //manuallyLocateRedFishingButton();//manually locate/show the bot where the red fishing button is
                }
                else//cancel
                    return;
            }
        }

        private static void openImageSettingsForm()
        {
            UpdateImages updateRecImages = new UpdateImages();
            try
            {
                updateRecImages.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Unable to perform this action", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private static void debugColorCoords(Image screenshot, Point coords)
        {
            // Create a new Bitmap object from the screenshot image
            Bitmap bitmap = new Bitmap(screenshot);

            // Create a new Graphics object from the Bitmap object
            Graphics graphics = Graphics.FromImage(bitmap);

            // Create a new Pen object for drawing the red square
            Pen pen = new Pen(Color.Green, 3);

            // Draw a red square around the point
            graphics.DrawRectangle(pen, new Rectangle(coords.X - 10, coords.Y - 10, 20, 20));

            // Display the image with the red square
            using (var form = new Form())
            {
                form.StartPosition = FormStartPosition.CenterScreen;
                form.ClientSize = new Size(bitmap.Width, bitmap.Height);
                form.BackgroundImage = bitmap;
                form.BackgroundImageLayout = ImageLayout.Zoom;
                form.ShowDialog();
            }
        }
    }
}
