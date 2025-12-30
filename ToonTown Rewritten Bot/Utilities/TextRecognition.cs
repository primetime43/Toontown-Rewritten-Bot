using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
            AppDomain.CurrentDomain.BaseDirectory, "tessdata");

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

            _engine = new TesseractEngine(dataPath, language, EngineMode.Default);
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
            catch
            {
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
            catch
            {
                return string.Empty;
            }
        }

        #region Helper Methods

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
