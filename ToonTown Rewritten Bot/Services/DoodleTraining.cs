using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Views;

namespace ToonTown_Rewritten_Bot.Services
{
    public class DoodleTraining : CommonFunctionality
    {
        public static int numberOfFeeds, numberOfScratches;
        private static string selectedTrick;
        private static bool infiniteTimeCheckBox, justFeedCheckBox, justScratchCheckBox;
        public static bool shouldStopTraining = false;
        public async Task startTrainingDoodle(int feeds, int scratches, bool unlimitedCheckBox, string trick, bool justFeed, bool justScratch)
        {
            numberOfFeeds = feeds;
            numberOfScratches = scratches;
            selectedTrick = trick;
            infiniteTimeCheckBox = unlimitedCheckBox;
            justFeedCheckBox = justFeed;
            justScratchCheckBox = justScratch;
            Thread.Sleep(2000);
            await feedAndScratch();
        }

        public async Task feedAndScratch()
        {
            if (!infiniteTimeCheckBox)//infinite checkbox is not checked
            {
                //code here is required so it doesn't get stuck in infinite loop below
                if (justFeedCheckBox)
                    numberOfScratches = 0;
                else if (justScratchCheckBox)
                    numberOfFeeds = 0;
                while (numberOfFeeds > 0 || numberOfScratches > 0 && !shouldStopTraining)
                {
                    Thread.Sleep(5000);
                    if (numberOfFeeds > 0)//feed doodle
                    {
                        await feedDoodle();
                        numberOfFeeds--;
                    }
                    if (numberOfScratches > 0)//scratch doodle
                    {
                        await scratchDoodle();
                        numberOfScratches--;
                    }
                    determineSelectedTrick();//perform trick
                }
            }
            else //infinite checkbox is checked, so loop until stopped
            {
                while (true && !shouldStopTraining)
                {
                    if (justFeedCheckBox)//just feed is checked
                        await feedDoodle();
                    else if (justScratchCheckBox)//just scratch is checked
                        await scratchDoodle();
                    else if (!justFeedCheckBox && !justScratchCheckBox)//neither are checked, so do both
                    {
                        await feedDoodle();
                        await scratchDoodle();
                    }
                    determineSelectedTrick();
                    Thread.Sleep(5000);
                }
            }
        }

        public async Task determineSelectedTrick()
        {
            Thread.Sleep(1000);
            switch (selectedTrick)
            {
                case "Jump (5 - 10 laff)":
                    for (int i = 0; i < 2; i++)//attempt trick 2 times incase doodle gets confused
                    {
                        await openSpeedChat();
                        await trainJump();
                    }
                    break;
                case "Beg (6 - 12 laff)":
                    for (int i = 0; i < 2; i++)//attempt trick 2 times incase doodle gets confused
                    {
                        await openSpeedChat();
                        await trainBeg();
                    }
                    break;
                case "Play Dead (7 - 14 laff)":
                    for (int i = 0; i < 2; i++)//attempt trick 2 times incase doodle gets confused
                    {
                        await openSpeedChat();
                        await trainPlayDead();
                    }
                    break;
                case "Rollover (8 - 16 laff)":
                    for (int i = 0; i < 2; i++)//attempt trick 2 times incase doodle gets confused
                    {
                        await openSpeedChat();
                        await trainRollover();
                    }
                    break;
                case "Backflip (9 - 18 laff)":
                    for (int i = 0; i < 2; i++)//attempt trick 2 times incase doodle gets confused
                    {
                        await openSpeedChat();
                        await trainBackflip();
                    }
                    break;
                case "Dance (10 - 20 laff)":
                    for (int i = 0; i < 2; i++)//attempt trick 2 times incase doodle gets confused
                    {
                        await openSpeedChat();
                        await trainDance();
                    }
                    break;
                case "Speak (11 - 22 laff)":
                    for (int i = 0; i < 2; i++)//attempt trick 2 times incase doodle gets confused
                    {
                        await openSpeedChat();
                        await trainSpeak();
                    }
                    break;
                default:
                    MessageBox.Show("Error!");
                    break;
            }
        }

