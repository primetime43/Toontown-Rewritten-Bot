using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services.FishingLocations
{
    public class DDLLullabyLaneFishing : FishingStrategyBase
    {
        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            InputSimulator.SimulateKeyDown(VirtualKeyCode.UP);
            await Task.Delay(4000, cancellationToken);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.UP);
            await SellFishAsync(cancellationToken);
            InputSimulator.SimulateKeyDown(VirtualKeyCode.DOWN);
            await Task.Delay(6500, cancellationToken);
            InputSimulator.SimulateKeyUp(VirtualKeyCode.DOWN);
        }
    }
}
