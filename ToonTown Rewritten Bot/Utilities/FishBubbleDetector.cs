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
        private const int MaxScanTimeSeconds = 5; // Reduced from 36 - don't wait too long

        private readonly FishingSpotConfig _spotConfig;
        private readonly string _currentLocationName;
        private readonly FishColorMatcher _colorMatcher;
        private readonly FishShadowAnalyzer _shadowAnalyzer;

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
            _currentLocationName = FishingSpotDatabase.NormalizeLocationName(locationName);
            _spotConfig = FishingSpotDatabase.GetConfig(locationName);
            _colorMatcher = new FishColorMatcher(_spotConfig, _currentLocationName, () => LearnedShadowColor);
            _shadowAnalyzer = new FishShadowAnalyzer(_colorMatcher);

            // Check if we have a learned color for this location
            lock (_learnedColorsLock)
            {
                if (_learnedColors.TryGetValue(_currentLocationName, out var learned))
                {
                    Logger.Debug("FishDetect", $"Using learned color for {_currentLocationName}: " +
                        $"RGB({learned.color.R},{learned.color.G},{learned.color.B}), confidence={learned.confidence}");
                }
            }

            Logger.Debug("FishDetect", $"Using config for {locationName}: " +
                $"Color=({_spotConfig.BubbleColor.R},{_spotConfig.BubbleColor.G},{_spotConfig.BubbleColor.B}), " +
                $"YAdj={_spotConfig.YAdjustment}");
        }

        /// <summary>
        /// Gets the learned shadow color for the current location (if any).
        /// </summary>
        private Color? LearnedShadowColor
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
        private int LearnedColorConfidence
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
            // Check for custom user-defined colors first
            var customColors = PondColorManager.GetPondColors(_currentLocationName);

            var result = new FishDetectionDebugResult
            {
                TargetBubbleColor = customColors?.ShadowColor ?? _spotConfig.BubbleColor,
                ColorTolerance = customColors != null
                    ? new Tolerance(customColors.ToleranceR, customColors.ToleranceG, customColors.ToleranceB)
                    : _spotConfig.ColorTolerance
            };

            if (customColors != null)
            {
                Logger.Debug("FishDetect", $"Using CUSTOM colors for '{_currentLocationName}': " +
                    $"Shadow=({customColors.ShadowR},{customColors.ShadowG},{customColors.ShadowB}), " +
                    $"Tolerance=({customColors.ToleranceR},{customColors.ToleranceG},{customColors.ToleranceB})");
            }

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
                Logger.Debug("FishDetect", $"Using CUSTOM scan area for '{_currentLocationName}': {result.ScanArea}");
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
            bool usingLearnedColor = LearnedShadowColor.HasValue && LearnedColorConfidence >= 1;

            result.UsingLearnedColor = usingLearnedColor;
            result.LearnedColor = LearnedShadowColor;
            result.NeedsCalibration = NeedsCalibration;

            if (usingLearnedColor)
            {
                var lc = LearnedShadowColor.Value;
                Logger.Debug("FishDetect", $"Using LEARNED color RGB({lc.R},{lc.G},{lc.B}) with tolerance ±35");
            }
            else
            {
                var bc = _spotConfig.BubbleColor;
                Logger.Debug("FishDetect", $"Using location config color RGB({bc.R},{bc.G},{bc.B}) + general dark detection (threshold={result.DarkThreshold})");
            }

            var fishShadowPixels = new List<Point>();

            for (int y = startY; y < endY; y += step)
            {
                for (int x = startX; x < endX; x += step)
                {
                    if (x >= 0 && x < screenshot.Width && y >= 0 && y < screenshot.Height)
                    {
                        var color = screenshot.GetPixel(x, y);

                        if (_colorMatcher.IsPixelFishShadow(color, usingLearnedColor, result.DarkThreshold))
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
            var allBlobs = _shadowAnalyzer.FindBlobs(fishShadowPixels, step * 3);
            result.Blobs = allBlobs;

            // Find best blob (fish shadow) - prefer ones with bubbles above
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

                if (!_shadowAnalyzer.IsCircularBlob(blob))
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

                if (!_shadowAnalyzer.IsSurroundedByWater(screenshot, blobCenter))
                {
                    rejectedCount++;
                    continue;
                }

                // Sample color at blob center (for display)
                Color blobColor = screenshot.GetPixel(
                    Math.Min(Math.Max(blobCenter.X, 0), screenshot.Width - 1),
                    Math.Min(Math.Max(blobCenter.Y, 0), screenshot.Height - 1));

                double castPower = CalculateCastPower(blobCenter.X, blobCenter.Y, screenshot.Width, screenshot.Height);

                double distance = Math.Sqrt(Math.Pow(blobCenter.X - toonPosition.X, 2) +
                                           Math.Pow(blobCenter.Y - toonPosition.Y, 2));

                candidates.Add((blobCenter, blobColor, blobSize, distance, castPower));
            }

            result.RejectedBlobCount = rejectedCount;
            result.CandidateCount = candidates.Count;

            // Populate AllCandidates for calibration UI
            foreach (var candidate in candidates)
            {
                result.AllCandidates.Add(new FishCandidate
                {
                    Position = candidate.center,
                    Color = candidate.color,
                    Size = candidate.size,
                    DistanceFromCenter = candidate.castPower,
                    CastPower = candidate.castPower,
                    HasBubblesAbove = false // Will be updated in the loop below
                });
            }

            // Check each candidate for bubbles, prefer those with bubbles
            Point? bestWithBubbles = null;
            double bestBubblesScore = double.MaxValue;
            Color bestBubblesColor = Color.Empty;

            Point? bestWithoutBubbles = null;
            double bestNoBubblesScore = double.MaxValue;
            Color bestNoBubblesColor = Color.Empty;

            int candidatesWithBubbles = 0;
            const int edgeMargin = 50;

            foreach (var candidate in candidates.OrderBy(c => c.castPower))
            {
                double score = candidate.castPower - candidate.size * 0.1;
                bool hasBubbles = _shadowAnalyzer.HasBubblesAbove(screenshot, candidate.center, result.AvgBrightness);

                // Update the AllCandidates list with bubble info
                var matchingCandidate = result.AllCandidates.FirstOrDefault(c => c.Position == candidate.center);
                if (matchingCandidate != null)
                {
                    matchingCandidate.HasBubblesAbove = hasBubbles;
                }

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
                else if (!isNearEdge)
                {
                    if (score < bestNoBubblesScore)
                    {
                        bestNoBubblesScore = score;
                        bestWithoutBubbles = candidate.center;
                        bestNoBubblesColor = candidate.color;
                    }
                }
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
                Logger.Debug("FishDetect", $"No candidates passed filters, using blob fallback");

                var centerX = (startX + endX) / 2;
                var centerY = (startY + endY) / 2;

                var bestFallbackBlob = allBlobs
                    .Where(b => b.Count >= 3)
                    .Select(b => {
                        int sumX = 0, sumY = 0;
                        foreach (var p in b) { sumX += p.X; sumY += p.Y; }
                        var center = new Point(sumX / b.Count, sumY / b.Count);
                        var distToCenter = Math.Sqrt(Math.Pow(center.X - centerX, 2) + Math.Pow(center.Y - centerY, 2));
                        return (blob: b, center: center, size: b.Count, distToCenter: distToCenter);
                    })
                    .OrderByDescending(x => x.size)
                    .ThenBy(x => x.distToCenter)
                    .FirstOrDefault();

                if (bestFallbackBlob.blob != null)
                {
                    bestBlob = bestFallbackBlob.center;
                    bestBlobColor = screenshot.GetPixel(
                        Math.Min(Math.Max(bestFallbackBlob.center.X, 0), screenshot.Width - 1),
                        Math.Min(Math.Max(bestFallbackBlob.center.Y, 0), screenshot.Height - 1));
                    result.HasBubblesAbove = false;

                    result.AllCandidates.Add(new FishCandidate
                    {
                        Position = bestFallbackBlob.center,
                        Color = bestBlobColor,
                        Size = bestFallbackBlob.size * step * step,
                        DistanceFromCenter = bestFallbackBlob.distToCenter,
                        CastPower = CalculateCastPower(bestFallbackBlob.center.X, bestFallbackBlob.center.Y, screenshot.Width, screenshot.Height),
                        HasBubblesAbove = false
                    });

                    Logger.Debug("FishDetect", $"Fallback blob at ({bestFallbackBlob.center.X},{bestFallbackBlob.center.Y}), size={bestFallbackBlob.size}");
                }
            }

            if (bestBlob.HasValue)
            {
                result.BestShadowPosition = bestBlob.Value;
                result.BestShadowColor = bestBlobColor;

                // Calculate casting destination
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
            Logger.Info("FishDetect", $"Starting NEW fish detection scan");

            // Get current game window info
            var windowRect = CoreFunctionality.GetGameWindowRect();
            if (windowRect.IsEmpty)
            {
                Logger.Warning("FishDetect", "Game window not found");
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
                scaledScanArea = new Rectangle(
                    customScanArea.Value.X + windowRect.X,
                    customScanArea.Value.Y + windowRect.Y,
                    customScanArea.Value.Width,
                    customScanArea.Value.Height
                );
                Logger.Debug("FishDetect", $"Using CUSTOM scan area: {scaledScanArea}");
            }
            else
            {
                scaledScanArea = new Rectangle(
                    (int)(_spotConfig.ScanArea.X * scaleX) + windowRect.X,
                    (int)(_spotConfig.ScanArea.Y * scaleY) + windowRect.Y,
                    (int)(_spotConfig.ScanArea.Width * scaleX),
                    (int)(_spotConfig.ScanArea.Height * scaleY)
                );
            }

            Logger.Debug("FishDetect", $"Scanning area: {scaledScanArea}, " +
                $"looking for color ({_spotConfig.BubbleColor.R},{_spotConfig.BubbleColor.G},{_spotConfig.BubbleColor.B})");

            Point? lastCoords = null;
            int stableCount = 0;
            const int requiredStableScans = 2;
            const int positionTolerance = 20;
            var startTime = DateTime.Now;

            // Scan and wait for fish to stop moving
            while ((DateTime.Now - startTime).TotalSeconds < MaxScanTimeSeconds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var newCoords = await ScanForBubbleAsync(scaledScanArea, scaleX, scaleY, cancellationToken);

                if (newCoords.HasValue)
                {
                    if (lastCoords.HasValue &&
                        Math.Abs(lastCoords.Value.X - newCoords.Value.X) <= positionTolerance &&
                        Math.Abs(lastCoords.Value.Y - newCoords.Value.Y) <= positionTolerance)
                    {
                        stableCount++;
                        Logger.Info("FishDetect", $"Fish stable at ({newCoords.Value.X}, {newCoords.Value.Y}) - count: {stableCount}/{requiredStableScans}");

                        if (stableCount >= requiredStableScans)
                        {
                            float refX = (newCoords.Value.X - windowRect.X) / scaleX + 20;
                            float refY = (newCoords.Value.Y - windowRect.Y) / scaleY + 20 + _spotConfig.YAdjustment;
                            var destCoords = CalculateCastingDestination((int)refX, (int)refY);

                            int screenCastX = (int)(destCoords.X * scaleX) + windowRect.X;
                            int screenCastY = (int)(destCoords.Y * scaleY) + windowRect.Y;
                            int screenRodX = (int)(RodButtonX * scaleX) + windowRect.X;
                            int screenRodY = (int)(RodButtonY * scaleY) + windowRect.Y;

                            Logger.Info("FishDetect", $"Fish stopped! Casting from ({screenRodX}, {screenRodY}) to ({screenCastX}, {screenCastY})");

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
                        stableCount = 0;
                        Logger.Debug("FishDetect", $"Fish moving... now at ({newCoords.Value.X}, {newCoords.Value.Y})");
                    }

                    lastCoords = newCoords;
                }
                else
                {
                    lastCoords = null;
                    stableCount = 0;
                    Logger.Warning("FishDetect", "No fish found in scan...");
                }

                await Task.Delay(200, cancellationToken);
            }

            Logger.Warning("FishDetect", "Timeout - no stable bubble found");
            return null;
        }

        /// <summary>
        /// Scans the specified area for fish shadows using computer vision (contrast detection).
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
                            Logger.Warning("FishDetect", "Failed to capture screenshot");
                            return null;
                        }

                        var windowOffset = CoreFunctionality.GetGameWindowOffset();

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
                            Logger.Info("FishDetect", $"Shadow detected at ({screenX}, {screenY})");
                            return new Point(screenX, screenY);
                        }

                        // Fallback to color matching if shadow detection fails
                        Logger.Debug("FishDetect", "Shadow detection failed, trying color match...");
                        return ScanForBubbleByColor(screenshot, sStartX, sStartY, sEndX, sEndY, windowOffset, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning("FishDetect", $"Scan error: {ex.Message}");
                }
                return (Point?)null;
            }, cancellationToken);
        }

        /// <summary>
        /// Detects fish shadows by finding dark blobs in the water area.
        /// Uses contrast-based detection to find areas darker than surroundings.
        /// </summary>
        private Point? DetectFishShadow(Bitmap screenshot, int startX, int startY, int endX, int endY)
        {
            const int step = 3;
            const int minBlobSize = 50;
            const int maxBlobSize = 2000;

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

            Logger.Debug("FishDetect", $"Avg brightness: {avgBrightness}");

            // Second pass: Find dark pixels that could be fish shadows
            int darkThreshold = Math.Max(10, avgBrightness - 25);
            var fishShadowPixels = new List<Point>();
            bool usingLearnedColor = LearnedShadowColor.HasValue && LearnedColorConfidence >= 1;

            for (int y = startY; y < endY; y += step)
            {
                for (int x = startX; x < endX; x += step)
                {
                    var color = screenshot.GetPixel(x, y);

                    if (_colorMatcher.IsPixelFishShadow(color, usingLearnedColor, darkThreshold))
                    {
                        fishShadowPixels.Add(new Point(x, y));
                    }
                }
            }

            Logger.Debug("FishDetect", $"Found {fishShadowPixels.Count} fish shadow colored pixels");

            if (fishShadowPixels.Count < minBlobSize / (step * step))
                return null;

            // Simple blob detection: Find clusters of fish shadow colored pixels
            var blobs = _shadowAnalyzer.FindBlobs(fishShadowPixels, step * 3);

            Logger.Debug("FishDetect", $"Found {blobs.Count} blobs");

            // Find the best blob that has bubbles above it (confirmed fish)
            float scaleX = (float)screenshot.Width / ReferenceWidth;
            float scaleY = (float)screenshot.Height / ReferenceHeight;
            Point? bestBlob = null;
            Color bestBlobColor = Color.Empty;

            // Collect all valid candidates first, then check for bubbles
            var candidates = new List<(Point center, Color color, int size, double castPower)>();

            foreach (var blob in blobs)
            {
                int blobSize = blob.Count * step * step;

                if (blobSize < minBlobSize || blobSize > maxBlobSize)
                    continue;

                if (!_shadowAnalyzer.IsCircularBlob(blob))
                    continue;

                // Calculate blob center
                int sumX = 0, sumY = 0;
                foreach (var p in blob)
                {
                    sumX += p.X;
                    sumY += p.Y;
                }
                Point blobCenter = new Point(sumX / blob.Count, sumY / blob.Count);

                if (!_shadowAnalyzer.IsSurroundedByWater(screenshot, blobCenter))
                    continue;

                Color blobColor = screenshot.GetPixel(blobCenter.X, blobCenter.Y);
                double castPower = CalculateCastPower(blobCenter.X, blobCenter.Y, screenshot.Width, screenshot.Height);

                candidates.Add((blobCenter, blobColor, blobSize, castPower));
            }

            Logger.Debug("FishDetect", $"{candidates.Count} candidates passed color check, now checking for bubbles...");

            // First pass: Look for shadows with bubbles above (confirmed fish)
            Point? bestWithBubbles = null;
            double bestBubblesScore = double.MaxValue;
            Color bestBubblesColor = Color.Empty;

            Point? bestWithoutBubbles = null;
            double bestNoBubblesScore = double.MaxValue;
            Color bestNoBubblesColor = Color.Empty;

            const int edgeMargin = 50;

            foreach (var candidate in candidates.OrderBy(c => c.castPower))
            {
                double score = candidate.castPower - candidate.size * 0.1;

                bool isNearEdge = candidate.center.X < startX + edgeMargin ||
                                  candidate.center.X > endX - edgeMargin ||
                                  candidate.center.Y < startY + edgeMargin ||
                                  candidate.center.Y > endY - edgeMargin;

                bool hasBubbles = _shadowAnalyzer.HasBubblesAbove(screenshot, candidate.center, avgBrightness);

                if (hasBubbles)
                {
                    if (score < bestBubblesScore)
                    {
                        bestBubblesScore = score;
                        bestWithBubbles = candidate.center;
                        bestBubblesColor = candidate.color;
                    }
                }
                else if (!isNearEdge)
                {
                    if (score < bestNoBubblesScore)
                    {
                        bestNoBubblesScore = score;
                        bestWithoutBubbles = candidate.center;
                        bestNoBubblesColor = candidate.color;
                    }
                }
            }

            if (bestWithBubbles.HasValue)
            {
                bestBlob = bestWithBubbles;
                bestBlobColor = bestBubblesColor;
                Logger.Info("FishDetect", $"CONFIRMED fish at ({bestBlob.Value.X}, {bestBlob.Value.Y}) - has bubbles above!");

                // AUTO-CALIBRATE: If we have high confidence (bubbles) and not yet calibrated, learn this color
                if (NeedsCalibration)
                {
                    LearnShadowColor(bestBlobColor);
                    Logger.Info("FishDetect", $"AUTO-CALIBRATED from confirmed fish: RGB({bestBlobColor.R},{bestBlobColor.G},{bestBlobColor.B})");
                }
            }
            else if (bestWithoutBubbles.HasValue)
            {
                bestBlob = bestWithoutBubbles;
                bestBlobColor = bestNoBubblesColor;
                Logger.Debug("FishDetect", $"Using shadow at ({bestBlob.Value.X}, {bestBlob.Value.Y}) - no bubbles detected but best candidate");
            }
            else
            {
                Logger.Warning("FishDetect", $"No valid fish shadows found");
            }

            return bestBlob;
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
                    _learnedColors[_currentLocationName] = (shadowColor, 1);
                    Logger.Info("FishDetect", $"Learned new shadow color for {_currentLocationName}: ({shadowColor.R},{shadowColor.G},{shadowColor.B})");
                }
                else
                {
                    var newColor = Color.FromArgb(
                        (existing.color.R + shadowColor.R) / 2,
                        (existing.color.G + shadowColor.G) / 2,
                        (existing.color.B + shadowColor.B) / 2
                    );
                    int newConfidence = Math.Min(existing.confidence + 1, 10);
                    _learnedColors[_currentLocationName] = (newColor, newConfidence);
                    Logger.Info("FishDetect", $"Updated learned color for {_currentLocationName} to ({newColor.R},{newColor.G},{newColor.B}), confidence={newConfidence}");
                }
            }
        }

        /// <summary>
        /// Scans for the learned shadow color with tolerance.
        /// </summary>
        private Point? ScanForLearnedColor(Bitmap screenshot, int startX, int startY, int endX, int endY)
        {
            if (!LearnedShadowColor.HasValue) return null;

            var targetColor = LearnedShadowColor.Value;
            var tolerance = new Tolerance(15, 15, 15);

            int step = 5;
            for (int y = startY; y < endY; y += step)
            {
                for (int x = startX; x < endX; x += step)
                {
                    var color = screenshot.GetPixel(x, y);
                    if (_colorMatcher.IsMatchingColor(color, targetColor, tolerance))
                    {
                        Logger.Debug("FishDetect", $"Learned color match at ({x}, {y})");
                        return new Point(x, y);
                    }
                }
            }
            return null;
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
                    if (_colorMatcher.IsMatchingColor(color, _spotConfig.BubbleColor, _spotConfig.ColorTolerance))
                    {
                        int screenX = x + windowOffset.X;
                        int screenY = y + windowOffset.Y;
                        Logger.Debug("FishDetect", $"Color match at ({screenX}, {screenY})");
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
            double factorX = 120.0 / 429.0;
            double factorY = 169.0 / 428.0;

            double yAdjustment = 0.75 + ((double)(BubbleRefY - bubbleY) / BubbleRefY) * 0.38;

            int destX = (int)(RodButtonX + factorX * (BubbleRefX - bubbleX) * yAdjustment);
            int destY = (int)(RodButtonY + factorY * (BubbleRefY - bubbleY));

            Logger.Debug("FishDetect", $"Cast calc: bubble({bubbleX},{bubbleY}) -> dest({destX},{destY})");

            return new Point(destX, destY);
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
            return LearnedShadowColor;
        }

        /// <summary>
        /// Gets the confidence level of the learned color (0-10).
        /// </summary>
        public int GetLearnedColorConfidence()
        {
            return LearnedColorConfidence;
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
            Logger.Info("FishDetect", $"Learned color reset for {_currentLocationName}");
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
            Logger.Info("FishDetect", "All learned colors reset");
        }

        /// <summary>
        /// Returns true if calibration is needed (no learned color yet).
        /// </summary>
        public bool NeedsCalibration => !LearnedShadowColor.HasValue || LearnedColorConfidence < 1;

        /// <summary>
        /// Gets the default scan area for the current location, scaled to the current window size.
        /// </summary>
        public Rectangle GetDefaultScanArea()
        {
            if (_spotConfig == null) return Rectangle.Empty;

            var windowRect = CoreFunctionality.GetGameWindowRect();
            if (windowRect.IsEmpty) return _spotConfig.ScanArea;

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
        /// </summary>
        public CastingResult CalculateCastFromPosition(int shadowX, int shadowY)
        {
            var windowRect = CoreFunctionality.GetGameWindowRect();
            if (windowRect.IsEmpty) return null;

            float scaleX = (float)windowRect.Width / ReferenceWidth;
            float scaleY = (float)windowRect.Height / ReferenceHeight;

            float refX = shadowX / scaleX;
            float refY = shadowY / scaleY + _spotConfig.YAdjustment;

            var destCoords = CalculateCastingDestination((int)refX, (int)refY);

            int screenDestX = (int)(destCoords.X * scaleX) + windowRect.X;
            int screenDestY = (int)(destCoords.Y * scaleY) + windowRect.Y;
            int screenRodX = (int)(RodButtonX * scaleX) + windowRect.X;
            int screenRodY = (int)(RodButtonY * scaleY) + windowRect.Y;

            Logger.Info("FishDetect", $"Cast: shadow({shadowX},{shadowY}) ref({refX:F0},{refY:F0}) -> screen dest({screenDestX},{screenDestY})");

            return new CastingResult
            {
                BubblePosition = new Point(shadowX, shadowY),
                RodButtonPosition = new Point(screenRodX, screenRodY),
                CastDestination = new Point(screenDestX, screenDestY)
            };
        }

        /// <summary>
        /// Calculates how much cast power (drag distance) is needed to reach a fish at the given position.
        /// </summary>
        public double CalculateCastPower(int fishX, int fishY, int screenshotWidth, int screenshotHeight)
        {
            float scaleX = (float)screenshotWidth / ReferenceWidth;
            float scaleY = (float)screenshotHeight / ReferenceHeight;

            float refX = fishX / scaleX;
            float refY = fishY / scaleY;

            double factorX = 120.0 / 429.0;
            double factorY = 169.0 / 428.0;
            double yAdjustment = 0.75 + ((double)(BubbleRefY - refY) / BubbleRefY) * 0.38;

            double destX = RodButtonX + factorX * (BubbleRefX - refX) * yAdjustment;
            double destY = RodButtonY + factorY * (BubbleRefY - refY);

            double dragX = destX - RodButtonX;
            double dragY = destY - RodButtonY;
            return Math.Sqrt(dragX * dragX + dragY * dragY);
        }

        /// <summary>
        /// Confirms that the detected shadow at the given position is a real fish.
        /// </summary>
        public void ConfirmFishShadow(Bitmap screenshot, Point shadowPosition)
        {
            if (screenshot == null) return;

            int x = Math.Min(Math.Max(shadowPosition.X, 0), screenshot.Width - 1);
            int y = Math.Min(Math.Max(shadowPosition.Y, 0), screenshot.Height - 1);
            Color shadowColor = screenshot.GetPixel(x, y);

            LearnShadowColor(shadowColor);
            Logger.Info("FishDetect", $"User confirmed fish at ({x},{y}) - learned color ({shadowColor.R},{shadowColor.G},{shadowColor.B})");
        }

        /// <summary>
        /// Confirms that the detected shadow is NOT a fish.
        /// </summary>
        public void RejectFishShadow()
        {
            Logger.Info("FishDetect", "User rejected detected shadow");
        }
    }
}