        public async Task openSpeedChat()
        {
            Thread.Sleep(1000);
            //Below is the location for the SpeedChat button location
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CommonFunctionality.CheckCoordinates("20"))
            {
                var (x, y) = GetCoords("20");
                CommonFunctionality.MoveCursor(x, y);
                CommonFunctionality.DoMouseClick();
                Thread.Sleep(1000);

                //Below is the location for pets tab
                //check if coordinates for the button is (0,0). True means they're not (0,0).
                if (CommonFunctionality.CheckCoordinates("21"))
                {
                    (x, y) = GetCoords("21");
                    CommonFunctionality.MoveCursor(x, y);
                    CommonFunctionality.DoMouseClick();
                    Thread.Sleep(1000);

                    //Below is the location for tricks tab
                    //check if coordinates for the button is (0,0). True means they're not (0,0).
                    if (CommonFunctionality.CheckCoordinates("22"))
                    {
                        (x, y) = GetCoords("22");
                        CommonFunctionality.MoveCursor(x, y);
                        CommonFunctionality.DoMouseClick();
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        await ManualUpdateCoordinates("22");
                        Thread.Sleep(2000);
                        await openSpeedChat();
                    }
                }
                else
                {
                    await ManualUpdateCoordinates("21");
                    Thread.Sleep(2000);
                    await openSpeedChat();
                }
            }
            else//means it was (0,0) and needs updated
            {
                await ManualUpdateCoordinates("20");
                Thread.Sleep(2000);
                await openSpeedChat();
            }
        }

        public async Task trainBeg()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CommonFunctionality.CheckCoordinates("24"))
            {
                var (x, y) = GetCoords("24");
                CommonFunctionality.MoveCursor(x, y);
                CommonFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                await ManualUpdateCoordinates("24");
                Thread.Sleep(2000);
                await trainBeg();
            }
        }

        public async Task trainPlayDead()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CommonFunctionality.CheckCoordinates("25"))
            {
                var (x, y) = GetCoords("25");
                CommonFunctionality.MoveCursor(x, y);
                CommonFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                await ManualUpdateCoordinates("25");
                Thread.Sleep(2000);
                await trainPlayDead();
            }
        }

        public async Task trainRollover()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CommonFunctionality.CheckCoordinates("26"))
            {
                var (x, y) = GetCoords("26");
                CommonFunctionality.MoveCursor(x, y);
                CommonFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                await ManualUpdateCoordinates("26");
                Thread.Sleep(2000);
                await trainRollover();
            }
        }

        public async Task trainBackflip()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CommonFunctionality.CheckCoordinates("27"))
            {
                var (x, y) = GetCoords("27");
                CommonFunctionality.MoveCursor(x, y);
                CommonFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                await ManualUpdateCoordinates("27");
                Thread.Sleep(2000);
                await trainBackflip();
            }
        }

        public async Task trainDance()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CommonFunctionality.CheckCoordinates("28"))
            {
                var (x, y) = GetCoords("28");
                CommonFunctionality.MoveCursor(x, y);
                CommonFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                await ManualUpdateCoordinates("28");
                Thread.Sleep(2000);
                await trainDance();
            }
        }

        public async Task trainSpeak()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CommonFunctionality.CheckCoordinates("29"))
            {
                var (x, y) = GetCoords("29");
                CommonFunctionality.MoveCursor(x, y);
                CommonFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                await ManualUpdateCoordinates("29");
                Thread.Sleep(2000);
                await trainSpeak();
            }
        }

        public async Task trainJump()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CommonFunctionality.CheckCoordinates("23"))
            {
                var (x, y) = GetCoords("23");
                CommonFunctionality.MoveCursor(x, y);
                CommonFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                await ManualUpdateCoordinates("23");
                Thread.Sleep(2000);
                await trainJump();
            }
        }

        public async Task feedDoodle()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CommonFunctionality.CheckCoordinates("18"))
            {
                var (x, y) = GetCoords("18");
                CommonFunctionality.MoveCursor(x, y);
                CommonFunctionality.DoMouseClick();
                Thread.Sleep(11500);
            }
            else//means it was (0,0) and needs updated
            {
                await ManualUpdateCoordinates("18");
                Thread.Sleep(2000);
                await feedDoodle();
            }
        }

        public async Task scratchDoodle()
        {
            if (CommonFunctionality.CheckCoordinates("19"))
            {
                var (x, y) = GetCoords("19");
                CommonFunctionality.MoveCursor(x, y);
                CommonFunctionality.DoMouseClick();
                Thread.Sleep(10000);
            }
            else
            {
                await ManualUpdateCoordinates("19");
                Thread.Sleep(2000);
                await scratchDoodle();
            }
        }
    }
}
