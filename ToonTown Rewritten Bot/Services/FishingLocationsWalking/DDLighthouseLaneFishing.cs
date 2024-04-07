using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services.FishingLocations
{
    public class DDLighthouseLaneFishing : FishingStrategyBase
    {
        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            // Simulation of leaving the fishing dock & walking over to the fisherman to sell
            InputSimulator.SimulateKeyDown(VirtualKeyCode.RIGHT);
            Thread.Sleep(330);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.RIGHT);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP); ;
            Thread.Sleep(2200);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            await SellFishAsync(cancellationToken);

            // Simulation of going back to the dock
            InputSimulator.SimulateKeyDown(VirtualKeyCode.DOWN);
            Thread.Sleep(4500);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.DOWN);
        }
    }
}
