using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using ToonTown_Rewritten_Bot.Utilities;
using static ToonTown_Rewritten_Bot.Models.Coordinates;

namespace ToonTown_Rewritten_Bot.Services.FishingLocationsWalking
{
    public abstract class FishingStrategyBase : CoreFunctionality
    {
        protected bool shouldStopFishing = false;
        /// <summary>
        /// The random variance of casting the fishing rod, if enabled.
        /// </summary>
        protected int _VARIANCE = 20;
        protected Random _rand = new Random();

        /// <summary>
        /// Fish bubble detector for automatic aiming.
        /// </summary>
        protected FishBubbleDetector _bubbleDetector;

        /// <summary>
        /// Current fishing location name.
        /// </summary>
        protected string _locationName = "FISH ANYWHERE";

        /// <summary>
        /// Sets the fishing location for proper bubble detection configuration.
        /// Also resets fishing state for a fresh start.
        /// </summary>
        public void SetFishingLocation(string locationName)
        {
            // Reset state from any previous fishing session
            shouldStopFishing = false;

            _locationName = locationName;
            _bubbleDetector = new FishBubbleDetector(locationName);

            System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Reset state and set location to {locationName}");
        }

        /// <summary>
        /// An abstract method to be implemented by derived classes, detailing the process
        /// of leaving the fishing dock, selling the caught fish at the fisherman, and returning
        /// to the dock. This method defines the required actions to perform the sell operation
        /// in specific fishing locations.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete,
        /// allowing the operation to be cancelled.</param>
        /// <returns>A task that represents the asynchronous operation of leaving the dock,
        /// selling fish, and returning.</returns>
        public abstract Task LeaveDockAndSellAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Initiates the fishing actions for a specified number of casts, applying variance if enabled, and handles the operation asynchronously.
        /// </summary>
        /// <param name="numberOfCasts">The total number of casts to attempt.</param>
        /// <param name="fishVariance">Indicates whether to apply a variance to casting, simulating a more natural fishing experience.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete, allowing the operation to be cancelled.</param>
        /// <returns>A task that represents the asynchronous fishing operation, performing casts, checking for catches, and optionally exiting fishing upon completion.</returns>
        /// <remarks>
        /// This method controls the flow of the fishing operation, including casting the line, waiting for a catch, and handling the asynchronous delays between actions.
        /// It also respects the cancellation token to safely exit the operation if requested and ensures that the fishing process is attempted for the specified number of casts.
        /// After completing the fishing attempts or if instructed to stop, it will exit the fishing operation.
        /// </remarks>
        public async Task StartFishingActionsAsync(int numberOfCasts, bool fishVariance, CancellationToken cancellationToken)
        {
            await StartFishingActionsAsync(numberOfCasts, fishVariance, autoDetectFish: false, cancellationToken);
        }

        /// <summary>
        /// Initiates fishing with optional automatic fish detection.
        /// </summary>
        /// <param name="numberOfCasts">The total number of casts to attempt.</param>
        /// <param name="fishVariance">Indicates whether to apply random variance to casting.</param>
        /// <param name="autoDetectFish">If true, automatically detects fish shadows and aims accordingly.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task StartFishingActionsAsync(int numberOfCasts, bool fishVariance, bool autoDetectFish, CancellationToken cancellationToken)
        {
            // Check if game window is available
            if (!IsGameWindowReady())
            {
                throw new InvalidOperationException("Toontown Rewritten window not found. Please make sure the game is running.");
            }

            Stopwatch stopwatch = new Stopwatch();
            while (numberOfCasts != 0 && !shouldStopFishing)
            {
                if (autoDetectFish)
                {
                    await CastLineAuto(cancellationToken);
                }
                else
                {
                    await CastLine(fishVariance, cancellationToken);
                }

                stopwatch.Start();
                while (stopwatch.Elapsed.Seconds < 30 && !await CheckIfFishCaught(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested) return;
                }
                stopwatch.Stop();
                stopwatch.Reset();
                numberOfCasts--;
                await Task.Delay(1000, cancellationToken);
            }
            if (!shouldStopFishing)
            {
                await ExitFishing(cancellationToken);
                await Task.Delay(3000, cancellationToken);
            }
        }

