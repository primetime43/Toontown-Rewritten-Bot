using InputSimulatorEx.Native;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using WindowsInput;

namespace ToonTown_Rewritten_Bot
{
    class Fishing : AdvancedSettings
    {
        /** The random variance of casting the fishing rod (if enabled).*/
        public static int VARIANCE = 20;
        private new static int x, y;
        private static Random rand = new Random();
        private static string redFishingButtonColor = "#FD0000";

        //location, num of casts, num of sells
        public static async void startFishing(string location, int numberOfCasts, int numberOfTimesToMeetFisherman, bool randomCasting)
        {
            if (numberOfTimesToMeetFisherman != 0)
            {
                Thread.Sleep(3000);
                if (!BotFunctions.checkCoordinates("15"))//if they're 0,0, enter. Checks the red fishing button
                {
                    //imgRecLocateRedCastBtn();//use the image rec to locate the image and set the coordinates

                    //manuallyLocateRedFishingButton();

                    //do the image search for color here. Make it so you can use the search or manual set (temp code testing)
                    Image screenshot = ImageRecognition.GetWindowScreenshot();
                    Point coords = await ImageRecognition.locateColorInImage(screenshot, redFishingButtonColor, 10);

                    if(coords.X == 0 && coords.Y == 0)//color not found, manually update
                    {
                        manuallyLocateRedFishingButton();
                    }
                    else
                        BotFunctions.manuallyUpdateCoordinatesNoUI("15", coords);
                }
                //start fishing
                startFishing(numberOfCasts, randomCasting);
                //walking to fisherman
                switch (location)
                {
                    case "TOONTOWN CENTRAL PUNCHLINE PLACE":
                        fishTTCPunchlinePlace();//goes to fisherman and back to dock
                        startFishing(location, numberOfCasts, numberOfTimesToMeetFisherman - 1, randomCasting);
                        break;
                    case "DONALD DREAM LAND LULLABY LANE":
                        fishDDLLullabyLane();
                        startFishing(location, numberOfCasts, numberOfTimesToMeetFisherman - 1, randomCasting);
                        break;
                    case "BRRRGH POLAR PLACE":
                        fishBrrrghPolarPlace();
                        startFishing(location, numberOfCasts, numberOfTimesToMeetFisherman - 1, randomCasting);
                        break;
                    case "BRRRGH WALRUS WAY":
                        fishBrrrghWalrusWay();
                        startFishing(location, numberOfCasts, numberOfTimesToMeetFisherman - 1, randomCasting);
                        break;
                    case "BRRRGH SLEET STREET":
                        fishBrrrghSleetSt();
                        startFishing(location, numberOfCasts, numberOfTimesToMeetFisherman - 1, randomCasting);
                        break;
                    case "MINNIE'S MELODYLAND TENOR TERRACE":
                        fishMMTenorTerrace();
                        startFishing(location, numberOfCasts, numberOfTimesToMeetFisherman - 1, randomCasting);
                        break;
                    case "DONALD DOCK LIGHTHOUSE LANE":
                        fishDDLighthouseLane();
                        startFishing(location, numberOfCasts, numberOfTimesToMeetFisherman - 1, randomCasting);
                        break;
                    case "DAISY'S GARDEN ELM STREET":
                        fishDaisyGardenElmSt();
                        startFishing(location, numberOfCasts, numberOfTimesToMeetFisherman - 1, randomCasting);
                        break;
                    case "FISH ANYWHERE":
                        break;
                }
            }
            MessageBox.Show("Done!");
        }

