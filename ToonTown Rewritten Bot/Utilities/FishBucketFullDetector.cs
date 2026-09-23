using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using ToonTown_Rewritten_Bot.Services;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Detects the "Your fish bucket is full" popup using template matching.
    /// A missing cast button alone is not evidence that this popup is visible.
    /// </summary>
    public static class FishBucketFullDetector
    {
        private const string PopupElementName = "FishBucketFullPopup";

        /// <summary>
        /// Checks if the "bucket full" popup is currently visible on screen using template matching.
        /// Searches silently so an ordinary bite timeout cannot trigger a capture dialog.
        /// </summary>
        /// <returns>True if the popup is detected on screen.</returns>
        public static async Task<bool> CheckForBucketFullPopupAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Only a fresh match proves visibility; manual/cached coordinates do not.
            var location = await UIElementManager.Instance.FindElementAsync(PopupElementName, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (location.HasValue)
            {
                Logger.Debug("FishDetect", $"Bucket full popup detected via template at ({location.Value.X}, {location.Value.Y})");
                return true;
            }

            Logger.Debug("FishDetect", "Bucket full popup not detected");
            return false;
        }

        /// <summary>
        /// Gets the position of the Exit button on the bucket full popup.
        /// The Exit button (red X) is at the bottom center of the centered popup,
        /// approximately 72% down the game window.
        /// </summary>
        public static Point? GetExitButtonPosition()
        {
            var windowRect = CoreFunctionality.GetGameWindowRect();
            if (windowRect.IsEmpty) return null;

            int exitX = windowRect.X + windowRect.Width / 2;
            int exitY = windowRect.Y + (int)(windowRect.Height * 0.72);

            Logger.Debug("FishDetect", $"Bucket full Exit button estimated at ({exitX}, {exitY})");
            return new Point(exitX, exitY);
        }
    }
}
