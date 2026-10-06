using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ToonTown_Rewritten_Bot.Services.FishingLocationsWalking;
using ToonTown_Rewritten_Bot.Utilities;

internal static class FishingLifecycleChecks
{
    private const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    internal static async Task Run(Action<bool, string> check)
    {
        var hold = typeof(FishingStrategyBase).Assembly.GetType("ToonTown_Rewritten_Bot.Utilities.HeldInput")
            .GetMethod("RunAsync", BindingFlags.Static | BindingFlags.NonPublic);
        Task Hold(Action down, Action up, CancellationToken ct, Func<int, CancellationToken, Task> delay) =>
            (Task)hold.Invoke(null, new object[] { down, up, 730, ct, delay });
        var events = new List<string>();
        await Hold(() => events.Add("down"), () => events.Add("up"), default,
            (ms, ct) => { events.Add(ms.ToString()); return Task.CompletedTask; });
        check(string.Join(",", events) == "down,730,up", "Walking keeps its recorded duration and releases the key");
        events.Clear();
        using var stop = new CancellationTokenSource();
        try
        {
            await Hold(() => events.Add("down"), () => events.Add("up"), stop.Token,
                (ms, ct) => { stop.Cancel(); return Task.FromCanceled(ct); });
            throw new Exception("Stop was ignored");
        }
        catch (OperationCanceledException)
        { check(string.Join(",", events) == "down,up", "Stop during a walk releases the held movement key"); }
        events.Clear();
        try { await Hold(() => events.Add("down"), () => events.Add("up"), stop.Token, Task.Delay); }
        catch (OperationCanceledException) { }
        check(events.Count == 0, "Already-stopped routes never press another key");
        try
        {
            await Hold(() => throw new InvalidOperationException("Press failed"), () => events.Add("up"), default, Task.Delay);
            throw new Exception("Input failure was ignored");
        }
        catch (InvalidOperationException)
        { check(events.Count == 1 && events[0] == "up", "An input failure still attempts release"); }

        events.Clear();
        try
        {
            await FishingRoute.ReplayAsync(new[] { new FishingRouteStep("UP+LEFT", 50) },
                key => { }, key => { events.Add(key); if (key == "UP") throw new InvalidOperationException("Release failed"); },
                _ => Task.CompletedTask, default, delay: (_, _) => Task.CompletedTask);
            throw new Exception("Release failure was ignored");
        }
        catch (InvalidOperationException)
        { check(string.Join(",", events) == "UP,LEFT", "Custom routes release remaining keys even if one release fails"); }

        var strategy = new Probe();
        var bucket = typeof(FishingStrategyBase).GetProperty("BucketWasFull");
        bucket.SetValue(strategy, true);
        strategy.SeedCounts();
        typeof(FishingStrategyBase).GetField("_cachedRedButtonPos", Hidden).SetValue(strategy, new Point(700, 800));
        typeof(FishingStrategyBase).GetMethod("ResetRoundState", Hidden).Invoke(strategy, null);
        check(!strategy.BucketWasFull, "An earlier full bucket cannot skip exiting the dock in later rounds");
        check(strategy.RoundIsEmpty && strategy.SessionCastCount == 24 && strategy.SessionFishCaught == 20,
            "Starting another round clears round counts while preserving session totals");
        check(typeof(FishingStrategyBase).GetField("_cachedRedButtonPos", Hidden).GetValue(strategy) == null,
            "New rounds discard the previous dock-button position");

        var type = typeof(FishingStrategyBase).Assembly.GetType("ToonTown_Rewritten_Bot.Utilities.AutomationSessionGate");
        var gate = Activator.CreateInstance(type);
        var enter = type.GetMethod("TryEnter", Hidden);
        var active = type.GetProperty("ActiveName", Hidden);
        int finished = 0;
        var fishing = (IDisposable)enter.Invoke(gate, new object[] { "Fishing", (Action)(() => finished++) });
        check(enter.Invoke(gate, new object[] { "Golf", null }) == null && (string)active.GetValue(gate) == "Fishing",
            "Another activity cannot replace fishing's input mode or cancellation source");
        fishing.Dispose();
        fishing.Dispose();
        check(finished == 1 && active.GetValue(gate) == null, "Session cleanup releases ownership exactly once");
        using var next = (IDisposable)enter.Invoke(gate, new object[] { "Golf", null });
        check(next != null, "Another activity can start after cleanup finishes");
    }

    private sealed class Probe : FishingStrategyBase
    {
        internal void SeedCounts() { _castCount = 4; _fishCaught = 3; _sessionCastCount = 24; _sessionFishCaught = 20; }
        internal bool RoundIsEmpty => _castCount == 0 && _fishCaught == 0;
        public override Task LeaveDockAndSellAsync(CancellationToken token) => throw new NotSupportedException();
    }
}
