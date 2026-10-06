using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services.FishingLocationsWalking
{
    public class BrrrghSleetStFishing : FishingStrategyBase
    {
        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            // Simulation of leaving the fishing dock & walking over to the fisherman to sell
            await HoldMovementKeyAsync(VirtualKeyCode.DOWN, 600, cancellationToken);
            await HoldMovementKeyAsync(VirtualKeyCode.RIGHT, 850, cancellationToken);
            await HoldMovementKeyAsync(VirtualKeyCode.UP, 1000, cancellationToken);

            await SellFishAsync(cancellationToken);

            // Simulation of going back to the dock
            await HoldMovementKeyAsync(VirtualKeyCode.DOWN, 1700, cancellationToken);
            await HoldMovementKeyAsync(VirtualKeyCode.LEFT, 850, cancellationToken);
            await HoldMovementKeyAsync(VirtualKeyCode.UP, 600, cancellationToken);
        }
    }
}
