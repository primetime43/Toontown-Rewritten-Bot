using System;
using System.Drawing;
using ToonTown_Rewritten_Bot.Services;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Detects the "You do not have any more jellybeans for bait!" popup
    /// that appears when the player runs out of money for fishing.
    /// Uses two-stage detection: fast color check, then OCR text verification.
    /// </summary>
    public static class NoJellybeansDetector
    {
        // Reusable OCR engine (lazy initialized)
        private static TextRecognition _ocrEngine;
        private static readonly object _ocrLock = new object();

        /// <summary>
        /// Checks if the "no jellybeans" popup is currently visible on screen.
        /// Uses color detection first (fast), then OCR verification (accurate).
        /// </summary>
        /// <returns>True if the popup is detected and verified, false otherwise.</returns>
        public static bool IsNoJellybeansPopupVisible()
        {
            var windowRect = CoreFunctionality.GetGameWindowRect();
            if (windowRect.IsEmpty) return false;

            // Stage 1: Fast color check for cream popup background
            if (!HasCreamPopupInCenter(windowRect))
            {
                return false;
            }

            System.Diagnostics.Debug.WriteLine("[NoJellybeans] Cream popup detected in center, verifying with OCR...");

            // Stage 2: OCR verification - read popup text and check for keywords
            return VerifyPopupTextWithOCR(windowRect);
        }

        /// <summary>
        /// Fast check for cream-colored popup in the center of the screen.
        /// </summary>
        private static bool HasCreamPopupInCenter(Rectangle windowRect)
        {
            int centerX = windowRect.X + windowRect.Width / 2;
            int centerY = windowRect.Y + windowRect.Height / 2;

            // Check multiple positions around the center where the popup would be
            var positionsToCheck = new[]
            {
                new Point(centerX, centerY - 50),      // Upper center of popup
                new Point(centerX, centerY),           // Center
                new Point(centerX - 100, centerY),     // Left of center
                new Point(centerX + 100, centerY),     // Right of center
                new Point(centerX, centerY - 100),     // Top of popup
            };

            int creamColorCount = 0;

            foreach (var pos in positionsToCheck)
            {
                var color = GetColorAt(pos.X, pos.Y);

                if (IsCreamPopupBackground(color))
                {
                    creamColorCount++;
                }
            }

            // Need at least 2 cream color matches to proceed to OCR
            return creamColorCount >= 2;
        }

        /// <summary>
        /// Uses OCR to read the popup text and verify it contains "jellybeans" or "bait".
        /// </summary>
        private static bool VerifyPopupTextWithOCR(Rectangle windowRect)
        {
            try
            {
                // Define the popup region (centered, roughly 400x250 pixels)
                int popupWidth = 400;
                int popupHeight = 250;
                int popupX = (windowRect.Width - popupWidth) / 2;
                int popupY = (windowRect.Height - popupHeight) / 2 - 30; // Slightly above center

                Rectangle popupRegion = new Rectangle(popupX, popupY, popupWidth, popupHeight);

                // Capture the popup region from screen
                using (var screenshot = ImageRecognition.GetWindowScreenshot() as Bitmap)
                {
                    if (screenshot == null) return false;

                    // Ensure region is within bounds
                    popupRegion.Intersect(new Rectangle(0, 0, screenshot.Width, screenshot.Height));
                    if (popupRegion.Width <= 0 || popupRegion.Height <= 0) return false;

                    // Get or create OCR engine
                    var ocr = GetOCREngine();
                    if (ocr == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[NoJellybeans] OCR engine not available, using color-only detection");
                        return true; // Fall back to color-only if OCR unavailable
                    }

                    // Read text from the popup region
                    string text = ocr.ReadTextFromRegion(screenshot, popupRegion, preprocess: true);

                    System.Diagnostics.Debug.WriteLine($"[NoJellybeans] OCR text: {text}");

                    // Check for keywords (case-insensitive)
                    string lowerText = text.ToLowerInvariant();

                    bool hasJellybeans = lowerText.Contains("jellybean") || lowerText.Contains("jelly bean");
                    bool hasBait = lowerText.Contains("bait");
                    bool hasPetShop = lowerText.Contains("pet shop");
                    bool hasTrolley = lowerText.Contains("trolley");

                    // The no jellybeans popup contains "jellybeans for bait" and mentions Pet Shop/Trolley
                    bool isNoJellybeansPopup = hasJellybeans || (hasBait && (hasPetShop || hasTrolley));

                    if (isNoJellybeansPopup)
                    {
                        System.Diagnostics.Debug.WriteLine("[NoJellybeans] OCR CONFIRMED: No jellybeans popup detected!");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[NoJellybeans] OCR did not find jellybeans/bait keywords - not the no jellybeans popup");
                    }

                    return isNoJellybeansPopup;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NoJellybeans] OCR error: {ex.Message}");
                // Fall back to color-only detection on error
                return true;
            }
        }

        /// <summary>
        /// Gets or creates the OCR engine (lazy initialization).
        /// </summary>
        private static TextRecognition GetOCREngine()
        {
            if (_ocrEngine == null)
            {
                lock (_ocrLock)
                {
                    if (_ocrEngine == null)
                    {
                        try
                        {
                            _ocrEngine = new TextRecognition();
                            System.Diagnostics.Debug.WriteLine("[NoJellybeans] OCR engine initialized");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[NoJellybeans] Failed to initialize OCR: {ex.Message}");
                            return null;
                        }
                    }
                }
            }
            return _ocrEngine;
        }

        /// <summary>
        /// Gets the position of the Exit button on the no jellybeans popup.
        /// The Exit button is at the bottom center of the popup.
        /// </summary>
        /// <returns>Screen position of the Exit button, or null if not calculable.</returns>
        public static Point? GetExitButtonPosition()
        {
            var windowRect = CoreFunctionality.GetGameWindowRect();
            if (windowRect.IsEmpty) return null;

            // Exit button is at the bottom center of the popup
            // Based on the screenshot, it's roughly at center X, and below center Y
            int exitX = windowRect.X + windowRect.Width / 2;
            int exitY = windowRect.Y + (int)(windowRect.Height * 0.65); // About 65% down the screen

            return new Point(exitX, exitY);
        }

        /// <summary>
        /// Checks if a color matches the cream/beige popup background.
        /// </summary>
        private static bool IsCreamPopupBackground(Color color)
        {
            // Cream/beige colors: high R (240-255), high G (240-255), lower B (170-220)
            // The popup background is approximately #FFFFBE which is RGB(255, 255, 190)
            return color.R >= 235 && color.G >= 235 && color.B >= 160 && color.B <= 230;
        }

        /// <summary>
        /// Gets the color of a pixel at the specified screen coordinates.
        /// </summary>
        private static Color GetColorAt(int x, int y)
        {
            try
            {
                using (Bitmap bmp = new Bitmap(1, 1))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(x, y, 0, 0, new Size(1, 1));
                    }
                    return bmp.GetPixel(0, 0);
                }
            }
            catch
            {
                return Color.Empty;
            }
        }
    }
}
