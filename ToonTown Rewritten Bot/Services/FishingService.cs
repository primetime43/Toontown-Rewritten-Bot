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
    public class FishingService
    {
        private readonly FishingEngine _engine = new FishingEngine();

        /// <summary>
        /// Session totals from the most recent fishing run.
        /// </summary>
        public int SessionFishCaught => _engine.SessionFishCaught;
        public int SessionCastCount => _engine.SessionCastCount;

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
            Logger.Info("Fishing", $"Session start: location={locationName}, casts={casts}, sells={sells}, variance={variance}, autoDetect={autoDetectFish}");

            // Set the fishing location for proper bubble detection configuration
            _engine.SetFishingLocation(locationName);

            bool isFirstCycle = true;
            while (sells > 0 && !cancellationToken.IsCancellationRequested)
            {
                await _engine.PrepareForFishing(cancellationToken).ConfigureAwait(false);
                await _engine.StartFishingActionsAsync(casts, variance, autoDetectFish, isFirstCycle, cancellationToken).ConfigureAwait(false);
                isFirstCycle = false;

                if (locationName == FishingLocationNames.FishAnywhere)
                {
                    // Fish Anywhere - just exit fishing without selling
                    if (!_engine.BucketWasFull)
                    {
                        await _engine.ExitFishing(cancellationToken).ConfigureAwait(false);
                    }
                    await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
                    sells = 0;
                }
                else if (locationName == FishingLocationNames.CustomFishingAction && customFishingFilePath != "")
                {
                    // Custom fishing action - sell using custom action file
                    if (!_engine.BucketWasFull)
                    {
                        await _engine.StraightenToonAsync(cancellationToken).ConfigureAwait(false);
                        await _engine.ExitFishing(cancellationToken).ConfigureAwait(false);
                    }
                    await Task.Delay(3000, cancellationToken).ConfigureAwait(false);

                    CustomActionsFishing customFishing = new CustomActionsFishing(customFishingFilePath);
                    await customFishing.LeaveDockAndSellAsync(cancellationToken).ConfigureAwait(false);
                    sells--;
                }
                else if (locationName == FishingLocationNames.EstateLeftDock)
                {
                    // Estate - sell using built-in estate action file
                    Logger.Info("Fishing", "Estate sell: starting sell trip.");
                    if (!_engine.BucketWasFull)
                    {
                        await _engine.StraightenToonAsync(cancellationToken).ConfigureAwait(false);
                        await _engine.ExitFishing(cancellationToken).ConfigureAwait(false);
                    }
                    await Task.Delay(3000, cancellationToken).ConfigureAwait(false);

                    string estateSellPath = Path.Combine(AppPaths.ExeDirectory, "Custom Fishing Actions", "EstateFishing Far Left Dock.json");
                    Logger.Debug("Fishing", $"Estate sell path: {estateSellPath}, exists: {System.IO.File.Exists(estateSellPath)}");
                    CustomActionsFishing estateFishing = new CustomActionsFishing(estateSellPath);
                    await estateFishing.LeaveDockAndSellAsync(cancellationToken).ConfigureAwait(false);
                    Logger.Info("Fishing", "Estate sell: sell trip complete.");
                    sells--;
                }
                else
                {
                    // Hardcoded fishing locations - sell using strategy pattern
                    if (!_engine.BucketWasFull)
                    {
                        await _engine.StraightenToonAsync(cancellationToken).ConfigureAwait(false);
                        await _engine.ExitFishing(cancellationToken).ConfigureAwait(false);
                    }
                    await Task.Delay(3000, cancellationToken).ConfigureAwait(false);

                    FishingStrategyBase fishingStrategy = DetermineFishingStrategy(locationName);
                    await fishingStrategy.LeaveDockAndSellAsync(cancellationToken).ConfigureAwait(false);
                    sells--;
                }
            }

            Logger.Info("Fishing", $"Session end: fish caught={SessionFishCaught}, casts={SessionCastCount}");
            // Notify MainForm to uncheck the overlay checkbox - fishing is completely done
            FishingStrategyBase.OnFishingEnded?.Invoke();
        }

        /// <summary>
        /// Starts a custom fishing debugging session using a specified JSON file.
        /// </summary>
        /// <param name="jsonPath">The path to the JSON file containing the custom actions to be executed.</param>
        /// <param name="cancellationToken">Token to signal the cancellation of the debugging operation.</param>
        /// <param name="showCompletionMessage">If true, shows a message box when debugging completes.</param>
        /// <remarks>
        /// This method is intended for debugging custom fishing actions. It simulates the fishing actions
        /// without actual fishing, focusing on the movement and actions defined in the JSON file.
        /// It ensures the game window is maximized and focused before starting the actions.
        /// </remarks>
        public static async Task StartCustomFishingDebugging(string jsonPath, CancellationToken cancellationToken, bool showCompletionMessage = true)
        {
            CustomActionsFishing customFishing = new CustomActionsFishing(jsonPath);

            // Prepare - focus TTR window
            CoreFunctionality.FocusTTRWindow();
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false); // Initial delay before starting.
            await customFishing.LeaveDockAndSellAsync(cancellationToken).ConfigureAwait(false); // Start the action sequence
        }

        private FishingStrategyBase DetermineFishingStrategy(string locationName)
        {
            Logger.Debug("Fishing", $"Selecting strategy for location: {locationName}");
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

        /// <summary>
        /// Internal engine that extends FishingStrategyBase to access its instance methods.
        /// FishingService delegates to this engine rather than inheriting from FishingStrategyBase directly.
        /// </summary>
        private class FishingEngine : FishingStrategyBase
        {
            public override Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
            {
                throw new NotSupportedException("FishingEngine does not walk to sell. Use a location strategy.");
            }

            public async Task PrepareForFishing(CancellationToken cancellationToken)
            {
                FocusTTRWindow();
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
