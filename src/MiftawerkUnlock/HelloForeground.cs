// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MiftawerkUnlock
{
    /// <summary>
    /// Keeps the Windows Hello dialog in front of KeePass. KeyCredentialManager has no
    /// window-owner parameter for signing, and on this machine the dialog opened behind
    /// KeePass (docs/phase0/02-experiment-results.md).
    /// </summary>
    internal static class HelloForeground
    {
        private const string DialogClass = "Credential Dialog Xaml Host";
        private const string DialogProcess = "CredentialUIBroker";
        private const int PollIntervalMs = 150;

        /// <summary>
        /// Runs a Windows Hello operation while a timer keeps its dialog in front.
        /// </summary>
        internal static async Task<T> RunAsync<T>(Form owner, Func<Task<T>> op)
        {
            if (owner != null && owner.Visible) owner.Activate();

            using (Timer timer = new Timer())
            {
                timer.Interval = PollIntervalMs;
                timer.Tick += delegate { TryBringDialogToFront(); };
                timer.Start();
                try { return await op(); }
                finally { timer.Stop(); }
            }
        }

        /// <summary>
        /// If the foreground window belongs to this process (KeePass) and a visible
        /// Windows Hello dialog exists, makes the dialog the foreground window.
        /// Matches by window class and process name, never by the localized title,
        /// and never takes the focus away from other applications.
        /// </summary>
        internal static bool TryBringDialogToFront()
        {
            IntPtr fg = NativeMethods.GetForegroundWindow();
            if (fg == IntPtr.Zero) return false;

            uint fgPid;
            NativeMethods.GetWindowThreadProcessId(fg, out fgPid);
            if (fgPid != (uint)Process.GetCurrentProcess().Id) return false;

            IntPtr dlg = FindDialog();
            if (dlg == IntPtr.Zero || dlg == fg) return false;

            uint dlgPid;
            NativeMethods.GetWindowThreadProcessId(dlg, out dlgPid);
            NativeMethods.AllowSetForegroundWindow(dlgPid);
            return NativeMethods.SetForegroundWindow(dlg);
        }

        private static IntPtr FindDialog()
        {
            IntPtr found = IntPtr.Zero;
            NativeMethods.EnumWindows(delegate(IntPtr h, IntPtr lp)
            {
                if (!NativeMethods.IsWindowVisible(h)) return true;

                StringBuilder cls = new StringBuilder(256);
                NativeMethods.GetClassName(h, cls, cls.Capacity);
                if (cls.ToString() != DialogClass) return true;

                uint pid;
                NativeMethods.GetWindowThreadProcessId(h, out pid);
                try
                {
                    using (Process p = Process.GetProcessById((int)pid))
                        if (p.ProcessName != DialogProcess) return true;
                }
                catch (ArgumentException) { return true; }

                found = h;
                return false;
            }, IntPtr.Zero);
            return found;
        }
    }
}
