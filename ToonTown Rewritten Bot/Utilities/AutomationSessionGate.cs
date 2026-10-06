using System;
using System.Threading;

namespace ToonTown_Rewritten_Bot.Utilities
{
    // Accessed on the UI thread. A session remains owned until its async handler finishes cleanup.
    internal sealed class AutomationSessionGate
    {
        internal string ActiveName { get; private set; }

        internal IDisposable TryEnter(string name, Action finished = null)
        {
            if (ActiveName != null) return null;
            ActiveName = name;
            return new Session(() => { ActiveName = null; finished?.Invoke(); });
        }

        private sealed class Session(Action release) : IDisposable
        {
            private Action _release = release;
            public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
        }
    }
}
