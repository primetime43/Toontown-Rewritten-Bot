using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ToonTown_Rewritten_Bot.Utilities;

namespace ToonTown_Rewritten_Bot.Utilities
{
    /// <summary>
    /// Provides global keyboard hook functionality to capture keypresses
    /// even when the application doesn't have focus.
    /// </summary>
    public class GlobalKeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;
        private bool _disposed = false;

        public bool IsRunning => _hookId != IntPtr.Zero;
        public int LastErrorCode { get; private set; }

        /// <summary>
        /// When true, the next handled key press will be suppressed (not passed to the game).
        /// Set by the event handler to consume the key.
        /// </summary>
        public bool SuppressKey { get; set; }

        /// <summary>
        /// Event raised when a key is pressed globally.
        /// </summary>
        public event EventHandler<Keys> KeyPressed;

        /// <summary>
        /// Event raised when a key is released globally.
        /// </summary>
        public event EventHandler<Keys> KeyReleased;

        /// <summary>
        /// Delegate for the low-level keyboard hook procedure.
        /// </summary>
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public GlobalKeyboardHook()
        {
            _proc = HookCallback;
        }

        /// <summary>
        /// Starts listening for global keyboard events.
        /// </summary>
        public bool Start()
        {
            if (_hookId != IntPtr.Zero)
                return true; // Already hooked

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(GlobalKeyboardHook));
            }

            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }

            if (_hookId == IntPtr.Zero)
            {
                LastErrorCode = Marshal.GetLastWin32Error();
                Logger.Error("Input", $"Failed to install global keyboard hook (Windows error {LastErrorCode}).");
                return false;
            }

            LastErrorCode = 0;
            Logger.Debug("Input", "Global keyboard hook installed successfully");
            return true;
        }

        /// <summary>
        /// Stops listening for global keyboard events.
        /// </summary>
        public void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                Debug.WriteLine("[GlobalKeyboardHook] Hook removed");
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                try
                {
                    SuppressKey = false;

                    if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                    {
                        KeyPressed?.Invoke(this, key);
                    }
                    else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                    {
                        KeyReleased?.Invoke(this, key);
                    }

                    // If the event handler set SuppressKey, don't pass the key to the game
                    if (SuppressKey)
                    {
                        return (IntPtr)1;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[GlobalKeyboardHook] Error in key handler: {ex.Message}");
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }

        ~GlobalKeyboardHook()
        {
            Dispose(false);
        }
    }
}
