using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ToonTown_Rewritten_Bot.Utilities
{
    internal static class GameInputAccess
    {
        internal static void EnsureAllowed(IntPtr gameWindow)
        {
            if (gameWindow == IntPtr.Zero)
                throw new InvalidOperationException(GameProfile.WindowNotFoundMessage);

            GetWindowThreadProcessId(gameWindow, out uint gameProcessId);
            bool gameElevated;
            bool botElevated;
            try
            {
                gameElevated = IsElevated(gameProcessId);
                botElevated = IsElevated((uint)Environment.ProcessId);
            }
            catch (Win32Exception ex)
            {
                Logger.Warning("Input", $"Could not check game input permissions: {ex.Message}");
                return;
            }

            Logger.Info("Input", $"Input permissions: game elevated={gameElevated}, bot elevated={botElevated}.");
            if (gameElevated && !botElevated)
                throw new InvalidOperationException(
                    $"{GameProfile.DisplayName} is running as administrator, but the bot is not. " +
                    "Windows blocks the bot's input. Restart the game normally, or close the bot " +
                    "and reopen it with Run as administrator, then try again.");
        }

        internal static bool IsElevated(uint processId)
        {
            using var process = OpenProcess(0x1000, false, processId);
            if (process.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
            if (!OpenProcessToken(process, 0x0008, out var token))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            using (token)
            {
                if (!GetTokenInformation(token, 20, out int elevated, sizeof(int), out _))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                return elevated != 0;
            }
        }

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeProcessHandle OpenProcess(uint access, bool inherit, uint processId);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(SafeProcessHandle process, uint access, out SafeAccessTokenHandle token);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetTokenInformation(SafeAccessTokenHandle token, int informationClass,
            out int information, int informationLength, out int returnLength);
    }
}
