using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ToonTown_Rewritten_Bot.Views;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services.FishingLocationsWalking
{
    public class TTCPunchlinePlaceFishing : FishingStrategyBase
    {
        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            // Simulation of leaving the fishing dock & walking over to the fisherman to sell
            await HoldMovementKeyAsync(VirtualKeyCode.DOWN, 2000, cancellationToken);
            await HoldMovementKeyAsync(VirtualKeyCode.RIGHT, 800, cancellationToken);
            await HoldMovementKeyAsync(VirtualKeyCode.UP, 700, cancellationToken);

            await SellFishAsync(cancellationToken); // Call to sell fish asynchronously

            // Simulation of going back to the dock
            await HoldMovementKeyAsync(VirtualKeyCode.DOWN, 600, cancellationToken);
            await HoldMovementKeyAsync(VirtualKeyCode.LEFT, 750, cancellationToken);
            await HoldMovementKeyAsync(VirtualKeyCode.UP, 2000, cancellationToken);
        }
    }
}
