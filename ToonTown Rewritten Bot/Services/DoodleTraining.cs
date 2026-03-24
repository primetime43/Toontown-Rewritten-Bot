using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Utilities;
using static ToonTown_Rewritten_Bot.Models.Coordinates;

namespace ToonTown_Rewritten_Bot.Services
{
    public class DoodleTraining : CoreFunctionality
    {
        private int _feedsPerCycle, _scratchesPerCycle, _totalCycles;
        private string _selectedTrick;
        private bool _infiniteTime, _justFeed, _justScratch;
        private TextRecognition _ocr;
        private Point? _lastTricksPosition;

        // Session counters
        private int _totalFeedsDone = 0;
        private int _totalScratchesDone = 0;
        private int _totalTricksDone = 0;

        private static volatile Views.DoodleOverlayForm _overlay;
        public static Views.DoodleOverlayForm Overlay
        {
            get => _overlay;
            set => _overlay = value;
        }

        private void UpdateOverlay(string status, string current, string next)
        {
            var overlay = _overlay;
            if (overlay == null || overlay.IsDisposed) return;
            try
            {
                if (overlay.InvokeRequired)
                {
                    overlay.BeginInvoke(new Action(() =>
                    {
                        if (!overlay.IsDisposed)
                        {
                            overlay.UpdateStatus(status, current, next);
                        }
                    }));
                }
                else
                {
                    overlay.UpdateStatus(status, current, next);
                }
            }
            catch { }
        }

        private void UpdateOverlayProgress()
        {
            var overlay = _overlay;
            if (overlay == null || overlay.IsDisposed) return;
            try
            {
                if (overlay.InvokeRequired)
                {
                    overlay.BeginInvoke(new Action(() =>
                    {
                        if (!overlay.IsDisposed)
                        {
                            overlay.UpdateProgress(_feedsPerCycle, _scratchesPerCycle, _infiniteTime, _selectedTrick,
                                _totalFeedsDone, _totalScratchesDone, _totalTricksDone, _totalCycles);
                        }
                    }));
                }
                else
                {
                    overlay.UpdateProgress(_feedsPerCycle, _scratchesPerCycle, _infiniteTime, _selectedTrick,
                        _totalFeedsDone, _totalScratchesDone, _totalTricksDone, _totalCycles);
                }
            }
            catch { }
        }
        public async Task StartDoodleTraining(int feedsPerCycle, int scratchesPerCycle, bool unlimitedCheckBox, string trick, bool justFeed, bool justScratch, CancellationToken cancellationToken, int cycles = 1)
        {
            Logger.Info("Doodle", $"Training start: feedsPerCycle={feedsPerCycle}, scratchesPerCycle={scratchesPerCycle}, trick={trick}, cycles={cycles}, unlimited={unlimitedCheckBox}");

            _feedsPerCycle = feedsPerCycle;
            _scratchesPerCycle = scratchesPerCycle;
            _totalCycles = cycles;
            _selectedTrick = trick;
            _infiniteTime = unlimitedCheckBox;
            _justFeed = justFeed;
            _justScratch = justScratch;

            // Only initialize OCR if tricks are selected (feed/scratch don't need it)
            if (_selectedTrick != "None")
            {
                try
                {
                    _ocr = new TextRecognition();
                }
                catch (Exception ex)
                {
                    Logger.Error("Doodle", $"OCR initialization failed: {ex.Message}");
                    throw new InvalidOperationException(
                        "OCR initialization failed. Trick training requires OCR.\n\n" +
                        "Try clicking 'Download OCR Data' on the Dev tab, then restart the app.\n\n" +
                        "Feed and scratch training can still be used without OCR by selecting trick 'None'.",
                        ex);
                }
            }

            // Doodle training always uses foreground mode (background mode is for fishing only)
            bool savedBackgroundMode = UseBackgroundInput;
            UseBackgroundInput = false;

            // Check if game window is available and focus it
            EnsureGameWindowReady();
            FocusTTRWindow();

            try
            {
                UpdateOverlayProgress();
                UpdateOverlay("Training", "Starting...", "Feed doodle");
                await Task.Delay(2000, cancellationToken);
                await feedAndScratch(cancellationToken);
                UpdateOverlay("Complete", "Done", "-");
            }
            finally
            {
                UseBackgroundInput = savedBackgroundMode;
                _ocr?.Dispose();
                _ocr = null;
            }
        }

