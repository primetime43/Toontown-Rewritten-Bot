using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Utilities;
using ToonTown_Rewritten_Bot.Views;

internal static class Program
{
    private static int _passed;
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            using var frame = new Bitmap(1600, 900);
            using (var graphics = Graphics.FromImage(frame))
            {
                graphics.Clear(Color.FromArgb(90, 150, 180));
                graphics.FillEllipse(Brushes.DarkSlateGray, 900, 200, 180, 90);
            }
            using var form = CreateForm(frame, null);
            Layout(form);
            Check(!Field<Button>(form, "_saveButton").Enabled, "New calibration requires both samples");
            var box = Field<PictureBox>(form, "_screenshotBox");
            var bounds = (Rectangle)Call(form, "ImageBounds");
            Check(Call(form, "ImagePoint", new Point(bounds.Right, bounds.Top)) == null, "Right edge outside image is ignored");
            Check(Call(form, "ImagePoint", new Point(bounds.Left, bounds.Top - 1)) == null, "Letterbox clicks are ignored");
            Click(form, new Point(bounds.Left + bounds.Width / 4, bounds.Top + bounds.Height / 2));
            Check(Field<Color?>(form, "_waterColor") == Color.FromArgb(90, 150, 180), "Water click samples the source pixels");
            Check(Field<object>(form, "_target").ToString() == "Shadow", "Sampling water advances to shadow");
            Click(form, new Point(bounds.Left + 990 * bounds.Width / 1600, bounds.Top + 245 * bounds.Height / 900));
            Check(Field<Color?>(form, "_shadowColor") == Color.DarkSlateGray.ToArgbColor(), "Shadow click samples the source pixels");
            Check(Field<Button>(form, "_saveButton").Enabled, "Two colors enable Save");
            var shadow = Field<Color?>(form, "_shadowColor");
            Click(form, new Point(bounds.Left + 20, bounds.Top + 20));
            Check(Field<Color?>(form, "_shadowColor") == shadow, "Ready state does not accidentally overwrite samples");
            Call(form, "UndoSample");
            Check(Field<Color?>(form, "_shadowColor") == null && Field<Color?>(form, "_waterColor").HasValue,
                "Undo restores the prior sample and disables Save");
            Call(form, "ClearSamples");
            Call(form, "UndoSample");
            Check(Field<Color?>(form, "_waterColor").HasValue, "Clear samples can be undone");

            var saved = new PondColorManager.PondColorData(Color.CadetBlue, Color.DarkSlateGray, 9, 22, 31);
            using var existing = CreateForm(frame, saved);
            Layout(existing);
            Check(Field<NumericUpDown>(existing, "_redTolerance").Value == 9 &&
                  Field<NumericUpDown>(existing, "_greenTolerance").Value == 22 &&
                  Field<NumericUpDown>(existing, "_blueTolerance").Value == 31,
                "Reopening preserves independent channel tolerances");
            Check(!(bool)Call(existing, "HasUnsavedChanges"), "Opening saved calibration does not mark it modified");
            var waterLabel = Field<Label>(existing, "_waterLabel");
            Check(waterLabel.Bottom <= waterLabel.Parent.ClientSize.Height, "Color values remain inside their sample row");
            Field<NumericUpDown>(existing, "_redTolerance").Value = 10;
            Check((bool)Call(existing, "HasUnsavedChanges"), "Tolerance edits are tracked as unsaved");
            CheckLayout(existing, new Size(900, 680));
            CheckLayout(existing, new Size(1400, 900));
            CheckRefresh(frame, saved);

            string previewPath = Path.Combine(AppContext.BaseDirectory, "calibration-preview.png");
            if (args.Length > 0)
            {
                using var actual = new Bitmap(args[0]);
                using var preview = CreateForm(actual, saved);
                Layout(preview);
                var previewBounds = (Rectangle)Call(preview, "ImageBounds");
                var hover = new Point(previewBounds.X + 650 * previewBounds.Width / actual.Width,
                    previewBounds.Y + 340 * previewBounds.Height / actual.Height);
                Call(preview, "ScreenshotMouseMove", null, new MouseEventArgs(MouseButtons.None, 0, hover.X, hover.Y, 0));
                Render(preview, previewPath);
                preview.Size = preview.MinimumSize;
                Layout(preview);
                Render(preview, Path.Combine(AppContext.BaseDirectory, "calibration-compact.png"));
            }
            else Render(existing, previewPath);

