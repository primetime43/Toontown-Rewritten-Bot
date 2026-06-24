using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsInput;
using ToonTown_Rewritten_Bot.Models;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Views
{
    /// <summary>
    /// Lets the user tell the bot which movement/jump keys they have bound in TTR's
    /// Options &amp; Codes → Controls screen. The bot then sends those keys instead of
    /// assuming the defaults. Bindings are applied to <see cref="GameControls"/> and
    /// persisted to <see cref="UserPreferences"/> on Save.
    /// </summary>
    public class GameControlsForm : Form
    {
        private enum ControlSlot { Forward, Reverse, Left, Right, Jump }

        private VirtualKeyCode _forward = GameControls.Forward;
        private VirtualKeyCode _reverse = GameControls.Reverse;
        private VirtualKeyCode _left = GameControls.Left;
        private VirtualKeyCode _right = GameControls.Right;
        private VirtualKeyCode _jump = GameControls.Jump;

        private readonly Button _btnForward = new Button();
        private readonly Button _btnReverse = new Button();
        private readonly Button _btnLeft = new Button();
        private readonly Button _btnRight = new Button();
        private readonly Button _btnJump = new Button();

        private ControlSlot? _capturing;

        public GameControlsForm()
        {
            BuildUi();
            RefreshButtonLabels();
        }

        private void BuildUi()
        {
            Text = "Game Controls";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(360, 340);

            var intro = new Label
            {
                Location = new Point(15, 12),
                Size = new Size(330, 56),
                Text = "Set these to match your in-game controls (Options & Codes → " +
                       "Controls in TTR). Click a button, then press the key you have bound."
            };
            Controls.Add(intro);

            int y = 80;
            AddRow("Forward / Up:", _btnForward, ControlSlot.Forward, ref y);
            AddRow("Reverse / Down:", _btnReverse, ControlSlot.Reverse, ref y);
            AddRow("Left:", _btnLeft, ControlSlot.Left, ref y);
            AddRow("Right:", _btnRight, ControlSlot.Right, ref y);
            AddRow("Jump:", _btnJump, ControlSlot.Jump, ref y);

            var btnReset = new Button
            {
                Text = "Reset to Defaults",
                Location = new Point(15, 292),
                Size = new Size(130, 32)
            };
            btnReset.Click += (s, e) =>
            {
                _forward = VirtualKeyCode.UP;
                _reverse = VirtualKeyCode.DOWN;
                _left = VirtualKeyCode.LEFT;
                _right = VirtualKeyCode.RIGHT;
                _jump = VirtualKeyCode.CONTROL;
                CancelCapture();
                RefreshButtonLabels();
            };
            Controls.Add(btnReset);

            var btnSave = new Button
            {
                Text = "Save",
                Location = new Point(185, 292),
                Size = new Size(75, 32),
                DialogResult = DialogResult.OK
            };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(270, 292),
                Size = new Size(75, 32),
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(btnCancel);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void AddRow(string labelText, Button button, ControlSlot slot, ref int y)
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
            var slot = (ControlSlot)((Button)sender).Tag;
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
            GameControls.Forward = _forward;
            GameControls.Reverse = _reverse;
            GameControls.Left = _left;
            GameControls.Right = _right;
            GameControls.Jump = _jump;

            var prefs = UserPreferences.Instance;
            GameControls.SaveTo(prefs);
            prefs.Save();
        }

        /// <summary>
        /// While a row is in capture mode, intercept the next key press and bind it.
        /// ProcessCmdKey reliably sees arrow keys and modifier keys (Control/Shift),
        /// which a normal KeyDown handler would otherwise consume for navigation.
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

                // VirtualKeyCode's underlying type is ushort, so Enum.IsDefined must be given a
                // ushort — passing an int throws ArgumentException ("underlying type was UInt16").
                // WinForms Keys values share the same virtual-key numbering, so the cast is safe.
                ushort vk = (ushort)keyCode;
                if (Enum.IsDefined(typeof(VirtualKeyCode), vk))
                {
                    AssignKey(_capturing.Value, (VirtualKeyCode)vk);
                    _capturing = null;
                    RefreshButtonLabels();
                    return true;
                }

                // Unsupported key — keep waiting for a valid one.
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void AssignKey(ControlSlot slot, VirtualKeyCode key)
        {
            switch (slot)
            {
                case ControlSlot.Forward: _forward = key; break;
                case ControlSlot.Reverse: _reverse = key; break;
                case ControlSlot.Left: _left = key; break;
                case ControlSlot.Right: _right = key; break;
                case ControlSlot.Jump: _jump = key; break;
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

        private Button ButtonFor(ControlSlot slot)
        {
            return slot switch
            {
                ControlSlot.Forward => _btnForward,
                ControlSlot.Reverse => _btnReverse,
                ControlSlot.Left => _btnLeft,
                ControlSlot.Right => _btnRight,
                ControlSlot.Jump => _btnJump,
                _ => _btnForward
            };
        }

        private void RefreshButtonLabels()
        {
            _btnForward.Text = GameControls.GetDisplayName(_forward);
            _btnReverse.Text = GameControls.GetDisplayName(_reverse);
            _btnLeft.Text = GameControls.GetDisplayName(_left);
            _btnRight.Text = GameControls.GetDisplayName(_right);
            _btnJump.Text = GameControls.GetDisplayName(_jump);
        }
    }
}
