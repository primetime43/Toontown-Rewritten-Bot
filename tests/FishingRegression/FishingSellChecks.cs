using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ToonTown_Rewritten_Bot.Services.FishingLocationsWalking;
using ToonTown_Rewritten_Bot.Utilities;

internal static class FishingSellChecks
{
    private static readonly Type Interaction = typeof(FishingStrategyBase).Assembly.GetType(
        "ToonTown_Rewritten_Bot.Utilities.FishingSellInteraction");
    private static readonly MethodInfo RunMethod = Interaction.GetMethod("RunAsync", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly MethodInfo ClickPoint = Interaction.GetMethod("GetClickPoint", BindingFlags.Static | BindingFlags.NonPublic);

    private static Task Run(Func<CancellationToken, Task<Rectangle?>> find,
        Func<Point, CancellationToken, Task> click, CancellationToken token = default) =>
        (Task)RunMethod.Invoke(null, new object[] { find, click, token,
            (Func<int, CancellationToken, Task>)((_, ct) => { ct.ThrowIfCancellationRequested(); return Task.CompletedTask; }) });

    internal static async Task Run(Action<bool, string> check)
    {
        var first = new Rectangle(500, 700, 140, 104);
        var moved = new Rectangle(800, 650, 140, 104);
        var frames = new Queue<Rectangle?>(new Rectangle?[] {
            null, null, first, first, null, first, first, first, first, moved, null, null });
        var clicks = new List<Point>();
        await Run(_ => Task.FromResult(frames.Dequeue()), (point, _) =>
        {
            clicks.Add(point);
            return Task.CompletedTask;
        });
        check(clicks.Count == 2, "Selling waits for the dialog and retries an ignored click; one missed frame is not success");
        check(moved.Contains(clicks[1]) && !first.Contains(clicks[1]), "Sell retry uses the newly detected position");
        check(clicks[0].Y == 734, "Sell click targets the confirmation area above the caption gap");

        int clicksSent = 0;
        Task Click(Point _, CancellationToken ct) { clicksSent++; return Task.CompletedTask; }
        try
        {
            await Run(_ => Task.FromResult<Rectangle?>(null), Click);
            throw new Exception("Missing offer was accepted");
        }
        catch (InvalidOperationException)
        { check(clicksSent == 0, "Missing Sell button stops the route without clicking cached coordinates"); }

        try
        {
            await Run(_ => Task.FromResult<Rectangle?>(first), Click);
            throw new Exception("Unresponsive offer was accepted");
        }
        catch (InvalidOperationException)
        { check(clicksSent == 3, "Unresponsive Sell button stops after three attempts"); }

        using var cancel = new CancellationTokenSource();
        try
        {
            await Run(_ => Task.FromResult<Rectangle?>(first), (point, ct) =>
            {
                cancel.Cancel();
                return Task.CompletedTask;
            }, cancel.Token);
            throw new Exception("Cancelled sale was accepted");
        }
        catch (OperationCanceledException)
        { check(true, "Stop cancels verification without starting the return walk"); }

        bool clicked = false;
        try
        {
            await Run(_ => clicked ? throw new System.IO.IOException("Capture failed") : Task.FromResult<Rectangle?>(first),
                (point, ct) => { clicked = true; return Task.CompletedTask; });
            throw new Exception("Capture failure was accepted as success");
        }
        catch (System.IO.IOException)
        { check(true, "Capture failure cannot count as a dismissed Sell offer"); }
    }

    internal static void CheckScreenshot(string screenshotPath, string templatePath)
    {
        using var frame = new Bitmap(screenshotPath);
        using var template = new Bitmap(templatePath);
        var match = ImageTemplateMatcher.FindTemplate(frame, template, 0.85);
        if (!match.Found) throw new Exception("Real Sell offer did not match");
        Point target = (Point)ClickPoint.Invoke(null, new object[] { match.Bounds });
        Color pixel = frame.GetPixel(target.X, target.Y);
        if (pixel.B <= pixel.R || pixel.B <= pixel.G)
            throw new Exception("Real Sell target is outside the blue confirmation button");
        Console.WriteLine($"PASS: Real Sell offer matched {match.Confidence:P1}; target {target} lies on blue confirmation button.");
    }
}
