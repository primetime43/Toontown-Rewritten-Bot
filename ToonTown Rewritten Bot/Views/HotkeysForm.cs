using System;
using System.Drawing;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>
    /// Lets the user rebind the bot's global stop/pause hotkeys, plus toggle whether Esc
    /// also stops. Bindings are applied to <see cref="Hotkeys"/> and persisted to
    /// <see cref="UserPreferences"/> on Save.
    /// </summary>
    public class HotkeysForm : Form
    {
        private enum HotkeySlot { Stop, Pause }

        private Keys _stop = Hotkeys.Stop;
        private Keys _pause = Hotkeys.Pause;
        private bool _allowEsc = Hotkeys.AllowEscToStop;

        private readonly Button _btnStop = new Button();
        private readonly Button _btnPause = new Button();
        private readonly CheckBox _chkEsc = new CheckBox();

        private HotkeySlot? _capturing;

        public HotkeysForm()
        {
            BuildUi();
            RefreshButtonLabels();
        }

        private void BuildUi()
        {
            Text = "Hotkeys";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(360, 240);

            var intro = new Label
            {
                Location = new Point(15, 12),
                Size = new Size(330, 56),
                Text = "Set the global keys that stop and pause the bot. Click a button, " +
                       "then press the key you want to use (these work even while TTR has focus)."
            };
            Controls.Add(intro);

            int y = 80;
            AddRow("Stop task:", _btnStop, HotkeySlot.Stop, ref y);
            AddRow("Pause / Resume:", _btnPause, HotkeySlot.Pause, ref y);

            _chkEsc.Location = new Point(175, y);
            _chkEsc.Size = new Size(170, 24);
            _chkEsc.Text = "Also allow Esc to stop";
            _chkEsc.Checked = _allowEsc;
            _chkEsc.CheckedChanged += (s, e) => _allowEsc = _chkEsc.Checked;
            Controls.Add(_chkEsc);

            var btnReset = new Button
            {
                Text = "Reset to Defaults",
                Location = new Point(15, 192),
                Size = new Size(130, 32)
            };
            btnReset.Click += (s, e) =>
            {
                _stop = Keys.F12;
                _pause = Keys.F11;
                _allowEsc = true;
                _chkEsc.Checked = true;
                CancelCapture();
                RefreshButtonLabels();
            };
            Controls.Add(btnReset);

            var btnSave = new Button
            {
                Text = "Save",
                Location = new Point(185, 192),
                Size = new Size(75, 32),
                DialogResult = DialogResult.OK
            };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(270, 192),
                Size = new Size(75, 32),
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(btnCancel);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void AddRow(string labelText, Button button, HotkeySlot slot, ref int y)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(15, y + 6),
                Size = new Size(150, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(label);

            button.Location = new Point(175, y);
            button.Size = new Size(170, 30);
            button.Tag = slot;
            button.Click += BindButton_Click;
            Controls.Add(button);

            y += 40;
        }

        private void BindButton_Click(object sender, EventArgs e)
        {
            // Clicking a row that is already capturing cancels it.
            var slot = (HotkeySlot)((Button)sender).Tag;
            if (_capturing == slot)
            {
                CancelCapture();
                return;
            }

            CancelCapture();
            _capturing = slot;
            ButtonFor(slot).Text = "Press a key…";
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            Hotkeys.Stop = _stop;
            Hotkeys.Pause = _pause;
            Hotkeys.AllowEscToStop = _allowEsc;

            var prefs = UserPreferences.Instance;
            Hotkeys.SaveTo(prefs);
            prefs.Save();
        }

        /// <summary>
        /// While a row is in capture mode, intercept the next key press and bind it.
        /// ProcessCmdKey reliably sees function keys and other keys that a normal KeyDown
        /// handler might otherwise consume for navigation. Esc cancels capture rather than
        /// binding (it has its own toggle), so the stop/pause keys are always a real key.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_capturing != null)
            {
                Keys keyCode = keyData & Keys.KeyCode;

                if (keyCode == Keys.Escape)
                {
                    CancelCapture();
                    return true;
                }

                // Ignore lone modifier presses so a bind doesn't capture just "Shift"/"Control".
                if (keyCode == Keys.ShiftKey || keyCode == Keys.ControlKey || keyCode == Keys.Menu)
                {
                    return true;
                }

                AssignKey(_capturing.Value, keyCode);
                _capturing = null;
                RefreshButtonLabels();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void AssignKey(HotkeySlot slot, Keys key)
        {
            switch (slot)
            {
                case HotkeySlot.Stop: _stop = key; break;
                case HotkeySlot.Pause: _pause = key; break;
            }
        }

        private void CancelCapture()
        {
            if (_capturing != null)
            {
                _capturing = null;
                RefreshButtonLabels();
            }
        }

        private Button ButtonFor(HotkeySlot slot)
        {
            return slot switch
            {
                HotkeySlot.Stop => _btnStop,
                HotkeySlot.Pause => _btnPause,
                _ => _btnStop
            };
        }

        private void RefreshButtonLabels()
        {
            _btnStop.Text = Hotkeys.GetDisplayName(_stop);
            _btnPause.Text = Hotkeys.GetDisplayName(_pause);
        }
    }
}
