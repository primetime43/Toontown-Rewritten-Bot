using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Utilities;
using ToonTown_Rewritten_Bot.Views;
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
        /// Static reference to the fishing overlay for visualization.
        /// Set from MainForm when overlay is enabled.
        /// </summary>
        public static FishingOverlayForm Overlay { get; set; }

        /// <summary>
        /// Callback to notify MainForm when fishing ends, so it can uncheck the overlay checkbox.
        /// </summary>
        public static Action OnFishingEnded { get; set; }

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

            try
            {
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
                // Note: ExitFishing is now called by FishingService after optionally straightening
            }
            finally
            {
                // Clear the overlay when fishing ends (completed or canceled)
                ClearOverlay();

                // Notify MainForm to uncheck the overlay checkbox
                OnFishingEnded?.Invoke();
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
        /// Detects all fish, picks the closest one, waits for it to stop moving, then casts.
        /// </summary>
        protected async Task CastLineAuto(CancellationToken cancellationToken)
        {
            // Ensure bubble detector is initialized
            if (_bubbleDetector == null)
            {
                _bubbleDetector = new FishBubbleDetector(_locationName);
            }

            System.Diagnostics.Debug.WriteLine($"[FishingStrategy] === CastLineAuto === Location: {_locationName}");

            // Find the cast button
            var (btnX, btnY) = await CoordinatesManager.GetCoordsWithImageRecAsync(FishingCoordinatesEnum.RedFishingButton);

            // Hold down on cast button to dismiss any popup and enter aim mode
            MoveCursor(btnX, btnY);
            await Task.Delay(100, cancellationToken);
            DoMouseClickDown(new Point(btnX, btnY));
            await Task.Delay(400, cancellationToken); // Wait for popup to close and aim mode to activate

            Point? castTarget = null;

            try
            {
                // Track fish position across multiple scans to detect when it stops moving
                const int maxScans = 25;  // More scans = more time to wait for fish to stop (5 seconds)
                const int scanDelayMs = 200;
                const int positionTolerance = 20; // Pixels - fish is "stopped" if within this range
                const int requiredStableScans = 3; // Need 3 consecutive stable scans to confirm stopped

                Point? lastClosestFish = null;
                int stableCount = 0;
                bool fishEverDetected = false;

                for (int scan = 0; scan < maxScans; scan++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                    {
                        if (screenshot == null)
                        {
                            System.Diagnostics.Debug.WriteLine("[FishingStrategy] Failed to capture screenshot");
                            await Task.Delay(scanDelayMs, cancellationToken);
                            continue;
                        }

                        // Run detection - get all candidates
                        var detectionResult = _bubbleDetector.DetectFromScreenshot(screenshot);

                        if (detectionResult.AllCandidates.Count == 0 && !detectionResult.BestShadowPosition.HasValue)
                        {
                            System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Scan {scan + 1}: No fish detected");
                            UpdateOverlay(detectionResult, null, "Scanning... no fish found");
                            // Don't reset lastClosestFish - fish might have temporarily disappeared
                            stableCount = 0;
                            await Task.Delay(scanDelayMs, cancellationToken);
                            continue;
                        }

                        // Mark that we've seen fish at some point
                        fishEverDetected = true;

                        // Find the closest fish to the cast button (center of screen)
                        Point closestFish;
                        if (detectionResult.AllCandidates.Count > 0)
                        {
                            // Sort by distance from center and pick closest
                            var closest = detectionResult.AllCandidates
                                .OrderBy(c => c.DistanceFromCenter)
                                .First();
                            closestFish = closest.Position;
                            System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Scan {scan + 1}: Found {detectionResult.AllCandidates.Count} fish, closest at ({closestFish.X}, {closestFish.Y})");
                        }
                        else
                        {
                            closestFish = detectionResult.BestShadowPosition.Value;
                            System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Scan {scan + 1}: Using best shadow at ({closestFish.X}, {closestFish.Y})");
                        }

                        // Check if fish is at same position as last scan (stopped moving)
                        string statusText;
                        if (lastClosestFish.HasValue)
                        {
                            int dx = Math.Abs(closestFish.X - lastClosestFish.Value.X);
                            int dy = Math.Abs(closestFish.Y - lastClosestFish.Value.Y);

                            if (dx <= positionTolerance && dy <= positionTolerance)
                            {
                                stableCount++;
                                statusText = $"Fish stable ({stableCount}/{requiredStableScans})";
                                System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Fish stable (moved {dx},{dy}px) - stable count: {stableCount}/{requiredStableScans}");

                                if (stableCount >= requiredStableScans)
                                {
                                    // Fish has stopped moving - use this position
                                    castTarget = closestFish;
                                    UpdateOverlay(detectionResult, closestFish, "CASTING!");
                                    System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Fish STOPPED at ({closestFish.X}, {closestFish.Y}) - casting!");
                                    break;
                                }
                            }
                            else
                            {
                                // Fish moved - reset stable count
                                stableCount = 0;
                                statusText = "Tracking fish movement...";
                                System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Fish moving (moved {dx},{dy}px) - waiting...");
                            }
                        }
                        else
                        {
                            statusText = "Fish detected - tracking...";
                        }

                        // Update overlay with current detection
                        UpdateOverlay(detectionResult, closestFish, statusText);
                        lastClosestFish = closestFish;
                    }

                    await Task.Delay(scanDelayMs, cancellationToken);
                }

                // Only use last known position if fish stabilized at some point
                // If fish was detected but NEVER stabilized (kept moving), don't cast to moving target
                if (!castTarget.HasValue && lastClosestFish.HasValue && stableCount > 0)
                {
                    // Fish was detected and showed some stability - use last known position
                    castTarget = lastClosestFish;
                    System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Using last known fish position (had some stability): ({castTarget.Value.X}, {castTarget.Value.Y})");
                }
                else if (!castTarget.HasValue && fishEverDetected)
                {
                    // Fish was detected but never stopped moving - do NOT cast to moving target
                    System.Diagnostics.Debug.WriteLine("[FishingStrategy] Fish detected but never stabilized - will do default cast instead");
                }

                // Calculate where to drag and release
                if (castTarget.HasValue)
                {
                    var castResult = _bubbleDetector.CalculateCastFromPosition(castTarget.Value.X, castTarget.Value.Y);

                    if (castResult != null)
                    {
                        // Calculate drag offset from current button position
                        int dragOffsetX = castResult.CastDestination.X - castResult.RodButtonPosition.X;
                        int dragOffsetY = castResult.CastDestination.Y - castResult.RodButtonPosition.Y;
                        int dragX = btnX + dragOffsetX;
                        int dragY = btnY + dragOffsetY;

                        System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Casting: drag to ({dragX}, {dragY})");

                        // Drag to target position and release
                        MoveCursor(dragX, dragY);
                        await Task.Delay(200, cancellationToken);
                        DoMouseClickUp(new Point(dragX, dragY));
                    }
                    else
                    {
                        // Cast calculation failed - do default cast
                        System.Diagnostics.Debug.WriteLine("[FishingStrategy] Cast calculation failed - default cast");
                        int defaultY = btnY + 150;
                        MoveCursor(btnX, defaultY);
                        await Task.Delay(200, cancellationToken);
                        DoMouseClickUp(new Point(btnX, defaultY));
                    }
                }
                else
                {
                    // No fish detected - do default cast (straight down)
                    System.Diagnostics.Debug.WriteLine("[FishingStrategy] No fish found after scanning - default cast");
                    int defaultY = btnY + 150;
                    MoveCursor(btnX, defaultY);
                    await Task.Delay(200, cancellationToken);
                    DoMouseClickUp(new Point(btnX, defaultY));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Error during cast: {ex.Message}");
                // Make sure we release the mouse button
                DoMouseClickUp(getCursorLocation());
            }
            finally
            {
                // Clear overlay after cast
                ClearOverlay();
            }

            await Task.Delay(100, cancellationToken);
        }

        protected async Task<bool> CheckIfFishCaught(CancellationToken cancellationToken)
        {
            // Get game window info for proper coordinate scaling
            var windowRect = CoreFunctionality.GetGameWindowRect();
            if (windowRect.IsEmpty) return false;

            // The fish caught popup appears in the upper-right area of the screen
            // It has a cream/beige background color and green border

            // Check multiple positions where the popup typically appears
            // Popup is roughly in the right third of the screen, upper half
            int popupCenterX = windowRect.X + (int)(windowRect.Width * 0.7);  // 70% from left
            int popupTopY = windowRect.Y + (int)(windowRect.Height * 0.15);   // 15% from top
            int popupMidY = windowRect.Y + (int)(windowRect.Height * 0.25);   // 25% from top

            // Check several positions for the cream popup background
            var positionsToCheck = new[]
            {
                new Point(popupCenterX, popupTopY),
                new Point(popupCenterX, popupMidY),
                new Point(popupCenterX - 50, popupTopY),
                new Point(popupCenterX + 50, popupTopY),
                new Point(popupCenterX, popupTopY + 50),
            };

            foreach (var pos in positionsToCheck)
            {
                var color = GetColorAt(pos.X, pos.Y);

                // Check for cream/beige background (the popup card)
                // Cream colors have high R and G, lower B
                if (IsCreamColor(color))
                {
                    System.Diagnostics.Debug.WriteLine($"[FishCatch] Detected cream popup at ({pos.X}, {pos.Y}) - RGB({color.R},{color.G},{color.B})");
                    return true;
                }

                // Also check for the green border of the popup
                if (IsPopupGreenBorder(color))
                {
                    System.Diagnostics.Debug.WriteLine($"[FishCatch] Detected green border at ({pos.X}, {pos.Y}) - RGB({color.R},{color.G},{color.B})");
                    return true;
                }
            }

            // Fallback: Check the original positions relative to cast button
            var (btnX, btnY) = await CoordinatesManager.GetCoordsWithImageRecAsync(FishingCoordinatesEnum.RedFishingButton);
            var fallbackColor = GetColorAt(btnX, Math.Max(windowRect.Y + 50, btnY - 600));
            if (IsCreamColor(fallbackColor))
            {
                System.Diagnostics.Debug.WriteLine($"[FishCatch] Detected via fallback position");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a color is the cream/beige background of the fish popup.
        /// </summary>
        private bool IsCreamColor(Color color)
        {
            // Cream/beige colors: high R (240-255), high G (240-255), lower B (170-210)
            // The popup background is approximately #FFFFBE which is RGB(255, 255, 190)
            return color.R >= 240 && color.G >= 240 && color.B >= 170 && color.B <= 220;
        }

        /// <summary>
        /// Checks if a color is the green border of the fish popup card.
        /// </summary>
        private bool IsPopupGreenBorder(Color color)
        {
            // The popup has a teal/green border, approximately RGB(91, 192, 137) or similar
            // Green border: G is highest, R and B are lower
            return color.G >= 150 && color.G > color.R && color.G > color.B &&
                   color.R >= 50 && color.R <= 150 &&
                   color.B >= 100 && color.B <= 180;
        }

        /// <summary>
        /// Straightens the toon by pulling the fishing rod straight ahead and canceling.
        /// This ensures the toon faces forward before walking to sell fish.
        /// Must be called while still in fishing mode (before ExitFishing).
        /// </summary>
        public async Task StraightenToonAsync(CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.WriteLine("[FishingStrategy] Straightening toon before leaving dock...");

            // Find the red fishing button
            var (btnX, btnY) = await CoordinatesManager.GetCoordsWithImageRecAsync(FishingCoordinatesEnum.RedFishingButton);

            // Click and hold the button
            MoveCursor(btnX, btnY);
            await Task.Delay(100, cancellationToken);
            DoMouseClickDown(new Point(btnX, btnY));
            await Task.Delay(200, cancellationToken);

            // Drag straight down (this makes the toon face forward/center)
            int straightY = btnY + 150; // Drag down 150 pixels
            MoveCursor(btnX, straightY);
            await Task.Delay(300, cancellationToken);

            // Press ESC to cancel the cast WHILE still holding the mouse button
            SendKeys.SendWait("{ESC}");
            await Task.Delay(200, cancellationToken);

            // Now release the mouse (cast is already cancelled)
            DoMouseClickUp(new Point(btnX, straightY));
            await Task.Delay(300, cancellationToken);

            System.Diagnostics.Debug.WriteLine("[FishingStrategy] Toon straightened.");
        }

        public async Task ExitFishing(CancellationToken cancellationToken)
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

        /// <summary>
        /// Updates the fishing overlay with current detection results.
        /// Thread-safe - can be called from any thread.
        /// </summary>
        private void UpdateOverlay(FishDetectionDebugResult result, Point? targetFish, string status)
        {
            if (Overlay == null || Overlay.IsDisposed)
                return;

            try
            {
                if (Overlay.InvokeRequired)
                {
                    Overlay.BeginInvoke(new Action(() =>
                    {
                        if (Overlay != null && !Overlay.IsDisposed)
                            Overlay.UpdateDetection(result, targetFish, status);
                    }));
                }
                else
                {
                    Overlay.UpdateDetection(result, targetFish, status);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Error updating overlay: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the fishing overlay.
        /// </summary>
        private void ClearOverlay()
        {
            if (Overlay == null || Overlay.IsDisposed)
                return;

            try
            {
                if (Overlay.InvokeRequired)
                {
                    Overlay.BeginInvoke(new Action(() =>
                    {
                        if (Overlay != null && !Overlay.IsDisposed)
                            Overlay.ClearOverlay();
                    }));
                }
                else
                {
                    Overlay.ClearOverlay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Error clearing overlay: {ex.Message}");
            }
        }
    }
}
