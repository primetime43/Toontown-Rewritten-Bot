using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ToonTown_Rewritten_Bot.Utilities
{
    // All detection coordinates are relative to ClientBounds, never the title bar or border.
    internal readonly record struct GameWindowGeometry(Rectangle WindowBounds, Rectangle ClientBounds, uint Dpi)
    {
        public Rectangle ClientCrop => new Rectangle(
            ClientBounds.X - WindowBounds.X, ClientBounds.Y - WindowBounds.Y,
            ClientBounds.Width, ClientBounds.Height);

        public static bool TryRead(IntPtr handle, out GameWindowGeometry geometry)
        {
            geometry = default;
            if (handle == IntPtr.Zero || !GetWindowRect(handle, out var outer) || !GetClientRect(handle, out var client))
                return false;
            var origin = Point.Empty;
            if (!ClientToScreen(handle, ref origin) || client.Right <= 0 || client.Bottom <= 0)
                return false;
            geometry = new GameWindowGeometry(
                Rectangle.FromLTRB(outer.Left, outer.Top, outer.Right, outer.Bottom),
                new Rectangle(origin, new Size(client.Right, client.Bottom)), GetDpiForWindow(handle));
            return true;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr handle, out NativeRect rect);
        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr handle, out NativeRect rect);
        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr handle, ref Point point);
        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr handle);
    }
}
