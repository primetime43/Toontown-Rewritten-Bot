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
            SendKeyDown(VirtualKeyCode.DOWN);
            await Task.Delay(2000, cancellationToken);
            SendKeyUp(VirtualKeyCode.DOWN);
            SendKeyDown(VirtualKeyCode.RIGHT);
            await Task.Delay(800, cancellationToken);
            SendKeyUp(VirtualKeyCode.RIGHT);
            SendKeyDown(VirtualKeyCode.UP);
            await Task.Delay(700, cancellationToken);
            SendKeyUp(VirtualKeyCode.UP);

            await SellFishAsync(cancellationToken); // Call to sell fish asynchronously

            // Simulation of going back to the dock
            SendKeyDown(VirtualKeyCode.DOWN);
            await Task.Delay(600, cancellationToken);
            SendKeyUp(VirtualKeyCode.DOWN);
            SendKeyDown(VirtualKeyCode.LEFT);
            await Task.Delay(750, cancellationToken);
            SendKeyUp(VirtualKeyCode.LEFT);
            SendKeyDown(VirtualKeyCode.UP);
            await Task.Delay(2000, cancellationToken);
            SendKeyUp(VirtualKeyCode.UP);
        }
    }
}
