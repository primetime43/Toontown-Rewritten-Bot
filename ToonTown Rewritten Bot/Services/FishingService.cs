using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Services.FishingLocations;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Services
{
    public class FishingService : FishingStrategyBase
    {
        /// <summary>
        /// Initiates the fishing process for a given location with specified parameters.
        /// </summary>
        /// <param name="location">The location where fishing will take place. If "FISH ANYWHERE" is specified, the selling process is skipped.</param>
        /// <param name="casts">The number of times to cast the fishing line.</param>
        /// <param name="sells">The number of times to visit the fisherman to sell fish. If the location is "FISH ANYWHERE", selling is not performed.</param>
        /// <param name="variance">Indicates whether a variance in casting should be applied for a more natural fishing experience.</param>
        /// <param name="cancellationToken">Token to signal the cancellation of the fishing operation.</param>
        /// <returns>A task representing the asynchronous fishing operation.</returns>
        /// <remarks>
        /// This method manages the entire fishing operation including preparing for fishing, casting, and optionally selling the fish based on the provided location. It utilizes different fishing strategies based on the specified location and handles selling operations unless fishing in the "FISH ANYWHERE" mode.
        /// </remarks>
        public async Task StartFishing(string locationName, int casts, int sells, bool variance, CancellationToken cancellationToken)
        {
            while (sells > 0 && !cancellationToken.IsCancellationRequested)
            {
                await PrepareForFishing(cancellationToken);
                await StartFishingActionsAsync(casts, variance, cancellationToken);

                if (locationName != FishingLocationNames.FishAnywhere)
                {
                    FishingStrategyBase fishingStrategy = DetermineFishingStrategy(locationName);
                    await fishingStrategy.LeaveDockAndSellAsync(cancellationToken);
                    sells--;
                }
                else
                {
                    // If "FISH ANYWHERE" is selected, skip the selling process.
                    sells = 0;
                }
            }

            BringBotWindowToFront();
            MessageBox.Show($"Done Fishing in '{locationName}'.");
        }

        /// <summary>
        /// Prepares the fishing environment by ensuring that the game window is focused and checking the necessary coordinates before starting the fishing process.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete, allowing the operation to be cancelled.</param>
        /// <returns>A task representing the asynchronous operation of preparing the environment for fishing.</returns>
        /// <remarks>
        /// This method focuses the game window and performs an initial delay to ensure the environment is ready for fishing actions.
        /// It checks for the presence of the red fishing button within the game window. If the button's coordinates are not set or if the button cannot be automatically detected,
        /// it prompts the user to manually locate the button. This preparation step is crucial for the subsequent fishing operations to run smoothly.
        /// </remarks>
        private async Task PrepareForFishing(CancellationToken cancellationToken)
        {
            // Prepare for fishing based on initial conditions
            maximizeAndFocus();
            await Task.Delay(3000, cancellationToken); // Initial delay before starting.

            if (!CheckCoordinates("15")) // Checks the red fishing button
            {
                //imgRecLocateRedCastBtn();//use the image rec to locate the image and set the coordinates

                //manuallyLocateRedFishingButton();

                if (isAutoDetectFishingBtnActive)
                {
                    //do the image search for color here. Make it so you can use the search or manual set (temp code testing)
                    Image screenshot = ImageRecognition.GetWindowScreenshot();
                    Point coords = await ImageRecognition.locateColorInImage(screenshot, _redFishingButtonColor, 10);

                    //debugColorCoords(screenshot, coords);

                    if (coords.X == 0 && coords.Y == 0)//color not found, manually update
                    {
                        MessageBox.Show("Unable to detect red fishing button.");
                        await ManuallyLocateRedFishingButton();
                    }
                    else
                        ManuallyUpdateCoordinatesNoUI("15", coords);
                }
                else
                    await ManuallyLocateRedFishingButton();
            }
        }

        private FishingStrategyBase DetermineFishingStrategy(string locationName)
        {
            return locationName switch
            {
                var location when location == FishingLocationNames.ToontownCentralPunchlinePlace => new TTCPunchlinePlaceFishing(),
                var location when location == FishingLocationNames.DonaldDreamLandLullabyLane => new DDLLullabyLaneFishing(),
                var location when location == FishingLocationNames.BrrrghPolarPlace => new BrrrghPolarPlaceFishing(),
                var location when location == FishingLocationNames.BrrrghWalrusWay => new BrrrghWalrusWayFishing(),
                var location when location == FishingLocationNames.BrrrghSleetStreet => new BrrrghSleetStFishing(),
                var location when location == FishingLocationNames.MinniesMelodylandTenorTerrace => new MMTenorTerraceFishing(),
                var location when location == FishingLocationNames.DonaldDockLighthouseLane => new DDLighthouseLaneFishing(),
                var location when location == FishingLocationNames.DaisysGardenElmStreet => new DaisyGardenElmStFishing(),
                _ => throw new NotImplementedException($"Fishing strategy for location '{locationName}' is not implemented."),
            };
        }

        public override Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
