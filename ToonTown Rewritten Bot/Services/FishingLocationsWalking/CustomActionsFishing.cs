using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Services.FishingLocationsWalking
{
    public class CustomActionsFishing : FishingStrategyBase
    {
        private readonly List<FishingRouteStep> steps;
        private readonly FishingActionKeys actionKeys = new();
        public CalibrationData EmbeddedCalibration { get; private set; }

        public CustomActionsFishing(string filePath)
        {
            var result = CustomFishingActionFileManager.Load(filePath);
            if (!result.Success) throw new InvalidDataException(result.ErrorMessage);
            steps = FishingRoute.Decode(result.File.Actions);
            EmbeddedCalibration = result.File.Calibration;
        }

        public CustomActionsFishing(List<FishingActionCommand> actions)
        {
            steps = FishingRoute.Decode(actions);
        }

        public override Task LeaveDockAndSellAsync(CancellationToken cancellationToken) => ReplayRouteAsync(cancellationToken);

        public Task ReplayRouteAsync(CancellationToken token, IProgress<int> progress = null) =>
            FishingRoute.ReplayAsync(steps,
                key => SendKeyDown(actionKeys.GetKeyCodeFromString(key).Value),
                key => SendKeyUp(actionKeys.GetKeyCodeFromString(key).Value),
                SellFishAsync, token, progress);
    }
}
