using System.Drawing;

namespace ToonTown_Rewritten_Bot
{
    /// <summary>Shared UI palette. Game colors and detection samples are separate from the theme.</summary>
    internal static class UiColors
    {
        public static readonly Color Primary = Color.FromArgb(35, 102, 185);
        public static readonly Color OnPrimary = Color.White;
        public static readonly Color Background = Color.FromArgb(242, 245, 249);
        public static readonly Color Surface = Color.White;
        public static readonly Color Border = Color.FromArgb(193, 205, 219);
        public static readonly Color Heading = Color.Black;
        public static readonly Color Text = Color.FromArgb(36, 53, 74);
        public static readonly Color MutedText = Color.DimGray;
        public static readonly Color InfoSurface = Color.FromArgb(231, 239, 250);
        public static readonly Color SuccessSurface = Color.FromArgb(225, 239, 227);
        public static readonly Color Success = Color.ForestGreen;
        public static readonly Color Warning = Color.DarkOrange;
        public static readonly Color Danger = Color.Firebrick;
        public static readonly Color Banner = Color.FromArgb(25, 46, 72);
    }
}
