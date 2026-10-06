using System;
using System.Threading;
using System.Threading.Tasks;

namespace ToonTown_Rewritten_Bot.Utilities
{
    internal static class HeldInput
    {
        internal static async Task RunAsync(Action press, Action release, int milliseconds,
            CancellationToken token, Func<int, CancellationToken, Task> delay = null)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                press();
                await (delay ?? Task.Delay)(milliseconds, token).ConfigureAwait(false);
            }
            finally { release(); }
        }
    }
}
