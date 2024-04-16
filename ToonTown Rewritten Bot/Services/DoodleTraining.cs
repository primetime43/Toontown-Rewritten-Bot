using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Views;
using static ToonTown_Rewritten_Bot.Models.Coordinates;

namespace ToonTown_Rewritten_Bot.Services
{
    public class DoodleTraining : CoreFunctionality
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
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.GreenSpeedChatButton))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.GreenSpeedChatButton);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(1000);

                //Below is the location for pets tab
                //check if coordinates for the button is (0,0). True means they're not (0,0).
                if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.PetsTabInSpeedChat))
                {
                    (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.PetsTabInSpeedChat);
                    CoreFunctionality.MoveCursor(x, y);
                    CoreFunctionality.DoMouseClick();
                    Thread.Sleep(1000);

                    //Below is the location for tricks tab
                    //check if coordinates for the button is (0,0). True means they're not (0,0).
                    if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.TricksTabInSpeedChat))
                    {
                        (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.TricksTabInSpeedChat);
                        CoreFunctionality.MoveCursor(x, y);
                        CoreFunctionality.DoMouseClick();
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.TricksTabInSpeedChat);
                        Thread.Sleep(2000);
                        await openSpeedChat();
                    }
                }
                else
                {
                    await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.PetsTabInSpeedChat);
                    Thread.Sleep(2000);
                    await openSpeedChat();
                }
            }
            else//means it was (0,0) and needs updated
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.GreenSpeedChatButton);
                Thread.Sleep(2000);
                await openSpeedChat();
            }
        }

        public async Task trainBeg()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.BegTrickOptionInSpeedChat))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.BegTrickOptionInSpeedChat);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.BegTrickOptionInSpeedChat);
                Thread.Sleep(2000);
                await trainBeg();
            }
        }

        public async Task trainPlayDead()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.PlayDeadTrickOptionInSpeedChat))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.PlayDeadTrickOptionInSpeedChat);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.PlayDeadTrickOptionInSpeedChat);
                Thread.Sleep(2000);
                await trainPlayDead();
            }
        }

        public async Task trainRollover()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.RolloverTrickOptionInSpeedChat))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.RolloverTrickOptionInSpeedChat);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.RolloverTrickOptionInSpeedChat);
                Thread.Sleep(2000);
                await trainRollover();
            }
        }

        public async Task trainBackflip()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.BackflipTrickOptionInSpeedChat))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.BackflipTrickOptionInSpeedChat);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.BackflipTrickOptionInSpeedChat);
                Thread.Sleep(2000);
                await trainBackflip();
            }
        }

        public async Task trainDance()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.DanceTrickOptionInSpeedChat))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.DanceTrickOptionInSpeedChat);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.DanceTrickOptionInSpeedChat);
                Thread.Sleep(2000);
                await trainDance();
            }
        }

        public async Task trainSpeak()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.SpeakTrickOptionInSpeedChat))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.SpeakTrickOptionInSpeedChat);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.SpeakTrickOptionInSpeedChat);
                Thread.Sleep(2000);
                await trainSpeak();
            }
        }

        public async Task trainJump()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.JumpTrickOptionInSpeedChat))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.JumpTrickOptionInSpeedChat);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(2000);
            }
            else//means it was (0,0) and needs updated
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.JumpTrickOptionInSpeedChat);
                Thread.Sleep(2000);
                await trainJump();
            }
        }

        public async Task feedDoodle()
        {
            //check if coordinates for the button is (0,0). True means they're not (0,0).
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.FeedDoodleButton))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.FeedDoodleButton);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(11500);
            }
            else//means it was (0,0) and needs updated
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.FeedDoodleButton);
                Thread.Sleep(2000);
                await feedDoodle();
            }
        }

        public async Task scratchDoodle()
        {
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.ScratchDoodleButton))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.ScratchDoodleButton);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                Thread.Sleep(10000);
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.ScratchDoodleButton);
                Thread.Sleep(2000);
                await scratchDoodle();
            }
        }
    }
}
