// Miftawerk Unlock - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Windows.Forms;

using KeePass.Plugins;
using KeePass.UI;

namespace MiftawerkUnlock
{
    /// <summary>
    /// Plugin entry point. KeePass requires the class name "{Namespace}Ext".
    /// </summary>
    public sealed class MiftawerkUnlockExt : Plugin
    {
        private const string Title = "Miftawerk Unlock";

        private IPluginHost m_host;
        private Enrollment m_enrollment;
        private QuickUnlock m_quickUnlock;

        public override bool Initialize(IPluginHost host)
        {
            if (host == null) return false;
            m_host = host;

            m_enrollment = new Enrollment(host);
            m_host.MainWindow.FileOpened += m_enrollment.OnFileOpened;

            m_quickUnlock = new QuickUnlock(host);
            GlobalWindowManager.WindowAdded += m_quickUnlock.OnWindowAdded;
            return true;
        }

        public override void Terminate()
        {
            if (m_quickUnlock != null)
                GlobalWindowManager.WindowAdded -= m_quickUnlock.OnWindowAdded;
            if (m_host != null && m_enrollment != null)
                m_host.MainWindow.FileOpened -= m_enrollment.OnFileOpened;
            m_quickUnlock = null;
            m_enrollment = null;
            m_host = null;
        }

        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            if (t != PluginMenuType.Main) return null;

            ToolStripMenuItem root = new ToolStripMenuItem(Title);

            ToolStripMenuItem forget = new ToolStripMenuItem("Forget all Windows Hello unlock data");
            forget.Click += OnForgetClick;
            root.DropDownItems.Add(forget);

            root.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem test = new ToolStripMenuItem("Windows Hello self-test");
            test.Click += OnSelfTestClick;
            root.DropDownItems.Add(test);

            ToolStripMenuItem del = new ToolStripMenuItem("Delete self-test key");
            del.Click += OnDeleteClick;
            root.DropDownItems.Add(del);

            return root;
        }

        private void OnForgetClick(object sender, EventArgs e)
        {
            string msg;
            try { msg = RecordStore.DeleteAll() + " stored unlock record(s) deleted."; }
            catch (Exception ex) { msg = "Delete failed: " + ex.Message; }
            MessageBox.Show(m_host.MainWindow, msg, Title, MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private async void OnSelfTestClick(object sender, EventArgs e)
        {
            Form owner = m_host.MainWindow;
            string report = await HelloSelfTest.RunAsync(owner);
            MessageBox.Show(owner, report, Title + " - self-test",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void OnDeleteClick(object sender, EventArgs e)
        {
            Form owner = m_host.MainWindow;
            string report = await HelloSelfTest.DeleteKeyAsync();
            MessageBox.Show(owner, report, Title + " - self-test",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
