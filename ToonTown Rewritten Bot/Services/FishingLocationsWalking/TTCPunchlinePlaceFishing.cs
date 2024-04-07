using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ToonTown_Rewritten_Bot.Views;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services.FishingLocations
{
    public class TTCPunchlinePlaceFishing : FishingStrategyBase
    {
        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            // Simulation of leaving the fishing dock & walking over to the fisherman to sell
            InputSimulator.SimulateKeyDown(VirtualKeyCode.DOWN);
            await Task.Delay(2000, cancellationToken);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.DOWN);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.RIGHT);
            await Task.Delay(800, cancellationToken);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.RIGHT);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            await Task.Delay(700, cancellationToken);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);

            await SellFishAsync(cancellationToken); // Call to sell fish asynchronously

            // Simulation of going back to the dock
            InputSimulator.SimulateKeyDown(VirtualKeyCode.DOWN);
            await Task.Delay(700, cancellationToken);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.DOWN);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.LEFT);
            await Task.Delay(750, cancellationToken);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.LEFT);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            await Task.Delay(2000, cancellationToken);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);
        }
    }
}
