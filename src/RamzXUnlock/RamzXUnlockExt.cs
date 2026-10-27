// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Windows.Forms;

using KeePass;
using KeePass.Plugins;
using KeePass.UI;

namespace RamzXUnlock
{
    /// <summary>
    /// Plugin entry point. KeePass requires the class name "{Namespace}Ext".
    /// </summary>
    public sealed class RamzXUnlockExt : Plugin
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

            m_settings = new Settings(host.CustomConfig);
            Strings.Select(Program.Translation.Properties.Iso6391Code, m_settings.Language);

            m_enrollment = new Enrollment(host, m_settings);
            m_host.MainWindow.FileOpened += m_enrollment.OnFileOpened;

            m_quickUnlock = new QuickUnlock(host);
            GlobalWindowManager.WindowAdded += m_quickUnlock.OnWindowAdded;

            m_lockWatcher = new WindowsLockWatcher(host, m_settings);

            // remembered "key provider" entries of earlier quick unlocks (see KeySources)
            try { KeySources.CleanUp(); } catch (Exception) { }
            WarnAboutOldPluginFile();
            return true;
        }

        // Up to 0.6 the plugin file was MiftawerkUnlock.dll. If it is still there, KeePass loads both and
        // each one asks for Windows Hello. The data (records, settings) is shared, so deleting it loses nothing.
        private void WarnAboutOldPluginFile()
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(typeof(RamzXUnlockExt).Assembly.Location);
                string old = System.IO.Path.Combine(dir ?? string.Empty, "MiftawerkUnlock.dll");
                if (!System.IO.File.Exists(old)) return;
                m_host.MainWindow.Shown += delegate
                {
                    Ui.Warn(m_host.MainWindow, Strings.Format(Strings.OldPluginFile, old));
                };
            }
            catch (Exception) { }
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
            root.Image = Brand.MenuIcon(m_host.MainWindow);
            AddItem(root, Strings.MenuSettings, OnSettingsClick);
            AddItem(root, Strings.MenuForget, OnForgetClick);
            root.DropDownItems.Add(new ToolStripSeparator());
            AddItem(root, Strings.MenuSelfTest, OnSelfTestClick);
            AddItem(root, Strings.MenuDeleteSelfTest, OnDeleteClick);

            // Brand line: RamzX logo and website.
            root.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem about = new ToolStripMenuItem("RamzX · ramzx.de");
            about.Image = Brand.MenuIcon(m_host.MainWindow);
            about.Click += delegate
            {
                try { System.Diagnostics.Process.Start(Brand.Website); }
                catch (Exception) { }
            };
            root.DropDownItems.Add(about);
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
            if (UIUtil.ShowDialogAndDestroy(new SettingsForm(m_settings)) == DialogResult.OK)
                Strings.Select(Program.Translation.Properties.Iso6391Code, m_settings.Language);
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