        private async Task feedAndScratch(CancellationToken cancellationToken)
        {
            int cyclesDone = 0;

            while (_infiniteTime || cyclesDone < _totalCycles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                cyclesDone++;
                string cycleLabel = _infiniteTime ? $"Cycle {cyclesDone}" : $"Cycle {cyclesDone}/{_totalCycles}";
                Logger.Debug("Doodle", $"Starting {cycleLabel}");

                // Feed phase
                if (!_justScratch)
                {
                    for (int i = 0; i < _feedsPerCycle; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        string nextAfterFeed = (i < _feedsPerCycle - 1) ? "Feed again"
                            : (!_justFeed ? "Scratch doodle" : (_selectedTrick != "None" ? "Perform trick" : "Wait"));
                        UpdateOverlay("Feeding", $"Feeding ({i + 1}/{_feedsPerCycle})", nextAfterFeed);
                        await feedDoodle(cancellationToken);
                        _totalFeedsDone++;
                        UpdateOverlayProgress();
                    }
                }

                // Scratch phase
                if (!_justFeed)
                {
                    for (int i = 0; i < _scratchesPerCycle; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        string nextAfterScratch = (i < _scratchesPerCycle - 1) ? "Scratch again"
                            : (_selectedTrick != "None" ? "Perform trick" : "Wait");
                        UpdateOverlay("Scratching", $"Scratching ({i + 1}/{_scratchesPerCycle})", nextAfterScratch);
                        await scratchDoodle(cancellationToken);
                        _totalScratchesDone++;
                        UpdateOverlayProgress();
                    }
                }

                // Trick phase
                if (_selectedTrick != "None")
                {
                    UpdateOverlay("Trick", $"Performing {_selectedTrick}", "Wait");
                    await DetermineSelectedTrick(cancellationToken);
                    _totalTricksDone++;
                    UpdateOverlayProgress();
                }

                UpdateOverlay("Training", "Waiting...", "Next cycle");
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
                UpdateOverlay("Opening Menu", "Opening SpeedChat", $"Select {_selectedTrick}");
                await OpenSpeedChat(cancellationToken);
                UpdateOverlay("Trick", $"Clicking {_selectedTrick}", "Wait for response");
                await trickAction(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private const int MaxMenuRetries = 3;

        public async Task OpenSpeedChat(CancellationToken cancellationToken)
        {
            for (int attempt = 1; attempt <= MaxMenuRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Logger.Debug("Doodle", $"OpenSpeedChat attempt {attempt}/{MaxMenuRetries}");

                await Task.Delay(1000, cancellationToken);

                // Click the green SpeedChat button (template matching works well for this unique element)
                var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(DoodleTrainingCoordinatesEnum.GreenSpeedChatButton);
                CoreFunctionality.MoveCursor(x, y);
                CoreFunctionality.DoMouseClick();
                await Task.Delay(1000, cancellationToken);

                // Click "PETS" to open its submenu
                if (!await FindAndClickMenuText("PETS", cancellationToken))
                {
                    Logger.Warning("Doodle", $"Could not find PETS in SpeedChat menu (attempt {attempt})");
                    await CloseSpeedChatMenu(cancellationToken);
                    continue;
                }
                await Task.Delay(1000, cancellationToken);

                // Click "TRICKS" to open the tricks submenu, then move cursor right into it
                if (!await FindAndClickMenuText("TRICKS", cancellationToken))
                {
                    Logger.Warning("Doodle", $"Could not find TRICKS in Pets submenu (attempt {attempt})");
                    await CloseSpeedChatMenu(cancellationToken);
                    continue;
                }
                await Task.Delay(500, cancellationToken);

                // Store where TRICKS was found (cursor is there now) for trick search region
                _lastTricksPosition = getCursorLocation();

                // Move cursor to the right into the tricks submenu so it stays open
                MoveCursor(_lastTricksPosition.Value.X + 150, _lastTricksPosition.Value.Y);
                await Task.Delay(500, cancellationToken);
                return;
            }

            Logger.Error("Doodle", "Failed to open Tricks submenu after all retries");
        }

        /// <summary>
        /// Closes the SpeedChat menu by clicking somewhere neutral (away from the menu).
        /// </summary>
        private async Task CloseSpeedChatMenu(CancellationToken cancellationToken)
        {
            var windowRect = GetGameWindowRect();
            if (!windowRect.IsEmpty)
            {
                // Click the center-right of the screen (away from the SpeedChat menu area)
                int closeX = windowRect.X + (int)(windowRect.Width * 0.75);
                int closeY = windowRect.Y + (int)(windowRect.Height * 0.5);
                CoreFunctionality.MoveCursor(closeX, closeY);
                CoreFunctionality.DoMouseClick();
            }
            await Task.Delay(500, cancellationToken);
        }

        /// <summary>
        /// Takes a screenshot, uses OCR to find the target text, and clicks on it.
        /// Converts window-relative OCR coordinates to screen coordinates.
        /// </summary>
        /// <param name="searchRegion">Optional region (in window-relative coords) to limit OCR search</param>
        private async Task<bool> FindAndClickMenuText(string text, CancellationToken cancellationToken, Rectangle? searchRegion = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
            {
                if (screenshot == null)
                {
                    return false;
                }

                var pos = _ocr.FindTextPosition(screenshot, text, searchRegion: searchRegion);
                if (!pos.HasValue)
                {
                    return false;
                }

                var windowOffset = GetGameWindowOffset();
                int screenX = pos.Value.X + windowOffset.X;
                int screenY = pos.Value.Y + windowOffset.Y;

                Logger.Debug("Doodle", $"Clicking '{text}' at screen ({screenX}, {screenY})");
                CoreFunctionality.MoveCursor(screenX, screenY);
                await Task.Delay(500, cancellationToken);
                SimulateDragMove(screenX, screenY);
                await Task.Delay(100, cancellationToken);
                SendInputMouseDown();
                await Task.Delay(100, cancellationToken);
                SendInputMouseUp();
                await Task.Delay(1000, cancellationToken);
                return true;
            }
        }

        /// <summary>
        /// Opens SpeedChat, navigates to Tricks, and clicks the specified trick by OCR text.
        /// </summary>
        private async Task PerformTrickByName(string trickText, CancellationToken cancellationToken)
        {
            // Search for the trick in a region to the right of where TRICKS was found
            Rectangle? searchRegion = null;
            if (_lastTricksPosition.HasValue)
            {
                var windowOffset = GetGameWindowOffset();
                // Convert screen coords to window-relative for the search region
                int tricksX = _lastTricksPosition.Value.X - windowOffset.X;
                int tricksY = _lastTricksPosition.Value.Y - windowOffset.Y;
                // Search region: to the right of TRICKS, covering the submenu area
                searchRegion = new Rectangle(tricksX + 50, tricksY - 20, 300, 300);
                Logger.Debug("Doodle", $"Trick search region: {searchRegion}");
            }

            if (!await FindAndClickMenuText(trickText, cancellationToken, searchRegion))
            {
                Logger.Warning("Doodle", $"Could not find trick '{trickText}' in Tricks submenu");
            }
            await Task.Delay(2000, cancellationToken);
        }

        public Task TrainJump(CancellationToken ct) => PerformTrickByName("Jump", ct);
        public Task TrainBeg(CancellationToken ct) => PerformTrickByName("Beg", ct);
        public Task TrainPlayDead(CancellationToken ct) => PerformTrickByName("Play Dead", ct);
        public Task TrainRollover(CancellationToken ct) => PerformTrickByName("Rollover", ct);
        public Task TrainBackflip(CancellationToken ct) => PerformTrickByName("Backflip", ct);
        public Task TrainDance(CancellationToken ct) => PerformTrickByName("Dance", ct);
        public Task TrainSpeak(CancellationToken ct) => PerformTrickByName("Speak", ct);

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
