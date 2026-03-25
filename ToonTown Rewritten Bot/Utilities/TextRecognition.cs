using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tesseract;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Provides OCR (Optical Character Recognition) functionality using Tesseract.
    /// Used for reading text and numbers from the game window.
    /// </summary>
    public class TextRecognition : IDisposable
    {
        private TesseractEngine _engine;
        private bool _disposed = false;
        private static readonly string DefaultTessDataPath = Path.Combine(
            AppPaths.ExeDirectory, "tessdata");

        /// <summary>
        /// Initializes the OCR engine with the specified language.
        /// Automatically downloads trained data if not present.
        /// </summary>
        /// <param name="tessDataPath">Path to tessdata folder (null for default)</param>
        /// <param name="language">Language code (default: "eng" for English)</param>
        public TextRecognition(string tessDataPath = null, string language = "eng")
        {
            string dataPath = tessDataPath ?? DefaultTessDataPath;

            // Auto-download trained data if it doesn't exist
            if (!TessdataDownloader.LanguageDataExists(language, dataPath))
            {
                System.Diagnostics.Debug.WriteLine($"OCR data not found, downloading automatically...");

                bool downloaded = TessdataDownloader.EnsureLanguageDataExists(language, dataPath);

                if (!downloaded)
                {
                    throw new FileNotFoundException(
                        $"Failed to download OCR trained data for language '{language}'.\n" +
                        "Please check your internet connection and try again.");
                }
            }

            // Check for native Tesseract DLLs before attempting to load
            VerifyNativeDlls();

            try
            {
                _engine = new TesseractEngine(dataPath, language, EngineMode.Default);
            }
            catch (Exception ex)
            {
                // Unwrap TargetInvocationException to show the real cause
                var innerEx = ex;
                while (innerEx is System.Reflection.TargetInvocationException && innerEx.InnerException != null)
                {
                    innerEx = innerEx.InnerException;
                }

                throw new InvalidOperationException(
                    $"Failed to initialize Tesseract OCR engine: {innerEx.Message}\n\n" +
                    "Make sure the x64 folder containing tesseract50.dll and leptonica-1.82.0.dll " +
                    "is in the same directory as the executable.",
                    innerEx);
            }
        }

        /// <summary>
        /// Creates a TextRecognition instance asynchronously, downloading data if needed.
        /// </summary>
        public static async Task<TextRecognition> CreateAsync(string tessDataPath = null, string language = "eng")
        {
            string dataPath = tessDataPath ?? DefaultTessDataPath;

            // Auto-download trained data if it doesn't exist
            if (!TessdataDownloader.LanguageDataExists(language, dataPath))
            {
                bool downloaded = await TessdataDownloader.EnsureLanguageDataExistsAsync(language, dataPath);

                if (!downloaded)
                {
                    throw new FileNotFoundException(
                        $"Failed to download OCR trained data for language '{language}'.\n" +
                        "Please check your internet connection and try again.");
                }
            }

            return new TextRecognition(dataPath, language);
        }

        /// <summary>
        /// Reads all text from an image.
        /// </summary>
        /// <param name="image">The image to read text from</param>
        /// <param name="preprocess">Whether to apply preprocessing for better results</param>
        /// <returns>Recognized text</returns>
        public string ReadText(Bitmap image, bool preprocess = true)
        {
            if (image == null) return string.Empty;

            Bitmap processedImage = preprocess ? PreprocessForGameOCR(image) : image;

            try
            {
                using (var pix = BitmapToPix(processedImage))
                using (var page = _engine.Process(pix))
                {
                    return page.GetText().Trim();
                }
            }
            finally
            {
                if (preprocess && processedImage != image)
                    processedImage?.Dispose();
            }
        }

        /// <summary>
        /// Reads text from a specific region of an image.
        /// </summary>
        /// <param name="image">The source image</param>
        /// <param name="region">The region to read from</param>
        /// <param name="preprocess">Whether to apply preprocessing for better results</param>
        /// <returns>Recognized text</returns>
        public string ReadTextFromRegion(Bitmap image, Rectangle region, bool preprocess = true)
        {
            if (image == null) return string.Empty;

            // Ensure region is within bounds
            region.Intersect(new Rectangle(0, 0, image.Width, image.Height));
            if (region.Width <= 0 || region.Height <= 0) return string.Empty;

            using (var cropped = image.Clone(region, image.PixelFormat))
            {
                return ReadText(cropped, preprocess);
            }
        }

        /// <summary>
        /// Reads only numbers from an image.
        /// </summary>
        /// <param name="image">The image to read from</param>
        /// <param name="preprocess">Whether to apply preprocessing for better results</param>
        /// <returns>Recognized numbers as string</returns>
        public string ReadNumbers(Bitmap image, bool preprocess = true)
        {
            if (image == null) return string.Empty;

            Bitmap processedImage = preprocess ? PreprocessForGameOCR(image) : image;

            // Configure engine to only recognize digits
            _engine.SetVariable("tessedit_char_whitelist", "0123456789/");

            try
            {
                using (var pix = BitmapToPix(processedImage))
                using (var page = _engine.Process(pix))
                {
                    return page.GetText().Trim();
                }
            }
            finally
            {
                // Reset to default
                _engine.SetVariable("tessedit_char_whitelist", "");
                if (preprocess && processedImage != image)
                    processedImage?.Dispose();
            }
        }

        /// <summary>
        /// Reads numbers from a specific region of an image.
        /// </summary>
        public string ReadNumbersFromRegion(Bitmap image, Rectangle region, bool preprocess = true)
        {
            if (image == null) return string.Empty;

            region.Intersect(new Rectangle(0, 0, image.Width, image.Height));
            if (region.Width <= 0 || region.Height <= 0) return string.Empty;

            using (var cropped = image.Clone(region, image.PixelFormat))
            {
                return ReadNumbers(cropped, preprocess);
            }
        }

        /// <summary>
        /// Attempts to parse an integer from the image.
        /// </summary>
        /// <param name="image">The image to read from</param>
        /// <param name="result">The parsed integer</param>
        /// <returns>True if successful</returns>
        public bool TryReadInt(Bitmap image, out int result)
        {
            string text = ReadNumbers(image);
            // Remove any non-digit characters
            text = Regex.Replace(text, @"[^\d]", "");
            return int.TryParse(text, out result);
        }

        /// <summary>
        /// Attempts to parse an integer from a region of the image.
        /// </summary>
        public bool TryReadIntFromRegion(Bitmap image, Rectangle region, out int result)
        {
            string text = ReadNumbersFromRegion(image, region);
            text = Regex.Replace(text, @"[^\d]", "");
            return int.TryParse(text, out result);
        }

        /// <summary>
        /// Preprocesses an image for better OCR results.
        /// Converts to grayscale and increases contrast.
        /// </summary>
        /// <param name="image">The image to preprocess</param>
        /// <returns>Preprocessed image (caller must dispose)</returns>
        public static Bitmap PreprocessForOCR(Bitmap image)
        {
            if (image == null) return null;

            Bitmap result = new Bitmap(image.Width, image.Height);

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);

                    // Convert to grayscale
                    int gray = (int)(pixel.R * 0.3 + pixel.G * 0.59 + pixel.B * 0.11);

                    // Apply threshold for better contrast (binarization)
                    int newValue = gray > 128 ? 255 : 0;

                    result.SetPixel(x, y, Color.FromArgb(newValue, newValue, newValue));
                }
            }

            return result;
        }

        /// <summary>
        /// Advanced preprocessing specifically optimized for game text.
        /// Scales up the image, enhances contrast, and handles both light and dark text.
        /// </summary>
        public static Bitmap PreprocessForGameOCR(Bitmap image)
        {
            if (image == null) return null;

            // Scale factor - OCR works much better on larger images
            int scaleFactor = 3;
            int newWidth = image.Width * scaleFactor;
            int newHeight = image.Height * scaleFactor;

            // Create scaled image with high quality interpolation
            Bitmap scaled = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            // Create result bitmap
            Bitmap result = new Bitmap(newWidth, newHeight);

            // Calculate average brightness to determine if we have light or dark text
            long totalBrightness = 0;
            for (int y = 0; y < scaled.Height; y++)
            {
                for (int x = 0; x < scaled.Width; x++)
                {
                    Color pixel = scaled.GetPixel(x, y);
                    totalBrightness += (pixel.R + pixel.G + pixel.B) / 3;
                }
            }
            int avgBrightness = (int)(totalBrightness / (scaled.Width * scaled.Height));

            // Use adaptive threshold based on image brightness
            // If mostly dark background, look for light text (invert)
            // If mostly light background, look for dark text
            bool invertForLightText = avgBrightness < 128;

            for (int y = 0; y < scaled.Height; y++)
            {
                for (int x = 0; x < scaled.Width; x++)
                {
                    Color pixel = scaled.GetPixel(x, y);

                    // Convert to grayscale using luminance formula
                    int gray = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);

                    // Apply Otsu-like adaptive threshold
                    // Use multiple thresholds and pick based on context
                    int threshold = avgBrightness;

                    int newValue;
                    if (invertForLightText)
                    {
                        // Light text on dark background - make text black on white
                        newValue = gray > threshold ? 0 : 255;
                    }
                    else
                    {
                        // Dark text on light background - keep as is
                        newValue = gray > threshold ? 255 : 0;
                    }

                    result.SetPixel(x, y, Color.FromArgb(newValue, newValue, newValue));
                }
            }

            scaled.Dispose();
            return result;
        }

        /// <summary>
        /// Extracts text of a specific color from an image.
        /// Useful for reading colored game text (e.g., yellow numbers, red warnings).
        /// </summary>
        /// <param name="image">Source image</param>
        /// <param name="targetColor">Color of the text to extract</param>
        /// <param name="tolerance">Color matching tolerance (0-255)</param>
        public static Bitmap ExtractTextByColor(Bitmap image, Color targetColor, int tolerance = 50)
        {
            if (image == null) return null;

            int scaleFactor = 3;
            int newWidth = image.Width * scaleFactor;
            int newHeight = image.Height * scaleFactor;

            Bitmap scaled = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            Bitmap result = new Bitmap(newWidth, newHeight);

            for (int y = 0; y < scaled.Height; y++)
            {
                for (int x = 0; x < scaled.Width; x++)
                {
                    Color pixel = scaled.GetPixel(x, y);

                    // Check if pixel is close to target color
                    bool isMatch = Math.Abs(pixel.R - targetColor.R) <= tolerance &&
                                   Math.Abs(pixel.G - targetColor.G) <= tolerance &&
                                   Math.Abs(pixel.B - targetColor.B) <= tolerance;

                    // Make matching pixels black (text), non-matching white (background)
                    int newValue = isMatch ? 0 : 255;
                    result.SetPixel(x, y, Color.FromArgb(newValue, newValue, newValue));
                }
            }

            scaled.Dispose();
            return result;
        }

        /// <summary>
        /// Reads text from the game window at the specified region.
        /// </summary>
        /// <param name="region">Screen region to read from</param>
        /// <returns>Recognized text</returns>
        public string ReadTextFromScreen(Rectangle region)
        {
            try
            {
                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                {
                    return ReadTextFromRegion(screenshot, region);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TextRecognition] Error reading text from screen: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Reads numbers from the game window at the specified region.
        /// </summary>
        public string ReadNumbersFromScreen(Rectangle region)
        {
            try
            {
                using (var screenshot = (Bitmap)ImageRecognition.GetWindowScreenshot())
                {
                    return ReadNumbersFromRegion(screenshot, region);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TextRecognition] Error reading numbers from screen: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Finds the position of a specific text string in a screenshot using OCR.
        /// Returns the center point of the matching text in image coordinates, or null if not found.
        /// </summary>
        /// <param name="screenshot">The screenshot to search in</param>
        /// <param name="targetText">The text to find (case-insensitive partial match)</param>
        /// <param name="searchRegion">Optional region to limit the search area</param>
        /// <returns>Center point of the found text in image coordinates, or null</returns>
        public Point? FindTextPosition(Bitmap screenshot, string targetText, Rectangle? searchRegion = null, bool saveDebugImage = false)
        {
            if (screenshot == null || string.IsNullOrEmpty(targetText))
                return null;

            Bitmap searchImage = screenshot;
            int offsetX = 0, offsetY = 0;

            if (searchRegion.HasValue)
            {
                var region = searchRegion.Value;
                region.Intersect(new Rectangle(0, 0, screenshot.Width, screenshot.Height));
                if (region.Width <= 0 || region.Height <= 0)
                    return null;
                searchImage = screenshot.Clone(region, screenshot.PixelFormat);
                offsetX = region.X;
                offsetY = region.Y;
            }

            try
            {
                // Scale up for better OCR accuracy
                int scaleFactor = 3;
                using (var scaled = new Bitmap(searchImage.Width * scaleFactor, searchImage.Height * scaleFactor))
                {
                    using (var g = Graphics.FromImage(scaled))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(searchImage, 0, 0, scaled.Width, scaled.Height);
                    }

                    Point? result = null;

                    // For debug image: draw on the original screenshot
                    Bitmap debugImage = null;
                    Graphics debugGraphics = null;
                    Font debugFont = null;
                    if (saveDebugImage)
                    {
                        debugImage = new Bitmap(screenshot);
                        debugGraphics = Graphics.FromImage(debugImage);
                        debugFont = new Font("Arial", 12, FontStyle.Bold);
                    }

                    using (var pix = BitmapToPix(scaled))
                    using (var page = _engine.Process(pix))
                    using (var iter = page.GetIterator())
                    {
                        iter.Begin();
                        do
                        {
                            string lineText = iter.GetText(PageIteratorLevel.TextLine)?.Trim();
                            if (string.IsNullOrEmpty(lineText))
                                continue;

                            bool isMatch = lineText.IndexOf(targetText, StringComparison.OrdinalIgnoreCase) >= 0;

                            if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out var bounds))
                            {
                                // Convert scaled coordinates back to original
                                int x1 = bounds.X1 / scaleFactor + offsetX;
                                int y1 = bounds.Y1 / scaleFactor + offsetY;
                                int x2 = bounds.X2 / scaleFactor + offsetX;
                                int y2 = bounds.Y2 / scaleFactor + offsetY;

                                if (saveDebugImage && debugGraphics != null)
                                {
                                    var pen = isMatch ? Pens.Green : Pens.Red;
                                    var textColor = isMatch ? Brushes.Lime : Brushes.OrangeRed;
                                    debugGraphics.DrawRectangle(pen, x1, y1, x2 - x1, y2 - y1);
                                    debugGraphics.DrawString(lineText, debugFont, textColor, x1, y1 - 16);
                                }

                                if (isMatch && !result.HasValue)
                                {
                                    int centerY = (y1 + y2) / 2;
                                    int centerX;

                                    // If the target is found within a longer line (e.g., "TRICKS Jump!"),
                                    // the target is in a submenu to the right — use the right portion
                                    int matchIndex = lineText.IndexOf(targetText, StringComparison.OrdinalIgnoreCase);
                                    int lineWidth = x2 - x1;
                                    if (lineText.Length > 0 && matchIndex > 0)
                                    {
                                        float startRatio = (float)matchIndex / lineText.Length;
                                        float endRatio = (float)(matchIndex + targetText.Length) / lineText.Length;
                                        centerX = x1 + (int)(lineWidth * (startRatio + endRatio) / 2);
                                    }
                                    else
                                    {
                                        centerX = (x1 + x2) / 2;
                                    }

                                    Logger.Debug("Doodle", $"OCR found '{targetText}' in line '{lineText}' at ({centerX}, {centerY})");
                                    result = new Point(centerX, centerY);
                                }
                            }
                        } while (iter.Next(PageIteratorLevel.TextLine));
                    }

                    // Save debug image
                    if (saveDebugImage && debugImage != null)
                    {
                        try
                        {
                            string debugDir = Path.Combine(AppPaths.ExeDirectory, "Templates", "debug");
                            Directory.CreateDirectory(debugDir);
                            string timestamp = DateTime.Now.ToString("HH-mm-ss-fff");
                            string status = result.HasValue ? "FOUND" : "NOTFOUND";
                            string path = Path.Combine(debugDir, $"ocr_{targetText}_{status}_{timestamp}.png");

                            // Draw search target label
                            debugGraphics.DrawString($"Looking for: \"{targetText}\" — {status}",
                                debugFont, Brushes.Yellow, 10, screenshot.Height - 30);

                            debugImage.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                            Logger.Debug("Doodle", $"Debug image saved: {path}");
                        }
                        catch (Exception ex)
                        {
                            Logger.Debug("Doodle", $"Failed to save debug image: {ex.Message}");
                        }
                    }

                    debugFont?.Dispose();
                    debugGraphics?.Dispose();
                    debugImage?.Dispose();

                    if (!result.HasValue)
                    {
                        Logger.Debug("Doodle", $"OCR did not find '{targetText}' in screenshot");
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Doodle", $"OCR error finding '{targetText}': {ex.Message}");
                return null;
            }
            finally
            {
                if (searchRegion.HasValue && searchImage != screenshot)
                {
                    searchImage.Dispose();
                }
            }
        }

        #region Helper Methods

        /// <summary>
        /// Verifies native Tesseract DLLs exist and pre-loads them into the process.
        /// In single-file published executables, Assembly.Location returns empty string,
        /// causing Tesseract's internal path resolver to pass null to Path.Combine and crash
        /// with "Value cannot be null (Parameter 'path1')". By loading the DLLs ourselves
        /// before TesseractEngine is created, the P/Invoke calls find them already loaded.
        /// </summary>
        private static void VerifyNativeDlls()
        {
            string x64Dir = Path.Combine(AppPaths.ExeDirectory, "x64");
            string leptonicaDll = Path.Combine(x64Dir, "leptonica-1.82.0.dll");
            string tesseractDll = Path.Combine(x64Dir, "tesseract50.dll");

            var missing = new System.Collections.Generic.List<string>();
            if (!File.Exists(leptonicaDll))
            {
                missing.Add("x64/leptonica-1.82.0.dll");
            }
            if (!File.Exists(tesseractDll))
            {
                missing.Add("x64/tesseract50.dll");
            }

            if (missing.Count > 0)
            {
                throw new FileNotFoundException(
                    $"Missing native OCR libraries: {string.Join(", ", missing)}\n\n" +
                    $"Expected location: {x64Dir}\n\n" +
                    "These files are required for OCR features (doodle tricks, auto golf).\n" +
                    "Please re-download the bot or copy the x64 folder next to the executable.");
            }

            // Pre-load native DLLs so Tesseract's P/Invoke finds them already in the process.
            // Leptonica must be loaded first since tesseract50.dll depends on it.
            if (!NativeLibrary.TryLoad(leptonicaDll, out _))
            {
                throw new InvalidOperationException(
                    $"Failed to load native library: {leptonicaDll}\n\n" +
                    "This may indicate a missing Visual C++ Redistributable.\n" +
                    "Download it from: https://aka.ms/vs/17/release/vc_redist.x64.exe");
            }
            if (!NativeLibrary.TryLoad(tesseractDll, out _))
            {
                throw new InvalidOperationException(
                    $"Failed to load native library: {tesseractDll}\n\n" +
                    "This may indicate a missing Visual C++ Redistributable.\n" +
                    "Download it from: https://aka.ms/vs/17/release/vc_redist.x64.exe");
            }
        }

        private Pix BitmapToPix(Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                return Pix.LoadFromMemory(stream.ToArray());
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _engine?.Dispose();
                }
                _disposed = true;
            }
        }

        ~TextRecognition()
        {
            Dispose(false);
        }

        #endregion
    }
}
