using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services.FishingLocationsWalking
{
    public class BrrrghWalrusWayFishing : FishingStrategyBase
    {
        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            // Simulation of leaving the fishing dock & walking over to the fisherman to sell
            await HoldMovementKeyAsync(VirtualKeyCode.UP, 100, cancellationToken);
            await HoldMovementKeyAsync(VirtualKeyCode.LEFT, 730, cancellationToken);
            await HoldMovementKeyAsync(VirtualKeyCode.UP, 2000, cancellationToken);

            await SellFishAsync(cancellationToken); //sell fish

            // Simulation of going back to the dock
            await HoldMovementKeyAsync(VirtualKeyCode.DOWN, 2100, cancellationToken);
            await HoldMovementKeyAsync(VirtualKeyCode.RIGHT, 700, cancellationToken);
            await HoldMovementKeyAsync(VirtualKeyCode.DOWN, 1000, cancellationToken);
        }
    }
}
