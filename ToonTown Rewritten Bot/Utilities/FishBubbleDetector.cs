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
        private const int MaxScanTimeSeconds = 36;

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

            Point? oldCoords = null;
            int coordsMatchCounter = 0;
            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalSeconds < MaxScanTimeSeconds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var newCoords = await ScanForBubbleAsync(scaledScanArea, scaleX, scaleY, cancellationToken);

                if (!newCoords.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine("[FishBubbleDetector] No bubble found in scan...");
                }

                if (newCoords.HasValue && oldCoords.HasValue &&
                    Math.Abs(oldCoords.Value.X - newCoords.Value.X) <= ScanStep &&
                    Math.Abs(oldCoords.Value.Y - newCoords.Value.Y) <= ScanStep)
                {
                    coordsMatchCounter++;
                }
                else
                {
                    oldCoords = newCoords;
                    coordsMatchCounter = 0;
                }

                // Calculate destination coordinates using the casting formula
                Point destCoords;
                if (!newCoords.HasValue)
                {
                    // Default destination if no bubble found
                    destCoords = new Point(800, 1009);
                }
                else
                {
                    // Convert screen coords back to reference coords
                    float refX = (newCoords.Value.X - windowRect.X) / scaleX + 20; // +20 offset from MouseClickSimulator
                    float refY = (newCoords.Value.Y - windowRect.Y) / scaleY + 20 + _spotConfig.YAdjustment;

                    // Apply the casting formula
                    destCoords = CalculateCastingDestination((int)refX, (int)refY);
                }

                // Scale destination back to screen coordinates
                int screenCastX = (int)(destCoords.X * scaleX) + windowRect.X;
                int screenCastY = (int)(destCoords.Y * scaleY) + windowRect.Y;

                // Calculate rod button screen position
                int screenRodX = (int)(RodButtonX * scaleX) + windowRect.X;
                int screenRodY = (int)(RodButtonY * scaleY) + windowRect.Y;

                // If we found stable coordinates twice, return the result
                if (coordsMatchCounter >= 2)
                {
                    System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Bubble confirmed! Cast from ({screenRodX}, {screenRodY}) to ({screenCastX}, {screenCastY})");

                    return new CastingResult
                    {
                        BubblePosition = newCoords ?? new Point(0, 0),
                        RodButtonPosition = new Point(screenRodX, screenRodY),
                        CastDestination = new Point(screenCastX, screenCastY)
                    };
                }

                await Task.Delay(500, cancellationToken);
            }

            System.Diagnostics.Debug.WriteLine("[FishBubbleDetector] Timeout - no stable bubble found");
            return null;
        }

        /// <summary>
        /// Scans the specified area for fish bubble colors using a screenshot for consistency.
        /// </summary>
        private async Task<Point?> ScanForBubbleAsync(Rectangle scanArea, float scaleX, float scaleY, CancellationToken cancellationToken)
        {
            // Use smaller step for better detection (step of 5 pixels)
            int step = 5;

            return await Task.Run(() =>
            {
                try
                {
                    // Take a screenshot for consistent scanning (fish shadows move/animate)
                    using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                    {
                        if (screenshot == null)
                        {
                            System.Diagnostics.Debug.WriteLine("[FishBubbleDetector] Failed to capture screenshot");
                            return null;
                        }

                        // Convert screen coordinates to window-relative for the screenshot
                        var windowOffset = CoreFunctionality.GetGameWindowOffset();
                        int startX = Math.Max(0, scanArea.X - windowOffset.X);
                        int startY = Math.Max(0, scanArea.Y - windowOffset.Y);
                        int endX = Math.Min(screenshot.Width, startX + scanArea.Width);
                        int endY = Math.Min(screenshot.Height, startY + scanArea.Height);

                        System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Scanning screenshot region ({startX},{startY}) to ({endX},{endY})");

                        for (int y = startY; y < endY; y += step)
                        {
                            for (int x = startX; x < endX; x += step)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                    return null;

                                var color = screenshot.GetPixel(x, y);
                                if (IsMatchingColor(color, _spotConfig.BubbleColor, _spotConfig.ColorTolerance))
                                {
                                    // Convert back to screen coordinates
                                    int screenX = x + windowOffset.X;
                                    int screenY = y + windowOffset.Y;
                                    System.Diagnostics.Debug.WriteLine($"[FishBubbleDetector] Found bubble at ({screenX}, {screenY}) - color ({color.R},{color.G},{color.B})");
                                    return new Point(screenX, screenY);
                                }
                            }
                        }
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
