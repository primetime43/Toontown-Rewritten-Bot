using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using ToonTown_Rewritten_Bot.Services;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Detects fish bubbles/shadows on screen and calculates casting parameters.
    /// Based on techniques from MouseClickSimulator project.
    /// </summary>
    public class FishBubbleDetector
    {
        // Reference window size (4:3 aspect ratio) - from MouseClickSimulator
        private const int ReferenceWidth = 1600;
        private const int ReferenceHeight = 1151;

        // Fishing rod button position (reference coordinates)
        private const int RodButtonX = 800;
        private const int RodButtonY = 846;

        // Reference point for bubble position calculations
        private const int BubbleRefX = 800;
        private const int BubbleRefY = 820;

        // Scan parameters
        private const int ScanStep = 15;
        private const int MaxScanTimeSeconds = 5; // Reduced from 36 - don't wait too long

        // Location-specific fishing spot data from MouseClickSimulator
        private static readonly Dictionary<string, FishingSpotConfig> FishingSpots = new()
        {
            // Toontown Central - Original working values (darker than general water)
            ["TOONTOWN CENTRAL PUNCHLINE PLACE"] = new FishingSpotConfig(
                new Rectangle(260, 196, 1089, 430),  // scan1 to scan2
                Color.FromArgb(20, 123, 114),        // fish shadow color (darker than water)
                new Tolerance(8, 8, 8),              // tighter tolerance
                15                                    // Y adjustment
            ),

            // Donald's Dreamland
            ["DONALD DREAM LAND LULLABY LANE"] = new FishingSpotConfig(
                new Rectangle(248, 239, 1244, 421),
                Color.FromArgb(55, 103, 116),
                new Tolerance(8, 14, 11),
                0
            ),

            // The Brrrgh
            ["BRRRGH POLAR PLACE"] = new FishingSpotConfig(
                new Rectangle(153, 134, 1297, 569),
                Color.FromArgb(25, 144, 148),
                new Tolerance(10, 11, 11),
                10
            ),
            ["BRRRGH WALRUS WAY"] = new FishingSpotConfig(
                new Rectangle(153, 134, 1297, 569),
                Color.FromArgb(25, 144, 148),
                new Tolerance(10, 11, 11),
                10
            ),
            ["BRRRGH SLEET STREET"] = new FishingSpotConfig(
                new Rectangle(153, 134, 1297, 569),
                Color.FromArgb(25, 144, 148),
                new Tolerance(10, 11, 11),
                10
            ),

            // Minnie's Melodyland
            ["MINNIE'S MELODYLAND TENOR TERRACE"] = new FishingSpotConfig(
                new Rectangle(200, 150, 1292, 510),
                Color.FromArgb(56, 129, 122),
                new Tolerance(10, 10, 10),
                20
            ),

            // Donald's Dock
            ["DONALD DOCK LIGHTHOUSE LANE"] = new FishingSpotConfig(
                new Rectangle(200, 150, 1292, 510),
                Color.FromArgb(22, 140, 118),
                new Tolerance(13, 13, 15),
                15
            ),

            // Daisy Gardens - From MouseClickSimulator
            ["DAISY'S GARDEN ELM STREET"] = new FishingSpotConfig(
                new Rectangle(200, 80, 1230, 712),   // scan area from MouseClickSimulator
                Color.FromArgb(17, 102, 75),         // bubble color from MouseClickSimulator
                new Tolerance(5, 4, 5),              // tolerance from MouseClickSimulator
                35
            ),

            // Estate (default for Fish Anywhere)
            ["FISH ANYWHERE"] = new FishingSpotConfig(
                new Rectangle(200, 150, 1292, 510),
                Color.FromArgb(56, 129, 122),
                new Tolerance(7, 5, 5),
                35
            ),

            // Custom fishing uses Estate settings as default
            ["CUSTOM FISHING ACTION"] = new FishingSpotConfig(
                new Rectangle(200, 150, 1292, 510),
                Color.FromArgb(56, 129, 122),
                new Tolerance(7, 5, 5),
                35
            )
        };

        private readonly FishingSpotConfig _spotConfig;

        /// <summary>
        /// Learned fish shadow color from successful detections.
        /// Once we find a valid shadow, we store its color for faster matching.
        /// </summary>
        private Color? _learnedShadowColor = null;
        private int _learnedColorConfidence = 0;

        public FishBubbleDetector() : this("FISH ANYWHERE")
        {
        }

        public FishBubbleDetector(string locationName)
        {
            if (FishingSpots.TryGetValue(locationName, out var config))
            {
                _spotConfig = config;
            }
            else
            {
                // Default to Estate/Fish Anywhere settings
                _spotConfig = FishingSpots["FISH ANYWHERE"];
            }

            // Reset learned color for new location
            _learnedShadowColor = null;
            _learnedColorConfidence = 0;

            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Using config for {locationName}: " +
                $"Color=({_spotConfig.BubbleColor.R},{_spotConfig.BubbleColor.G},{_spotConfig.BubbleColor.B}), " +
                $"YAdj={_spotConfig.YAdjustment}");
        }

        /// <summary>
        /// Scans for a fish bubble and returns the casting destination.
        /// </summary>
        public async Task<CastingResult> DetectFishAndCalculateCastAsync(CancellationToken cancellationToken = default)
        {
            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] === Starting NEW fish detection scan ===");

            // Get current game window info
            var windowRect = CoreFunctionality.GetGameWindowRect();
            if (windowRect.IsEmpty)
            {
                System.Diagnostics.Debug.WriteLine("[FishBubbleDetector] Game window not found");
                return null;
            }

            // Calculate scale factors
            float scaleX = (float)windowRect.Width / ReferenceWidth;
            float scaleY = (float)windowRect.Height / ReferenceHeight;

            // Scale the scan area to current window size
            var scaledScanArea = new Rectangle(
                (int)(_spotConfig.ScanArea.X * scaleX) + windowRect.X,
                (int)(_spotConfig.ScanArea.Y * scaleY) + windowRect.Y,
                (int)(_spotConfig.ScanArea.Width * scaleX),
                (int)(_spotConfig.ScanArea.Height * scaleY)
            );

            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Scanning area: {scaledScanArea}, " +
                $"looking for color ({_spotConfig.BubbleColor.R},{_spotConfig.BubbleColor.G},{_spotConfig.BubbleColor.B})");

            Point? lastCoords = null;
            int stableCount = 0;
            const int requiredStableScans = 2; // Need 2 scans at same position
            const int positionTolerance = 20;  // Pixels tolerance for "same position"
            var startTime = DateTime.Now;

            // Scan and wait for fish to stop moving
            while ((DateTime.Now - startTime).TotalSeconds < MaxScanTimeSeconds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var newCoords = await ScanForBubbleAsync(scaledScanArea, scaleX, scaleY, cancellationToken);

                if (newCoords.HasValue)
                {
                    // Check if fish is at same position as last scan
                    if (lastCoords.HasValue &&
                        Math.Abs(lastCoords.Value.X - newCoords.Value.X) <= positionTolerance &&
                        Math.Abs(lastCoords.Value.Y - newCoords.Value.Y) <= positionTolerance)
                    {
                        stableCount++;
                        System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Fish stable at ({newCoords.Value.X}, {newCoords.Value.Y}) - count: {stableCount}/{requiredStableScans}");

                        // Fish has stopped moving - cast now!
                        if (stableCount >= requiredStableScans)
                        {
                            float refX = (newCoords.Value.X - windowRect.X) / scaleX + 20;
                            float refY = (newCoords.Value.Y - windowRect.Y) / scaleY + 20 + _spotConfig.YAdjustment;
                            var destCoords = CalculateCastingDestination((int)refX, (int)refY);

                            int screenCastX = (int)(destCoords.X * scaleX) + windowRect.X;
                            int screenCastY = (int)(destCoords.Y * scaleY) + windowRect.Y;
                            int screenRodX = (int)(RodButtonX * scaleX) + windowRect.X;
                            int screenRodY = (int)(RodButtonY * scaleY) + windowRect.Y;

                            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Fish stopped! Casting from ({screenRodX}, {screenRodY}) to ({screenCastX}, {screenCastY})");

                            return new CastingResult
                            {
                                BubblePosition = newCoords.Value,
                                RodButtonPosition = new Point(screenRodX, screenRodY),
                                CastDestination = new Point(screenCastX, screenCastY)
                            };
                        }
                    }
                    else
                    {
                        // Fish moved - reset stability counter
                        stableCount = 0;
                        System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Fish moving... now at ({newCoords.Value.X}, {newCoords.Value.Y})");
                    }

                    lastCoords = newCoords;
                }
                else
                {
                    // No fish found - reset
                    lastCoords = null;
                    stableCount = 0;
                    System.Diagnostics.Debug.WriteLine("[FishBubbleDetector] No fish found in scan...");
                }

                await Task.Delay(200, cancellationToken); // Fast scan rate to track movement
            }

            System.Diagnostics.Debug.WriteLine("[FishBubbleDetector] Timeout - no stable bubble found");
            return null;
        }

        /// <summary>
        /// Scans the specified area for fish shadows using computer vision (contrast detection).
        /// This method detects dark spots in the water that are likely fish shadows.
        /// </summary>
        private async Task<Point?> ScanForBubbleAsync(Rectangle scanArea, float scaleX, float scaleY, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                    {
                        if (screenshot == null)
                        {
                            System.Diagnostics.Debug.WriteLine("[FishBubbleDetector] Failed to capture screenshot");
                            return null;
                        }

                        var windowOffset = CoreFunctionality.GetGameWindowOffset();
                        int startX = Math.Max(0, scanArea.X - windowOffset.X);
                        int startY = Math.Max(0, scanArea.Y - windowOffset.Y);
                        int endX = Math.Min(screenshot.Width, startX + scanArea.Width);
                        int endY = Math.Min(screenshot.Height, startY + scanArea.Height);

                        // Try computer vision approach first (shadow detection)
                        var shadowResult = DetectFishShadow(screenshot, startX, startY, endX, endY);
                        if (shadowResult.HasValue)
                        {
                            int screenX = shadowResult.Value.X + windowOffset.X;
                            int screenY = shadowResult.Value.Y + windowOffset.Y;
                            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Shadow detected at ({screenX}, {screenY})");
                            return new Point(screenX, screenY);
                        }

                        // Fallback to color matching if shadow detection fails
                        System.Diagnostics.Debug.WriteLine("[FishBubbleDetector] Shadow detection failed, trying color match...");
                        return ScanForBubbleByColor(screenshot, startX, startY, endX, endY, windowOffset, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Error scanning: {ex.Message}");
                }
                return (Point?)null;
            }, cancellationToken);
        }

        /// <summary>
        /// Detects fish shadows by finding dark blobs in the water area.
        /// Uses contrast-based detection to find areas darker than surroundings.
        /// Also validates the blob color is teal/cyan (fish shadow) not brown (dock).
        /// </summary>
        private Point? DetectFishShadow(Bitmap screenshot, int startX, int startY, int endX, int endY)
        {
            const int step = 3;
            const int minBlobSize = 50;    // Minimum pixels to consider a valid shadow
            const int maxBlobSize = 2000;  // Maximum pixels (too big = probably not a fish)

            // NOTE: Disabled learned color fast-path - it was causing inaccurate casts
            // because it just finds the first matching pixel, not actual fish shadow blobs.
            // Always use full blob detection for accuracy.

            // First pass: Calculate average brightness of the scan area
            long totalBrightness = 0;
            int pixelCount = 0;

            for (int y = startY; y < endY; y += step * 2)
            {
                for (int x = startX; x < endX; x += step * 2)
                {
                    var color = screenshot.GetPixel(x, y);
                    totalBrightness += (color.R + color.G + color.B) / 3;
                    pixelCount++;
                }
            }

            if (pixelCount == 0) return null;
            int avgBrightness = (int)(totalBrightness / pixelCount);

            // Threshold for "dark" pixels - shadows are darker than average
            int darkThreshold = Math.Max(10, avgBrightness - 25);

            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Avg brightness: {avgBrightness}, dark threshold: {darkThreshold}");

            // Second pass: Find dark pixels and group into blobs
            var darkPixels = new List<Point>();
            for (int y = startY; y < endY; y += step)
            {
                for (int x = startX; x < endX; x += step)
                {
                    var color = screenshot.GetPixel(x, y);
                    int brightness = (color.R + color.G + color.B) / 3;

                    if (brightness < darkThreshold)
                    {
                        darkPixels.Add(new Point(x, y));
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Found {darkPixels.Count} dark pixels");

            if (darkPixels.Count < minBlobSize / (step * step))
                return null;

            // Simple blob detection: Find clusters of dark pixels
            var blobs = FindBlobs(darkPixels, step * 3); // Group pixels within 3*step distance

            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Found {blobs.Count} blobs");

            // Find the best blob (closest to center of scan area, reasonable size, and fish-colored)
            Point scanCenter = new Point((startX + endX) / 2, (startY + endY) / 2);
            Point? bestBlob = null;
            double bestScore = double.MaxValue;
            Color bestBlobColor = Color.Empty;

            foreach (var blob in blobs)
            {
                int blobSize = blob.Count * step * step; // Approximate actual pixel count

                if (blobSize < minBlobSize || blobSize > maxBlobSize)
                    continue;

                // Calculate blob center
                int sumX = 0, sumY = 0;
                foreach (var p in blob)
                {
                    sumX += p.X;
                    sumY += p.Y;
                }
                Point blobCenter = new Point(sumX / blob.Count, sumY / blob.Count);

                // Sample the color at blob center
                Color blobColor = screenshot.GetPixel(blobCenter.X, blobCenter.Y);

                // Validate: fish shadows are teal/cyan (more green+blue than red)
                // Reject brown/wood colors (dock posts) where R > G
                if (!IsFishShadowColor(blobColor))
                {
                    System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Blob at ({blobCenter.X},{blobCenter.Y}) rejected - color ({blobColor.R},{blobColor.G},{blobColor.B}) not fish-like");
                    continue;
                }

                // Score based on distance to scan center (prefer center of pond)
                double distance = Math.Sqrt(Math.Pow(blobCenter.X - scanCenter.X, 2) +
                                           Math.Pow(blobCenter.Y - scanCenter.Y, 2));

                // Prefer larger blobs slightly
                double score = distance - blobSize * 0.1;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestBlob = blobCenter;
                    bestBlobColor = blobColor;
                }
            }

            if (bestBlob.HasValue)
            {
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Best shadow blob at ({bestBlob.Value.X}, {bestBlob.Value.Y}) with color ({bestBlobColor.R},{bestBlobColor.G},{bestBlobColor.B})");
            }

            return bestBlob;
        }

        /// <summary>
        /// Checks if a color looks like a fish shadow (teal/cyan tones, not brown/wood).
        /// Fish shadows typically have more green and blue than red.
        /// This is a lenient check to avoid rejecting valid fish.
        /// </summary>
        private bool IsFishShadowColor(Color color)
        {
            // Fish shadows are teal/cyan: G and B should be higher than R
            // Also should be reasonably dark (not bright water surface)
            int brightness = (color.R + color.G + color.B) / 3;

            // Must be somewhat dark (allow up to 180 for lighter shadows)
            if (brightness > 180)
            {
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Color rejected: too bright ({brightness})");
                return false;
            }

            // Reject obvious brown/wood (dock posts) - R significantly greater than G and B
            // Only reject if clearly brown (R > G + 20 AND R > B + 20)
            if (color.R > color.G + 20 && color.R > color.B + 20)
            {
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Color rejected: brown/wood ({color.R},{color.G},{color.B})");
                return false;
            }

            // Accept most other dark colors - fish shadows can vary
            return true;
        }

        /// <summary>
        /// Stores a detected shadow color to improve future detection.
        /// </summary>
        private void LearnShadowColor(Color shadowColor)
        {
            if (!_learnedShadowColor.HasValue)
            {
                _learnedShadowColor = shadowColor;
                _learnedColorConfidence = 1;
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Learned new shadow color: ({shadowColor.R},{shadowColor.G},{shadowColor.B})");
            }
            else
            {
                // Average with existing learned color for stability
                var existing = _learnedShadowColor.Value;
                _learnedShadowColor = Color.FromArgb(
                    (existing.R + shadowColor.R) / 2,
                    (existing.G + shadowColor.G) / 2,
                    (existing.B + shadowColor.B) / 2
                );
                _learnedColorConfidence = Math.Min(_learnedColorConfidence + 1, 10);
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Updated learned color to ({_learnedShadowColor.Value.R},{_learnedShadowColor.Value.G},{_learnedShadowColor.Value.B}), confidence={_learnedColorConfidence}");
            }
        }

        /// <summary>
        /// Scans for the learned shadow color with tolerance.
        /// </summary>
        private Point? ScanForLearnedColor(Bitmap screenshot, int startX, int startY, int endX, int endY)
        {
            if (!_learnedShadowColor.HasValue) return null;

            var targetColor = _learnedShadowColor.Value;
            var tolerance = new Tolerance(15, 15, 15); // Wider tolerance for learned color

            int step = 5;
            for (int y = startY; y < endY; y += step)
            {
                for (int x = startX; x < endX; x += step)
                {
                    var color = screenshot.GetPixel(x, y);
                    if (IsMatchingColor(color, targetColor, tolerance))
                    {
                        System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Learned color match at ({x}, {y})");
                        return new Point(x, y);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Groups nearby points into blobs using simple clustering.
        /// </summary>
        private List<List<Point>> FindBlobs(List<Point> points, int maxDistance)
        {
            var blobs = new List<List<Point>>();
            var visited = new HashSet<int>();

            for (int i = 0; i < points.Count; i++)
            {
                if (visited.Contains(i)) continue;

                var blob = new List<Point>();
                var queue = new Queue<int>();
                queue.Enqueue(i);
                visited.Add(i);

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    blob.Add(points[current]);

                    // Find neighbors
                    for (int j = 0; j < points.Count; j++)
                    {
                        if (visited.Contains(j)) continue;

                        double dist = Math.Sqrt(Math.Pow(points[current].X - points[j].X, 2) +
                                               Math.Pow(points[current].Y - points[j].Y, 2));
                        if (dist <= maxDistance)
                        {
                            queue.Enqueue(j);
                            visited.Add(j);
                        }
                    }
                }

                if (blob.Count > 0)
                    blobs.Add(blob);
            }

            return blobs;
        }

        /// <summary>
        /// Original color-based scanning as fallback.
        /// </summary>
        private Point? ScanForBubbleByColor(Bitmap screenshot, int startX, int startY, int endX, int endY,
            Point windowOffset, CancellationToken cancellationToken)
        {
            int step = 5;
            for (int y = startY; y < endY; y += step)
            {
                for (int x = startX; x < endX; x += step)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return null;

                    var color = screenshot.GetPixel(x, y);
                    if (IsMatchingColor(color, _spotConfig.BubbleColor, _spotConfig.ColorTolerance))
                    {
                        int screenX = x + windowOffset.X;
                        int screenY = y + windowOffset.Y;
                        System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Color match at ({screenX}, {screenY})");
                        return new Point(screenX, screenY);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Calculates the casting destination based on bubble position.
        /// Formula adapted from MouseClickSimulator project with bounds clamping.
        /// </summary>
        private Point CalculateCastingDestination(int bubbleX, int bubbleY)
        {
            // Formula from MouseClickSimulator:
            // destX = 800 + (120/429) * (800 - bubbleX) * (0.75 + (820 - bubbleY) / 820 * 0.38)
            // destY = 846 + (169/428) * (820 - bubbleY)

            double factorX = 120.0 / 429.0;
            double factorY = 220.0 / 428.0;  // Increased from 169 to reach far fish better

            double yAdjustment = 0.75 + ((double)(BubbleRefY - bubbleY) / BubbleRefY) * 0.38;

            int destX = (int)(RodButtonX + factorX * (BubbleRefX - bubbleX) * yAdjustment);
            int destY = (int)(RodButtonY + factorY * (BubbleRefY - bubbleY));

            // Clamp destination to stay within reasonable casting bounds
            // MouseClickSimulator uses 1009 as default max Y, we'll use that as upper bound
            // and keep it within the reference window
            destX = Math.Max(100, Math.Min(destX, ReferenceWidth - 100));
            destY = Math.Max(RodButtonY - 200, Math.Min(destY, 1009)); // Don't go too far above or below rod

            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Cast calc: bubble({bubbleX},{bubbleY}) -> dest({destX},{destY})");

            return new Point(destX, destY);
        }

        /// <summary>
        /// Checks if two colors match within a tolerance.
        /// </summary>
        private bool IsMatchingColor(Color actual, Color target, Tolerance tolerance)
        {
            return Math.Abs(actual.R - target.R) <= tolerance.R &&
                   Math.Abs(actual.G - target.G) <= tolerance.G &&
                   Math.Abs(actual.B - target.B) <= tolerance.B;
        }

        /// <summary>
        /// Helper method to sample the color at a screen position (for calibration).
        /// </summary>
        public static Color GetColorAt(int screenX, int screenY)
        {
            return CoreFunctionality.GetColorAt(screenX, screenY);
        }

        /// <summary>
        /// Gets the currently learned shadow color (if any).
        /// </summary>
        public Color? GetLearnedShadowColor()
        {
            return _learnedShadowColor;
        }

        /// <summary>
        /// Gets the confidence level of the learned color (0-10).
        /// </summary>
        public int GetLearnedColorConfidence()
        {
            return _learnedColorConfidence;
        }

        /// <summary>
        /// Resets the learned color data.
        /// </summary>
        public void ResetLearnedColor()
        {
            _learnedShadowColor = null;
            _learnedColorConfidence = 0;
            System.Diagnostics.Debug.WriteLine("[FishBubbleDetector] Learned color reset");
        }
    }

    /// <summary>
    /// Configuration for a specific fishing spot.
    /// </summary>
    public class FishingSpotConfig
    {
        public Rectangle ScanArea { get; }
        public Color BubbleColor { get; }
        public Tolerance ColorTolerance { get; }
        public int YAdjustment { get; }

        public FishingSpotConfig(Rectangle scanArea, Color bubbleColor, Tolerance tolerance, int yAdjustment)
        {
            ScanArea = scanArea;
            BubbleColor = bubbleColor;
            ColorTolerance = tolerance;
            YAdjustment = yAdjustment;
        }
    }

    /// <summary>
    /// RGB tolerance values for color matching.
    /// </summary>
    public struct Tolerance
    {
        public int R { get; }
        public int G { get; }
        public int B { get; }

        public Tolerance(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }
    }

    /// <summary>
    /// Result of fish detection and casting calculation.
    /// </summary>
    public class CastingResult
    {
        public Point BubblePosition { get; set; }
        public Point RodButtonPosition { get; set; }
        public Point CastDestination { get; set; }
    }
}
