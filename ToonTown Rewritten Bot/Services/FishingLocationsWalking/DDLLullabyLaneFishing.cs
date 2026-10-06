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
            await HoldMovementKeyAsync(VirtualKeyCode.UP, 4000, cancellationToken);
            await SellFishAsync(cancellationToken);
            await HoldMovementKeyAsync(VirtualKeyCode.DOWN, 6500, cancellationToken);
        }
    }
}
