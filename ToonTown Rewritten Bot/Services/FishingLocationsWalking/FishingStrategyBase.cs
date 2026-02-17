using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Utilities;
using ToonTown_Rewritten_Bot.Views;
using static ToonTown_Rewritten_Bot.Models.Coordinates;

namespace ToonTown_Rewritten_Bot.Services.FishingLocationsWalking
{
    public abstract class FishingStrategyBase : CoreFunctionality
    {
        protected bool shouldStopFishing = false;

        /// <summary>
        /// Set when the bucket-full popup was detected and dismissed.
        /// The toon is already off the dock, so StraightenToon and ExitFishing should be skipped.
        /// Reset at the start of each fishing session via SetFishingLocation.
        /// </summary>
        public bool BucketWasFull { get; private set; } = false;

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
        private static volatile FishingOverlayForm _overlay;
        public static FishingOverlayForm Overlay
        {
            get => _overlay;
            set => _overlay = value;
        }

        /// <summary>
        /// Callback to notify MainForm when fishing ends, so it can uncheck the overlay checkbox.
        /// </summary>
        private static volatile Action _onFishingEnded;
        public static Action OnFishingEnded
        {
            get => _onFishingEnded;
            set => _onFishingEnded = value;
        }

        /// <summary>
        /// Static flag to pause/resume fishing from anywhere (e.g., global keyboard hook).
        /// </summary>
        private static volatile bool _isPaused = false;
        public static bool IsPaused
        {
            get => _isPaused;
            private set => _isPaused = value;
        }

        /// <summary>
        /// Static flag indicating a simulated key press (like ESC) is in progress.
        /// Used to prevent global keyboard hooks from interpreting bot-generated keypresses as user input.
        /// </summary>
        private static volatile bool _isSimulatedKeyPress = false;
        public static bool IsSimulatedKeyPress
        {
            get => _isSimulatedKeyPress;
            set => _isSimulatedKeyPress = value;
        }

        /// <summary>
        /// Maximum time in seconds to wait for a fish bite before timing out.
        /// Default is 30 seconds. Can be adjusted via UI.
        /// </summary>
        private static volatile int _biteTimeoutSeconds = 30;
        public static int BiteTimeoutSeconds
        {
            get => _biteTimeoutSeconds;
            set => _biteTimeoutSeconds = value;
        }

        /// <summary>
        /// If true, waits for a fish shadow to be detected before casting.
        /// When no fish is detected, it will wait and rescan instead of casting anyway.
        /// </summary>
        private static volatile bool _waitForFishBeforeCasting = false;
        public static bool WaitForFishBeforeCasting
        {
            get => _waitForFishBeforeCasting;
            set => _waitForFishBeforeCasting = value;
        }

        /// <summary>
        /// Maximum number of scan attempts when waiting for fish before giving up.
        /// Only used when WaitForFishBeforeCasting is true. Default is 10 attempts.
        /// </summary>
        private static volatile int _maxFishWaitAttempts = 10;
        public static int MaxFishWaitAttempts
        {
            get => _maxFishWaitAttempts;
            set => _maxFishWaitAttempts = value;
        }

        /// <summary>
        /// Delay in milliseconds between fish detection scans when waiting for fish.
        /// Default is 2000ms (2 seconds).
        /// </summary>
        private static volatile int _fishWaitScanDelayMs = 2000;
        public static int FishWaitScanDelayMs
        {
            get => _fishWaitScanDelayMs;
            set => _fishWaitScanDelayMs = value;
        }

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
        /// Cycle counts reset each fishing/sell cycle; session counts accumulate across the entire session.
        /// </summary>
        protected int _fishCaught = 0;
        protected int _castCount = 0;
        protected int _sessionFishCaught = 0;
        protected int _sessionCastCount = 0;

        /// <summary>
        /// Session totals for the completed fishing run (accumulated across all sell cycles).
        /// </summary>
        public int SessionFishCaught => _sessionFishCaught;
        public int SessionCastCount => _sessionCastCount;

        /// <summary>
        /// Cached red fishing button position to avoid expensive template matching during catch detection.
        /// Set during CastLine/CastLineAuto; used by CheckIfFishCaught fallback.
        /// </summary>
        private Point? _cachedRedButtonPos;

