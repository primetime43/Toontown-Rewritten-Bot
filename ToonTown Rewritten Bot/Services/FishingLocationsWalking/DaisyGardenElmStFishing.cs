using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services.FishingLocationsWalking
{
    public class DaisyGardenElmStFishing : FishingStrategyBase
    {
        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            // Simulation of leaving the fishing dock & walking over to the fisherman to sell
            SendKeyDown(VirtualKeyCode.LEFT);
            await Task.Delay(80, cancellationToken);
            SendKeyUp(VirtualKeyCode.LEFT);
            SendKeyDown(VirtualKeyCode.UP); ;
            await Task.Delay(2000, cancellationToken);
            SendKeyUp(VirtualKeyCode.UP);

            await SellFishAsync(cancellationToken);

            // Simulation of going back to the dock
            SendKeyDown(VirtualKeyCode.DOWN);
            await Task.Delay(4500, cancellationToken);
            SendKeyUp(VirtualKeyCode.DOWN);
        }
    }
}
