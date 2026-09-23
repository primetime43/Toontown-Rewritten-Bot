using System.Drawing;
using System.Windows.Forms;

namespace ToonTown_Rewritten_Bot
{
    internal static class UiTheme
    {
        public static void ApplyButtonColors(Button button, bool primary = false)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.FlatAppearance.BorderColor = UiColors.Border;
            button.BackColor = primary ? UiColors.Primary : UiColors.Surface;
            button.ForeColor = primary ? UiColors.OnPrimary : UiColors.Text;
            button.FlatAppearance.MouseOverBackColor = button.BackColor;
            button.FlatAppearance.MouseDownBackColor = button.BackColor;
            button.Paint -= PaintDisabledPrimaryButton;
            if (primary) button.Paint += PaintDisabledPrimaryButton;
        }

        private static void PaintDisabledPrimaryButton(object sender, PaintEventArgs e)
        {
            var button = (Button)sender;
            if (button.Enabled) return;

            // WinForms ignores ForeColor for disabled buttons; retain the shared blue and white.
            var bounds = Rectangle.Inflate(button.ClientRectangle, -1, -1);
            using var background = new SolidBrush(UiColors.Primary);
            e.Graphics.FillRectangle(background, bounds);
            TextRenderer.DrawText(e.Graphics, button.Text, button.Font, bounds, UiColors.OnPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.HidePrefix);
        }
    }
}
