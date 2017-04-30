using System;
using System.Threading;
using System.Windows.Forms;
using WindowsInput;

namespace ToonTown_Rewritten_Bot
{
    class DoodleTraining
    {
        public static int numberOfFeeds, numberOfScratches;
        private static string selectedTrick;
        private static bool infiniteTimeCheckBox, justFeedCheckBox, justScratchCheckBox;
        public static void startTrainingDoodle(int feeds, int scratches, bool unlimitedCheckBox, string trick, bool justFeed, bool justScratch)
        {
            numberOfFeeds = feeds;
            numberOfScratches = scratches;
            selectedTrick = trick;
            infiniteTimeCheckBox = unlimitedCheckBox;
            justFeedCheckBox = justFeed;
            justScratchCheckBox = justScratch;
            Thread.Sleep(2000);
            feedAndScratch();
        }

        public static void feedAndScratch()
        {
            if (!infiniteTimeCheckBox)//infinite checkbox is not checked
            {
                //code here is required so it doesn't get stuck in infinite loop below
                if (justFeedCheckBox)
                    numberOfScratches = 0;
                else if (justScratchCheckBox)
                    numberOfFeeds = 0;
                while (numberOfFeeds > 0 || numberOfScratches > 0)
                {
                    Thread.Sleep(5000);
                    if (numberOfFeeds > 0)//feed doodle
                    {
                        feedDoodle();
                        numberOfFeeds--;
                    }
                    if (numberOfScratches > 0)//scratch doodle
                    {
                        scratchDoodle();
                        numberOfScratches--; 
                    }
                    determineSelectedTrick();//perform trick
                }
            }
            else //infinite checkbox is checked, so loop until stopped
            {
                while (true)
                {
                    if (justFeedCheckBox)//just feed is checked
                        feedDoodle();
                    else if (justScratchCheckBox)//just scratch is checked
                        scratchDoodle();
                    else if(!justFeedCheckBox && !justScratchCheckBox)//neither are checked, so do both
                    {
                        feedDoodle();
                        scratchDoodle();
                    }
                    determineSelectedTrick();
                    Thread.Sleep(5000);
                }
            }
        }

        public static void determineSelectedTrick()
        {
            Thread.Sleep(1000);
            switch (selectedTrick)
            {
                case "Jump (5 - 10 laff)":
                    for(int i = 0; i < 2; i++)//attempt trick 2 times incase doodle gets confused
                    {
                        openSpeedChat();
                        trainJump();
                    }
                    break;
                case "Beg (6 - 12 laff)":
                    for (int i = 0; i < 2; i++)//attempt trick 2 times incase doodle gets confused
                    {
                        openSpeedChat();
                        trainBeg();
                    }
                    break;
                case "Play Dead (7 - 14 laff)":
                    for (int i = 0; i < 2; i++)//attempt trick 2 times incase doodle gets confused
                    {
                        openSpeedChat();
                        trainPlayDead();
                    }
                    break;
                case "Rollover (8 - 16 laff)":
                    for (int i = 0; i < 2; i++)//attempt trick 2 times incase doodle gets confused
                    {
                        openSpeedChat();
                        trainRollover();
                    }
                    break;
                case "Backflip (9 - 18 laff)":
                    for (int i = 0; i < 2; i++)//attempt trick 2 times incase doodle gets confused
                    {
                        openSpeedChat();
                        trainBackflip();
                    }
                    break;
                case "Dance (10 - 20 laff)":
                    for (int i = 0; i < 2; i++)//attempt trick 2 times incase doodle gets confused
                    {
                        openSpeedChat();
                        trainDance();
                    }
                    break;
                case "Speak (11 - 22 laff)":
                    for (int i = 0; i < 2; i++)//attempt trick 2 times incase doodle gets confused
                    {
                        openSpeedChat();
                        trainSpeak();
                    }
                    break;
                default:
                    MessageBox.Show("Error!");
                    break;
            }
        }

        public static void openSpeedChat()
        {
            Thread.Sleep(1000);
            //Below is the location for the SpeedChat button location
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (BotFunctions.checkCoordinates("20"))
            {
                getCoords("20");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(1000);

                //Below is the location for pets tab
                //check if coordinates for the button is (0,0). True means they're not (0,0).
                if (BotFunctions.checkCoordinates("21"))
                {
                    getCoords("21");
                    BotFunctions.MoveCursor(x, y);
                    BotFunctions.DoMouseClick();
                    Thread.Sleep(1000);

                    //Below is the location for tricks tab
                    //check if coordinates for the button is (0,0). True means they're not (0,0).
                    if (BotFunctions.checkCoordinates("22"))
                    {
                        getCoords("22");
                        BotFunctions.MoveCursor(x, y);
                        BotFunctions.DoMouseClick();
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        BotFunctions.updateCoordinates("22");
                        Thread.Sleep(2000);
                        openSpeedChat();
                    }
                }
                else
                {
                    BotFunctions.updateCoordinates("21");
                    Thread.Sleep(2000);
                    openSpeedChat();
                }
            }
            else//means it was (0,0) and needs updated
            {
                BotFunctions.updateCoordinates("20");
                Thread.Sleep(2000);
                openSpeedChat();
            }
        }

        public static void trainBeg()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (BotFunctions.checkCoordinates("24"))
            {
                getCoords("24");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                BotFunctions.updateCoordinates("24");
                Thread.Sleep(2000);
                trainBeg();
            }
        }

        public static void trainPlayDead()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (BotFunctions.checkCoordinates("25"))
            {
                getCoords("25");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                BotFunctions.updateCoordinates("25");
                Thread.Sleep(2000);
                trainPlayDead();
            }
        }

        public static void trainRollover()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (BotFunctions.checkCoordinates("26"))
            {
                getCoords("26");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                BotFunctions.updateCoordinates("26");
                Thread.Sleep(2000);
                trainRollover();
            }
        }

        public static void trainBackflip()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (BotFunctions.checkCoordinates("27"))
            {
                getCoords("27");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                BotFunctions.updateCoordinates("27");
                Thread.Sleep(2000);
                trainBackflip();
            }
        }

        public static void trainDance()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (BotFunctions.checkCoordinates("28"))
            {
                getCoords("28");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                BotFunctions.updateCoordinates("28");
                Thread.Sleep(2000);
                trainDance();
            }
        }

        public static void trainSpeak()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (BotFunctions.checkCoordinates("29"))
            {
                getCoords("29");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                BotFunctions.updateCoordinates("29");
                Thread.Sleep(2000);
                trainSpeak();
            }
        }

        public static void trainJump()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (BotFunctions.checkCoordinates("23"))
            {
                getCoords("23");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                BotFunctions.updateCoordinates("23");
                Thread.Sleep(2000);
                trainJump();
            }
        }

        public static void feedDoodle()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (BotFunctions.checkCoordinates("18"))
            {
                getCoords("18");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(11500);
            }
            else//means it was (0,0) and needs updated
            {
                BotFunctions.updateCoordinates("18");
                Thread.Sleep(2000);
                feedDoodle();
            }
        }

        public static void scratchDoodle()
        {
            if (BotFunctions.checkCoordinates("19"))
            {
                getCoords("19");
                BotFunctions.MoveCursor(x, y);
                BotFunctions.DoMouseClick();
                Thread.Sleep(10000);
            }
            else
            {
                BotFunctions.updateCoordinates("19");
                Thread.Sleep(2000);
                scratchDoodle();
            }
        }

        private static int x, y;
        private static void getCoords(String item)
        {
            int[] coordinates = BotFunctions.getCoordinates(item);
            x = coordinates[0];
            y = coordinates[1];
        }
    }
}
