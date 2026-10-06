using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace ToonTown_Rewritten_Bot.Utilities
{
    internal static class FishingSellInteraction
    {
        internal static Point GetClickPoint(Rectangle bounds) =>
            // Templates often include the caption below the round confirmation button.
            new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 3);

        internal static async Task RunAsync(
            Func<CancellationToken, Task<Rectangle?>> findButton,
            Func<Point, CancellationToken, Task> clickButton,
            CancellationToken token,
            Func<int, CancellationToken, Task> delay = null)
        {
            delay ??= Task.Delay;
            Rectangle? button = null;
            for (int poll = 0; poll < 12; poll++)
            {
                token.ThrowIfCancellationRequested();
                button = await findButton(token).ConfigureAwait(false);
                if (button.HasValue) break;
                await delay(500, token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
            if (!button.HasValue)
                throw new InvalidOperationException("Sell All was not visible at the fisherman. Stopped before walking back; check the route and Sell All template.");

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                token.ThrowIfCancellationRequested();
                await clickButton(GetClickPoint(button.Value), token).ConfigureAwait(false);
                Logger.Info("Fishing", $"Sell All click {attempt}/3 sent; waiting for the offer to close.");
                int missingFrames = 0;
                for (int poll = 0; poll < 6; poll++)
                {
                    await delay(500, token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    var current = await findButton(token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    if (!current.HasValue)
                    {
                        if (++missingFrames >= 2)
                        {
                            Logger.Info("Fishing", "Sell All offer closed after clicking; continuing the route.");
                            return;
                        }
                    }
                    else
                    {
                        missingFrames = 0;
                        button = current;
                    }
                }
                // Never retry a click using a position from a dialog that may have closed.
                button = await findButton(token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                if (!button.HasValue)
                    throw new InvalidOperationException("Could not confirm the Sell All result. Stopped before walking back.");
            }
            throw new InvalidOperationException("Sell All did not respond after 3 clicks. Stopped at the fisherman; the game was kept in its selected input mode.");
        }
    }
}
