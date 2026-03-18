using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services.FishingLocationsWalking
{
    public class BrrrghPolarPlaceFishing : FishingStrategyBase
    {
        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            // Simulation of leaving the fishing dock & walking over to the fisherman to sell
            SendKeyDown(VirtualKeyCode.RIGHT);
            await Task.Delay(800, cancellationToken);
            SendKeyUp(VirtualKeyCode.RIGHT);
            SendKeyDown(VirtualKeyCode.UP); ;
            await Task.Delay(2000, cancellationToken);
            SendKeyUp(VirtualKeyCode.UP);

            await SellFishAsync(cancellationToken);

            // Simulation of going back to the dock
            SendKeyDown(VirtualKeyCode.DOWN);
            await Task.Delay(2000, cancellationToken);
            SendKeyUp(VirtualKeyCode.DOWN);
        }
    }
}