        /// <summary>
        /// Casts the fishing line with random variance (original method).
        /// </summary>
        protected async Task CastLine(bool fishVariance, CancellationToken cancellationToken)
        {
            // Use image recognition to find the red fishing button (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(FishingCoordinatesEnum.RedFishingButton);

            int randX = fishVariance ? _rand.Next(-_VARIANCE, _VARIANCE + 1) : 0;
            int randY = fishVariance ? _rand.Next(-_VARIANCE, _VARIANCE + 1) : 0;
            MoveCursor(x + randX, y + randY);
            DoFishingClick();
            await Task.Delay(100, cancellationToken);
        }

        /// <summary>
        /// Casts the fishing line by automatically detecting fish shadows and aiming at them.
        /// Holds the cast button down while scanning to dismiss the fish popup.
        /// </summary>
        protected async Task CastLineAuto(CancellationToken cancellationToken)
        {
            // Ensure bubble detector is initialized
            if (_bubbleDetector == null)
            {
                _bubbleDetector = new FishBubbleDetector(_locationName);
            }

            // First, find the cast button location
            var (actualBtnX, actualBtnY) = await CoordinatesManager.GetCoordsWithImageRecAsync(FishingCoordinatesEnum.RedFishingButton);

            System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Cast button at ({actualBtnX}, {actualBtnY}), clicking down to dismiss popup...");

            // Move to cast button and click DOWN (this dismisses the fish popup)
            MoveCursor(actualBtnX, actualBtnY);
            await Task.Delay(100, cancellationToken);
            DoMouseClickDown(new System.Drawing.Point(actualBtnX, actualBtnY));

            // Wait for popup to close
            await Task.Delay(300, cancellationToken);

            System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Scanning for fish at {_locationName} while holding button...");

            // Now scan for fish shadows while button is held down (popup is gone, pond is visible)
            var castResult = await _bubbleDetector.DetectFishAndCalculateCastAsync(cancellationToken);

            if (castResult != null)
            {
                // Calculate the drag offset from the bubble detector's calculation
                int dragOffsetX = castResult.CastDestination.X - castResult.RodButtonPosition.X;
                int dragOffsetY = castResult.CastDestination.Y - castResult.RodButtonPosition.Y;

                // Calculate actual drag destination based on real button position
                int actualDragX = actualBtnX + dragOffsetX;
                int actualDragY = actualBtnY + dragOffsetY;

                System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Fish found! Dragging to ({actualDragX}, {actualDragY})");

                // Drag to the target position
                await Task.Delay(200, cancellationToken);
                MoveCursor(actualDragX, actualDragY);
                await Task.Delay(300, cancellationToken);

                // Release to cast
                DoMouseClickUp(getCursorLocation());

                System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Cast completed: Button({actualBtnX},{actualBtnY}) -> Drag({actualDragX},{actualDragY})");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[FishingStrategy] No fish detected, using default cast (dragging down)");

                // No fish found - do a default cast by dragging down
                await Task.Delay(200, cancellationToken);
                MoveCursor(actualBtnX, actualBtnY + 150);
                await Task.Delay(300, cancellationToken);
                DoMouseClickUp(getCursorLocation());
            }

            await Task.Delay(100, cancellationToken);
        }

        protected async Task<bool> CheckIfFishCaught(CancellationToken cancellationToken)
        {
            // Use cached coords from image rec (already found during CastLine)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(FishingCoordinatesEnum.RedFishingButton);
            string color = HexConverter(GetColorAt(x, y - 600));
            if (color.Equals("#FFFFBE") || color.Equals("#FFFFBF")) return true;

            color = HexConverter(GetColorAt(x, 110));
            return color.Equals("#FFFFBE") || color.Equals("#FFFFBF");
        }

        protected async Task ExitFishing(CancellationToken cancellationToken)
        {
            // Use image recognition to find exit button (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(FishingCoordinatesEnum.ExitFishingButton);
            MoveCursor(x, y);
            DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        protected async Task SellFishAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(2100, cancellationToken);
            // Use image recognition to find sell button (will prompt for template capture if needed)
            var (x, y) = await CoordinatesManager.GetCoordsWithImageRecAsync(FishingCoordinatesEnum.BlueSellAllButton);
            MoveCursor(x, y);
            DoMouseClick();
            await Task.Delay(2000, cancellationToken);
        }

        protected async Task ManuallyLocateRedFishingButton()
        {
            await CoordinatesManager.ManualUpdateCoordinates(FishingCoordinatesEnum.RedFishingButton);//update the red fishing button coords
        }
    }
}
