using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Services.FishingLocationsWalking;
using ToonTown_Rewritten_Bot.Utilities;
using static ToonTown_Rewritten_Bot.Models.Coordinates;

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
        /// <param name="customFishingFilePath">Optional path to custom fishing JSON file.</param>
        /// <param name="autoDetectFish">If true, automatically detects fish shadows and aims at them.</param>
        /// <returns>A task representing the asynchronous fishing operation.</returns>
        /// <remarks>
        /// This method manages the entire fishing operation including preparing for fishing, casting, and optionally selling the fish based on the provided location. It utilizes different fishing strategies based on the specified location and handles selling operations unless fishing in the "FISH ANYWHERE" mode.
        /// </remarks>
        public async Task StartFishing(string locationName, int casts, int sells, bool variance, CancellationToken cancellationToken, string customFishingFilePath = "", bool autoDetectFish = false)
        {
            // Set the fishing location for proper bubble detection configuration
            SetFishingLocation(locationName);

            while (sells > 0 && !cancellationToken.IsCancellationRequested)
            {
                await PrepareForFishing(cancellationToken);
                await StartFishingActionsAsync(casts, variance, autoDetectFish, cancellationToken);

                if (locationName != FishingLocationNames.FishAnywhere && locationName != FishingLocationNames.CustomFishingAction)
                {
                    // Hardcoded Fishing Locations' if
                    FishingStrategyBase fishingStrategy = DetermineFishingStrategy(locationName);
                    await fishingStrategy.LeaveDockAndSellAsync(cancellationToken);
                    sells--;
                }
                else if(locationName == FishingLocationNames.CustomFishingAction && customFishingFilePath != "")
                {
                    // Custom Fishing's if
                    CustomActionsFishing customFishing = new CustomActionsFishing(customFishingFilePath);
                    await customFishing.LeaveDockAndSellAsync(cancellationToken); // Start the action sequence
                    sells--;
                }
                else
                {
                    // If "FISH ANYWHERE" is selected, skip the selling process.
                    sells = 0;
                }
            }

            if(locationName == FishingLocationNames.CustomFishingAction && customFishingFilePath != "")
                // Update the location name to only the file name without the extension
                locationName = Path.GetFileNameWithoutExtension(customFishingFilePath);

            BringBotWindowToFront();
            MessageBox.Show($"Done Fishing in '{locationName}'.");
        }

        /// <summary>
        /// Starts a custom fishing debugging session using a specified JSON file.
        /// </summary>
        /// <param name="jsonPath">The path to the JSON file containing the custom actions to be executed.</param>
        /// <remarks>
        /// This method is intended for debugging custom fishing actions. It simulates the fishing actions
        /// without actual fishing, focusing on the movement and actions defined in the JSON file.
        /// It ensures the game window is maximized and focused before starting the actions
        /// </remarks>
        public async Task StartCustomFishingDebugging(string jsonPath)
        {
            CustomActionsFishing customFishing = new CustomActionsFishing(jsonPath);

            // Prepare
            FocusTTRWindow();
            await Task.Delay(1000, CancellationToken.None); // Initial delay before starting.
            await customFishing.LeaveDockAndSellAsync(CancellationToken.None); // Start the action sequence
            MessageBox.Show("Done Debugging Custom Action");
        }


        /// <summary>
        /// Prepares the fishing environment by ensuring that the game window is focused.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete, allowing the operation to be cancelled.</param>
        /// <returns>A task representing the asynchronous operation of preparing the environment for fishing.</returns>
        /// <remarks>
        /// This method focuses the game window and performs an initial delay to ensure the environment is ready for fishing actions.
        /// Template matching is used during casting to find the red fishing button, prompting for capture if needed.
        /// </remarks>
        private async Task PrepareForFishing(CancellationToken cancellationToken)
        {
            // Focus the game window without maximizing
            FocusTTRWindow();
            await Task.Delay(1000, cancellationToken);
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
