using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services.FishingLocations
{
    public class BrrrghWalrusWayFishing : FishingStrategyBase
    {
        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            // Simulation of leaving the fishing dock & walking over to the fisherman to sell
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP); ;
            Thread.Sleep(100);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            Thread.Sleep(730);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP); ;
            Thread.Sleep(2000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            await SellFishAsync(cancellationToken); //sell fish

            // Simulation of going back to the dock
            InputSimulator.SimulateKeyDown(VirtualKeyCode.DOWN);
            Thread.Sleep(2100);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.DOWN);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.RIGHT);
            Thread.Sleep(700);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.RIGHT);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.DOWN);
            Thread.Sleep(1000);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.DOWN);
        }
    }
}
