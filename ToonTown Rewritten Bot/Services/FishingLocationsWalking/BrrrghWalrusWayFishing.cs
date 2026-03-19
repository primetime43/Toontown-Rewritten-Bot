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
            SendKeyDown(VirtualKeyCode.UP); ;
            await Task.Delay(100, cancellationToken);
            SendKeyUp(VirtualKeyCode.UP);
            SendKeyDown(VirtualKeyCode.LEFT);
            await Task.Delay(730, cancellationToken);
            SendKeyUp(VirtualKeyCode.LEFT);
            SendKeyDown(VirtualKeyCode.UP); ;
            await Task.Delay(2000, cancellationToken);
            SendKeyUp(VirtualKeyCode.UP);

            await SellFishAsync(cancellationToken); //sell fish

            // Simulation of going back to the dock
            SendKeyDown(VirtualKeyCode.DOWN);
            await Task.Delay(2100, cancellationToken);
            SendKeyUp(VirtualKeyCode.DOWN);
            SendKeyDown(VirtualKeyCode.RIGHT);
            await Task.Delay(700, cancellationToken);
            SendKeyUp(VirtualKeyCode.RIGHT);
            SendKeyDown(VirtualKeyCode.DOWN);
            await Task.Delay(1000, cancellationToken);
            SendKeyUp(VirtualKeyCode.DOWN);
        }
    }
}