            CheckSaveFailure();
            ShadowScanChecks.Run();
            Console.WriteLine($"{_passed} pond calibration checks passed. Preview: {previewPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void CheckSaveFailure()
    {
        // Persistence paths resolve next to this test executable, never the user's running bot.
        string folder = Path.Combine(AppContext.BaseDirectory, "Templates");
        string path = Path.Combine(folder, "PondColors.json");
        byte[] previous = File.Exists(path) ? File.ReadAllBytes(path) : null;
        try
        {
            Check(PondColorManager.SetPondColors("Regression pond", Color.CadetBlue, Color.DarkSlateGray, 9, 22, 31),
                "Saving reports success when data reaches disk");
            using (var fileLock = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Check(!PondColorManager.SetPondColors("Regression pond", Color.White, Color.Black),
                    "A locked settings file reports save failure");
                Check(PondColorManager.GetPondColors("Regression pond").WaterColor == Color.CadetBlue.ToArgbColor(),
                    "Failed save restores the prior in-memory colors");
            }
            PondColorManager.Reload();
            Check(PondColorManager.GetPondColors("Regression pond").ToleranceG == 22,
                "Failed save leaves the previous file intact");
        }
        finally
        {
            if (previous == null) File.Delete(path);
            else File.WriteAllBytes(path, previous);
            PondColorManager.Reload();
        }
    }

    private static void CheckRefresh(Bitmap frame, PondColorManager.PondColorData saved)
    {
        using var form = (PondColorCalibrationForm)Activator.CreateInstance(typeof(PondColorCalibrationForm), Private, null,
            new object[] { "FISH ANYWHERE", frame, saved, new Func<Bitmap>(() => new Bitmap(1200, 800)) }, null);
        Layout(form);
        WaitForRefresh(form);
        Check(Field<PictureBox>(form, "_screenshotBox").Image.Size == new Size(1200, 800), "Refresh replaces the frozen frame");
        Check(Field<Color?>(form, "_waterColor") == saved.WaterColor && Field<NumericUpDown>(form, "_greenTolerance").Value == 22,
            "Refresh preserves sampled colors and tolerance");
        Check(form.Visible && form.Opacity == 1 && !Field<bool>(form, "_capturing"), "Refresh restores the dialog and controls");

        using var failure = (PondColorCalibrationForm)Activator.CreateInstance(typeof(PondColorCalibrationForm), Private, null,
            new object[] { "FISH ANYWHERE", frame, saved, new Func<Bitmap>(() => throw new InvalidOperationException("Test capture failure")) }, null);
        Layout(failure);
        var originalFrame = Field<PictureBox>(failure, "_screenshotBox").Image;
        WaitForRefresh(failure);
        Check(ReferenceEquals(originalFrame, Field<PictureBox>(failure, "_screenshotBox").Image), "Failed refresh retains the previous frame");
        Check(failure.Visible && failure.Opacity == 1 && Field<Button>(failure, "_refreshButton").Enabled,
            "Failed refresh restores the dialog and enables retry");
        Check(Field<Label>(failure, "_status").Text.Contains("Test capture failure"), "Capture failures appear in the dialog");
    }

    private static void WaitForRefresh(object form)
    {
        var task = (Task)Call(form, "RefreshScreenshotAsync");
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!task.IsCompleted && DateTime.UtcNow < deadline)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        if (!task.IsCompleted) throw new Exception("Refresh did not finish");
        task.GetAwaiter().GetResult();
    }

    private static PondColorCalibrationForm CreateForm(Bitmap screenshot, PondColorManager.PondColorData saved)
        => (PondColorCalibrationForm)Activator.CreateInstance(typeof(PondColorCalibrationForm), Private, null,
            new object[] { "FISH ANYWHERE", screenshot, saved }, null);

    private static T Field<T>(object form, string name) => (T)form.GetType().GetField(name, Private).GetValue(form);
    private static object Call(object form, string name, params object[] args) => form.GetType().GetMethod(name, Private).Invoke(form, args);
    private static void Click(object form, Point point) => Call(form, "ScreenshotMouseClick", null,
        new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0));
    private static Color ToArgbColor(this Color color) => Color.FromArgb(color.R, color.G, color.B);

    private static void Layout(Control control)
    {
        if (control is Form form && !form.Visible)
        {
            // WinForms only paints child controls of a visible form. Keep the test
            // window off-screen and out of the taskbar; never expose a live bot UI.
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-20000, -20000);
            form.ShowInTaskbar = false;
            form.Show();
        }
        control.CreateControl();
        control.PerformLayout();
        foreach (Control child in control.Controls) Layout(child);
    }

    private static void CheckLayout(Form form, Size size)
    {
        form.Size = size;
        Layout(form);
        var save = Field<Button>(form, "_saveButton");
        Check(save.Right <= save.Parent.ClientSize.Width && save.Bottom <= save.Parent.ClientSize.Height,
            $"Save stays inside footer at {size.Width} x {size.Height}");
        var box = Field<PictureBox>(form, "_screenshotBox");
        var bounds = (Rectangle)Call(form, "ImageBounds");
        Check(bounds.Width > 0 && bounds.Height > 0 && box.ClientRectangle.Contains(bounds),
            $"Screenshot mapping fits after resizing to {size.Width} x {size.Height}");
    }

    private static void Render(Form form, string path)
    {
        using var image = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
        image.Save(path);
    }

    private static void Check(bool passed, string description)
    {
        if (!passed) throw new Exception("FAILED: " + description);
        _passed++;
        Console.WriteLine("PASS: " + description);
    }
}
