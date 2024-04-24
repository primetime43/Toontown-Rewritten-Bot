using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services.FishingLocationsWalking
{
    public class MMTenorTerraceFishing : FishingStrategyBase
    {
        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            // Simulation of leaving the fishing dock & walking over to the fisherman to sell
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            await Task.Delay(1090, cancellationToken);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP); ;
            await Task.Delay(2200, cancellationToken);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            await SellFishAsync(cancellationToken);

            // Simulation of going back to the dock
            InputSimulator.SimulateKeyDown(VirtualKeyCode.DOWN);
            await Task.Delay(3000, cancellationToken);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.DOWN);
        }
    }
}