        /// <summary>
        /// Sets the fishing location for proper bubble detection configuration.
        /// Also resets fishing state for a fresh start.
        /// </summary>
        public void SetFishingLocation(string locationName)
        {
            // Reset state from any previous fishing session
            shouldStopFishing = false;
            BucketWasFull = false;
            _fishCaught = 0;
            _castCount = 0;
            _sessionFishCaught = 0;
            _sessionCastCount = 0;
            ResetPause(); // Ensure not paused when starting new session

            _locationName = locationName;
            _bubbleDetector = new FishBubbleDetector(locationName);

            // Update overlay with location
            UpdateOverlayLocation(locationName);

            System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Reset state and set location to {locationName}");
        }

        /// <summary>
        /// Safely invokes an action on the overlay form, handling thread marshalling and disposal checks.
        /// </summary>
        private void SafeOverlayInvoke(Action<FishingOverlayForm> action)
        {
            var overlay = Overlay;
            if (overlay == null || overlay.IsDisposed)
                return;

            try
            {
                if (overlay.InvokeRequired)
                {
                    overlay.Invoke(new Action(() =>
                    {
                        if (overlay != null && !overlay.IsDisposed)
                            action(overlay);
                    }));
                }
                else
                {
                    action(overlay);
                }
            }
            catch (ObjectDisposedException)
            {
                // Overlay was disposed between check and invoke - ignore
            }
            catch (InvalidOperationException)
            {
                // Handle not created yet or already destroyed - ignore
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Error invoking on overlay: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the overlay with the current action status.
        /// </summary>
        protected void UpdateOverlayAction(string currentAction, string nextAction, string status)
            => SafeOverlayInvoke(o => o.UpdateActionStatus(currentAction, nextAction, status));

        /// <summary>
        /// Updates the overlay with fishing statistics.
        /// </summary>
        protected void UpdateOverlayStats()
            => SafeOverlayInvoke(o => o.UpdateStats(_fishCaught, _castCount, _sessionFishCaught, _sessionCastCount));

        /// <summary>
        /// Updates the overlay with the fishing location.
        /// </summary>
        protected void UpdateOverlayLocation(string location)
            => SafeOverlayInvoke(o => o.SetLocation(location));

        /// <summary>
        /// Shows the initial scan area on the overlay at the start of fishing.
        /// This ensures the scan area rectangle is always visible, even when auto-detect is off.
        /// </summary>
        private void ShowInitialScanAreaOnOverlay()
        {
            if (Overlay == null || Overlay.IsDisposed) return;
            if (_bubbleDetector == null) return;

            try
            {
                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                {
                    if (screenshot != null)
                    {
                        var result = _bubbleDetector.DetectFromScreenshot(screenshot);
                        Debug.WriteLine($"[FishingStrategy] Initial scan area for '{_locationName}': {result.ScanArea} (IsEmpty={result.ScanArea.IsEmpty})");
                        UpdateOverlay(result, null, "");
                    }
                    else
                    {
                        Debug.WriteLine($"[FishingStrategy] Initial scan area: screenshot was null for '{_locationName}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FishingStrategy] Error showing initial scan area for '{_locationName}': {ex.Message}");
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
        /// Waits for the dock UI to be ready after returning from a sell trip.
        /// Uses a simple timed delay since the sell strategies already handle walking back to the dock.
        /// </summary>
        private async Task WaitForDockReadyAsync(CancellationToken cancellationToken)
        {
            const int dockSettleDelayMs = 3000;

            Debug.WriteLine("[FishingStrategy] Waiting for dock UI to settle after sell trip...");
            UpdateOverlayAction("Returning to dock...", "Waiting for fishing UI", "Waiting");

            await Task.Delay(dockSettleDelayMs, cancellationToken);

            Debug.WriteLine("[FishingStrategy] Dock settle delay complete, proceeding to fish.");
        }

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
            await StartFishingActionsAsync(numberOfCasts, fishVariance, autoDetectFish: false, isFirstCycle: true, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Initiates fishing with optional automatic fish detection.
        /// </summary>
        /// <param name="numberOfCasts">The total number of casts to attempt.</param>
        /// <param name="fishVariance">Indicates whether to apply random variance to casting.</param>
        /// <param name="autoDetectFish">If true, automatically detects fish shadows and aims accordingly.</param>
        /// <param name="isFirstCycle">If true, skips the dock-ready wait since the user already confirmed they're at the dock.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task StartFishingActionsAsync(int numberOfCasts, bool fishVariance, bool autoDetectFish, bool isFirstCycle, CancellationToken cancellationToken)
        {
            // Reset cycle counts for this fishing round (session totals keep accumulating)
            _fishCaught = 0;
            _castCount = 0;

            // Check if game window is available
            EnsureGameWindowReady();

            // On subsequent cycles (after a sell trip), wait for the dock UI to settle.
            // Skip on the first cycle since the user already confirmed they're at the dock.
            if (!isFirstCycle)
            {
                await WaitForDockReadyAsync(cancellationToken);
            }

            int totalCasts = numberOfCasts;

            // Show initial scan area on overlay so it's always visible (even without auto-detect)
            ShowInitialScanAreaOnOverlay();

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
                    _sessionCastCount++;
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
                    bool fishCaught = false;
                    while (stopwatch.Elapsed.TotalSeconds < BiteTimeoutSeconds)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        if (await CheckIfFishCaught(cancellationToken))
                        {
                            fishCaught = true;
                            break;
                        }
                        await Task.Delay(100, cancellationToken);
                    }
                    stopwatch.Stop();
                    stopwatch.Reset();

                    // Only count if fish was actually caught (not timeout)
                    if (fishCaught)
                    {
                        _fishCaught++;
                        _sessionFishCaught++;
                        UpdateOverlayStats();
                        UpdateOverlayAction("Fish caught!", numberOfCasts > 1 ? "Cast again" : "Finish up", "Fishing");

                        // Close the fish caught popup so we can see the pond for the next cast
                        await Task.Delay(200, cancellationToken);
                        await CloseFishCaughtPopup(cancellationToken);
                    }
                    else
                    {
                        // Timed out with no bite — check if the red fishing button is still visible.
                        // If it's gone, a popup (like bucket full) may have appeared over it.
                        if (_cachedRedButtonPos.HasValue)
                        {
                            string redButtonName = CoordinateActions.GetDescription(
                                Convert.ToInt32(FishingCoordinatesEnum.RedFishingButton).ToString())
                                ?? $"Element_{Convert.ToInt32(FishingCoordinatesEnum.RedFishingButton)}";

                            bool redButtonStillVisible = await UIElementManager.Instance
                                .VerifyElementAtLocationAsync(redButtonName, _cachedRedButtonPos.Value);

                            if (!redButtonStillVisible)
                            {
                                System.Diagnostics.Debug.WriteLine("[FishingStrategy] Red fishing button gone after timeout - checking for bucket full popup...");

                                if (await FishBucketFullDetector.CheckForBucketFullPopupAsync(cancellationToken))
                                {
                                    UpdateOverlayAction("Bucket full!", "Going to sell fish", "Selling");
                                    System.Diagnostics.Debug.WriteLine("[FishingStrategy] BUCKET FULL - Going to sell fish.");
                                    await HandleBucketFullPopup(cancellationToken);
                                    BucketWasFull = true;
                                    return;
                                }
                            }
                        }

                        UpdateOverlayAction("No bite (timeout)", numberOfCasts > 1 ? "Cast again" : "Finish up", "Fishing");
                    }

                    numberOfCasts--;
                    await Task.Delay(500, cancellationToken);
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
            _cachedRedButtonPos = new Point(x, y);

            System.Diagnostics.Debug.WriteLine($"[FishingStrategy] CastLine: Red button at screen ({x}, {y})");

            int randX = fishVariance ? _rand.Next(-_VARIANCE, _VARIANCE + 1) : 0;
            int randY = fishVariance ? _rand.Next(-_VARIANCE, _VARIANCE + 1) : 0;
            MoveCursor(x + randX, y + randY);
            DoFishingClick();
            await Task.Delay(100, cancellationToken);
        }

        /// <summary>
        /// Waits for a fish shadow to be detected before casting.
        /// Returns true if a fish was found, false if gave up after max attempts.
        /// </summary>
        protected async Task<bool> WaitForFishDetectionAsync(CancellationToken cancellationToken)
        {
            if (!WaitForFishBeforeCasting)
                return true; // Feature disabled, proceed with casting

            // Ensure bubble detector is initialized
            if (_bubbleDetector == null)
            {
                _bubbleDetector = new FishBubbleDetector(_locationName);
            }

            Debug.WriteLine($"[FishingStrategy] Waiting for fish detection (max {MaxFishWaitAttempts} attempts)...");
            UpdateOverlayAction("Scanning for fish...", "Waiting", "Detecting");

            for (int attempt = 1; attempt <= MaxFishWaitAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check for pause
                while (IsPaused)
                {
                    UpdateOverlayAction("PAUSED", "Press F11 to resume", "Paused");
                    await Task.Delay(500, cancellationToken);
                }

                UpdateOverlayAction($"Scanning for fish... ({attempt}/{MaxFishWaitAttempts})", "Waiting", "Detecting");

                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                {
                    if (screenshot != null)
                    {
                        var detectionResult = _bubbleDetector.DetectFromScreenshot(screenshot);

                        bool fishFound = detectionResult.AllCandidates.Count > 0 ||
                                        detectionResult.BestShadowPosition.HasValue;

                        // Update overlay with detection visuals (scan area, blobs, candidates)
                        UpdateOverlay(detectionResult, detectionResult.BestShadowPosition,
                            fishFound ? "Fish detected!" : $"Scanning... ({attempt}/{MaxFishWaitAttempts})");

                        if (fishFound)
                        {
                            Debug.WriteLine($"[FishingStrategy] Fish detected on attempt {attempt}!");
                            UpdateOverlayAction("Fish found!", "Casting", "Detected");
                            return true;
                        }
                    }
                }

                Debug.WriteLine($"[FishingStrategy] No fish detected, attempt {attempt}/{MaxFishWaitAttempts}");

                if (attempt < MaxFishWaitAttempts)
                {
                    await Task.Delay(FishWaitScanDelayMs, cancellationToken);
                }
            }

            Debug.WriteLine($"[FishingStrategy] No fish found after {MaxFishWaitAttempts} attempts, giving up on this cast.");
            UpdateOverlayAction("No fish found", "Skipping cast", "No fish");
            return false;
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

            // Wait for fish detection if enabled — but always proceed to cast
            if (WaitForFishBeforeCasting)
            {
                bool fishFound = await WaitForFishDetectionAsync(cancellationToken);
                if (!fishFound)
                {
                    System.Diagnostics.Debug.WriteLine("[FishingStrategy] No fish found after waiting, casting straight ahead...");
                }
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

            // Find the cast button using image recognition — use the actual detected position
            var (btnX, btnY) = await CoordinatesManager.GetCoordsWithImageRecAsync(FishingCoordinatesEnum.RedFishingButton);
            _cachedRedButtonPos = new Point(btnX, btnY);

            System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Red button found at screen ({btnX}, {btnY}), window rect: {windowRect}");

            // Move to the actual red fishing button position (from image recognition) and press down
            SimulateDragMove(btnX, btnY);
            await Task.Delay(150, cancellationToken);
            SendInputMouseDown();
            System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Mouse down at ({btnX}, {btnY})");
            await Task.Delay(400, cancellationToken); // Wait for aim mode to activate

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
                            // Extract the intended drag vector from the formula (relative to its reference rod position)
                            // and apply it from the ACTUAL button position. Without this, any offset between where
                            // image recognition found the button and the formula's reference position causes the
                            // cast to aim in the wrong direction.
                            int dragX = castResult.CastDestination.X - castResult.RodButtonPosition.X;
                            int dragY = castResult.CastDestination.Y - castResult.RodButtonPosition.Y;

                            // The original formula's horizontal factor (factorX ≈ 0.28) undershoots for
                            // fish to the left/right. Boost the horizontal drag so side casts reach.
                            const double horizontalBoost = 1.25;
                            dragX = (int)(dragX * horizontalBoost);

                            castDestination = new Point(btnX + dragX, btnY + dragY);
                            System.Diagnostics.Debug.WriteLine($"[FishingStrategy] Drag vector: ({dragX},{dragY}) [hBoost={horizontalBoost}], btn offset from ref: ({btnX - castResult.RodButtonPosition.X},{btnY - castResult.RodButtonPosition.Y})");
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

        /// <summary>
        /// Minimum number of sample positions that must match cream/green to consider the popup visible
        /// via the secondary color-spot check.
        /// </summary>
        private const int MinPopupMatchCount = 2;

        /// <summary>
        /// Fraction of pond sample points that must be non-water to confirm occlusion by a popup.
        /// </summary>
        private const double PondOcclusionThreshold = 0.50;

        protected Task<bool> CheckIfFishCaught(CancellationToken cancellationToken)
        {
            var windowRect = CoreFunctionality.GetGameWindowRect();
            if (windowRect.IsEmpty) return Task.FromResult(false);

            // --- Primary: Pond occlusion detection ---
            // When a catch popup appears it covers most of the pond area.
            // Sample a grid across the pond region and count how many points
            // are NOT the distinctive teal/cyan water color.
            const int cols = 6;
            const int rows = 4;
            int nonWaterCount = 0;
            int totalSamples = cols * rows;

            for (int r = 0; r < rows; r++)
            {
                // Y spans ~8-45% of window height (the visible pond from the dock)
                double yFrac = 0.08 + (0.37 * (r + 0.5) / rows);
                int y = windowRect.Y + (int)(windowRect.Height * yFrac);

                for (int c = 0; c < cols; c++)
                {
                    // X spans ~20-80% of window width (center of the pond)
                    double xFrac = 0.20 + (0.60 * (c + 0.5) / cols);
                    int x = windowRect.X + (int)(windowRect.Width * xFrac);

                    var color = GetColorAt(x, y);
                    if (!IsWaterColor(color))
                        nonWaterCount++;
                }
            }

            double occlusionRatio = (double)nonWaterCount / totalSamples;
            System.Diagnostics.Debug.WriteLine($"[FishCatch] Pond occlusion: {nonWaterCount}/{totalSamples} non-water ({occlusionRatio:P0})");

            if (occlusionRatio >= PondOcclusionThreshold)
            {
                System.Diagnostics.Debug.WriteLine($"[FishCatch] Popup confirmed via pond occlusion ({occlusionRatio:P0} >= {PondOcclusionThreshold:P0})");
                return Task.FromResult(true);
            }

            // --- Secondary: Centered cream/green color-spot check ---
            int popupCenterX = windowRect.X + (int)(windowRect.Width * 0.50);
            int popupTopY = windowRect.Y + (int)(windowRect.Height * 0.10);
            int popupMidY = windowRect.Y + (int)(windowRect.Height * 0.25);

            var positionsToCheck = new[]
            {
                new Point(popupCenterX, popupTopY),
                new Point(popupCenterX, popupMidY),
                new Point(popupCenterX - 50, popupTopY),
                new Point(popupCenterX + 50, popupTopY),
                new Point(popupCenterX, popupTopY + 50),
                new Point(popupCenterX - 30, popupMidY),
                new Point(popupCenterX + 30, popupMidY),
            };

            int matchCount = 0;

            foreach (var pos in positionsToCheck)
            {
                var color = GetColorAt(pos.X, pos.Y);

                if (IsCreamColor(color))
                {
                    matchCount++;
                    System.Diagnostics.Debug.WriteLine($"[FishCatch] Cream match at ({pos.X}, {pos.Y}) - RGB({color.R},{color.G},{color.B}) [{matchCount}/{MinPopupMatchCount}]");
                }
                else if (IsPopupGreenBorder(color))
                {
                    matchCount++;
                    System.Diagnostics.Debug.WriteLine($"[FishCatch] Green match at ({pos.X}, {pos.Y}) - RGB({color.R},{color.G},{color.B}) [{matchCount}/{MinPopupMatchCount}]");
                }

                if (matchCount >= MinPopupMatchCount)
                {
                    System.Diagnostics.Debug.WriteLine($"[FishCatch] Popup confirmed via color-spot ({matchCount} matches)");
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Checks if a color matches the distinctive teal/cyan pond water.
        /// Water has dominant green, moderate blue, low red.
        /// </summary>
        private bool IsWaterColor(Color color)
        {
            return color.G >= 140
                && color.G >= color.R && color.G >= color.B  // G is highest channel
                && color.R <= 170
                && (color.G - color.R) >= 30                 // noticeably more green than red
                && (color.G + color.B) > 250;                // not too dark
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
        /// Closes the fish caught popup by clicking the small red X button in the bottom-right corner.
        /// Uses image recognition to find the X button. Will prompt for template capture if needed.
        /// </summary>
        protected async Task CloseFishCaughtPopup(CancellationToken cancellationToken)
        {
            const string elementName = "FishPopupCloseButton";

            Debug.WriteLine($"[FishingStrategy] Looking for fish popup close button...");

            // Use silent search (FindElementAsync) — no prompts during active fishing.
            // Color-based catch detection can have false positives, so we don't want to
            // prompt for template capture when there may be no popup on screen.
            var buttonLocation = await UIElementManager.Instance.FindElementAsync(elementName, cancellationToken);

            if (buttonLocation.HasValue)
            {
                Debug.WriteLine($"[FishingStrategy] Found close button at ({buttonLocation.Value.X}, {buttonLocation.Value.Y})");

                MoveCursor(buttonLocation.Value.X, buttonLocation.Value.Y);
                await Task.Delay(100, cancellationToken);
                DoMouseClick();
                await Task.Delay(300, cancellationToken);
            }
            else
            {
                Debug.WriteLine($"[FishingStrategy] Close button not found, using fallback position...");

                // Fallback to estimated position — handles both missing template and false positive cases
                var windowRect = GetGameWindowRect();
                if (!windowRect.IsEmpty)
                {
                    int closeButtonX = windowRect.X + (int)(windowRect.Width * 0.65);
                    int closeButtonY = windowRect.Y + (int)(windowRect.Height * 0.58);

                    MoveCursor(closeButtonX, closeButtonY);
                    await Task.Delay(100, cancellationToken);
                    DoMouseClick();
                    await Task.Delay(300, cancellationToken);
                }
            }
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
                // Set flag to prevent global keyboard hook from treating this as a user-initiated cancel
                System.Diagnostics.Debug.WriteLine("[FishingStrategy] Could not find Exit button, pressing ESC...");
                IsSimulatedKeyPress = true;
                try
                {
                    SendKeys.SendWait("{ESC}");
                }
                finally
                {
                    IsSimulatedKeyPress = false;
                }
                await Task.Delay(500, cancellationToken);
            }
        }

        /// <summary>
        /// Handles the "fish bucket full" popup by clicking the Exit button.
        /// Does NOT set shouldStopFishing so the outer sell loop continues naturally.
        /// </summary>
        protected async Task HandleBucketFullPopup(CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.WriteLine("[FishingStrategy] Handling 'bucket full' popup - clicking Exit...");

            var exitPos = FishBucketFullDetector.GetExitButtonPosition();
            if (exitPos.HasValue)
            {
                MoveCursor(exitPos.Value.X, exitPos.Value.Y);
                await Task.Delay(100, cancellationToken);
                DoMouseClick();
                await Task.Delay(500, cancellationToken);
                System.Diagnostics.Debug.WriteLine("[FishingStrategy] Exit button clicked. Proceeding to sell fish.");
            }
            else
            {
                // Fallback: press ESC to close the popup
                System.Diagnostics.Debug.WriteLine("[FishingStrategy] Could not find Exit button, pressing ESC...");
                IsSimulatedKeyPress = true;
                try
                {
                    SendKeys.SendWait("{ESC}");
                }
                finally
                {
                    IsSimulatedKeyPress = false;
                }
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
            // Set flag to prevent global keyboard hook from treating this as a user-initiated cancel
            IsSimulatedKeyPress = true;
            try
            {
                SendKeys.SendWait("{ESC}");
            }
            finally
            {
                IsSimulatedKeyPress = false;
            }
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
            if (result != null)
            {
                Debug.WriteLine($"[FishingStrategy] UpdateOverlay: ScanArea={result.ScanArea}, IsEmpty={result.ScanArea.IsEmpty}, Candidates={result.AllCandidates?.Count ?? 0}, Status='{status}'");
            }
            SafeOverlayInvoke(o => o.UpdateDetection(result, targetFish, status));
        }

        /// <summary>
        /// Clears the fishing overlay.
        /// </summary>
        private void ClearOverlay()
            => SafeOverlayInvoke(o => o.ClearOverlay());
    }
}