        private static void fishTTCPunchlinePlace()
        {
            //Go to fisherman
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.DOWN);
            Thread.Sleep(2000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.RIGHT);
            Thread.Sleep(800);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.RIGHT);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(700);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
            Thread.Sleep(2000);
            sellFish();//sell fish
            //Go back to dock
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.DOWN);
            Thread.Sleep(700);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(750);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(2000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
        }

        private static void fishDDLLullabyLane()
        {
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP);
            Thread.Sleep(4000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
            sellFish();
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.DOWN);
            Thread.Sleep(6500);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
        }

        private static void fishBrrrghPolarPlace()
        {
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.RIGHT);
            Thread.Sleep(800);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.RIGHT);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP); ;
            Thread.Sleep(2000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
            sellFish();
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.DOWN);
            Thread.Sleep(2000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
        }

        private static void fishMMTenorTerrace()
        {
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(1090);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP); ;
            Thread.Sleep(2200);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
            sellFish();
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.DOWN);
            Thread.Sleep(3000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
        }

        private static void fishBrrrghWalrusWay()
        {
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP); ;
            Thread.Sleep(100);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(730);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP); ;
            Thread.Sleep(2000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
            sellFish();
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.DOWN);
            Thread.Sleep(2100);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.RIGHT);
            Thread.Sleep(700);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.RIGHT);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.DOWN);
            Thread.Sleep(1000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
        }

        private static void fishBrrrghSleetSt()
        {
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.DOWN);
            Thread.Sleep(600);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.RIGHT);
            Thread.Sleep(850);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.RIGHT);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP); ;
            Thread.Sleep(1000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
            sellFish();
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.DOWN);
            Thread.Sleep(1700);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(850);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP); ;
            Thread.Sleep(600);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
        }

        private static void fishDaisyGardenElmSt()
        {
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(80);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.LEFT);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP); ;
            Thread.Sleep(2000);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
            sellFish();
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.DOWN);
            Thread.Sleep(4500);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
        }

        private static void fishDDLighthouseLane()
        {
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.RIGHT);
            Thread.Sleep(330);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.RIGHT);
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.UP); ;
            Thread.Sleep(2200);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.UP);
            sellFish();
            InputManager.InputSim.Keyboard.KeyDown(VirtualKeyCode.DOWN);
            Thread.Sleep(4500);
            InputManager.InputSim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
        }

        private static void startFishing(int numberOfCasts, bool fishVariance)
        {
            Stopwatch stopwatch = new Stopwatch();
            while (numberOfCasts != 0)
            {
                castLine(fishVariance);
                stopwatch.Start();
                while (stopwatch.Elapsed.Seconds < 30 && !checkIfFishCaught())
                {
                    checkIfFishCaught();
                }
                stopwatch.Stop();
                stopwatch.Reset();
                numberOfCasts--;
                Thread.Sleep(1000);
            }
            exitFishing();
            Thread.Sleep(3000);
        }

        private static void sellFish()
        {
            retry:
            if (BotFunctions.checkCoordinates("17"))//returns true if they are not 0,0
            {
                Thread.Sleep(2100);
                getCoords("17");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
            }
            else
            {
                BotFunctions.manualUpdateCoordinates("17");
                //imgRecLocateSellBtn();
                goto retry;
            }
            Thread.Sleep(2000);
        }

        private static void exitFishing()
        {
            retry:
            if (BotFunctions.checkCoordinates("16"))//returns true if they are not 0,0
            {
                getCoords("16");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
            }
            else
            {
                BotFunctions.manualUpdateCoordinates("16");
                //imgRecLocateExitBtn();//use image rec to find the exit fishing button
                goto retry;
            }
        }

        private static void castLine(bool fishVariance)
        {
            getCoords("15");

            int randX = 0;
            int randY = 0;
            if (fishVariance) { 
                randX = rand.Next(-VARIANCE, VARIANCE+1);
                randY = rand.Next(-VARIANCE, VARIANCE+1);
            } 
            BotFunctions.MoveCursor(x + randX, y + randY);
            //Debug.WriteLine("X variance: " + randX + " \nY Variance: " + randY);
            BotFunctions.DoFishingClick();
        }

        private static bool checkIfFishCaught()
        {
            bool result = false;
            getCoords("15");
            String color = BotFunctions.HexConverter(BotFunctions.GetColorAt(x, y - 600));
            if (color.Equals("#FFFFBE") || color.Equals("#FFFFBF"))
                result = true;//fish caught
            // Check if boot caught (smaller catch window)
            color = BotFunctions.HexConverter(BotFunctions.GetColorAt(x, 110));
            if (color.Equals("#FFFFBE") || color.Equals("#FFFFBF"))
                result = true;//fish caught
            return result;
        }

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
                            BotFunctions.updateTextFile(lines);//changes the coordinate values in the data file
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
                    BotFunctions.manualUpdateCoordinates("16");
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
                            BotFunctions.updateTextFile(lines);//changes the coordinate values in the data file
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
                    BotFunctions.manualUpdateCoordinates("17");
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
                            BotFunctions.updateTextFile(lines);//changes the coordinate values in the data file
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
                    manuallyLocateRedFishingButton();//manually locate/show the bot where the red fishing button is
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

        private static void manuallyLocateRedFishingButton()
        {
            BotFunctions.manualUpdateCoordinates("15");//update the red fishing button coords
        }

        private static void getCoords(String item)
        {
            int[] coordinates = BotFunctions.getCoordinates(item);
            x = coordinates[0];
            y = coordinates[1];
        }
    }
}
