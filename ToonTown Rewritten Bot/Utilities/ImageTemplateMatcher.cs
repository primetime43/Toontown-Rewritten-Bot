using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Provides template matching functionality to find images within screenshots.
    /// Uses a custom implementation with System.Drawing for finding buttons/icons.
    /// </summary>
    public class ImageTemplateMatcher
    {
        /// <summary>
        /// Result of a template match operation.
        /// </summary>
        public class MatchResult
        {
            public bool Found { get; set; }
            public Point Location { get; set; }
            public Point Center { get; set; }
            public double Confidence { get; set; }
            public Rectangle Bounds { get; set; }
        }

        /// <summary>
        /// Finds a template image within a larger source image.
        /// </summary>
        /// <param name="source">The source image (screenshot) to search in</param>
        /// <param name="template">The template image to find</param>
        /// <param name="threshold">Match threshold 0.0-1.0 (higher = stricter match, default 0.9)</param>
        /// <param name="cancellationToken">Optional cancellation token to stop the search</param>
        /// <param name="progressCallback">Optional callback for progress updates (0-100)</param>
        /// <returns>MatchResult with location if found</returns>
        public static MatchResult FindTemplate(Bitmap source, Bitmap template, double threshold = 0.9,
            CancellationToken cancellationToken = default, Action<int> progressCallback = null)
        {
            if (source == null || template == null)
                return new MatchResult { Found = false };

            if (template.Width > source.Width || template.Height > source.Height)
                return new MatchResult { Found = false };

            var sourceData = GetPixelData(source);
            var templateData = GetPixelData(template);

            double bestMatch = 0;
            Point bestLocation = Point.Empty;

            int searchWidth = source.Width - template.Width;
            int searchHeight = source.Height - template.Height;
            int lastReportedProgress = -1;

            // Search through the source image
            for (int y = 0; y <= searchHeight; y++)
            {
                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    return new MatchResult { Found = false, Confidence = bestMatch };
                }

                // Report progress
                int progress = (int)((y * 100.0) / searchHeight);
                if (progress != lastReportedProgress)
                {
                    lastReportedProgress = progress;
                    progressCallback?.Invoke(progress);
                }

                for (int x = 0; x <= searchWidth; x++)
                {
                    // Quick pre-check using key points - skip if obviously not a match
                    if (!QuickMatchCheck(sourceData, templateData, source.Width, template.Width, template.Height, x, y))
                    {
                        continue;
                    }

                    // Full match calculation with early termination
                    double match = CalculateMatch(sourceData, templateData,
                        source.Width, template.Width, template.Height, x, y, threshold);

                    if (match > bestMatch)
                    {
                        bestMatch = match;
                        bestLocation = new Point(x, y);

                        // Early exit if we found a very good match
                        if (match >= 0.99)
                            break;
                    }
                }
                if (bestMatch >= 0.99)
                    break;
            }

            progressCallback?.Invoke(100);

            bool found = bestMatch >= threshold;
            return new MatchResult
            {
                Found = found,
                Location = found ? bestLocation : Point.Empty,
                Center = found ? new Point(bestLocation.X + template.Width / 2,
                                          bestLocation.Y + template.Height / 2) : Point.Empty,
                Confidence = bestMatch,
                Bounds = found ? new Rectangle(bestLocation, template.Size) : Rectangle.Empty
            };
        }

        /// <summary>
        /// Finds a template image within a larger source image using file paths.
        /// </summary>
        public static MatchResult FindTemplate(Bitmap source, string templatePath, double threshold = 0.9)
        {
            if (!File.Exists(templatePath))
                return new MatchResult { Found = false };

            using (var template = new Bitmap(templatePath))
            {
                return FindTemplate(source, template, threshold);
            }
        }

        /// <summary>
        /// Finds all occurrences of a template in the source image.
        /// </summary>
        /// <param name="source">The source image to search in</param>
        /// <param name="template">The template image to find</param>
        /// <param name="threshold">Match threshold 0.0-1.0</param>
        /// <param name="minDistance">Minimum distance between matches to avoid duplicates</param>
        /// <param name="cancellationToken">Optional cancellation token to stop the search</param>
        /// <param name="progressCallback">Optional callback for progress updates (0-100)</param>
        /// <returns>List of all match results</returns>
        public static List<MatchResult> FindAllTemplates(Bitmap source, Bitmap template,
            double threshold = 0.9, int minDistance = 10,
            CancellationToken cancellationToken = default, Action<int> progressCallback = null)
        {
            var results = new List<MatchResult>();

            if (source == null || template == null)
                return results;

            if (template.Width > source.Width || template.Height > source.Height)
                return results;

            var sourceData = GetPixelData(source);
            var templateData = GetPixelData(template);

            int searchWidth = source.Width - template.Width;
            int searchHeight = source.Height - template.Height;
            int lastReportedProgress = -1;

            for (int y = 0; y <= searchHeight; y++)
            {
                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    return results; // Return whatever we found so far
                }

                // Report progress
                int progress = (int)((y * 100.0) / searchHeight);
                if (progress != lastReportedProgress)
                {
                    lastReportedProgress = progress;
                    progressCallback?.Invoke(progress);
                }

                for (int x = 0; x <= searchWidth; x++)
                {
                    // Quick pre-check using key points - skip if obviously not a match
                    if (!QuickMatchCheck(sourceData, templateData, source.Width, template.Width, template.Height, x, y))
                    {
                        continue;
                    }

                    // Full match calculation with early termination
                    double match = CalculateMatch(sourceData, templateData,
                        source.Width, template.Width, template.Height, x, y, threshold);

                    if (match >= threshold)
                    {
                        // Check if this location is too close to an existing match
                        bool tooClose = false;
                        foreach (var existing in results)
                        {
                            int dx = Math.Abs(existing.Location.X - x);
                            int dy = Math.Abs(existing.Location.Y - y);
                            if (dx < minDistance && dy < minDistance)
                            {
                                tooClose = true;
                                // Keep the better match
                                if (match > existing.Confidence)
                                {
                                    existing.Location = new Point(x, y);
                                    existing.Center = new Point(x + template.Width / 2,
                                                               y + template.Height / 2);
                                    existing.Confidence = match;
                                    existing.Bounds = new Rectangle(new Point(x, y), template.Size);
                                }
                                break;
                            }
                        }

                        if (!tooClose)
                        {
                            results.Add(new MatchResult
                            {
                                Found = true,
                                Location = new Point(x, y),
                                Center = new Point(x + template.Width / 2, y + template.Height / 2),
                                Confidence = match,
                                Bounds = new Rectangle(new Point(x, y), template.Size)
                            });
                        }
                    }
                }
            }

            progressCallback?.Invoke(100);
            return results;
        }

        /// <summary>
        /// Checks if a template exists in the source image.
        /// </summary>
        public static bool TemplateExists(Bitmap source, Bitmap template, double threshold = 0.9)
        {
            return FindTemplate(source, template, threshold).Found;
        }

        /// <summary>
        /// Checks if a template exists in the source image using file path.
        /// </summary>
        public static bool TemplateExists(Bitmap source, string templatePath, double threshold = 0.9)
        {
            return FindTemplate(source, templatePath, threshold).Found;
        }

        /// <summary>
        /// Gets the center point of a template if found in the source image.
        /// Useful for clicking on buttons/icons.
        /// </summary>
        public static Point? GetTemplateCenter(Bitmap source, Bitmap template, double threshold = 0.9)
        {
            var result = FindTemplate(source, template, threshold);
            return result.Found ? result.Center : null;
        }

        /// <summary>
        /// Waits for a template to appear on screen (captures new screenshots).
        /// </summary>
        /// <param name="template">Template to find</param>
        /// <param name="timeoutMs">Maximum time to wait in milliseconds</param>
        /// <param name="checkIntervalMs">How often to check in milliseconds</param>
        /// <param name="threshold">Match threshold</param>
        /// <returns>MatchResult if found, or not found result if timeout</returns>
        public static MatchResult WaitForTemplate(Bitmap template, int timeoutMs = 5000,
            int checkIntervalMs = 100, double threshold = 0.9)
        {
            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
            {
                try
                {
                    using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                    {
                        var result = FindTemplate(screenshot, template, threshold);
                        if (result.Found)
                            return result;
                    }
                }
                catch
                {
                    // Window might not be available, continue waiting
                }

                System.Threading.Thread.Sleep(checkIntervalMs);
            }

            return new MatchResult { Found = false };
        }

        #region Private Helper Methods

        private static byte[] GetPixelData(Bitmap bitmap)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int bytes = Math.Abs(bitmapData.Stride) * bitmap.Height;
            byte[] pixelData = new byte[bytes];

            Marshal.Copy(bitmapData.Scan0, pixelData, 0, bytes);
            bitmap.UnlockBits(bitmapData);

            return pixelData;
        }

        private static double CalculateMatch(byte[] source, byte[] template,
            int sourceWidth, int templateWidth, int templateHeight, int offsetX, int offsetY,
            double threshold = 0.0)
        {
            int sourceStride = sourceWidth * 4;
            int templateStride = templateWidth * 4;
            int totalPixels = templateWidth * templateHeight;

            // For early termination: calculate max allowed difference based on threshold
            long maxAllowedDiff = (long)((1.0 - threshold) * totalPixels * 255 * 3);
            long totalDiff = 0;
            int pixelsChecked = 0;

            // Check pixels - use sampling for large templates
            int stepX = templateWidth > 50 ? 2 : 1;
            int stepY = templateHeight > 50 ? 2 : 1;
            int sampledPixels = 0;

            for (int y = 0; y < templateHeight; y += stepY)
            {
                for (int x = 0; x < templateWidth; x += stepX)
                {
                    int sourceIndex = ((offsetY + y) * sourceStride) + ((offsetX + x) * 4);
                    int templateIndex = (y * templateStride) + (x * 4);

                    // Compare RGB values (skip alpha)
                    int diffB = Math.Abs(source[sourceIndex] - template[templateIndex]);
                    int diffG = Math.Abs(source[sourceIndex + 1] - template[templateIndex + 1]);
                    int diffR = Math.Abs(source[sourceIndex + 2] - template[templateIndex + 2]);

                    totalDiff += diffR + diffG + diffB;
                    sampledPixels++;
                    pixelsChecked++;

                    // Early termination - check every 100 pixels if we're way off
                    if (pixelsChecked % 100 == 0 && threshold > 0)
                    {
                        // Estimate final diff based on current rate
                        double estimatedTotalDiff = (double)totalDiff / sampledPixels * totalPixels;
                        if (estimatedTotalDiff > maxAllowedDiff * 1.5) // 1.5x buffer for estimation error
                        {
                            return 0.0; // Early exit - definitely won't match
                        }
                    }
                }
            }

            // Calculate match based on sampled pixels
            long maxDiff = (long)sampledPixels * 255 * 3;
            return 1.0 - ((double)totalDiff / maxDiff);
        }

        // Fast pre-check using corners and center
        private static bool QuickMatchCheck(byte[] source, byte[] template,
            int sourceWidth, int templateWidth, int templateHeight, int offsetX, int offsetY,
            int tolerance = 50)
        {
            int sourceStride = sourceWidth * 4;
            int templateStride = templateWidth * 4;

            // Check 5 key points: 4 corners + center
            int[][] checkPoints = new int[][]
            {
                new int[] { 0, 0 },                                          // Top-left
                new int[] { templateWidth - 1, 0 },                           // Top-right
                new int[] { 0, templateHeight - 1 },                          // Bottom-left
                new int[] { templateWidth - 1, templateHeight - 1 },          // Bottom-right
                new int[] { templateWidth / 2, templateHeight / 2 }           // Center
            };

            foreach (var point in checkPoints)
            {
                int x = point[0];
                int y = point[1];

                int sourceIndex = ((offsetY + y) * sourceStride) + ((offsetX + x) * 4);
                int templateIndex = (y * templateStride) + (x * 4);

                int diffB = Math.Abs(source[sourceIndex] - template[templateIndex]);
                int diffG = Math.Abs(source[sourceIndex + 1] - template[templateIndex + 1]);
                int diffR = Math.Abs(source[sourceIndex + 2] - template[templateIndex + 2]);

                // If any key point differs too much, skip this location
                if (diffR > tolerance || diffG > tolerance || diffB > tolerance)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
