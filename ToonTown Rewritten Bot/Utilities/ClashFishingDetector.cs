using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace ToonTown_Rewritten_Bot.Utilities
{
    internal static class ClashFishingDetector
    {
        internal static Point? FindCatchButton(Bitmap frame, IEnumerable<string> templatePaths,
            CancellationToken cancellationToken)
        {
            foreach (string path in templatePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var template = new Bitmap(path);
                var match = ImageTemplateMatcher.FindTemplate(frame, template, 0.93, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (match.Found) return match.Center;
            }
            return null;
        }
    }
}
