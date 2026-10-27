// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;

using Microsoft.Win32;

using KeePass;
using KeePass.Forms;
using KeePass.Plugins;

namespace RamzXUnlock
{
    /// <summary>
    /// Locks the KeePass workspace when Windows is locked, the session is disconnected
    /// or the computer goes to sleep. KeePass has its own options for this (off by
    /// default); if the user enabled them, KeePass handles it and this class does nothing.
    /// KeePass settings are never changed by the plugin.
    /// </summary>
    internal sealed class WindowsLockWatcher : IDisposable
    {
        private readonly IPluginHost m_host;
        private readonly Settings m_settings;

        internal WindowsLockWatcher(IPluginHost host, Settings settings)
        {
            m_host = host;
            m_settings = settings;
            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        public void Dispose()
        {
            // Static events: must be removed, otherwise they keep the plugin alive.
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason != SessionSwitchReason.SessionLock &&
                e.Reason != SessionSwitchReason.ConsoleDisconnect &&
                e.Reason != SessionSwitchReason.RemoteDisconnect) return;

            if (Program.Config.Security.WorkspaceLocking.LockOnSessionSwitch) return;
            LockWorkspace();
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode != PowerModes.Suspend) return;

            if (Program.Config.Security.WorkspaceLocking.LockOnSuspend) return;
            LockWorkspace();
        }

        private void LockWorkspace()
        {
            if (!m_settings.LockWithWindows) return;

            // SystemEvents may fire on a background thread; KeePass UI calls need the UI thread.
            MainForm mf = m_host.MainWindow;
            if (mf == null || mf.IsDisposed) return;
            mf.BeginInvoke(new Action(() =>
            {
                if (!mf.IsDisposed) mf.LockAllDocuments();
            }));
        }
    }
}
