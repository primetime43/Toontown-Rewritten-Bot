using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>Samples pond colors from a frozen game frame without changing saved colors until Save.</summary>
    public class PondColorCalibrationForm : Form
    {
        private static readonly Color Surface = Color.FromArgb(36, 42, 52);
        private static readonly Color Muted = Color.FromArgb(182, 194, 208);
        private static readonly Color WaterAccent = Color.FromArgb(91, 201, 236);
        private static readonly Color ShadowAccent = Color.FromArgb(255, 187, 103);
        private readonly string _locationName;
        private readonly PondColorManager.PondColorData _original;
        private readonly Func<Bitmap> _captureScreenshot;
        private readonly Stack<SampleState> _history = new Stack<SampleState>();
        private Bitmap _screenshot;
        private Color? _waterColor, _shadowColor;
        private Point? _waterPoint, _shadowPoint, _hoverPoint;
        private SampleTarget _target;
        private PictureBox _screenshotBox, _magnifier;
        private Panel _waterSwatch, _shadowSwatch;
        private Label _instructions, _status, _waterLabel, _shadowLabel, _quality, _hoverLabel;
        private Button _waterButton, _shadowButton, _saveButton, _refreshButton, _undoButton, _resetButton;
        private NumericUpDown _redTolerance, _greenTolerance, _blueTolerance;
        private bool _capturing;
        public bool WasSaved { get; private set; }

        private enum SampleTarget { None, Water, Shadow }
        private record SampleState(Color? Water, Color? Shadow, Point? WaterPoint, Point? ShadowPoint, SampleTarget Target);

        private sealed class CalibrationButton : Button
        {
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Enabled) return;
                // The native disabled text is nearly black and disappears on this dark surface.
                using var background = new SolidBrush(Surface);
                using var border = new Pen(Color.FromArgb(78, 91, 108));
                e.Graphics.FillRectangle(background, ClientRectangle);
                e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
                TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.HidePrefix);
            }
        }

        public PondColorCalibrationForm(string locationName)
            : this(locationName, null, PondColorManager.GetPondColors(locationName)) { }

        // The supplied frame is cloned so preview/regression callers retain ownership.
        internal PondColorCalibrationForm(string locationName, Bitmap screenshot, PondColorManager.PondColorData existing)
            : this(locationName, screenshot, existing, () => ImageRecognition.GetWindowScreenshot() as Bitmap) { }

        internal PondColorCalibrationForm(string locationName, Bitmap screenshot, PondColorManager.PondColorData existing,
            Func<Bitmap> captureScreenshot)
        {
            _locationName = locationName;
            _original = existing;
            _captureScreenshot = captureScreenshot;
            _waterColor = existing?.WaterColor;
            _shadowColor = existing?.ShadowColor;
            _target = existing == null ? SampleTarget.Water : SampleTarget.None;
            _screenshot = screenshot == null ? null : (Bitmap)screenshot.Clone();
            InitializeForm();
            UpdateState();
            Shown += async (s, e) => { if (_screenshot == null) await RefreshScreenshotAsync(); };
        }

        private static Label LabelFor(string text, Color color, int height = 38) => new Label
        {
            Text = text, ForeColor = color, Dock = DockStyle.Fill, AutoSize = false,
            Height = height, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 2, 0, 2)
        };

        private static Button ButtonFor(string text) => new CalibrationButton
        {
            Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(104, 34), Padding = new Padding(10, 4, 10, 4),
            BackColor = Surface, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Margin = new Padding(4), UseVisualStyleBackColor = false
        };

        private void InitializeForm()
        {
            Text = $"Pond colors - {_locationName}";
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 10);
            BackColor = Color.FromArgb(24, 29, 37);
            ForeColor = Color.White;
            ClientSize = new Size(1120, 780);
            MinimumSize = new Size(900, 680);
            StartPosition = FormStartPosition.CenterParent;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 4 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            Controls.Add(root);

            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var title = LabelFor($"Pond colors  /  {_locationName}", Color.White);
            title.Font = new Font(Font.FontFamily, 15, FontStyle.Bold);
            title.AutoEllipsis = true;
            header.Controls.Add(title, 0, 0);
            _instructions = LabelFor("", Muted);
            header.Controls.Add(_instructions, 0, 1);
            root.Controls.Add(header, 0, 0);

            var toolbar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _refreshButton = ButtonFor("&Refresh screenshot");
            _refreshButton.Click += async (s, e) => await RefreshScreenshotAsync();
            _undoButton = ButtonFor("&Undo sample");
            _undoButton.Click += (s, e) => UndoSample();
            toolbar.Controls.Add(_refreshButton, 0, 0);
            toolbar.Controls.Add(_undoButton, 1, 0);
            toolbar.Controls.Add(LabelFor("Frozen image - take your time choosing a color.", Muted), 2, 0);
            root.Controls.Add(toolbar, 0, 1);

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 8, 0, 0) };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(body, 0, 2);
            var imageArea = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 12, 0) };
            imageArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            imageArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            _screenshotBox = new PictureBox
            {
                Name = "Screenshot", Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(14, 18, 24), Image = _screenshot, Margin = Padding.Empty
            };
            _screenshotBox.MouseClick += ScreenshotMouseClick;
            _screenshotBox.MouseMove += ScreenshotMouseMove;
            _screenshotBox.MouseLeave += (s, e) => { _hoverPoint = null; UpdateHover(); };
            _screenshotBox.Paint += ScreenshotPaint;
            _status = LabelFor(_screenshot == null ? "Capture a screenshot to begin. Keep the pond visible in the game." : "Screenshot ready. Your saved settings stay unchanged until you save.", Muted);
            imageArea.Controls.Add(_screenshotBox, 0, 0);
            imageArea.Controls.Add(_status, 0, 1);
            body.Controls.Add(imageArea, 0, 0);

            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Surface, Margin = Padding.Empty };
            var sidebar = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1, Padding = new Padding(12), BackColor = Surface
            };
            sidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            scroll.Controls.Add(sidebar);
            body.Controls.Add(scroll, 1, 0);
            void AddSide(Control control, int height)
            {
                int row = sidebar.RowCount++;
                sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
                sidebar.Controls.Add(control, 0, row);
            }

            _waterButton = ButtonFor("&1  Sample water");
            _waterButton.Dock = DockStyle.Fill;
            _waterButton.Click += (s, e) => SelectTarget(SampleTarget.Water);
            AddSide(_waterButton, 44);
            AddSide(ColorRow(out _waterSwatch, out _waterLabel), 48);
            _shadowButton = ButtonFor("&2  Sample fish shadow");
            _shadowButton.Dock = DockStyle.Fill;
            _shadowButton.Click += (s, e) => SelectTarget(SampleTarget.Shadow);
            AddSide(_shadowButton, 44);
            AddSide(ColorRow(out _shadowSwatch, out _shadowLabel), 48);
            _quality = LabelFor("", Muted);
            AddSide(_quality, 64);

            AddSide(LabelFor("Color tolerance", Color.White), 28);
            var toleranceRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Margin = Padding.Empty };
            foreach (string channel in new[] { "Red", "Green", "Blue" })
            {
                int col = toleranceRow.ColumnStyles.Count;
                toleranceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
                toleranceRow.Controls.Add(LabelFor(channel, Muted), col, 0);
            }
            toleranceRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
            toleranceRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
            _redTolerance = ToleranceInput(_original?.ToleranceR ?? 15, "Red tolerance");
            _greenTolerance = ToleranceInput(_original?.ToleranceG ?? 15, "Green tolerance");
            _blueTolerance = ToleranceInput(_original?.ToleranceB ?? 15, "Blue tolerance");
            toleranceRow.Controls.Add(_redTolerance, 0, 1);
            toleranceRow.Controls.Add(_greenTolerance, 1, 1);
            toleranceRow.Controls.Add(_blueTolerance, 2, 1);
            AddSide(toleranceRow, 56);
            AddSide(LabelFor("Higher values include more color variations, but may also match water.", Muted), 44);

            _magnifier = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(14, 18, 24), Margin = new Padding(0, 4, 0, 0) };
            _magnifier.Paint += MagnifierPaint;
            AddSide(_magnifier, 100);
            _hoverLabel = LabelFor("Hover over the image for a closer look.", Muted);
            AddSide(_hoverLabel, 44);

            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = new Padding(0, 8, 0, 0) };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _resetButton = ButtonFor("Clear samples");
            _resetButton.Click += (s, e) => ClearSamples();
            var cancel = ButtonFor("Cancel");
            cancel.Click += (s, e) => Close();
            _saveButton = ButtonFor("Save calibration");
            _saveButton.BackColor = Color.FromArgb(31, 114, 90);
            _saveButton.Click += SaveClicked;
            footer.Controls.Add(_resetButton, 0, 0);
            footer.Controls.Add(cancel, 2, 0);
            footer.Controls.Add(_saveButton, 3, 0);
            root.Controls.Add(footer, 0, 3);
            AcceptButton = _saveButton;
            CancelButton = cancel;
        }

        private Control ColorRow(out Panel swatch, out Label label)
        {
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(4, 0, 4, 4) };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            swatch = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 4, 10, 4) };
            label = LabelFor("", Muted);
            row.Controls.Add(swatch, 0, 0);
            row.Controls.Add(label, 1, 0);
            return row;
        }

        private NumericUpDown ToleranceInput(int value, string accessibleName)
        {
            var input = new NumericUpDown
            {
                Minimum = 0, Maximum = 255, Value = Math.Clamp(value, 0, 255),
                Dock = DockStyle.Fill, BackColor = Color.FromArgb(50, 58, 71), ForeColor = Color.White,
                AccessibleName = accessibleName, Margin = new Padding(0, 2, 8, 2)
            };
            input.ValueChanged += (s, e) => UpdateState();
            return input;
        }

        private void SelectTarget(SampleTarget target)
        {
            _target = target;
            UpdateState();
        }

        private void UpdateState()
        {
            if (_saveButton == null) return;
            _instructions.Text = _target switch
            {
                SampleTarget.Water => "1  Choose clear pond water, away from shadows, bubbles and the shore.",
                SampleTarget.Shadow => "2  Choose the dark center of a fish shadow, below its bubbles.",
                _ => "Ready to save. To change a color, select Sample water or Sample fish shadow."
            };
            _waterSwatch.BackColor = _waterColor ?? Color.FromArgb(70, 78, 90);
            _shadowSwatch.BackColor = _shadowColor ?? Color.FromArgb(70, 78, 90);
            _waterLabel.Text = ColorText(_waterColor);
            _shadowLabel.Text = ColorText(_shadowColor);
            StyleTarget(_waterButton, _target == SampleTarget.Water, WaterAccent);
            StyleTarget(_shadowButton, _target == SampleTarget.Shadow, ShadowAccent);
            _waterButton.Enabled = _shadowButton.Enabled = !_capturing && _screenshot != null;
            _screenshotBox.Cursor = _target == SampleTarget.None ? Cursors.Default : Cursors.Cross;
            _saveButton.Enabled = !_capturing && _waterColor.HasValue && _shadowColor.HasValue;
            _undoButton.Enabled = !_capturing && _history.Count > 0;
            _resetButton.Enabled = !_capturing && (_waterColor.HasValue || _shadowColor.HasValue);
            _refreshButton.Enabled = !_capturing;
            _quality.ForeColor = Muted;
            if (!_waterColor.HasValue || !_shadowColor.HasValue)
                _quality.Text = "Sample both colors. Changes apply only to this fishing location.";
            else
            {
                var water = _waterColor.Value;
                var shadow = _shadowColor.Value;
                bool waterMatches = Math.Abs(water.R - shadow.R) <= _redTolerance.Value &&
                    Math.Abs(water.G - shadow.G) <= _greenTolerance.Value && Math.Abs(water.B - shadow.B) <= _blueTolerance.Value;
                bool darker = shadow.R + shadow.G + shadow.B < water.R + water.G + water.B;
                _quality.Text = waterMatches
                    ? "Water also matches this shadow tolerance. Sample a darker shadow or lower tolerance."
                    : !darker ? "The shadow is lighter than the water. Check that you sampled the dark center."
                    : "Colors are distinct at this tolerance. Ready to try in the pond.";
                if (waterMatches || !darker) _quality.ForeColor = ShadowAccent;
            }
            _screenshotBox.Invalidate();
            UpdateHover();
        }

        private static string ColorText(Color? color) => color.HasValue
            ? $"#{color.Value.R:X2}{color.Value.G:X2}{color.Value.B:X2}\nRGB {color.Value.R}, {color.Value.G}, {color.Value.B}"
            : "Not sampled yet";

        private static void StyleTarget(Button button, bool active, Color accent)
        {
            button.FlatAppearance.BorderSize = active ? 2 : 1;
            button.FlatAppearance.BorderColor = active ? accent : Color.FromArgb(78, 91, 108);
            button.ForeColor = active ? accent : Color.White;
        }

        private Rectangle ImageBounds()
        {
            if (_screenshot == null) return Rectangle.Empty;
            var size = _screenshotBox.ClientSize;
            double scale = Math.Min((double)size.Width / _screenshot.Width, (double)size.Height / _screenshot.Height);
            int width = (int)(_screenshot.Width * scale), height = (int)(_screenshot.Height * scale);
            return new Rectangle((size.Width - width) / 2, (size.Height - height) / 2, width, height);
        }

        private Point? ImagePoint(Point clientPoint)
        {
            var bounds = ImageBounds();
            if (bounds.Width <= 0 || bounds.Height <= 0 || !bounds.Contains(clientPoint)) return null;
            return new Point((clientPoint.X - bounds.X) * _screenshot.Width / bounds.Width,
                (clientPoint.Y - bounds.Y) * _screenshot.Height / bounds.Height);
        }

        private void ScreenshotMouseMove(object sender, MouseEventArgs e)
        {
            _hoverPoint = ImagePoint(e.Location);
            UpdateHover();
        }

        private void UpdateHover()
        {
            if (_hoverLabel == null) return;
            _hoverLabel.Text = _hoverPoint.HasValue && _screenshot != null
                ? $"7 x 7 average: {ColorText(SampleAreaColor(_hoverPoint.Value)).Replace("\n", "  ")}"
                : "Hover over the image for a closer look.";
            _magnifier.Invalidate();
        }

        private void ScreenshotMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _capturing) return;
            var point = ImagePoint(e.Location);
            if (!point.HasValue) return;
            if (_target == SampleTarget.None)
            {
                _status.Text = "Select Sample water or Sample fish shadow before choosing a new color.";
                return;
            }
            _history.Push(new SampleState(_waterColor, _shadowColor, _waterPoint, _shadowPoint, _target));
            var sampled = SampleAreaColor(point.Value);
            if (_target == SampleTarget.Water)
            {
                _waterColor = sampled;
                _waterPoint = point;
                _target = _shadowColor.HasValue ? SampleTarget.None : SampleTarget.Shadow;
                _status.Text = "Water sampled. You can undo or select Sample water to try another point.";
            }
            else
            {
                _shadowColor = sampled;
                _shadowPoint = point;
                _target = _waterColor.HasValue ? SampleTarget.None : SampleTarget.Water;
                _status.Text = "Shadow sampled. Review the colors, then save when ready.";
            }
            UpdateState();
        }

        private Color SampleAreaColor(Point center)
        {
            int red = 0, green = 0, blue = 0, count = 0;
            for (int y = Math.Max(0, center.Y - 3); y <= Math.Min(_screenshot.Height - 1, center.Y + 3); y++)
                for (int x = Math.Max(0, center.X - 3); x <= Math.Min(_screenshot.Width - 1, center.X + 3); x++)
                {
                    var pixel = _screenshot.GetPixel(x, y);
                    red += pixel.R; green += pixel.G; blue += pixel.B; count++;
                }
            return Color.FromArgb(red / count, green / count, blue / count);
        }

        private void ScreenshotPaint(object sender, PaintEventArgs e)
        {
            if (_screenshot == null)
            {
                TextRenderer.DrawText(e.Graphics, "No screenshot yet\nOpen the pond in Toontown, then select Refresh screenshot.",
                    Font, _screenshotBox.ClientRectangle, Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                return;
            }
            var bounds = ImageBounds();
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            void Marker(Point? point, Color color, string name)
            {
                if (!point.HasValue) return;
                int x = bounds.X + point.Value.X * bounds.Width / _screenshot.Width;
                int y = bounds.Y + point.Value.Y * bounds.Height / _screenshot.Height;
                using var pen = new Pen(color, 2);
                e.Graphics.DrawEllipse(pen, x - 9, y - 9, 18, 18);
                e.Graphics.DrawLine(pen, x - 13, y, x + 13, y);
                e.Graphics.DrawLine(pen, x, y - 13, x, y + 13);
                Size textSize = TextRenderer.MeasureText(name, Font);
                var label = new Rectangle(Math.Clamp(x + 15, bounds.Left, Math.Max(bounds.Left, bounds.Right - textSize.Width)),
                    Math.Clamp(y + 10, bounds.Top, Math.Max(bounds.Top, bounds.Bottom - textSize.Height)), textSize.Width, textSize.Height);
                TextRenderer.DrawText(e.Graphics, name, Font, label, color, Color.FromArgb(24, 29, 37));
            }
            Marker(_waterPoint, WaterAccent, "Water");
            Marker(_shadowPoint, ShadowAccent, "Shadow");
        }

        private void MagnifierPaint(object sender, PaintEventArgs e)
        {
            if (_screenshot == null || !_hoverPoint.HasValue)
            {
                TextRenderer.DrawText(e.Graphics, "Sampling preview", Font, _magnifier.ClientRectangle, Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            const int zoom = 4;
            var point = _hoverPoint.Value;
            int centerX = _magnifier.ClientSize.Width / 2, centerY = _magnifier.ClientSize.Height / 2;
            // Draw only the visible source neighborhood. The crosshair always marks the sampled pixel.
            int left = Math.Max(0, point.X - centerX / zoom - 1);
            int top = Math.Max(0, point.Y - centerY / zoom - 1);
            int right = Math.Min(_screenshot.Width, point.X + centerX / zoom + 2);
            int bottom = Math.Min(_screenshot.Height, point.Y + centerY / zoom + 2);
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.DrawImage(_screenshot, new Rectangle(centerX + (left - point.X) * zoom,
                centerY + (top - point.Y) * zoom, (right - left) * zoom, (bottom - top) * zoom),
                new Rectangle(left, top, right - left, bottom - top), GraphicsUnit.Pixel);
            using var pen = new Pen(_target == SampleTarget.Shadow ? ShadowAccent : WaterAccent, 2);
            e.Graphics.DrawRectangle(pen, centerX - 3 * zoom, centerY - 3 * zoom, 7 * zoom, 7 * zoom);
        }

        private void UndoSample()
        {
            if (_history.Count == 0) return;
            var state = _history.Pop();
            _waterColor = state.Water; _shadowColor = state.Shadow;
            _waterPoint = state.WaterPoint; _shadowPoint = state.ShadowPoint; _target = state.Target;
            _status.Text = "Sample change undone. Saved settings are unchanged.";
            UpdateState();
        }

        private void ClearSamples()
        {
            _history.Push(new SampleState(_waterColor, _shadowColor, _waterPoint, _shadowPoint, _target));
            _waterColor = _shadowColor = null;
            _waterPoint = _shadowPoint = null;
            _target = SampleTarget.Water;
            _status.Text = "Samples cleared from this draft. Undo restores them; saved settings are unchanged.";
            UpdateState();
        }

        private async Task RefreshScreenshotAsync()
        {
            if (_capturing) return;
            _capturing = true;
            UpdateState();
            Form owner = Owner;
            double previousOpacity = Opacity;
            double ownerOpacity = owner?.Opacity ?? 1;
            try
            {
                // Temporarily make both forms transparent without ending the modal dialog.
                // Visible-screen capture must not sample the calibration UI or its owner.
                Opacity = 0;
                if (owner != null) owner.Opacity = 0;
                await Task.Delay(200);
                var fresh = await Task.Run(_captureScreenshot);
                if (fresh == null) throw new InvalidOperationException("The game did not return a screenshot.");
                if (IsDisposed) { fresh.Dispose(); return; }
                var previous = _screenshot;
                _screenshot = fresh;
                _screenshotBox.Image = fresh;
                previous?.Dispose();
                _waterPoint = _shadowPoint = _hoverPoint = null;
                // Colors remain valid, but old points/undo markers belong to the previous frame.
                _history.Clear();
                _status.Text = $"Screenshot refreshed at {DateTime.Now:t}. Your sampled colors have been kept.";
                _status.ForeColor = Muted;
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    _status.Text = $"Could not refresh: {ex.Message}" + (_screenshot == null ? " Try Refresh screenshot again." : " Previous screenshot kept.");
                    _status.ForeColor = ShadowAccent;
                }
            }
            finally
            {
                _capturing = false;
                if (owner != null && !owner.IsDisposed) owner.Opacity = ownerOpacity;
                if (!IsDisposed) { Opacity = previousOpacity; Activate(); UpdateState(); }
            }
        }

        private bool HasUnsavedChanges() => _waterColor != _original?.WaterColor || _shadowColor != _original?.ShadowColor ||
            _redTolerance.Value != (_original?.ToleranceR ?? 15) || _greenTolerance.Value != (_original?.ToleranceG ?? 15) ||
            _blueTolerance.Value != (_original?.ToleranceB ?? 15);

        private void SaveClicked(object sender, EventArgs e)
        {
            if (!_waterColor.HasValue || !_shadowColor.HasValue || _capturing) return;
            if (!PondColorManager.SetPondColors(_locationName, _waterColor.Value, _shadowColor.Value,
                (int)_redTolerance.Value, (int)_greenTolerance.Value, (int)_blueTolerance.Value))
            {
                _status.Text = "Could not save colors. Check that the bot's Templates folder is writable, then try again.";
                _status.ForeColor = ShadowAccent;
                return;
            }
            WasSaved = true;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_capturing && e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
            else if (!WasSaved && e.CloseReason == CloseReason.UserClosing && HasUnsavedChanges())
            {
                e.Cancel = MessageBox.Show(this, "Discard the changes to this calibration? Your saved colors will stay unchanged.",
                    "Unsaved calibration", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes;
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_screenshotBox != null) _screenshotBox.Image = null;
                _screenshot?.Dispose();
                _screenshot = null;
            }
            base.Dispose(disposing);
        }
    }
}
