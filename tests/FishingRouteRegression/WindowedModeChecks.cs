using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Newtonsoft.Json;
using ToonTown_Rewritten_Bot;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Services;
using ToonTown_Rewritten_Bot.Utilities;

internal static class WindowedModeChecks
{
    private static int passed;
    private static readonly Type Geometry = typeof(MainForm).Assembly.GetType("ToonTown_Rewritten_Bot.Utilities.GameWindowGeometry");
    private static readonly Type Capture = typeof(MainForm).Assembly.GetType("ToonTown_Rewritten_Bot.Utilities.ImageRecognition");

    public static void Run()
    {
        var size = new Size(800, 600);
        var manual = new CoordinateActions { X = 150, Y = 220, CoordinateSpace = "Client", ClientSize = size, ClientDpi = 96 };
        Check(manual.ResolveScreenPoint(new Rectangle(100, 200, 800, 600), 96) == new Point(250, 420), "Manual clicks include the client origin once");
        Check(manual.ResolveScreenPoint(new Rectangle(-1000, 50, 800, 600), 96) == new Point(-850, 270), "Manual clicks follow moved windows, including negative monitor coordinates");
        Check(!manual.ResolveScreenPoint(new Rectangle(0, 0, 1024, 768), 96).HasValue, "Resizing rejects stale manual coordinates");
        Check(!manual.ResolveScreenPoint(new Rectangle(Point.Empty, size), 144).HasValue, "DPI changes reject stale manual coordinates");
        Check(!new CoordinateActions { X = 900, Y = 700 }.ResolveScreenPoint(new Rectangle(0, 0, 1600, 900), 96).HasValue,
            "Legacy absolute positions are preserved but not guessed in a new coordinate space");
        var restored = JsonConvert.DeserializeObject<CoordinateActions>(JsonConvert.SerializeObject(manual));
        Check(restored.ResolveScreenPoint(new Rectangle(100, 200, 800, 600), 96) == new Point(250, 420), "Manual coordinate geometry survives JSON round trip");
        var element = new UIElementData { CachedCenter = new Point(150, 220), CachedClientSize = size, CachedDpi = 96 };
        Check(element.IsCacheValidFor(size, 96), "Cached client coordinates remain valid when only the window position changes");
        Check(!element.IsCacheValidFor(new Size(1024, 768), 96) && !element.IsCacheValidFor(size, 144), "Template cache invalidates after resize or DPI change");
        Check(!new UIElementData { CachedCenter = new Point(150, 220) }.IsCacheValidFor(size, 96), "Old window-relative caches cannot cause a border offset");

        using var window = new CaptureFixture { ClientSize = new Size(320, 240), StartPosition = FormStartPosition.Manual,
            Location = new Point(Screen.PrimaryScreen.WorkingArea.Left + 30, Screen.PrimaryScreen.WorkingArea.Top + 30),
            TopMost = true, ShowInTaskbar = false, Text = "Window capture regression fixture" };
        window.Show();
        window.Update();
        VerifyCapture(window);
        window.Location = new Point(window.Left + 80, window.Top + 60);
        window.ClientSize = new Size(480, 320);
        window.Invalidate();
        window.Update();
        VerifyCapture(window);
        window.FormBorderStyle = FormBorderStyle.None;
        window.Invalidate();
        window.Update();
        VerifyCapture(window);
        using (var cover = new CaptureCover { Bounds = window.Bounds, BackColor = Color.Lime,
            TopMost = true, ShowInTaskbar = false, FormBorderStyle = FormBorderStyle.None })
        {
            cover.Show();
            cover.Update();
            using var covered = CaptureFrame(window.Handle);
            Check(covered.GetPixel(5, 5).ToArgb() == Color.Red.ToArgb()
                && covered.GetPixel(covered.Width - 5, covered.Height - 5).ToArgb() == Color.Blue.ToArgb(),
                "Background capture reads the target behind another window");
            using var next = CaptureFrame(window.Handle);
            Check(next.GetPixel(next.Width / 2 + 5, next.Height / 2 + 5)
                != covered.GetPixel(covered.Width / 2 + 5, covered.Height / 2 + 5),
                "Covered-window capture continues receiving new rendered frames");
        }
        window.WindowState = FormWindowState.Minimized;
        try
        {
            using var unexpected = CaptureFrame(window.Handle);
            throw new Exception("Minimized window capture was accepted");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
        {
            Check(true, "Minimized capture fails clearly instead of producing stale pixels");
        }
        Restore(window.Handle);
        Check(window.WindowState == FormWindowState.Normal, "Focus preparation restores a minimized window");
        typeof(MainForm).Assembly.GetType("ToonTown_Rewritten_Bot.Utilities.GameGraphicsCapture")
            .GetMethod("Stop", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        Console.WriteLine($"{passed} windowed-mode checks passed.");
    }

    private static void VerifyCapture(Form window)
    {
        window.Refresh();
        Application.DoEvents();
        System.Threading.Thread.Sleep(150); // Allow the compositor to present the resized surface.
        var args = new object[] { window.Handle, null };
        Check((bool)Geometry.GetMethod("TryRead").Invoke(null, args), "Read native window geometry");
        var bounds = (Rectangle)Geometry.GetProperty("ClientBounds").GetValue(args[1]);
        var originalBounds = window.Bounds;
        Restore(window.Handle);
        Check(window.Bounds == originalBounds && window.WindowState == FormWindowState.Normal, "Focus preparation preserves window size and position");
        Check(bounds == window.RectangleToScreen(window.ClientRectangle), "Native geometry matches the playable area, excluding borders");
        foreach (bool background in new[] { true, false })
        {
            using var frame = CaptureFrame(window.Handle, background);
            Check(frame.Size == window.ClientSize, "Captured pixels have the exact client dimensions (background=" + background + ")");
            var topLeft = frame.GetPixel(5, 5);
            var bottomRight = frame.GetPixel(frame.Width - 5, frame.Height - 5);
            Check(topLeft.ToArgb() == Color.Red.ToArgb() && bottomRight.ToArgb() == Color.Blue.ToArgb(),
                $"Capture removes decorations without shifting corner pixels (background={background}, topLeft={topLeft}, bottomRight={bottomRight})");
        }
    }

    private static Bitmap CaptureFrame(IntPtr handle, bool background = true)
    {
        // The fixture is in this process, unlike the game. Keep its message pump
        // running while Windows delivers the captured surface to the worker.
        var pending = System.Threading.Tasks.Task.Run(() => (Bitmap)Capture
            .GetMethod("CaptureGameClient", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { handle, background }));
        while (!pending.IsCompleted)
        {
            Application.DoEvents();
            System.Threading.Thread.Sleep(10);
        }
        return pending.GetAwaiter().GetResult();
    }
    private static void Restore(IntPtr handle) => typeof(CoreFunctionality).GetMethod("RestoreGameWindowIfMinimized", BindingFlags.Static | BindingFlags.NonPublic)
        .Invoke(null, new object[] { handle });
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        passed++;
        Console.WriteLine("PASS " + name);
    }

    private sealed class CaptureCover : Form
    {
        protected override bool ShowWithoutActivation => true;
    }

    private sealed class CaptureFixture : Form
    {
        private readonly Timer renderTimer = new() { Interval = 30 };
        private int frameNumber;

        public CaptureFixture()
        {
            renderTimer.Tick += (_, _) => { frameNumber++; Invalidate(); };
            renderTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) renderTimer.Dispose();
            base.Dispose(disposing);
        }

        protected override bool ShowWithoutActivation => true;
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.White);
            e.Graphics.FillRectangle(Brushes.Red, 0, 0, ClientSize.Width / 2, ClientSize.Height / 2);
            e.Graphics.FillRectangle(Brushes.Blue, ClientSize.Width / 2, ClientSize.Height / 2, ClientSize.Width, ClientSize.Height);
            // Like a game, keep presenting changing frames while capture is running.
            using var marker = new SolidBrush(Color.FromArgb(frameNumber % 256, 128, 128));
            e.Graphics.FillRectangle(marker, ClientSize.Width / 2, ClientSize.Height / 2, 20, 20);
        }
    }
}
