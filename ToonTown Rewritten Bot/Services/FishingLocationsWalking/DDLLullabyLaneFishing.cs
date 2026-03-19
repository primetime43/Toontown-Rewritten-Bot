using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace ToonTown_Rewritten_Bot.Services.FishingLocationsWalking
{
    public class DDLLullabyLaneFishing : FishingStrategyBase
    {
        public override async Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        {
            SendKeyDown(VirtualKeyCode.UP);
            await Task.Delay(4000, cancellationToken);
            SendKeyUp(VirtualKeyCode.UP);
            await SellFishAsync(cancellationToken);
            SendKeyDown(VirtualKeyCode.DOWN);
            await Task.Delay(6500, cancellationToken);
            SendKeyUp(VirtualKeyCode.DOWN);
        }
    }
}
