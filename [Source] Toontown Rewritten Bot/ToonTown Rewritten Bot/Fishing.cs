using InputSimulatorEx.Native;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using WindowsInput;

namespace ToonTown_Rewritten_Bot
{
    class Fishing
    {
        /** The random variance of casting the fishing rod (if enabled).*/
        public static int VARIANCE = 20;
        private static int x, y;
        private static Random rand = new Random();


        public static void startFishing(String location, int numberOfCasts, int numberOfTimesToMeetFisherman, bool randomCasting)
        {
            if (numberOfTimesToMeetFisherman != 0)
            {
                Thread.Sleep(3000);
                if (!BotFunctions.checkCoordinates("15"))//if they're 0,0, enter. Checks the red fishing button
                    locateRedFishingButton();
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
                        MessageBox.Show("Done!");
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
            if (BotFunctions.checkCoordinates("17"))//returns true if they are not 0,0
            {
                Thread.Sleep(2100);
                getCoords("17");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
            }
            else
            {
                BotFunctions.updateCoordinates("17");
                sellFish();
            }
            Thread.Sleep(2000);
        }

        private static void exitFishing()
        {
            if (BotFunctions.checkCoordinates("16"))//returns true if they are not 0,0
            {
                getCoords("16");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
            }
            else
            {
                BotFunctions.updateCoordinates("16");
                exitFishing();
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
            Debug.WriteLine("X variance: " + randX + " \nY Variance: " + randY);
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

        private static void locateRedFishingButton()
        {
            BotFunctions.updateCoordinates("15");//update the red fishing button coords
        }

        private static void getCoords(String item)
        {
            int[] coordinates = BotFunctions.getCoordinates(item);
            x = coordinates[0];
            y = coordinates[1];
        }
    }
}
