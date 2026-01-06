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
        /// Static flag to pause/resume fishing from anywhere (e.g., global keyboard hook).
        /// </summary>
        public static bool IsPaused { get; private set; } = false;

        /// <summary>
        /// Event raised when pause state changes.
        /// </summary>
        public static event Action<bool> PauseStateChanged;

        /// <summary>
        /// Toggles the pause state for fishing.
        /// </summary>
        public static void TogglePause()
        {
            IsPaused = !IsPaused;
            Debug.WriteLine($"[FishingStrategy] Pause toggled: {(IsPaused ? "PAUSED" : "RESUMED")}");
            PauseStateChanged?.Invoke(IsPaused);
        }

        /// <summary>
        /// Resets pause state (call when starting new fishing session).
        /// </summary>
        public static void ResetPause()
        {
            IsPaused = false;
        }

        /// <summary>
        /// Tracks fishing statistics for overlay display.
        /// </summary>
        protected int _fishCaught = 0;
        protected int _castCount = 0;

        /// <summary>
        /// Sets the fishing location for proper bubble detection configuration.
        /// Also resets fishing state for a fresh start.
        /// </summary>
        public void SetFishingLocation(string locationName)
        {
            // Reset state from any previous fishing session
            shouldStopFishing = false;
            _fishCaught = 0;
            _castCount = 0;
            ResetPause(); // Ensure not paused when starting new session

            _locationName = locationName;
            _bubbleDetector = new FishBubbleDetector(locationName);

            // Update overlay with location
            UpdateOverlayLocation(locationName);

            System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Reset state and set location to {locationName}");
        }

        /// <summary>
        /// Updates the overlay with the current action status.
        /// </summary>
        protected void UpdateOverlayAction(string currentAction, string nextAction, string status)
        {
            if (Overlay != null && !Overlay.IsDisposed)
            {
                try
                {
                    Overlay.BeginInvoke(new Action(() =>
                    {
                        Overlay.UpdateActionStatus(currentAction, nextAction, status);
                    }));
                }
                catch { }
            }
        }

        /// <summary>
        /// Updates the overlay with fishing statistics.
        /// </summary>
        protected void UpdateOverlayStats()
        {
            if (Overlay != null && !Overlay.IsDisposed)
            {
                try
                {
                    Overlay.BeginInvoke(new Action(() =>
                    {
                        Overlay.UpdateStats(_fishCaught, _castCount);
                    }));
                }
                catch { }
            }
        }

        /// <summary>
        /// Updates the overlay with the fishing location.
        /// </summary>
        protected void UpdateOverlayLocation(string location)
        {
            if (Overlay != null && !Overlay.IsDisposed)
            {
                try
                {
                    Overlay.BeginInvoke(new Action(() =>
                    {
                        Overlay.SetLocation(location);
                    }));
                }
                catch { }
            }
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

            int totalCasts = numberOfCasts;

            try
            {
                Stopwatch stopwatch = new Stopwatch();
                while (numberOfCasts != 0 && !shouldStopFishing)
                {
                    // Check for pause
                    while (IsPaused && !cancellationToken.IsCancellationRequested)
                    {
                        UpdateOverlayAction("PAUSED", "Press F11 to resume", "Paused");
                        await Task.Delay(250, cancellationToken);
                    }
                    if (cancellationToken.IsCancellationRequested) return;

                    _castCount++;
                    UpdateOverlayStats();

                    // Update overlay - casting
                    UpdateOverlayAction(autoDetectFish ? "Scanning for fish..." : "Casting line", "Wait for bite", "Casting");

                    if (autoDetectFish)
                    {
                        await CastLineAuto(cancellationToken);
                    }
                    else
                    {
                        await CastLine(fishVariance, cancellationToken);
                    }

                    // Check if "no jellybeans" popup appeared (out of bait money)
                    await Task.Delay(300, cancellationToken); // Brief delay for popup to appear
                    if (NoJellybeansDetector.IsNoJellybeansPopupVisible())
                    {
                        UpdateOverlayAction("Out of jellybeans!", "-", "Stopped");
                        System.Diagnostics.Debug.WriteLine("[FishingStrategy] NO JELLYBEANS - Out of bait! Stopping fishing.");
                        await HandleNoJellybeansPopup(cancellationToken);
                        shouldStopFishing = true;
                        return;
                    }

                    // Update overlay - waiting for bite
                    UpdateOverlayAction("Waiting for bite...", $"Cast {totalCasts - numberOfCasts + 1}/{totalCasts}", "Fishing");

                    stopwatch.Start();
                    while (stopwatch.Elapsed.Seconds < 30 && !await CheckIfFishCaught(cancellationToken))
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                    }
                    stopwatch.Stop();
                    stopwatch.Reset();

                    // Fish caught (or timeout)
                    _fishCaught++;
                    UpdateOverlayStats();
                    UpdateOverlayAction("Fish caught!", numberOfCasts > 1 ? "Cast again" : "Finish up", "Fishing");

                    numberOfCasts--;
                    await Task.Delay(1000, cancellationToken);
                }

                UpdateOverlayAction("Fishing complete", "-", "Complete");
                // Note: ExitFishing is now called by FishingService after optionally straightening
            }
            finally
            {
                // Clear the overlay display (but keep it open during sell trips)
                ClearOverlay();
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
        /// Moves the mouse to track the fish in real-time while holding the cast button,
        /// then releases when fish position is stable (like MouseClickSimulator approach).
        /// </summary>
        protected async Task CastLineAuto(CancellationToken cancellationToken)
        {
            // Ensure bubble detector is initialized
            if (_bubbleDetector == null)
            {
                _bubbleDetector = new FishBubbleDetector(_locationName);
            }

            System.Diagnostics.Debug.WriteLine($"[FishingStrategy] === CastLineAuto === Location: {_locationName}");

            // Get window info for coordinate calculations
            var windowRect = GetGameWindowRect();
            if (windowRect.IsEmpty)
            {
                System.Diagnostics.Debug.WriteLine("[FishingStrategy] Window not found!");
                return;
            }

            // Calculate default straight-ahead cast position (like MouseClickSimulator's 800, 1009)
            float scaleX = (float)windowRect.Width / 1600f;
            float scaleY = (float)windowRect.Height / 1151f;
            int defaultCastX = (int)(800 * scaleX) + windowRect.X;
            int defaultCastY = (int)(1009 * scaleY) + windowRect.Y;

            // Find the cast button and calculate rod button screen position
            var (btnX, btnY) = await CoordinatesManager.GetCoordsWithImageRecAsync(FishingCoordinatesEnum.RedFishingButton);
            int rodButtonX = (int)(800 * scaleX) + windowRect.X;
            int rodButtonY = (int)(846 * scaleY) + windowRect.Y;

            // Move to rod button and press down
            SimulateDragMove(rodButtonX, rodButtonY);
            await Task.Delay(100, cancellationToken);
            SendInputMouseDown();
            await Task.Delay(400, cancellationToken); // Wait for popup to close and aim mode to activate

            try
            {
                // Settings matching MouseClickSimulator
                const int maxScanTimeMs = 36000;  // 36 seconds max like MouseClickSimulator
                const int scanDelayMs = 500;      // 500ms between scans like MouseClickSimulator
                const int scanStep = 15;          // Position tolerance

                Point? oldFishPosition = null;
                int coordsMatchCounter = 0;
                var startTime = DateTime.Now;

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Point? newFishPosition = null;
                    Point castDestination;

                    using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                    {
                        if (screenshot != null)
                        {
                            var detectionResult = _bubbleDetector.DetectFromScreenshot(screenshot);

                            // Find fish position
                            if (detectionResult.AllCandidates.Count > 0)
                            {
                                var easiest = detectionResult.AllCandidates
                                    .OrderBy(c => c.CastPower)
                                    .First();
                                newFishPosition = easiest.Position;
                            }
                            else if (detectionResult.BestShadowPosition.HasValue)
                            {
                                newFishPosition = detectionResult.BestShadowPosition.Value;
                            }

                            // Update overlay
                            if (newFishPosition.HasValue)
                            {
                                UpdateOverlay(detectionResult, newFishPosition, $"Found fish at ({newFishPosition.Value.X},{newFishPosition.Value.Y})");
                            }
                            else
                            {
                                UpdateOverlay(detectionResult, null, "Scanning for fish...");
                            }
                        }
                    }

                    // Check if fish position is stable (same as last scan within tolerance)
                    if (newFishPosition.HasValue && oldFishPosition.HasValue &&
                        Math.Abs(oldFishPosition.Value.X - newFishPosition.Value.X) <= scanStep &&
                        Math.Abs(oldFishPosition.Value.Y - newFishPosition.Value.Y) <= scanStep)
                    {
                        coordsMatchCounter++;
                        System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Fish stable, match count: {coordsMatchCounter}");
                    }
                    else
                    {
                        oldFishPosition = newFishPosition;
                        coordsMatchCounter = 0;
                    }

                    // Calculate cast destination - ALWAYS move mouse every iteration
                    if (newFishPosition.HasValue)
                    {
                        // Calculate cast position for detected fish
                        var castResult = _bubbleDetector.CalculateCastFromPosition(newFishPosition.Value.X, newFishPosition.Value.Y);
                        if (castResult != null)
                        {
                            castDestination = castResult.CastDestination;
                        }
                        else
                        {
                            castDestination = new Point(defaultCastX, defaultCastY);
                        }
                    }
                    else
                    {
                        // No fish found - use default straight-ahead position
                        castDestination = new Point(defaultCastX, defaultCastY);
                    }

                    // ALWAYS move mouse to current cast destination (this is the key difference!)
                    SimulateDragMove(castDestination.X, castDestination.Y);
                    System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Moving mouse to ({castDestination.X},{castDestination.Y})");

                    // Release if fish position is stable (2 consecutive matches)
                    if (coordsMatchCounter >= 2)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Fish stable - releasing at ({castDestination.X},{castDestination.Y})!");
                        UpdateOverlay(null, newFishPosition, "CASTING!");
                        SendInputMouseUp();
                        break;
                    }

                    // Wait before next scan
                    await Task.Delay(scanDelayMs, cancellationToken);

                    // Timeout check
                    if ((DateTime.Now - startTime).TotalMilliseconds >= maxScanTimeMs)
                    {
                        System.Diagnostics.Debug.WriteLine("[FishingStrategy] Timeout - releasing!");
                        SendInputMouseUp();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Error during cast: {ex.Message}");
            }
            finally
            {
                // ALWAYS release mouse button
                SendInputMouseUp();
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
        /// Handles the "no jellybeans" popup by clicking the Exit button.
        /// </summary>
        protected async Task HandleNoJellybeansPopup(CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.WriteLine("[FishingStrategy] Handling 'no jellybeans' popup - clicking Exit...");

            // Get the Exit button position
            var exitPos = NoJellybeansDetector.GetExitButtonPosition();
            if (exitPos.HasValue)
            {
                MoveCursor(exitPos.Value.X, exitPos.Value.Y);
                await Task.Delay(100, cancellationToken);
                DoMouseClick();
                await Task.Delay(500, cancellationToken);
                System.Diagnostics.Debug.WriteLine("[FishingStrategy] Exit button clicked. Fishing stopped due to no jellybeans.");
            }
            else
            {
                // Fallback: press ESC to close the popup
                System.Diagnostics.Debug.WriteLine("[FishingStrategy] Could not find Exit button, pressing ESC...");
                SendKeys.SendWait("{ESC}");
                await Task.Delay(500, cancellationToken);
            }
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
