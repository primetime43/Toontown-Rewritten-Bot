using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
        private const int WM_SYSKEYDOWN = 0x0104;

        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;
        private bool _disposed = false;

        /// <summary>
        /// Event raised when a key is pressed globally.
        /// </summary>
        public event EventHandler<Keys> KeyPressed;

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
        public void Start()
        {
            if (_hookId != IntPtr.Zero)
                return; // Already hooked

            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }

            if (_hookId == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Debug.WriteLine($"[GlobalKeyboardHook] Failed to set hook. Error code: {errorCode}");
            }
            else
            {
                Debug.WriteLine("[GlobalKeyboardHook] Hook installed successfully");
            }
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
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                // Raise event on UI thread if possible
                try
                {
                    KeyPressed?.Invoke(this, key);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[GlobalKeyboardHook] Error in KeyPressed handler: {ex.Message}");
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
