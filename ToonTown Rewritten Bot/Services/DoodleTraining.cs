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
        public async Task StartDoodleTraining(int feeds, int scratches, bool unlimitedCheckBox, string trick, bool justFeed, bool justScratch, CancellationToken cancellationToken)
        {
            numberOfFeeds = feeds;
            numberOfScratches = scratches;
            selectedTrick = trick;
            infiniteTimeCheckBox = unlimitedCheckBox;
            justFeedCheckBox = justFeed;
            justScratchCheckBox = justScratch;

            await Task.Delay(2000, cancellationToken); // Use cancellation token
            await feedAndScratch(cancellationToken); // Pass cancellation token down
        }

        private async Task feedAndScratch(CancellationToken cancellationToken)
        {
            // Continue looping indefinitely if unlimited, or until tasks are done
            while (infiniteTimeCheckBox || numberOfFeeds > 0 || numberOfScratches > 0)
            {
                cancellationToken.ThrowIfCancellationRequested(); // Check for cancellation

                if (!justScratchCheckBox && numberOfFeeds > 0) // Feed if not just scratching and feeds are left
                {
                    await feedDoodle(cancellationToken);
                    if (!infiniteTimeCheckBox) numberOfFeeds--; // Only decrement if not unlimited
                }

                if (!justFeedCheckBox && numberOfScratches > 0) // Scratch if not just feeding and scratches are left
                {
                    await scratchDoodle(cancellationToken);
                    if (!infiniteTimeCheckBox) numberOfScratches--; // Only decrement if not unlimited
                }

                if (selectedTrick != "None") // If a trick is selected, perform it
                    await DetermineSelectedTrick(cancellationToken);

                await Task.Delay(5000, cancellationToken); // Wait for 5 seconds between actions, respect cancellation
            }
        }

        public async Task DetermineSelectedTrick(CancellationToken cancellationToken)
        {
            // Ensure there's a small delay before starting the trick (simulating setup time).
            await Task.Delay(1000, cancellationToken);

            // Check if the selected trick is recognized and perform the associated actions.
            switch (selectedTrick)
            {
                case "Jump (5 - 10 laff)":
                    await PerformTrickAsync(TrainJump, cancellationToken);
                    break;
                case "Beg (6 - 12 laff)":
                    await PerformTrickAsync(TrainBeg, cancellationToken);
                    break;
                case "Play Dead (7 - 14 laff)":
                    await PerformTrickAsync(TrainPlayDead, cancellationToken);
                    break;
                case "Rollover (8 - 16 laff)":
                    await PerformTrickAsync(TrainRollover, cancellationToken);
                    break;
                case "Backflip (9 - 18 laff)":
                    await PerformTrickAsync(TrainBackflip, cancellationToken);
                    break;
                case "Dance (10 - 20 laff)":
                    await PerformTrickAsync(TrainDance, cancellationToken);
                    break;
                case "Speak (11 - 22 laff)":
                    await PerformTrickAsync(TrainSpeak, cancellationToken);
                    break;
                default:
                    MessageBox.Show("Selected trick is not recognized. Please check the trick name and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        private async Task PerformTrickAsync(Func<CancellationToken, Task> trickAction, CancellationToken cancellationToken)
        {
            // Try the trick two times in case the first attempt fails (doodle might get confused).
            for (int i = 0; i < 2; i++)
            {
                await OpenSpeedChat(cancellationToken);  // Ensure OpenSpeedChat is now designed to accept and use CancellationToken.
                await trickAction(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();  // Properly handle cancellation between attempts.
            }
        }

        public async Task OpenSpeedChat(CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken); // Simulate delay before starting the operation

            // Check coordinates for the SpeedChat button
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.GreenSpeedChatButton))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.GreenSpeedChatButton);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                await Task.Delay(1000, cancellationToken); // Delay after clicking SpeedChat

                // Check coordinates for the Pets tab
                if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.PetsTabInSpeedChat))
                {
                    (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.PetsTabInSpeedChat);
                    CoreFunctionality.MoveCursor(x, y);
                    CoreFunctionality.DoMouseClick();
                    await Task.Delay(1000, cancellationToken); // Delay after clicking Pets tab

                    // Check coordinates for the Tricks tab
                    if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.TricksTabInSpeedChat))
                    {
                        (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.TricksTabInSpeedChat);
                        CoreFunctionality.MoveCursor(x, y);
                        CoreFunctionality.DoMouseClick();
                        await Task.Delay(1000, cancellationToken); // Delay after clicking Tricks tab
                    }
                    else
                    {
                        await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.TricksTabInSpeedChat);
                        await Task.Delay(2000, cancellationToken);
                        await OpenSpeedChat(cancellationToken); // Recursively call with cancellation support
                    }
                }
                else
                {
                    await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.PetsTabInSpeedChat);
                    await Task.Delay(2000, cancellationToken);
                    await OpenSpeedChat(cancellationToken); // Recursively call with cancellation support
                }
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.GreenSpeedChatButton);
                await Task.Delay(2000, cancellationToken);
                await OpenSpeedChat(cancellationToken); // Recursively call with cancellation support
            }
        }

        public async Task TrainBeg(CancellationToken cancellationToken)
        {
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.BegTrickOptionInSpeedChat))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.BegTrickOptionInSpeedChat);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                await Task.Delay(2000, cancellationToken);
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.BegTrickOptionInSpeedChat);
                await Task.Delay(2000, cancellationToken);
                await TrainBeg(cancellationToken);
            }
        }

        public async Task TrainPlayDead(CancellationToken cancellationToken)
        {
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.PlayDeadTrickOptionInSpeedChat))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.PlayDeadTrickOptionInSpeedChat);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                await Task.Delay(2000, cancellationToken);
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.PlayDeadTrickOptionInSpeedChat);
                await Task.Delay(2000, cancellationToken);
                await TrainPlayDead(cancellationToken);
            }
        }

        public async Task TrainRollover(CancellationToken cancellationToken)
        {
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.RolloverTrickOptionInSpeedChat))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.RolloverTrickOptionInSpeedChat);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                await Task.Delay(2000, cancellationToken);
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.RolloverTrickOptionInSpeedChat);
                await Task.Delay(2000, cancellationToken);
                await TrainRollover(cancellationToken);
            }
        }

        public async Task TrainBackflip(CancellationToken cancellationToken)
        {
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.BackflipTrickOptionInSpeedChat))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.BackflipTrickOptionInSpeedChat);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                await Task.Delay(2000, cancellationToken);
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.BackflipTrickOptionInSpeedChat);
                await Task.Delay(2000, cancellationToken);
                await TrainBackflip(cancellationToken);
            }
        }

        public async Task TrainDance(CancellationToken cancellationToken)
        {
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.DanceTrickOptionInSpeedChat))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.DanceTrickOptionInSpeedChat);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                await Task.Delay(2000, cancellationToken);
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.DanceTrickOptionInSpeedChat);
                await Task.Delay(2000, cancellationToken);
                await TrainDance(cancellationToken);
            }
        }

        public async Task TrainSpeak(CancellationToken cancellationToken)
        {
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.SpeakTrickOptionInSpeedChat))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.SpeakTrickOptionInSpeedChat);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                await Task.Delay(2000, cancellationToken);
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.SpeakTrickOptionInSpeedChat);
                await Task.Delay(2000, cancellationToken);
                await TrainSpeak(cancellationToken);
            }
        }

        public async Task TrainJump(CancellationToken cancellationToken)
        {
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.JumpTrickOptionInSpeedChat))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.JumpTrickOptionInSpeedChat);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                await Task.Delay(2000, cancellationToken);
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.JumpTrickOptionInSpeedChat);
                await Task.Delay(2000, cancellationToken);
                await TrainJump(cancellationToken);
            }
        }

        public async Task feedDoodle(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.FeedDoodleButton))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.FeedDoodleButton);
                MoveCursor(x, y);
                DoMouseClick();
                await Task.Delay(11500, cancellationToken); // Respect cancellation
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.FeedDoodleButton);
                await Task.Delay(2000, cancellationToken);
                await feedDoodle(cancellationToken); // Recursive call with cancellation
            }
        }

        public async Task scratchDoodle(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (CoordinatesManager.CheckCoordinates(DoodleTrainingCoordinatesEnum.ScratchDoodleButton))
            {
                var (x, y) = CoordinatesManager.GetCoordsFromMap(DoodleTrainingCoordinatesEnum.ScratchDoodleButton);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                await Task.Delay(10000, cancellationToken);
            }
            else
            {
                await CoordinatesManager.ManualUpdateCoordinates(DoodleTrainingCoordinatesEnum.ScratchDoodleButton);
                await Task.Delay(2000, cancellationToken);
                await scratchDoodle(cancellationToken);
            }
        }
    }
}
