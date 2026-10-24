// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Windows.Forms;

using KeePass;
using KeePass.Plugins;
using KeePass.UI;

namespace MiftawerkUnlock
{
    /// <summary>
    /// Plugin entry point. KeePass requires the class name "{Namespace}Ext".
    /// </summary>
    public sealed class MiftawerkUnlockExt : Plugin
    {
        private IPluginHost m_host;
        private Settings m_settings;
        private Enrollment m_enrollment;
        private QuickUnlock m_quickUnlock;
        private WindowsLockWatcher m_lockWatcher;

        public override bool Initialize(IPluginHost host)
        {
            if (host == null) return false;
            m_host = host;

            Strings.Select(Program.Translation.Properties.Iso6391Code);
            m_settings = new Settings(host.CustomConfig);

            m_enrollment = new Enrollment(host, m_settings);
            m_host.MainWindow.FileOpened += m_enrollment.OnFileOpened;

            m_quickUnlock = new QuickUnlock(host);
            GlobalWindowManager.WindowAdded += m_quickUnlock.OnWindowAdded;

            m_lockWatcher = new WindowsLockWatcher(host, m_settings);
            return true;
        }

        public override void Terminate()
        {
            if (m_lockWatcher != null) m_lockWatcher.Dispose();
            m_lockWatcher = null;
            if (m_quickUnlock != null)
                GlobalWindowManager.WindowAdded -= m_quickUnlock.OnWindowAdded;
            if (m_host != null && m_enrollment != null)
                m_host.MainWindow.FileOpened -= m_enrollment.OnFileOpened;
            m_quickUnlock = null;
            m_enrollment = null;
            m_settings = null;
            m_host = null;
        }

        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            if (t != PluginMenuType.Main) return null;

            ToolStripMenuItem root = new ToolStripMenuItem(Ui.Title);
            AddItem(root, Strings.MenuSettings, OnSettingsClick);
            AddItem(root, Strings.MenuForget, OnForgetClick);
            root.DropDownItems.Add(new ToolStripSeparator());
            AddItem(root, Strings.MenuSelfTest, OnSelfTestClick);
            AddItem(root, Strings.MenuDeleteSelfTest, OnDeleteClick);
            return root;
        }

        private static void AddItem(ToolStripMenuItem root, string key, EventHandler onClick)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(Strings.Get(key));
            item.Click += onClick;
            root.DropDownItems.Add(item);
        }

        private void OnSettingsClick(object sender, EventArgs e)
        {
            UIUtil.ShowDialogAndDestroy(new SettingsForm(m_settings));
        }

        private void OnForgetClick(object sender, EventArgs e)
        {
            try { Ui.Info(m_host.MainWindow, Strings.Format(Strings.ForgetDone, RecordStore.DeleteAll())); }
            catch (Exception ex) { Ui.Warn(m_host.MainWindow, Strings.Format(Strings.DeleteFailed, ex.Message)); }
        }

        // Self-test output is technical and stays English.
        private async void OnSelfTestClick(object sender, EventArgs e)
        {
            Form owner = m_host.MainWindow;
            string report = await HelloSelfTest.RunAsync(owner);
            MessageBox.Show(owner, report, Ui.Title + " - self-test",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void OnDeleteClick(object sender, EventArgs e)
        {
            Form owner = m_host.MainWindow;
            string report = await HelloSelfTest.DeleteKeyAsync();
            MessageBox.Show(owner, report, Ui.Title + " - self-test",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
