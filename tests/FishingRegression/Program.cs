using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ToonTown_Rewritten_Bot.Services.FishingLocationsWalking;

// Run with: dotnet run --project tests/FishingRegression -- [optional catch screenshot]
// Default checks sample supplied bitmaps; opt-in live probes never send input.
if (args.Length > 0 && args[0] == "--analyze-shadows")
{
    ShadowSelectionProbe.Analyze(args[1], args[2..]);
    return;
}
if (args.Length > 0 && args[0] == "--capture-clash-background")
{
    ClashCaptureProbe.CaptureBackground(args[1]);
    return;
}
if (args.Length > 0 && args[0] == "--capture-ttr-background")
{
    ClashCaptureProbe.CaptureBackground(args[1], "Rewritten");
    return;
}
if (args.Length > 0 && args[0] == "--watch-ttr-sell")
{
    SellButtonProbe.Watch(args[1], args[2]);
    return;
}
if (args.Length > 0 && args[0] == "--check-sell")
{
    await FishingSellChecks.Run((ok, name) =>
    {
        if (!ok) throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    });
    if (args.Length > 2) FishingSellChecks.CheckScreenshot(args[1], args[2]);
    return;
}
if (args.Length > 0 && args[0] == "--check-clash-input")
{
    var access = typeof(FishingStrategyBase).Assembly.GetType("ToonTown_Rewritten_Bot.Utilities.GameInputAccess");
    var flags = BindingFlags.Static | BindingFlags.NonPublic;
    var elevated = access.GetMethod("IsElevated", flags);
    var ensure = access.GetMethod("EnsureAllowed", flags);
    bool botElevated = (bool)elevated.Invoke(null, new object[] { (uint)Environment.ProcessId });
    var games = System.Diagnostics.Process.GetProcessesByName("CorporateClash");
    if (games.Length == 0) throw new Exception("Start Clash before running the input permission probe.");
    foreach (var game in games)
    {
        using (game)
        {
            bool gameElevated = (bool)elevated.Invoke(null, new object[] { (uint)game.Id });
            Console.WriteLine($"Game elevated={gameElevated}, probe elevated={botElevated}");
            try
            {
                ensure.Invoke(null, new object[] { game.MainWindowHandle });
                if (gameElevated && !botElevated) throw new Exception("Blocked input was not reported.");
                Console.WriteLine("PASS: Compatible privileges accepted without sending input.");
            }
            catch (TargetInvocationException ex) when (gameElevated && !botElevated &&
                ex.InnerException is InvalidOperationException && ex.InnerException.Message.Contains("administrator"))
            {
                Console.WriteLine("PASS: Elevated game rejected with an actionable message before sending input.");
            }
        }
    }
    return;
}
if (args.Length > 0 && args[0] == "--watch-clash-catch")
{
    ClashCaptureProbe.Watch(args[1], args[2]);
    return;
}
var strategy = new DetectionProbe();
var detect = typeof(FishingStrategyBase).GetMethod("CheckIfFishCaughtCore",
    BindingFlags.Instance | BindingFlags.NonPublic)
    ?? throw new Exception("Catch detection method not found");
int passed = 0;

bool Detect(Bitmap frame, Point offset) => (bool)detect.Invoke(strategy,
    new object[] { new Rectangle(offset, frame.Size), frame, offset });

void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    passed++;
}

using (var frame = new Bitmap(800, 600))
using (var graphics = Graphics.FromImage(frame))
{
    graphics.Clear(Color.FromArgb(50, 200, 200));
    Check(!Detect(frame, Point.Empty), "Unobscured teal water is not a catch");
    using var popupBackground = new SolidBrush(Color.FromArgb(255, 255, 190));
    graphics.FillRectangle(popupBackground, 320, 30, 160, 250);
    Check(Detect(frame, Point.Empty), "Catch card colors work without a close-button template");
    Check(Detect(frame, new Point(-1200, 80)), "Moved-window color sampling uses the window offset");
}

FishTargetSelectorChecks.Run(Check);
FallbackShadowChecks.Run(Check);
LowContrastShadowChecks.Run(Check);
await FishingSellChecks.Run(Check);

if (args.Length > 0 && args[0] == "--clash")
{
    var clashType = typeof(FishingStrategyBase).Assembly.GetType("ToonTown_Rewritten_Bot.Utilities.ClashFishingDetector");
    var find = clashType.GetMethod("FindCatchButton", BindingFlags.Static | BindingFlags.NonPublic);
    Point? Find(Bitmap frame, string[] paths, CancellationToken token = default) =>
        (Point?)find.Invoke(null, new object[] { frame, paths, token });
    using var pond = new Bitmap(args[1]);
    string[] templates = { args[2] };
    Check(Detect(pond, Point.Empty), "Real Clash pond reproduces the old color-based false catch");
    Check(!Find(pond, templates).HasValue, "Real Clash pond does not count as a catch");
    Check(!Find(pond, Array.Empty<string>()).HasValue, "Missing Clash templates never fall back to pond colors");
    using var cancel = new CancellationTokenSource();
    cancel.Cancel();
    try { Find(pond, templates, cancel.Token); throw new Exception("Cancelled match was accepted"); }
    catch (TargetInvocationException ex) when (ex.InnerException is OperationCanceledException)
    { Check(true, "Stop cancels Clash popup matching"); }
    if (args.Length > 3)
    {
        using var caught = new Bitmap(args[3]);
        var location = Find(caught, templates);
        Check(location.HasValue, "Real Clash catch popup is detected");
        Check(new Rectangle(Point.Empty, caught.Size).Contains(location.Value), "Clash close target stays in client coordinates");
    }
}
else if (args.Length > 0)
{
    using var screenshot = new Bitmap(args[0]);
    Check(Detect(screenshot, Point.Empty), "Real catch screenshot is detected");
    Check(Detect(screenshot, new Point(200, 100)), "Real catch screenshot is detected with a window offset");
}

Console.WriteLine($"{passed} fishing regression checks passed.");

sealed class DetectionProbe : FishingStrategyBase
{
    public override Task LeaveDockAndSellAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException("This probe never controls the game.");
}
