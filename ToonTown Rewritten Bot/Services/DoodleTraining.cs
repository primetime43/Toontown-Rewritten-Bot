using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Utilities;
using ToonTown_Rewritten_Bot.Views;
using static ToonTown_Rewritten_Bot.Models.Coordinates;

namespace ToonTown_Rewritten_Bot.Services
{
    public class DoodleTraining : CoreFunctionality
    {
        private int _numberOfFeeds, _numberOfScratches;
        private string _selectedTrick;
        private bool _infiniteTime, _justFeed, _justScratch;
        public async Task StartDoodleTraining(int feeds, int scratches, bool unlimitedCheckBox, string trick, bool justFeed, bool justScratch, CancellationToken cancellationToken)
        {
            Logger.Info("Doodle", $"Training start: feeds={feeds}, scratches={scratches}, trick={trick}, unlimited={unlimitedCheckBox}");

            _numberOfFeeds = feeds;
            _numberOfScratches = scratches;
            _selectedTrick = trick;
            _infiniteTime = unlimitedCheckBox;
            _justFeed = justFeed;
            _justScratch = justScratch;

            // Check if game window is available and focus it
            EnsureGameWindowReady();
            FocusTTRWindow();

            await Task.Delay(2000, cancellationToken);
            await feedAndScratch(cancellationToken);
        }

        private async Task feedAndScratch(CancellationToken cancellationToken)
        {
            while (_infiniteTime || _numberOfFeeds > 0 || _numberOfScratches > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_justScratch && _numberOfFeeds > 0)
                {
                    await feedDoodle(cancellationToken);
                    if (!_infiniteTime) _numberOfFeeds--;
                }

                if (!_justFeed && _numberOfScratches > 0)
                {
                    await scratchDoodle(cancellationToken);
                    if (!_infiniteTime) _numberOfScratches--;
                }

                if (_selectedTrick != "None")
                    await DetermineSelectedTrick(cancellationToken);

                await Task.Delay(5000, cancellationToken);
            }
        }

        public async Task DetermineSelectedTrick(CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken);

            switch (_selectedTrick)
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
            Logger.Debug("Doodle", $"Performing trick: {_selectedTrick}");
            for (int i = 0; i < 2; i++)
            {
                await OpenSpeedChat(cancellationToken);
                await trickAction(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        public async Task OpenSpeedChat(CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken);

            // Use image recognition to find SpeedChat button (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(DoodleTrainingCoordinatesEnum.GreenSpeedChatButton);
            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(1000, cancellationToken);

            // Use image recognition to find Pets tab (will prompt for template capture if needed)
            (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(DoodleTrainingCoordinatesEnum.PetsTabInSpeedChat);
            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(1000, cancellationToken);

            // Use image recognition to find Tricks tab (will prompt for template capture if needed)
            (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(DoodleTrainingCoordinatesEnum.TricksTabInSpeedChat);
            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(1000, cancellationToken);
        }

        public async Task TrainBeg(CancellationToken cancellationToken)
        {
            // Use image recognition (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(DoodleTrainingCoordinatesEnum.BegTrickOptionInSpeedChat);
            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        public async Task TrainPlayDead(CancellationToken cancellationToken)
        {
            // Use image recognition (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(DoodleTrainingCoordinatesEnum.PlayDeadTrickOptionInSpeedChat);
            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        public async Task TrainRollover(CancellationToken cancellationToken)
        {
            // Use image recognition (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(DoodleTrainingCoordinatesEnum.RolloverTrickOptionInSpeedChat);
            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        public async Task TrainBackflip(CancellationToken cancellationToken)
        {
            // Use image recognition (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(DoodleTrainingCoordinatesEnum.BackflipTrickOptionInSpeedChat);
            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        public async Task TrainDance(CancellationToken cancellationToken)
        {
            // Use image recognition (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(DoodleTrainingCoordinatesEnum.DanceTrickOptionInSpeedChat);
            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        public async Task TrainSpeak(CancellationToken cancellationToken)
        {
            // Use image recognition (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(DoodleTrainingCoordinatesEnum.SpeakTrickOptionInSpeedChat);
            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        public async Task TrainJump(CancellationToken cancellationToken)
        {
            // Use image recognition (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(DoodleTrainingCoordinatesEnum.JumpTrickOptionInSpeedChat);
            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        public async Task feedDoodle(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Logger.Debug("Doodle", "Feeding doodle");
            // Use image recognition (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(DoodleTrainingCoordinatesEnum.FeedDoodleButton);
            MoveCursor(x, y);
            DoMouseClick();
            await Task.Delay(11500, cancellationToken);
        }

        public async Task scratchDoodle(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Logger.Debug("Doodle", "Scratching doodle");
            // Use image recognition (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(DoodleTrainingCoordinatesEnum.ScratchDoodleButton);
            CoreFunctionality.MoveCursor(x, y);
            CoreFunctionality.DoMouseClick();
            await Task.Delay(10000, cancellationToken);
        }
    }
}
