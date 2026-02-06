using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Computer vision primitives for fish shadow detection.
    /// Handles blob detection, shape analysis, water surroundings check, bubble detection, and pond area detection.
    /// </summary>
    internal class FishShadowAnalyzer
    {
        private readonly FishColorMatcher _colorMatcher;

        public FishShadowAnalyzer(FishColorMatcher colorMatcher)
        {
            _colorMatcher = colorMatcher;
        }

        /// <summary>
        /// Checks if a blob shape is roughly circular (like a fish shadow).
        /// Fish shadows are oval/circular, not long thin lines or irregular shapes.
        /// </summary>
        public bool IsCircularBlob(List<Point> blob)
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
                System.Diagnostics.Debug.WriteLine($"[FishShadowAnalyzer] Blob rejected: bad aspect ratio {aspectRatio:F2} ({blobWidth}x{blobHeight})");
                return false;
            }

            // Check compactness - how much of the bounding box is filled
            // A circle fills about 78.5% of its bounding box (pi/4)
            // Fish shadows should fill at least 30% of their bounding box
            float boundingArea = blobWidth * blobHeight;
            float fillRatio = (blob.Count * 9) / boundingArea; // *9 because we scan every 3rd pixel

            if (fillRatio < 0.2f)
            {
                System.Diagnostics.Debug.WriteLine($"[FishShadowAnalyzer] Blob rejected: sparse/not compact (fill={fillRatio:F2})");
                return false;
            }

            // Minimum size check - fish shadows should be reasonably sized
            if (blobWidth < 15 || blobHeight < 15)
            {
                System.Diagnostics.Debug.WriteLine($"[FishShadowAnalyzer] Blob rejected: too small ({blobWidth}x{blobHeight})");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a blob is surrounded by water-colored pixels.
        /// This helps reject text labels which are surrounded by gray UI backgrounds.
        /// </summary>
        public bool IsSurroundedByWater(Bitmap screenshot, Point blobCenter, int checkRadius = 30)
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
                    if (_colorMatcher.IsWaterColor(color))
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
                System.Diagnostics.Debug.WriteLine($"[FishShadowAnalyzer] Blob at ({blobCenter.X},{blobCenter.Y}) rejected: not surrounded by water ({waterRatio:P0})");
            }

            return surrounded;
        }

        /// <summary>
        /// Groups nearby points into blobs using simple clustering.
        /// </summary>
        public List<List<Point>> FindBlobs(List<Point> points, int maxDistance)
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
        /// Checks for bubbles above a shadow position to verify it's a fish.
        /// Fish in TTR have characteristic white/light bubbles rising above their shadow.
        /// </summary>
        public bool HasBubblesAbove(Bitmap screenshot, Point shadowCenter, int avgWaterBrightness)
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
            System.Diagnostics.Debug.WriteLine($"[FishShadowAnalyzer] Bubble check at ({shadowCenter.X},{shadowCenter.Y}): " +
                $"found {bubblePixelCount} bubble pixels (threshold={bubbleThreshold}, need {minBubblePixels}) -> {(hasBubbles ? "HAS BUBBLES" : "no bubbles")}");

            return hasBubbles;
        }

        /// <summary>
        /// Dynamically detects the pond/water area in the screenshot.
        /// Uses color detection to find the teal/cyan water region.
        /// </summary>
        public Rectangle DetectPondArea(Bitmap screenshot)
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

                    if (_colorMatcher.IsWaterColor(color))
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
                System.Diagnostics.Debug.WriteLine($"[FishShadowAnalyzer] Pond detection: insufficient water pixels ({waterPixelCount})");
                return Rectangle.Empty;
            }

            // Add small padding but stay within bounds
            int padding = 15;
            minX = Math.Max(sideMargin, minX - padding);
            minY = Math.Max(topMargin, minY - padding);
            maxX = Math.Min(width - sideMargin, maxX + padding);
            maxY = Math.Min(height - bottomMargin, maxY + padding);

            var pondRect = new Rectangle(minX, minY, maxX - minX, maxY - minY);

            System.Diagnostics.Debug.WriteLine($"[FishShadowAnalyzer] Pond detected: ({pondRect.X}, {pondRect.Y}) - {pondRect.Width}x{pondRect.Height} ({waterPixelCount} water pixels)");

            return pondRect;
        }
    }
}
