using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToonTown_Rewritten_Bot.Models;

namespace ToonTown_Rewritten_Bot.Utilities
{
    public record FishingRouteStep(string Command, int Milliseconds = 0)
    {
        public string DisplayName => FishingRoute.Names.TryGetValue(Command, out var name) ? name : Command;
    }

    /// <summary>Converts the legacy movement/TIME pairs into editable, timed steps.</summary>
    public static class FishingRoute
    {
        public static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>
        {
            ["UP"] = "Walk forward", ["DOWN"] = "Walk backward",
            ["LEFT"] = "Turn left", ["RIGHT"] = "Turn right",
            ["UP+LEFT"] = "Walk forward + turn left", ["UP+RIGHT"] = "Walk forward + turn right",
            ["DOWN+LEFT"] = "Walk backward + turn left", ["DOWN+RIGHT"] = "Walk backward + turn right",
            ["WAIT"] = "Wait", ["SELL"] = "Sell fish"
        };

        public static List<FishingRouteStep> Decode(IReadOnlyList<FishingActionCommand> actions)
        {
            if (actions == null) throw new InvalidDataException("This route has no actions.");
            var steps = new List<FishingRouteStep>();
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i] ?? throw new InvalidDataException($"Action {i + 1} is empty.");
                string command = action.Command?.Trim().ToUpperInvariant();
                if (action.Action == "TIME" || command?.StartsWith("TIME") == true)
                {
                    steps.Add(new("WAIT", ReadTime(action.Command)));
                    continue;
                }
                if (command == null || !Names.ContainsKey(command) || command == "WAIT")
                    throw new InvalidDataException($"Action {i + 1} has an unsupported command: {action.Command}");
                int duration = command == "SELL" ? 0 : 500;
                if (command != "SELL" && i + 1 < actions.Count && actions[i + 1]?.Action == "TIME")
                    duration = ReadTime(actions[++i].Command);
                steps.Add(new(command, duration));
            }
            return steps;
        }

        private static int ReadTime(string text)
        {
            int duration = DurationFormatter.ParseToMilliseconds(text);
            if (duration <= 0) throw new InvalidDataException($"Invalid step duration: '{text}'. Use a time greater than zero.");
            return duration;
        }

        public static List<FishingActionCommand> Encode(IEnumerable<FishingRouteStep> steps)
        {
            var actions = new List<FishingActionCommand>();
            foreach (var step in steps)
            {
                if (!Names.ContainsKey(step.Command) || (step.Command != "SELL" && step.Milliseconds <= 0))
                    throw new InvalidDataException("Every movement or wait needs a positive duration.");
                if (step.Command != "WAIT")
                    actions.Add(new FishingActionCommand { Action = step.Command switch
                    {
                        "UP" => "WALK FORWARDS", "DOWN" => "WALK BACKWARDS",
                        "LEFT" => "TURN LEFT", "RIGHT" => "TURN RIGHT", "SELL" => "SELL FISH",
                        _ => step.DisplayName.ToUpperInvariant()
                    }, Command = step.Command });
                if (step.Command != "SELL")
                    actions.Add(new FishingActionCommand { Action = "TIME", Command = step.Milliseconds.ToString(CultureInfo.InvariantCulture) });
            }
            return actions;
        }

        public static string Validate(IReadOnlyList<FishingRouteStep> steps)
        {
            if (steps.Count(s => s.Command == "SELL") != 1) return "Add exactly one Sell fish step between the outward and return paths.";
            int sell = steps.ToList().FindIndex(s => s.Command == "SELL");
            if (!steps.Take(sell).Any(s => s.Command != "WAIT")) return "Record or add the walk to the fisherman first.";
            if (!steps.Skip(sell + 1).Any(s => s.Command != "WAIT")) return "Record or add the walk back to the dock.";
            return null;
        }

        // Input delegates make cancellation and key release verifiable without controlling the game.
        public static async Task ReplayAsync(IEnumerable<FishingRouteStep> steps, Action<string> keyDown,
            Action<string> keyUp, Func<CancellationToken, Task> sell, CancellationToken token,
            IProgress<int> progress = null, Func<int, CancellationToken, Task> delay = null)
        {
            delay ??= Task.Delay;
            int index = 0;
            foreach (var step in steps)
            {
                token.ThrowIfCancellationRequested();
                progress?.Report(index++);
                if (step.Command == "SELL")
                {
                    await sell(token).ConfigureAwait(false);
                    await delay(3000, token).ConfigureAwait(false);
                    continue;
                }
                var held = new List<string>();
                try
                {
                    if (step.Command != "WAIT")
                        foreach (string key in step.Command.Split('+'))
                        {
                            held.Add(key);
                            keyDown(key);
                        }
                    await delay(step.Milliseconds, token).ConfigureAwait(false);
                }
                finally
                {
                    foreach (string key in held) keyUp(key);
                }
            }
        }
    }

    /// <summary>Records changes in held movement keys, including walking while turning.</summary>
    public sealed class FishingRouteRecorder
    {
        private readonly HashSet<string> held = new();
        private long changedAt;
        public List<FishingRouteStep> Steps { get; } = new();

        public void Change(string key, bool down, long milliseconds)
        {
            if (held.Contains(key) == down) return; // Ignore keyboard auto-repeat.
            Flush(milliseconds);
            if (down) held.Add(key); else held.Remove(key);
        }

        public void ReleaseAll(long milliseconds)
        {
            Flush(milliseconds);
            held.Clear();
        }

        private void Flush(long now)
        {
            string command = string.Join("+", new[] { "UP", "DOWN", "LEFT", "RIGHT" }.Where(held.Contains));
            int duration = (int)Math.Min(int.MaxValue, Math.Max(0, now - changedAt));
            if (duration > 0 && FishingRoute.Names.ContainsKey(command))
            {
                if (Steps.Count > 0 && Steps[^1].Command == command && Steps[^1].Milliseconds <= int.MaxValue - duration)
                    Steps[^1] = Steps[^1] with { Milliseconds = Steps[^1].Milliseconds + duration };
                else Steps.Add(new(command, duration));
            }
            changedAt = now;
        }
    }
}
