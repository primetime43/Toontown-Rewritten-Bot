using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToonTown_Rewritten_Bot.Services;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// A single fish shadow candidate for calibration.
    /// </summary>
    public class FishCandidate
    {
        public Point Position { get; set; }
        public Color Color { get; set; }
        public int Size { get; set; }
        public bool HasBubblesAbove { get; set; }
        public double DistanceFromCenter { get; set; }
        public double CastPower { get; set; }  // How much drag power needed (lower = closer/easier)
    }

    /// <summary>
    /// Result of fish detection for debugging/visualization purposes.
    /// </summary>
    public class FishDetectionDebugResult
    {
        public Rectangle ScanArea { get; set; }
        public bool UsedDynamicPondDetection { get; set; }
        public Color TargetBubbleColor { get; set; }
        public Tolerance ColorTolerance { get; set; }
        public int AvgBrightness { get; set; }
        public int DarkThreshold { get; set; }
        public int DarkPixelCount { get; set; }
        public List<List<Point>> Blobs { get; set; } = new List<List<Point>>();
        public int RejectedBlobCount { get; set; }
        public Point? BestShadowPosition { get; set; }
        public Color BestShadowColor { get; set; }
        public bool HasBubblesAbove { get; set; }
        public int CandidateCount { get; set; }
        public int CandidatesWithBubbles { get; set; }
        public Point? RodButtonPosition { get; set; }
        public Point? CastDestination { get; set; }
        public bool UsingLearnedColor { get; set; }
        public Color? LearnedColor { get; set; }
        public bool NeedsCalibration { get; set; }
        public List<FishCandidate> AllCandidates { get; set; } = new List<FishCandidate>();
    }

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
        // Supports both full location names and short debug UI names
        private static readonly Dictionary<string, FishingSpotConfig> FishingSpots = new()
        {
            // Toontown Central - Original working values (darker than general water)
            ["TOONTOWN CENTRAL PUNCHLINE PLACE"] = new FishingSpotConfig(
                new Rectangle(260, 196, 1089, 430),  // scan1 to scan2
                Color.FromArgb(20, 123, 114),        // fish shadow color (darker than water)
                new Tolerance(8, 8, 8),              // tighter tolerance
                15                                    // Y adjustment
            ),
            ["TTC Punchline Place"] = new FishingSpotConfig(
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
            ["DDL Lullaby Lane"] = new FishingSpotConfig(
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
            ["Brrrgh Polar Place"] = new FishingSpotConfig(
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
            ["Brrrgh Walrus Way"] = new FishingSpotConfig(
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
            ["Brrrgh Sleet Street"] = new FishingSpotConfig(
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
            ["MML Tenor Terrace"] = new FishingSpotConfig(
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
            ["DD Lighthouse Lane"] = new FishingSpotConfig(
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
            ["DG Elm Street"] = new FishingSpotConfig(
                new Rectangle(200, 80, 1230, 712),
                Color.FromArgb(17, 102, 75),
                new Tolerance(5, 4, 5),
                35
            ),

            // Estate (default for Fish Anywhere)
            ["FISH ANYWHERE"] = new FishingSpotConfig(
                new Rectangle(200, 150, 1292, 510),
                Color.FromArgb(56, 129, 122),
                new Tolerance(7, 5, 5),
                35
            ),
            ["Fish Anywhere"] = new FishingSpotConfig(
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
        private readonly string _currentLocationName;

        /// <summary>
        /// Static storage for learned fish shadow colors, keyed by location name.
        /// This persists across detector instances so calibration works everywhere.
        /// </summary>
        private static readonly Dictionary<string, (Color color, int confidence)> _learnedColors = new();
        private static readonly object _learnedColorsLock = new object();

        public FishBubbleDetector() : this("FISH ANYWHERE")
        {
        }

        public FishBubbleDetector(string locationName)
        {
            _currentLocationName = NormalizeLocationName(locationName);

            if (FishingSpots.TryGetValue(locationName, out var config))
            {
                _spotConfig = config;
            }
            else
            {
                // Default to Estate/Fish Anywhere settings
                _spotConfig = FishingSpots["FISH ANYWHERE"];
            }

            // Check if we have a learned color for this location
            lock (_learnedColorsLock)
            {
                if (_learnedColors.TryGetValue(_currentLocationName, out var learned))
                {
                    System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Using learned color for {_currentLocationName}: " +
                        $"RGB({learned.color.R},{learned.color.G},{learned.color.B}), confidence={learned.confidence}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Using config for {locationName}: " +
                $"Color=({_spotConfig.BubbleColor.R},{_spotConfig.BubbleColor.G},{_spotConfig.BubbleColor.B}), " +
                $"YAdj={_spotConfig.YAdjustment}");
        }

        /// <summary>
        /// Normalizes location name to a consistent key for learned color storage.
        /// </summary>
        private static string NormalizeLocationName(string locationName)
        {
            // Map short names to their full versions for consistent storage
            return locationName?.ToUpperInvariant()?.Trim() ?? "FISH ANYWHERE";
        }

        /// <summary>
        /// Gets the learned shadow color for the current location (if any).
        /// </summary>
        private Color? _learnedShadowColor
        {
            get
            {
                lock (_learnedColorsLock)
                {
                    if (_learnedColors.TryGetValue(_currentLocationName, out var learned))
                        return learned.color;
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the confidence level for the learned color (0-10).
        /// </summary>
        private int _learnedColorConfidence
        {
            get
            {
                lock (_learnedColorsLock)
                {
                    if (_learnedColors.TryGetValue(_currentLocationName, out var learned))
                        return learned.confidence;
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the current spot configuration (for debug display).
        /// </summary>
        public FishingSpotConfig GetCurrentConfig() => _spotConfig;

        /// <summary>
        /// Detects fish in a provided screenshot (for debug UI use).
        /// Returns detailed results for visualization.
        /// </summary>
        public FishDetectionDebugResult DetectFromScreenshot(Bitmap screenshot)
        {
            var result = new FishDetectionDebugResult
            {
                TargetBubbleColor = _spotConfig.BubbleColor,
                ColorTolerance = _spotConfig.ColorTolerance
            };

            if (screenshot == null) return result;

            // Calculate scale factors
            float scaleX = (float)screenshot.Width / ReferenceWidth;
            float scaleY = (float)screenshot.Height / ReferenceHeight;

            // Check for custom user-defined scan area first
            var customScanArea = CustomScanAreaManager.GetCustomScanArea(
                _currentLocationName, screenshot.Width, screenshot.Height);

            if (customScanArea.HasValue)
            {
                result.ScanArea = customScanArea.Value;
                result.UsedDynamicPondDetection = false;
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Using CUSTOM scan area for '{_currentLocationName}': {result.ScanArea}");
            }
            else
            {
                // Use the predefined config scan area
                result.ScanArea = new Rectangle(
                    (int)(_spotConfig.ScanArea.X * scaleX),
                    (int)(_spotConfig.ScanArea.Y * scaleY),
                    (int)(_spotConfig.ScanArea.Width * scaleX),
                    (int)(_spotConfig.ScanArea.Height * scaleY)
                );
                result.UsedDynamicPondDetection = false;
            }

            int startX = result.ScanArea.X;
            int startY = result.ScanArea.Y;
            int endX = Math.Min(screenshot.Width, startX + result.ScanArea.Width);
            int endY = Math.Min(screenshot.Height, startY + result.ScanArea.Height);

            // Run detection - only find pixels that look like fish shadows (teal/cyan)
            const int step = 3;
            const int minBlobSize = 50;
            const int maxBlobSize = 2000;

            // First pass: Calculate average brightness
            long totalBrightness = 0;
            int pixelCount = 0;

            for (int y = startY; y < endY; y += step * 2)
            {
                for (int x = startX; x < endX; x += step * 2)
                {
                    if (x >= 0 && x < screenshot.Width && y >= 0 && y < screenshot.Height)
                    {
                        var color = screenshot.GetPixel(x, y);
                        totalBrightness += (color.R + color.G + color.B) / 3;
                        pixelCount++;
                    }
                }
            }

            if (pixelCount == 0) return result;
            result.AvgBrightness = (int)(totalBrightness / pixelCount);
            result.DarkThreshold = Math.Max(10, result.AvgBrightness - 25);

            // Second pass: Find dark pixels that could be fish shadows
            // Priority: 1) Learned color, 2) Location-specific config color, 3) General dark detection
            var fishShadowPixels = new List<Point>();
            bool usingLearnedColor = _learnedShadowColor.HasValue && _learnedColorConfidence >= 1;

            result.UsingLearnedColor = usingLearnedColor;
            result.LearnedColor = _learnedShadowColor;
            result.NeedsCalibration = NeedsCalibration;

            if (usingLearnedColor)
            {
                var lc = _learnedShadowColor.Value;
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Using LEARNED color RGB({lc.R},{lc.G},{lc.B}) with tolerance ±35");
            }
            else
            {
                var bc = _spotConfig.BubbleColor;
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Using location config color RGB({bc.R},{bc.G},{bc.B}) + general dark detection (threshold={result.DarkThreshold})");
            }

            for (int y = startY; y < endY; y += step)
            {
                for (int x = startX; x < endX; x += step)
                {
                    if (x >= 0 && x < screenshot.Width && y >= 0 && y < screenshot.Height)
                    {
                        var color = screenshot.GetPixel(x, y);

                        bool isMatch;
                        if (usingLearnedColor)
                        {
                            isMatch = MatchesLearnedColor(color);
                        }
                        else
                        {
                            // Try location-specific config color first, then general dark detection
                            isMatch = MatchesConfigColor(color) || IsFishShadowColor(color, result.DarkThreshold);
                        }

                        if (isMatch)
                        {
                            fishShadowPixels.Add(new Point(x, y));
                        }
                    }
                }
            }

            result.DarkPixelCount = fishShadowPixels.Count;

            if (fishShadowPixels.Count < minBlobSize / (step * step))
                return result;

            // Find blobs from fish shadow colored pixels only
            var allBlobs = FindBlobs(fishShadowPixels, step * 3);
            result.Blobs = allBlobs;

            // Find best blob (fish shadow) - prefer ones with bubbles above
            // Calculate distance from toon position (rod button), not scan area center
            Point toonPosition = new Point(
                (int)(RodButtonX * scaleX),
                (int)(RodButtonY * scaleY));
            int rejectedCount = 0;

            // Collect valid candidates
            var candidates = new List<(Point center, Color color, int size, double distance, double castPower)>();

            foreach (var blob in allBlobs)
            {
                int blobSize = blob.Count * step * step;

                if (blobSize < minBlobSize || blobSize > maxBlobSize)
                {
                    rejectedCount++;
                    continue;
                }

                // Check if blob is roughly circular (fish shadows are oval/round)
                if (!IsCircularBlob(blob))
                {
                    rejectedCount++;
                    continue;
                }

                // Calculate blob center
                int sumX = 0, sumY = 0;
                foreach (var p in blob)
                {
                    sumX += p.X;
                    sumY += p.Y;
                }
                Point blobCenter = new Point(sumX / blob.Count, sumY / blob.Count);

                // Check if blob is surrounded by water (rejects text on gray UI backgrounds)
                if (!IsSurroundedByWater(screenshot, blobCenter))
                {
                    rejectedCount++;
                    continue;
                }

                // Sample color at blob center (for display)
                Color blobColor = screenshot.GetPixel(
                    Math.Min(Math.Max(blobCenter.X, 0), screenshot.Width - 1),
                    Math.Min(Math.Max(blobCenter.Y, 0), screenshot.Height - 1));

                // Calculate cast power needed (lower = closer/easier to catch)
                double castPower = CalculateCastPower(blobCenter.X, blobCenter.Y, screenshot.Width, screenshot.Height);

                // Also keep distance for reference
                double distance = Math.Sqrt(Math.Pow(blobCenter.X - toonPosition.X, 2) +
                                           Math.Pow(blobCenter.Y - toonPosition.Y, 2));

                candidates.Add((blobCenter, blobColor, blobSize, distance, castPower));
            }

            result.RejectedBlobCount = rejectedCount;
            result.CandidateCount = candidates.Count;

            // Populate AllCandidates for calibration UI (will add bubble info in the loop below)
            foreach (var candidate in candidates)
            {
                result.AllCandidates.Add(new FishCandidate
                {
                    Position = candidate.center,
                    Color = candidate.color,
                    Size = candidate.size,
                    DistanceFromCenter = candidate.castPower,  // Use cast power for sorting
                    CastPower = candidate.castPower,
                    HasBubblesAbove = false // Will be updated in the loop below
                });
            }

            // Check each candidate for bubbles, prefer those with bubbles
            // Also reject edge candidates unless they have high confidence (bubbles)
            Point? bestWithBubbles = null;
            double bestBubblesScore = double.MaxValue;
            Color bestBubblesColor = Color.Empty;

            Point? bestWithoutBubbles = null;
            double bestNoBubblesScore = double.MaxValue;
            Color bestNoBubblesColor = Color.Empty;

            int candidatesWithBubbles = 0;
            const int edgeMargin = 50; // Reject edge candidates without bubbles

            // Sort by cast power (lowest first = easiest to catch)
            foreach (var candidate in candidates.OrderBy(c => c.castPower))
            {
                double score = candidate.castPower - candidate.size * 0.1;  // Lower cast power = better
                bool hasBubbles = HasBubblesAbove(screenshot, candidate.center, result.AvgBrightness);

                // Update the AllCandidates list with bubble info
                var matchingCandidate = result.AllCandidates.FirstOrDefault(c => c.Position == candidate.center);
                if (matchingCandidate != null)
                {
                    matchingCandidate.HasBubblesAbove = hasBubbles;
                }

                // Check if candidate is near the edge of scan area
                bool isNearEdge = candidate.center.X < startX + edgeMargin ||
                                  candidate.center.X > endX - edgeMargin ||
                                  candidate.center.Y < startY + edgeMargin ||
                                  candidate.center.Y > endY - edgeMargin;

                if (hasBubbles)
                {
                    candidatesWithBubbles++;
                    if (score < bestBubblesScore)
                    {
                        bestBubblesScore = score;
                        bestWithBubbles = candidate.center;
                        bestBubblesColor = candidate.color;
                    }
                }
                else if (!isNearEdge) // Only accept non-bubble candidates if not near edge
                {
                    if (score < bestNoBubblesScore)
                    {
                        bestNoBubblesScore = score;
                        bestWithoutBubbles = candidate.center;
                        bestNoBubblesColor = candidate.color;
                    }
                }
                // Edge candidates without bubbles are silently rejected
            }

            result.CandidatesWithBubbles = candidatesWithBubbles;

            // Prefer shadows with bubbles, fall back to best shadow
            Point? bestBlob = null;
            Color bestBlobColor = Color.Empty;

            if (bestWithBubbles.HasValue)
            {
                bestBlob = bestWithBubbles;
                bestBlobColor = bestBubblesColor;
                result.HasBubblesAbove = true;
            }
            else if (bestWithoutBubbles.HasValue)
            {
                bestBlob = bestWithoutBubbles;
                bestBlobColor = bestNoBubblesColor;
                result.HasBubblesAbove = false;
            }
            else if (allBlobs.Count > 0 && candidates.Count == 0)
            {
                // FALLBACK: No candidates passed filters, but we have raw blobs
                // Use the largest blob that's roughly in the center of scan area
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] No candidates passed filters, using blob fallback");

                var centerX = (startX + endX) / 2;
                var centerY = (startY + endY) / 2;

                // Find the largest blob closest to center
                var bestFallbackBlob = allBlobs
                    .Where(b => b.Count >= 3) // At least 3 points
                    .Select(b => {
                        int sumX = 0, sumY = 0;
                        foreach (var p in b) { sumX += p.X; sumY += p.Y; }
                        var center = new Point(sumX / b.Count, sumY / b.Count);
                        var distToCenter = Math.Sqrt(Math.Pow(center.X - centerX, 2) + Math.Pow(center.Y - centerY, 2));
                        return (blob: b, center: center, size: b.Count, distToCenter: distToCenter);
                    })
                    .OrderByDescending(x => x.size) // Prefer larger blobs
                    .ThenBy(x => x.distToCenter)    // Then prefer closer to center
                    .FirstOrDefault();

                if (bestFallbackBlob.blob != null)
                {
                    bestBlob = bestFallbackBlob.center;
                    bestBlobColor = screenshot.GetPixel(
                        Math.Min(Math.Max(bestFallbackBlob.center.X, 0), screenshot.Width - 1),
                        Math.Min(Math.Max(bestFallbackBlob.center.Y, 0), screenshot.Height - 1));
                    result.HasBubblesAbove = false;

                    // Add to AllCandidates so casting logic can use it
                    result.AllCandidates.Add(new FishCandidate
                    {
                        Position = bestFallbackBlob.center,
                        Color = bestBlobColor,
                        Size = bestFallbackBlob.size * step * step,
                        DistanceFromCenter = bestFallbackBlob.distToCenter,
                        CastPower = CalculateCastPower(bestFallbackBlob.center.X, bestFallbackBlob.center.Y, screenshot.Width, screenshot.Height),
                        HasBubblesAbove = false
                    });

                    System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Fallback blob at ({bestFallbackBlob.center.X},{bestFallbackBlob.center.Y}), size={bestFallbackBlob.size}");
                }
            }

            if (bestBlob.HasValue)
            {
                result.BestShadowPosition = bestBlob.Value;
                result.BestShadowColor = bestBlobColor;

                // Calculate casting destination
                // Note: No +20 offset - we detect blob CENTER, not edge
                float refX = bestBlob.Value.X / scaleX;
                float refY = bestBlob.Value.Y / scaleY + _spotConfig.YAdjustment;
                var destCoords = CalculateCastingDestination((int)refX, (int)refY);

                result.RodButtonPosition = new Point(
                    (int)(RodButtonX * scaleX),
                    (int)(RodButtonY * scaleY));
                result.CastDestination = new Point(
                    (int)(destCoords.X * scaleX),
                    (int)(destCoords.Y * scaleY));
            }

            return result;
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

            // Check for custom user-defined scan area first
            Rectangle scaledScanArea;
            var customScanArea = CustomScanAreaManager.GetCustomScanArea(
                _currentLocationName, windowRect.Width, windowRect.Height);

            if (customScanArea.HasValue)
            {
                // Custom scan area - add window offset
                scaledScanArea = new Rectangle(
                    customScanArea.Value.X + windowRect.X,
                    customScanArea.Value.Y + windowRect.Y,
                    customScanArea.Value.Width,
                    customScanArea.Value.Height
                );
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Using CUSTOM scan area: {scaledScanArea}");
            }
            else
            {
                // Scale the default config scan area to current window size
                scaledScanArea = new Rectangle(
                    (int)(_spotConfig.ScanArea.X * scaleX) + windowRect.X,
                    (int)(_spotConfig.ScanArea.Y * scaleY) + windowRect.Y,
                    (int)(_spotConfig.ScanArea.Width * scaleX),
                    (int)(_spotConfig.ScanArea.Height * scaleY)
                );
            }

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

                        // Use the predefined config scan area
                        int sStartX = Math.Max(0, scanArea.X - windowOffset.X);
                        int sStartY = Math.Max(0, scanArea.Y - windowOffset.Y);
                        int sEndX = Math.Min(screenshot.Width, sStartX + scanArea.Width);
                        int sEndY = Math.Min(screenshot.Height, sStartY + scanArea.Height);

                        // Try computer vision approach first (shadow detection)
                        var shadowResult = DetectFishShadow(screenshot, sStartX, sStartY, sEndX, sEndY);
                        if (shadowResult.HasValue)
                        {
                            int screenX = shadowResult.Value.X + windowOffset.X;
                            int screenY = shadowResult.Value.Y + windowOffset.Y;
                            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Shadow detected at ({screenX}, {screenY})");
                            return new Point(screenX, screenY);
                        }

                        // Fallback to color matching if shadow detection fails
                        System.Diagnostics.Debug.WriteLine("[FishBubbleDetector] Shadow detection failed, trying color match...");
                        return ScanForBubbleByColor(screenshot, sStartX, sStartY, sEndX, sEndY, windowOffset, cancellationToken);
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

            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Avg brightness: {avgBrightness}");

            // Second pass: Find dark pixels that could be fish shadows
            // Priority: 1) Learned color, 2) Location-specific config color, 3) General dark detection
            int darkThreshold = Math.Max(10, avgBrightness - 25);
            var fishShadowPixels = new List<Point>();
            bool usingLearnedColor = _learnedShadowColor.HasValue && _learnedColorConfidence >= 1;

            for (int y = startY; y < endY; y += step)
            {
                for (int x = startX; x < endX; x += step)
                {
                    var color = screenshot.GetPixel(x, y);

                    bool isMatch;
                    if (usingLearnedColor)
                    {
                        isMatch = MatchesLearnedColor(color);
                    }
                    else
                    {
                        // Try location-specific config color first, then general dark detection
                        isMatch = MatchesConfigColor(color) || IsFishShadowColor(color, darkThreshold);
                    }

                    if (isMatch)
                    {
                        fishShadowPixels.Add(new Point(x, y));
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Found {fishShadowPixels.Count} fish shadow colored pixels");

            if (fishShadowPixels.Count < minBlobSize / (step * step))
                return null;

            // Simple blob detection: Find clusters of fish shadow colored pixels
            var blobs = FindBlobs(fishShadowPixels, step * 3);

            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Found {blobs.Count} blobs");

            // Find the best blob that has bubbles above it (confirmed fish)
            // Calculate distance from toon position (rod button), not scan area center
            float scaleX = (float)screenshot.Width / ReferenceWidth;
            float scaleY = (float)screenshot.Height / ReferenceHeight;
            Point toonPosition = new Point(
                (int)(RodButtonX * scaleX),
                (int)(RodButtonY * scaleY));
            Point? bestBlob = null;
            Color bestBlobColor = Color.Empty;

            // Collect all valid candidates first, then check for bubbles
            var candidates = new List<(Point center, Color color, int size, double castPower)>();

            foreach (var blob in blobs)
            {
                int blobSize = blob.Count * step * step; // Approximate actual pixel count

                if (blobSize < minBlobSize || blobSize > maxBlobSize)
                    continue;

                // Check if blob is roughly circular (fish shadows are oval/round)
                if (!IsCircularBlob(blob))
                    continue;

                // Calculate blob center
                int sumX = 0, sumY = 0;
                foreach (var p in blob)
                {
                    sumX += p.X;
                    sumY += p.Y;
                }
                Point blobCenter = new Point(sumX / blob.Count, sumY / blob.Count);

                // Check if blob is surrounded by water (rejects text on gray UI backgrounds)
                if (!IsSurroundedByWater(screenshot, blobCenter))
                    continue;

                // Sample the color at blob center (for logging/debug)
                Color blobColor = screenshot.GetPixel(blobCenter.X, blobCenter.Y);

                // Calculate cast power needed (lower = closer/easier to catch)
                double castPower = CalculateCastPower(blobCenter.X, blobCenter.Y, screenshot.Width, screenshot.Height);

                candidates.Add((blobCenter, blobColor, blobSize, castPower));
            }

            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] {candidates.Count} candidates passed color check, now checking for bubbles...");

            // First pass: Look for shadows with bubbles above (confirmed fish)
            // Also reject edge candidates unless they have high confidence (bubbles)
            Point? bestWithBubbles = null;
            double bestBubblesScore = double.MaxValue;
            Color bestBubblesColor = Color.Empty;

            // Second pass backup: Best shadow without bubble confirmation
            Point? bestWithoutBubbles = null;
            double bestNoBubblesScore = double.MaxValue;
            Color bestNoBubblesColor = Color.Empty;

            const int edgeMargin = 50; // Reject edge candidates without bubbles

            // Sort by cast power (lowest first = easiest to catch)
            foreach (var candidate in candidates.OrderBy(c => c.castPower))
            {
                double score = candidate.castPower - candidate.size * 0.1;

                // Check if candidate is near the edge of scan area
                bool isNearEdge = candidate.center.X < startX + edgeMargin ||
                                  candidate.center.X > endX - edgeMargin ||
                                  candidate.center.Y < startY + edgeMargin ||
                                  candidate.center.Y > endY - edgeMargin;

                // Check for bubbles above this shadow
                bool hasBubbles = HasBubblesAbove(screenshot, candidate.center, avgBrightness);

                if (hasBubbles)
                {
                    if (score < bestBubblesScore)
                    {
                        bestBubblesScore = score;
                        bestWithBubbles = candidate.center;
                        bestBubblesColor = candidate.color;
                    }
                }
                else if (!isNearEdge) // Only accept non-bubble candidates if not near edge
                {
                    // Track best candidate without bubbles as fallback
                    if (score < bestNoBubblesScore)
                    {
                        bestNoBubblesScore = score;
                        bestWithoutBubbles = candidate.center;
                        bestNoBubblesColor = candidate.color;
                    }
                }
                // Edge candidates without bubbles are silently rejected
            }

            // Prefer shadows with confirmed bubbles, but fall back to best shadow if none have bubbles
            if (bestWithBubbles.HasValue)
            {
                bestBlob = bestWithBubbles;
                bestBlobColor = bestBubblesColor;
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] CONFIRMED fish at ({bestBlob.Value.X}, {bestBlob.Value.Y}) - has bubbles above!");

                // AUTO-CALIBRATE: If we have high confidence (bubbles) and not yet calibrated, learn this color
                if (NeedsCalibration)
                {
                    LearnShadowColor(bestBlobColor);
                    System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] AUTO-CALIBRATED from confirmed fish: RGB({bestBlobColor.R},{bestBlobColor.G},{bestBlobColor.B})");
                }
            }
            else if (bestWithoutBubbles.HasValue)
            {
                bestBlob = bestWithoutBubbles;
                bestBlobColor = bestNoBubblesColor;
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Using shadow at ({bestBlob.Value.X}, {bestBlob.Value.Y}) - no bubbles detected but best candidate");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] No valid fish shadows found");
            }

            return bestBlob;
        }

        /// <summary>
        /// Checks if a blob shape is roughly circular (like a fish shadow).
        /// Fish shadows are oval/circular, not long thin lines or irregular shapes.
        /// </summary>
        private bool IsCircularBlob(List<Point> blob)
        {
            if (blob == null || blob.Count < 5)
                return false;

            // Find bounding box of the blob
            int minX = blob.Min(p => p.X);
            int maxX = blob.Max(p => p.X);
            int minY = blob.Min(p => p.Y);
            int maxY = blob.Max(p => p.Y);

            int blobWidth = maxX - minX;
            int blobHeight = maxY - minY;

            // Avoid division by zero
            if (blobWidth < 3 || blobHeight < 3)
                return false;

            // Check aspect ratio - should be between 0.3 and 3.0 for oval/circular shapes
            // Fish shadows can be somewhat elongated but not extremely thin
            float aspectRatio = (float)blobWidth / blobHeight;
            if (aspectRatio < 0.3f || aspectRatio > 3.0f)
            {
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Blob rejected: bad aspect ratio {aspectRatio:F2} ({blobWidth}x{blobHeight})");
                return false;
            }

            // Check compactness - how much of the bounding box is filled
            // A circle fills about 78.5% of its bounding box (π/4)
            // Fish shadows should fill at least 30% of their bounding box
            float boundingArea = blobWidth * blobHeight;
            float fillRatio = (blob.Count * 9) / boundingArea; // *9 because we scan every 3rd pixel

            if (fillRatio < 0.2f)
            {
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Blob rejected: sparse/not compact (fill={fillRatio:F2})");
                return false;
            }

            // Minimum size check - fish shadows should be reasonably sized
            if (blobWidth < 15 || blobHeight < 15)
            {
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Blob rejected: too small ({blobWidth}x{blobHeight})");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a blob is surrounded by water-colored pixels.
        /// This helps reject text labels which are surrounded by gray UI backgrounds.
        /// </summary>
        private bool IsSurroundedByWater(Bitmap screenshot, Point blobCenter, int checkRadius = 30)
        {
            int waterPixelCount = 0;
            int totalChecked = 0;

            // Check pixels in a ring around the blob
            for (int angle = 0; angle < 360; angle += 30)
            {
                double radians = angle * Math.PI / 180;
                int checkX = blobCenter.X + (int)(checkRadius * Math.Cos(radians));
                int checkY = blobCenter.Y + (int)(checkRadius * Math.Sin(radians));

                if (checkX >= 0 && checkX < screenshot.Width && checkY >= 0 && checkY < screenshot.Height)
                {
                    var color = screenshot.GetPixel(checkX, checkY);
                    totalChecked++;

                    // Check if this looks like water (blue/teal tones)
                    if (IsWaterColor(color))
                    {
                        waterPixelCount++;
                    }
                }
            }

            // At least 35% of surrounding pixels should be water (relaxed for fish near edges)
            float waterRatio = totalChecked > 0 ? (float)waterPixelCount / totalChecked : 0;
            bool surrounded = waterRatio >= 0.35f;

            if (!surrounded)
            {
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Blob at ({blobCenter.X},{blobCenter.Y}) rejected: not surrounded by water ({waterRatio:P0})");
            }

            return surrounded;
        }

        /// <summary>
        /// Checks if a color could be a fish shadow (darker than water, teal-ish).
        /// Fish shadows are in water, so they should have a teal/cyan quality.
        /// Supports various water colors including TTC green/teal and other locations.
        /// </summary>
        private bool IsFishShadowColor(Color color, int darkThreshold)
        {
            int brightness = (color.R + color.G + color.B) / 3;

            // Must be reasonably dark (fish shadows are darker than surrounding water)
            // Increased max from 110 to 130 to catch lighter shadows in TTC green water
            if (brightness > Math.Min(darkThreshold, 130))
                return false;

            // Reject very dark pixels (black text, UI borders)
            // Fish shadows are dark but not pure black
            if (brightness < 12)
                return false;

            // Reject grayscale pixels (black text has R ≈ G ≈ B)
            int maxChannel = Math.Max(color.R, Math.Max(color.G, color.B));
            int minChannel = Math.Min(color.R, Math.Min(color.G, color.B));
            int colorRange = maxChannel - minChannel;

            // If all channels are very similar (grayscale) AND very dark, reject - likely text
            if (colorRange < 10 && brightness < 40)
                return false;

            // Reject obvious brown/wood (dock posts) - R much higher than G and B
            if (color.R > color.G + 25 && color.R > color.B + 25)
                return false;

            // Fish shadows should have some blue/teal/green quality
            // Either G or B should be >= R (not strictly greater, allow equal)
            if (color.R > color.G + 15 && color.R > color.B + 15)
                return false;

            // Distinguish from pure grass (G much higher than B)
            // But allow teal/cyan water (G and B both present)
            // TTC water is green-tinted so we need to be more lenient
            // Only reject if B is very low compared to G AND G is high (pure grass)
            if (color.G > 80 && color.B < color.G * 0.25)
                return false;

            return true;
        }

        /// <summary>
        /// Checks if a color matches the learned/confirmed fish shadow color.
        /// </summary>
        private bool MatchesLearnedColor(Color color)
        {
            if (!_learnedShadowColor.HasValue)
                return false;

            var learned = _learnedShadowColor.Value;
            int tolerance = 35; // Allow more variation for different lighting/fish

            bool matches = Math.Abs(color.R - learned.R) <= tolerance &&
                          Math.Abs(color.G - learned.G) <= tolerance &&
                          Math.Abs(color.B - learned.B) <= tolerance;

            return matches;
        }

        /// <summary>
        /// Checks if a color matches the location-specific configured fish shadow color.
        /// Uses a wider tolerance than the strict color matching since fish shadows vary.
        /// </summary>
        private bool MatchesConfigColor(Color color)
        {
            var target = _spotConfig.BubbleColor;
            var tolerance = _spotConfig.ColorTolerance;

            // Use wider tolerance (2x config tolerance) for more reliable detection
            int tolR = Math.Max(tolerance.R * 2, 20);
            int tolG = Math.Max(tolerance.G * 2, 20);
            int tolB = Math.Max(tolerance.B * 2, 20);

            bool matches = Math.Abs(color.R - target.R) <= tolR &&
                          Math.Abs(color.G - target.G) <= tolG &&
                          Math.Abs(color.B - target.B) <= tolB;

            return matches;
        }

        /// <summary>
        /// Dynamically detects the pond/water area in the screenshot.
        /// Uses color detection to find the teal/cyan water region.
        /// </summary>
        private Rectangle DetectPondArea(Bitmap screenshot)
        {
            // Water in TTR is typically teal/cyan - G and B are higher than R
            // We'll scan for water-colored pixels and find their bounding box

            int width = screenshot.Width;
            int height = screenshot.Height;

            // Limit scan area to exclude UI and dock
            int topMargin = 80;          // Skip top UI
            int bottomMargin = 250;      // Skip bottom dock area (important!)
            int sideMargin = 80;         // Skip sides

            int minX = width, maxX = 0, minY = height, maxY = 0;
            int waterPixelCount = 0;

            const int step = 5; // Scan every 5th pixel for speed

            for (int y = topMargin; y < height - bottomMargin; y += step)
            {
                for (int x = sideMargin; x < width - sideMargin; x += step)
                {
                    var color = screenshot.GetPixel(x, y);

                    if (IsWaterColor(color))
                    {
                        waterPixelCount++;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            // Need at least some water pixels to consider it valid
            if (waterPixelCount < 50)
            {
                System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Pond detection: insufficient water pixels ({waterPixelCount})");
                return Rectangle.Empty;
            }

            // Add small padding but stay within bounds
            int padding = 15;
            minX = Math.Max(sideMargin, minX - padding);
            minY = Math.Max(topMargin, minY - padding);
            maxX = Math.Min(width - sideMargin, maxX + padding);
            maxY = Math.Min(height - bottomMargin, maxY + padding);

            var pondRect = new Rectangle(minX, minY, maxX - minX, maxY - minY);

            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Pond detected: ({pondRect.X}, {pondRect.Y}) - {pondRect.Width}x{pondRect.Height} ({waterPixelCount} water pixels)");

            return pondRect;
        }

        /// <summary>
        /// Checks if a color looks like water (teal/cyan/green tones).
        /// Water in TTR varies by location - from blue/cyan to green/teal.
        /// TTC has notably green-tinted water.
        /// </summary>
        private bool IsWaterColor(Color color)
        {
            int brightness = (color.R + color.G + color.B) / 3;

            // Water should be moderately bright (relaxed range for darker/lighter water)
            if (brightness < 35 || brightness > 210)
                return false;

            // Water has G and/or B higher than R
            // Relaxed: only need one of G or B to be notably higher
            if (color.G < color.R + 5 && color.B < color.R + 5)
                return false;

            // Distinguish water from pure grass
            // Water: B is reasonably present (teal/cyan/green-teal)
            // Pure grass: G is MUCH higher than B
            // TTC water is green-tinted, so allow lower B ratio
            // Only reject if B is very low compared to G
            if (color.B < color.G * 0.4)
                return false;

            // G should be reasonably present for water
            // Relaxed B requirement for green-tinted water like TTC
            if (color.G < 50)
                return false;

            // R should be lower than G for water (more lenient)
            if (color.R > 120)
                return false;

            return true;
        }

        /// <summary>
        /// Checks for bubbles above a shadow position to verify it's a fish.
        /// Fish in TTR have characteristic white/light bubbles rising above their shadow.
        /// </summary>
        private bool HasBubblesAbove(Bitmap screenshot, Point shadowCenter, int avgWaterBrightness)
        {
            // Bubbles appear above the shadow - scan a rectangular area above the shadow
            // Bubbles are small white/light spots that are significantly brighter than water
            const int scanWidth = 60;   // Width of area to scan for bubbles
            const int scanHeight = 80;  // How far above the shadow to look
            const int minBubblePixels = 3; // Minimum bright pixels to count as bubbles

            // Bubbles should be notably brighter than the water
            int bubbleThreshold = Math.Max(avgWaterBrightness + 40, 150);

            int startX = Math.Max(0, shadowCenter.X - scanWidth / 2);
            int endX = Math.Min(screenshot.Width - 1, shadowCenter.X + scanWidth / 2);
            int startY = Math.Max(0, shadowCenter.Y - scanHeight); // Above the shadow
            int endY = Math.Max(0, shadowCenter.Y - 10); // Stop just above the shadow itself

            if (startY >= endY) return false;

            int bubblePixelCount = 0;
            int step = 3;

            for (int y = startY; y < endY; y += step)
            {
                for (int x = startX; x < endX; x += step)
                {
                    var color = screenshot.GetPixel(x, y);
                    int brightness = (color.R + color.G + color.B) / 3;

                    // Bubbles are bright white/light colored
                    if (brightness >= bubbleThreshold)
                    {
                        // Additional check: bubbles tend to be somewhat white (R, G, B similar)
                        int maxDiff = Math.Max(Math.Abs(color.R - color.G),
                                     Math.Max(Math.Abs(color.G - color.B), Math.Abs(color.R - color.B)));
                        if (maxDiff < 50) // Relatively neutral/white color
                        {
                            bubblePixelCount++;
                        }
                    }
                }
            }

            bool hasBubbles = bubblePixelCount >= minBubblePixels;
            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Bubble check at ({shadowCenter.X},{shadowCenter.Y}): " +
                $"found {bubblePixelCount} bubble pixels (threshold={bubbleThreshold}, need {minBubblePixels}) -> {(hasBubbles ? "HAS BUBBLES" : "no bubbles")}");

            return hasBubbles;
        }

        /// <summary>
        /// Stores a detected shadow color to improve future detection.
        /// </summary>
        private void LearnShadowColor(Color shadowColor)
        {
            lock (_learnedColorsLock)
            {
                if (!_learnedColors.TryGetValue(_currentLocationName, out var existing))
                {
                    // First time learning for this location
                    _learnedColors[_currentLocationName] = (shadowColor, 1);
                    System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Learned new shadow color for {_currentLocationName}: ({shadowColor.R},{shadowColor.G},{shadowColor.B})");
                }
                else
                {
                    // Average with existing learned color for stability
                    var newColor = Color.FromArgb(
                        (existing.color.R + shadowColor.R) / 2,
                        (existing.color.G + shadowColor.G) / 2,
                        (existing.color.B + shadowColor.B) / 2
                    );
                    int newConfidence = Math.Min(existing.confidence + 1, 10);
                    _learnedColors[_currentLocationName] = (newColor, newConfidence);
                    System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Updated learned color for {_currentLocationName} to ({newColor.R},{newColor.G},{newColor.B}), confidence={newConfidence}");
                }
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
        /// Uses the EXACT formula from MouseClickSimulator project.
        /// </summary>
        private Point CalculateCastingDestination(int bubbleX, int bubbleY)
        {
            // EXACT formula from MouseClickSimulator - do not modify!
            // destX = 800 + (120/429) * (800 - bubbleX) * (0.75 + (820 - bubbleY) / 820 * 0.38)
            // destY = 846 + (169/428) * (820 - bubbleY)

            double factorX = 120.0 / 429.0;  // ~0.28 - ORIGINAL value
            double factorY = 169.0 / 428.0;  // ~0.39 - ORIGINAL value

            double yAdjustment = 0.75 + ((double)(BubbleRefY - bubbleY) / BubbleRefY) * 0.38;

            int destX = (int)(RodButtonX + factorX * (BubbleRefX - bubbleX) * yAdjustment);
            int destY = (int)(RodButtonY + factorY * (BubbleRefY - bubbleY));

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
            lock (_learnedColorsLock)
            {
                _learnedColors.Remove(_currentLocationName);
            }
            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Learned color reset for {_currentLocationName}");
        }

        /// <summary>
        /// Resets all learned colors for all locations.
        /// </summary>
        public static void ResetAllLearnedColors()
        {
            lock (_learnedColorsLock)
            {
                _learnedColors.Clear();
            }
            System.Diagnostics.Debug.WriteLine("[FishBubbleDetector] All learned colors reset");
        }

        /// <summary>
        /// Returns true if calibration is needed (no learned color yet).
        /// When true, the UI should prompt the user to confirm detected shadows.
        /// </summary>
        public bool NeedsCalibration => !_learnedShadowColor.HasValue || _learnedColorConfidence < 1;

        /// <summary>
        /// Gets the default scan area for the current location, scaled to the current window size.
        /// Used for the scan area calibration UI.
        /// </summary>
        /// <returns>The default scan area rectangle, or Empty if no config exists.</returns>
        public Rectangle GetDefaultScanArea()
        {
            if (_spotConfig == null) return Rectangle.Empty;

            var windowRect = CoreFunctionality.GetGameWindowRect();
            if (windowRect.IsEmpty) return _spotConfig.ScanArea;

            // Scale to current window size
            float scaleX = (float)windowRect.Width / ReferenceWidth;
            float scaleY = (float)windowRect.Height / ReferenceHeight;

            return new Rectangle(
                (int)(_spotConfig.ScanArea.X * scaleX),
                (int)(_spotConfig.ScanArea.Y * scaleY),
                (int)(_spotConfig.ScanArea.Width * scaleX),
                (int)(_spotConfig.ScanArea.Height * scaleY)
            );
        }

        /// <summary>
        /// Calculates the casting destination for a given shadow position.
        /// Returns SCREEN coordinates (including window offset) for direct mouse positioning.
        /// Uses the same calculation as DetectFromScreenshot for consistency.
        /// </summary>
        /// <param name="shadowX">X position of shadow in screenshot coordinates</param>
        /// <param name="shadowY">Y position of shadow in screenshot coordinates</param>
        /// <returns>CastingResult with rod position and cast destination in SCREEN coordinates, or null if calculation fails</returns>
        public CastingResult CalculateCastFromPosition(int shadowX, int shadowY)
        {
            var windowRect = CoreFunctionality.GetGameWindowRect();
            if (windowRect.IsEmpty) return null;

            float scaleX = (float)windowRect.Width / ReferenceWidth;
            float scaleY = (float)windowRect.Height / ReferenceHeight;

            // Convert shadow position to reference coordinates
            // Note: We detect blob CENTER, so no +20 offset needed (MouseClickSimulator adds +20 because they detect edges)
            float refX = shadowX / scaleX;
            float refY = shadowY / scaleY + _spotConfig.YAdjustment;

            // Use the same CalculateCastingDestination method as DetectFromScreenshot
            var destCoords = CalculateCastingDestination((int)refX, (int)refY);

            // Convert to SCREEN coordinates by scaling and adding window offset
            int screenDestX = (int)(destCoords.X * scaleX) + windowRect.X;
            int screenDestY = (int)(destCoords.Y * scaleY) + windowRect.Y;
            int screenRodX = (int)(RodButtonX * scaleX) + windowRect.X;
            int screenRodY = (int)(RodButtonY * scaleY) + windowRect.Y;

            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Cast: shadow({shadowX},{shadowY}) ref({refX:F0},{refY:F0}) -> screen dest({screenDestX},{screenDestY})");

            return new CastingResult
            {
                BubblePosition = new Point(shadowX, shadowY),
                RodButtonPosition = new Point(screenRodX, screenRodY),
                CastDestination = new Point(screenDestX, screenDestY)
            };
        }

        /// <summary>
        /// Calculates how much cast power (drag distance) is needed to reach a fish at the given position.
        /// Lower values mean the fish is closer/easier to catch.
        /// </summary>
        /// <param name="fishX">Fish X position in screenshot coordinates</param>
        /// <param name="fishY">Fish Y position in screenshot coordinates</param>
        /// <param name="screenshotWidth">Width of the screenshot</param>
        /// <param name="screenshotHeight">Height of the screenshot</param>
        /// <returns>Cast power needed (drag distance from rod button to cast destination)</returns>
        public double CalculateCastPower(int fishX, int fishY, int screenshotWidth, int screenshotHeight)
        {
            float scaleX = (float)screenshotWidth / ReferenceWidth;
            float scaleY = (float)screenshotHeight / ReferenceHeight;

            // Convert fish position to reference coordinates
            float refX = fishX / scaleX;
            float refY = fishY / scaleY;

            // Calculate cast destination using same formula as CalculateCastingDestination
            double factorX = 120.0 / 429.0;  // ORIGINAL value
            double factorY = 169.0 / 428.0;  // ORIGINAL value
            double yAdjustment = 0.75 + ((double)(BubbleRefY - refY) / BubbleRefY) * 0.38;

            double destX = RodButtonX + factorX * (BubbleRefX - refX) * yAdjustment;
            double destY = RodButtonY + factorY * (BubbleRefY - refY);

            // Cast power = distance from rod button to cast destination (in reference coords)
            double dragX = destX - RodButtonX;
            double dragY = destY - RodButtonY;
            return Math.Sqrt(dragX * dragX + dragY * dragY);
        }

        /// <summary>
        /// Confirms that the detected shadow at the given position is a real fish.
        /// This learns the color at that position for future detection.
        /// </summary>
        public void ConfirmFishShadow(Bitmap screenshot, Point shadowPosition)
        {
            if (screenshot == null) return;

            // Sample the color at the shadow position
            int x = Math.Min(Math.Max(shadowPosition.X, 0), screenshot.Width - 1);
            int y = Math.Min(Math.Max(shadowPosition.Y, 0), screenshot.Height - 1);
            Color shadowColor = screenshot.GetPixel(x, y);

            LearnShadowColor(shadowColor);
            System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] User confirmed fish at ({x},{y}) - learned color ({shadowColor.R},{shadowColor.G},{shadowColor.B})");
        }

        /// <summary>
        /// Confirms that the detected shadow is NOT a fish.
        /// This could be used to train the detector to avoid similar colors in the future.
        /// </summary>
        public void RejectFishShadow()
        {
            // For now, just log - could add negative learning in the future
            System.Diagnostics.Debug.WriteLine("[FishBubbleDetector] User rejected detected shadow");
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
