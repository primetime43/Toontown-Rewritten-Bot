using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ToonTown_Rewritten_Bot.Services.FishingLocations
{
    public abstract class FishingStrategyBase : CoreFunctionality
    {
        protected bool shouldStopFishing = false;
        /// <summary>
        /// The random variance of casting the fishing rod, if enabled.
        /// </summary>
        protected int _VARIANCE = 20;
        protected Random _rand = new Random();
        protected string _redFishingButtonColor = "#FD0000";

        /// <summary>
        /// An abstract method to be implemented by derived classes, detailing the process
        /// of leaving the fishing dock, selling the caught fish at the fisherman, and returning
        /// to the dock. This method defines the required actions to perform the sell operation
        /// in specific fishing locations.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete,
        /// allowing the operation to be cancelled.</param>
        /// <returns>A task that represents the asynchronous operation of leaving the dock,
        /// selling fish, and returning.</returns>
        public abstract Task LeaveDockAndSellAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Initiates the fishing actions for a specified number of casts, applying variance if enabled, and handles the operation asynchronously.
        /// </summary>
        /// <param name="numberOfCasts">The total number of casts to attempt.</param>
        /// <param name="fishVariance">Indicates whether to apply a variance to casting, simulating a more natural fishing experience.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete, allowing the operation to be cancelled.</param>
        /// <returns>A task that represents the asynchronous fishing operation, performing casts, checking for catches, and optionally exiting fishing upon completion.</returns>
        /// <remarks>
        /// This method controls the flow of the fishing operation, including casting the line, waiting for a catch, and handling the asynchronous delays between actions.
        /// It also respects the cancellation token to safely exit the operation if requested and ensures that the fishing process is attempted for the specified number of casts.
        /// After completing the fishing attempts or if instructed to stop, it will exit the fishing operation.
        /// </remarks>
        public async Task StartFishingActionsAsync(int numberOfCasts, bool fishVariance, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = new Stopwatch();
            while (numberOfCasts != 0 && !shouldStopFishing)
            {
                await CastLine(fishVariance, cancellationToken);
                stopwatch.Start();
                while (stopwatch.Elapsed.Seconds < 30 && !await CheckIfFishCaught(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested) return;
                }
                stopwatch.Stop();
                stopwatch.Reset();
                numberOfCasts--;
                await Task.Delay(1000, cancellationToken);
            }
            if (!shouldStopFishing)
            {
                await ExitFishing(cancellationToken);
                await Task.Delay(3000, cancellationToken);
            }
        }

        protected async Task CastLine(bool fishVariance, CancellationToken cancellationToken)
        {
            var (x, y) = GetCoordsFromMap("15");

            int randX = fishVariance ? _rand.Next(-_VARIANCE, _VARIANCE + 1) : 0;
            int randY = fishVariance ? _rand.Next(-_VARIANCE, _VARIANCE + 1) : 0;
            MoveCursor(x + randX, y + randY);
            DoFishingClick();
            await Task.Delay(100, cancellationToken); // Adding a small delay for realism or replace with necessary async call
        }

        protected async Task<bool> CheckIfFishCaught(CancellationToken cancellationToken)
        {
            var (x, y) = GetCoordsFromMap("15");
            string color = HexConverter(GetColorAt(x, y - 600));
            if (color.Equals("#FFFFBE") || color.Equals("#FFFFBF")) return true;

            color = HexConverter(GetColorAt(x, 110));
            return color.Equals("#FFFFBE") || color.Equals("#FFFFBF");
        }

        protected async Task ExitFishing(CancellationToken cancellationToken)
        {
            if (CheckCoordinates("16"))
            {
                var (x, y) = GetCoordsFromMap("16");
                MoveCursor(x, y);
                DoMouseClick();
            }
            else
            {
                await ManualUpdateCoordinates("16");
                await ExitFishing(cancellationToken);
            }
            await Task.Delay(2000, cancellationToken);
        }

        protected async Task SellFishAsync(CancellationToken cancellationToken)
        {
        retry:
            if (CheckCoordinates("17"))//returns true if they are not 0,0
            {
                await Task.Delay(2100, cancellationToken);
                var (x, y) = GetCoordsFromMap("17");
                MoveCursor(x, y);
                DoMouseClick();
            }
            else
            {
                await ManualUpdateCoordinates("17");
                //imgRecLocateSellBtn();
                goto retry;
            }
            await Task.Delay(2000, cancellationToken);
        }

        protected async Task ManuallyLocateRedFishingButton()
        {
            await ManualUpdateCoordinates("15");//update the red fishing button coords
        }
    }
}
