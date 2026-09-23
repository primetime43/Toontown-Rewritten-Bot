using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ToonTown_Rewritten_Bot.Services.FishingLocationsWalking;

// Run with: dotnet run --project tests/FishingRegression -- [optional catch screenshot]
// These checks sample supplied bitmaps without capturing the screen or sending input.
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

if (args.Length > 0)
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
