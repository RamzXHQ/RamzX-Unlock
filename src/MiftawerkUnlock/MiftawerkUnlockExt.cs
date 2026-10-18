// Miftawerk Unlock - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Windows.Forms;

using KeePass.Plugins;

namespace MiftawerkUnlock
{
    /// <summary>
    /// Plugin entry point. KeePass requires the class name "{Namespace}Ext".
    /// Spike stage: only offers a Windows Hello self-test, does not touch any database.
    /// </summary>
    public sealed class MiftawerkUnlockExt : Plugin
    {
        private IPluginHost m_host;

        public override bool Initialize(IPluginHost host)
        {
            if (host == null) return false;
            m_host = host;
            return true;
        }

        public override void Terminate()
        {
            m_host = null;
        }

        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            if (t != PluginMenuType.Main) return null;

            ToolStripMenuItem root = new ToolStripMenuItem("Miftawerk Unlock");

            ToolStripMenuItem test = new ToolStripMenuItem("Windows Hello self-test");
            test.Click += OnSelfTestClick;
            root.DropDownItems.Add(test);

            ToolStripMenuItem del = new ToolStripMenuItem("Delete self-test key");
            del.Click += OnDeleteClick;
            root.DropDownItems.Add(del);

            return root;
        }

        private async void OnSelfTestClick(object sender, EventArgs e)
        {
            Form owner = m_host.MainWindow;
            string report = await HelloSelfTest.RunAsync(owner);
            MessageBox.Show(owner, report, "Miftawerk Unlock - self-test",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void OnDeleteClick(object sender, EventArgs e)
        {
            Form owner = m_host.MainWindow;
            string report = await HelloSelfTest.DeleteKeyAsync();
            MessageBox.Show(owner, report, "Miftawerk Unlock - self-test",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
