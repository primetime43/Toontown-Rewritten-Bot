using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using ToonTown_Rewritten_Bot.Services;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Detects the "Your fish bucket is full" popup using template matching.
    /// If no template has been captured yet, prompts the user to capture one
    /// when the red fishing button disappears (suggesting a popup appeared).
    /// </summary>
    public static class FishBucketFullDetector
    {
        private const string PopupElementName = "FishBucketFullPopup";
        private const string PopupDescription = "Your fish bucket appears to be full. Please select the 'Your fish bucket is full' popup dialog on screen.";

        /// <summary>
        /// Checks if the "bucket full" popup is currently visible on screen using template matching.
        /// If no template exists, prompts the user to capture one first.
        /// </summary>
        /// <returns>True if the popup is detected on screen.</returns>
        public static async Task<bool> CheckForBucketFullPopupAsync(CancellationToken cancellationToken = default)
        {
            // GetElementLocationAsync will prompt for template capture if none exists
            var location = await UIElementManager.Instance.GetElementLocationAsync(
                PopupElementName, PopupDescription, forceSearch: true);

            if (location.HasValue)
            {
                Debug.WriteLine($"[BucketFull] Popup detected via template at ({location.Value.X}, {location.Value.Y})");
                return true;
            }

            Debug.WriteLine("[BucketFull] Popup not detected (template didn't match or user cancelled capture)");
            return false;
        }

        /// <summary>
        /// Gets the position of the Exit button on the bucket full popup.
        /// The Exit button is at the bottom center of the centered popup.
        /// </summary>
        public static Point? GetExitButtonPosition()
        {
            var windowRect = CoreFunctionality.GetGameWindowRect();
            if (windowRect.IsEmpty) return null;

            int exitX = windowRect.X + windowRect.Width / 2;
            int exitY = windowRect.Y + (int)(windowRect.Height * 0.65);

            return new Point(exitX, exitY);
        }
    }
}
