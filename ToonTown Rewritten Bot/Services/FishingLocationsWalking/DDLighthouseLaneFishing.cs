using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services.FishingLocationsWalking
{
    public class DDLighthouseLaneFishing : FishingStrategyBase
    {
        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            // Simulation of leaving the fishing dock & walking over to the fisherman to sell
            await HoldMovementKeyAsync(VirtualKeyCode.RIGHT, 330, cancellationToken);
            await HoldMovementKeyAsync(VirtualKeyCode.UP, 2200, cancellationToken);

            await SellFishAsync(cancellationToken);

            // Simulation of going back to the dock
            await HoldMovementKeyAsync(VirtualKeyCode.DOWN, 4500, cancellationToken);
        }
    }
}
