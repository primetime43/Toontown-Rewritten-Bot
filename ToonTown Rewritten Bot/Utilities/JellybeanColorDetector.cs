using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Detects jellybean buttons by their distinct colors.
    /// This is more reliable than template matching for colored buttons.
    /// </summary>
    public static class JellybeanColorDetector
    {
        /// <summary>
        /// Color definitions for each jellybean type.
        /// Based on the in-game flower planting UI colors in Toontown Rewritten.
        /// Colors are approximated from the jellybean button icons.
        /// </summary>
        public static readonly Dictionary<char, JellybeanColor> BeanColors = new Dictionary<char, JellybeanColor>
        {
            // Red jellybean - bright red
            { 'r', new JellybeanColor("Red", Color.FromArgb(220, 50, 50), 60) },
            // Green jellybean - bright green
            { 'g', new JellybeanColor("Green", Color.FromArgb(50, 200, 50), 60) },
            // Orange jellybean - orange/amber
            { 'o', new JellybeanColor("Orange", Color.FromArgb(255, 150, 50), 60) },
            // Purple jellybean - violet/purple
            { 'u', new JellybeanColor("Purple", Color.FromArgb(150, 50, 200), 60) },
            // Blue jellybean - bright blue
            { 'b', new JellybeanColor("Blue", Color.FromArgb(50, 100, 220), 60) },
            // Pink jellybean - light pink
            { 'i', new JellybeanColor("Pink", Color.FromArgb(255, 150, 200), 60) },
            // Yellow jellybean - bright yellow
            { 'y', new JellybeanColor("Yellow", Color.FromArgb(255, 230, 50), 60) },
            // Cyan jellybean - light blue/cyan
            { 'c', new JellybeanColor("Cyan", Color.FromArgb(50, 220, 220), 60) },
            // Silver jellybean - gray/white
            { 's', new JellybeanColor("Silver", Color.FromArgb(180, 180, 180), 50) },
        };

        /// <summary>
        /// Represents a jellybean's color profile.
        /// </summary>
        public class JellybeanColor
        {
            public string Name { get; }
            public Color TargetColor { get; }
            public int Tolerance { get; }

            public JellybeanColor(string name, Color targetColor, int tolerance)
            {
                Name = name;
                TargetColor = targetColor;
                Tolerance = tolerance;
            }

            public bool Matches(Color pixel)
            {
                return Math.Abs(pixel.R - TargetColor.R) <= Tolerance &&
                       Math.Abs(pixel.G - TargetColor.G) <= Tolerance &&
                       Math.Abs(pixel.B - TargetColor.B) <= Tolerance;
            }
        }

        /// <summary>
        /// Finds a jellybean button by its color in the screenshot.
        /// Uses the calibrated scan area if available.
        /// </summary>
        /// <param name="screenshot">Screenshot of the game window</param>
        /// <param name="beanType">Bean type character (r, g, o, u, b, i, y, c, s)</param>
        /// <param name="searchRegion">Optional region to search within (null = use calibrated area or full image)</param>
        /// <returns>Center point of the bean button if found, null otherwise</returns>
        public static Point? FindBeanByColor(Bitmap screenshot, char beanType, Rectangle? searchRegion = null)
        {
            if (!BeanColors.TryGetValue(beanType, out var beanColor))
            {
                System.Diagnostics.Debug.WriteLine($"[JellybeanColorDetector] Unknown bean type: {beanType}");
                return null;
            }

            // Use calibrated scan area if no explicit region provided
            Rectangle? region = searchRegion;
            if (!region.HasValue && GardeningScanAreaManager.HasCustomScanArea())
            {
                region = GardeningScanAreaManager.GetJellybeanPanelArea(screenshot.Width, screenshot.Height);
                System.Diagnostics.Debug.WriteLine($"[JellybeanColorDetector] Using calibrated scan area: {region}");
            }

            return FindColorCluster(screenshot, beanColor, region);
        }

        /// <summary>
        /// Finds a cluster of matching color pixels and returns the center.
        /// </summary>
        private static Point? FindColorCluster(Bitmap screenshot, JellybeanColor beanColor, Rectangle? searchRegion = null)
        {
            Rectangle region = searchRegion ?? new Rectangle(0, 0, screenshot.Width, screenshot.Height);

            // Ensure region is within bounds
            region.Intersect(new Rectangle(0, 0, screenshot.Width, screenshot.Height));
            if (region.Width <= 0 || region.Height <= 0)
                return null;

            // Get pixel data for faster access
            var bitmapData = screenshot.LockBits(region, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride = bitmapData.Stride;
                byte[] pixelData = new byte[stride * region.Height];
                Marshal.Copy(bitmapData.Scan0, pixelData, 0, pixelData.Length);

                // Find all matching pixels
                List<Point> matchingPixels = new List<Point>();
                int minClusterSize = 100; // Minimum pixels to be considered a jellybean button

                for (int y = 0; y < region.Height; y++)
                {
                    for (int x = 0; x < region.Width; x++)
                    {
                        int index = (y * stride) + (x * 4);
                        Color pixel = Color.FromArgb(
                            pixelData[index + 3], // A
                            pixelData[index + 2], // R
                            pixelData[index + 1], // G
                            pixelData[index]);    // B

                        if (beanColor.Matches(pixel))
                        {
                            matchingPixels.Add(new Point(region.X + x, region.Y + y));
                        }
                    }
                }

                if (matchingPixels.Count < minClusterSize)
                {
                    System.Diagnostics.Debug.WriteLine($"[JellybeanColorDetector] {beanColor.Name}: Only found {matchingPixels.Count} pixels (need {minClusterSize})");
                    return null;
                }

                // Find the largest cluster using simple bounding box
                // For jellybeans, we expect a roughly circular cluster
                var clusters = FindClusters(matchingPixels, 20); // 20 pixel gap tolerance

                if (clusters.Count == 0)
                    return null;

                // Get the largest cluster
                var largestCluster = clusters[0];
                foreach (var cluster in clusters)
                {
                    if (cluster.Count > largestCluster.Count)
                        largestCluster = cluster;
                }

                // Calculate center of the cluster
                int sumX = 0, sumY = 0;
                foreach (var point in largestCluster)
                {
                    sumX += point.X;
                    sumY += point.Y;
                }

                Point center = new Point(sumX / largestCluster.Count, sumY / largestCluster.Count);
                System.Diagnostics.Debug.WriteLine($"[JellybeanColorDetector] {beanColor.Name}: Found at ({center.X}, {center.Y}) with {largestCluster.Count} pixels");
                return center;
            }
            finally
            {
                screenshot.UnlockBits(bitmapData);
            }
        }

        /// <summary>
        /// Groups nearby pixels into clusters.
        /// </summary>
        private static List<List<Point>> FindClusters(List<Point> points, int maxGap)
        {
            var clusters = new List<List<Point>>();
            var visited = new bool[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                if (visited[i])
                    continue;

                var cluster = new List<Point>();
                var queue = new Queue<int>();
                queue.Enqueue(i);
                visited[i] = true;

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    cluster.Add(points[current]);

                    // Find nearby unvisited points
                    for (int j = 0; j < points.Count; j++)
                    {
                        if (visited[j])
                            continue;

                        int dx = Math.Abs(points[current].X - points[j].X);
                        int dy = Math.Abs(points[current].Y - points[j].Y);

                        if (dx <= maxGap && dy <= maxGap)
                        {
                            visited[j] = true;
                            queue.Enqueue(j);
                        }
                    }
                }

                if (cluster.Count >= 50) // Minimum cluster size
                    clusters.Add(cluster);
            }

            return clusters;
        }

        /// <summary>
        /// Finds all jellybean buttons in the screenshot.
        /// Useful for calibration and debugging.
        /// </summary>
        public static Dictionary<char, Point> FindAllBeans(Bitmap screenshot)
        {
            var results = new Dictionary<char, Point>();

            foreach (var kvp in BeanColors)
            {
                var location = FindBeanByColor(screenshot, kvp.Key);
                if (location.HasValue)
                {
                    results[kvp.Key] = location.Value;
                }
            }

            return results;
        }

        /// <summary>
        /// Adjusts color definitions based on a calibration screenshot.
        /// Call this with a screenshot of the jellybean UI visible.
        /// </summary>
        public static void CalibrateColors(Bitmap screenshot, Point knownRedLocation)
        {
            // Sample the actual color at known locations to adjust tolerances
            // This is optional - the default colors should work for most setups
            System.Diagnostics.Debug.WriteLine("[JellybeanColorDetector] Calibration not yet implemented - using default colors");
        }

        /// <summary>
        /// Saves a debug screenshot with detected bean locations marked.
        /// Useful for troubleshooting detection issues.
        /// </summary>
        public static void SaveDebugScreenshot(Bitmap screenshot, string outputPath)
        {
            try
            {
                using (var debugImage = new Bitmap(screenshot))
                using (var graphics = Graphics.FromImage(debugImage))
                {
                    var beans = FindAllBeans(screenshot);

                    foreach (var bean in beans)
                    {
                        var color = BeanColors[bean.Key];
                        using (var pen = new Pen(color.TargetColor, 3))
                        using (var brush = new SolidBrush(Color.FromArgb(128, color.TargetColor)))
                        {
                            // Draw a circle around the detected location
                            graphics.DrawEllipse(pen, bean.Value.X - 20, bean.Value.Y - 20, 40, 40);
                            graphics.FillEllipse(brush, bean.Value.X - 5, bean.Value.Y - 5, 10, 10);

                            // Draw label
                            graphics.DrawString(color.Name, SystemFonts.DefaultFont, Brushes.White,
                                bean.Value.X + 25, bean.Value.Y - 10);
                        }
                    }

                    debugImage.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
                    System.Diagnostics.Debug.WriteLine($"[JellybeanColorDetector] Debug screenshot saved to: {outputPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JellybeanColorDetector] Failed to save debug screenshot: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests jellybean detection and saves a debug image.
        /// Returns true if at least some beans were detected.
        /// </summary>
        public static bool TestDetection(string debugOutputPath = null)
        {
            try
            {
                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                {
                    var beans = FindAllBeans(screenshot);

                    System.Diagnostics.Debug.WriteLine($"[JellybeanColorDetector] Test results: Found {beans.Count} jellybeans");
                    foreach (var bean in beans)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - {BeanColors[bean.Key].Name}: ({bean.Value.X}, {bean.Value.Y})");
                    }

                    if (!string.IsNullOrEmpty(debugOutputPath))
                    {
                        SaveDebugScreenshot(screenshot, debugOutputPath);
                    }

                    return beans.Count > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JellybeanColorDetector] Test failed: {ex.Message}");
                return false;
            }
        }
    }
}
