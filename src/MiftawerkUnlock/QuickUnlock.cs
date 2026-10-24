// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Forms;

using KeePass.Forms;
using KeePass.Plugins;
using KeePass.UI;
using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Serialization;

using Windows.Security.Credentials;

namespace MiftawerkUnlock
{
    /// <summary>
    /// When KeePass shows its master key prompt for a database with a current record,
    /// asks Windows Hello first. On success the prompt is closed and the database is
    /// opened through the public MainForm.OpenDatabase with the rebuilt key.
    /// On cancel or any failure the normal prompt simply stays (fail-safe).
    /// </summary>
    internal sealed class QuickUnlock
    {
        private readonly IPluginHost m_host;
        // Path hashes whose next prompt must not be intercepted (after a failed quick unlock).
        private readonly HashSet<string> m_skipNext = new HashSet<string>();

        internal QuickUnlock(IPluginHost host)
        {
            m_host = host;
        }

        internal void OnWindowAdded(object sender, GwmWindowEventArgs e)
        {
            KeyPromptForm kpf = e.Form as KeyPromptForm;
            if (kpf == null) return;
            if (kpf.SecureDesktopMode) return; // Hello dialog would not be visible there (H14)
            if (SystemInformation.TerminalServerSession) return; // no Windows Hello over Remote Desktop (H12)

            IOConnectionInfo ioc = GetPromptConnection(kpf);
            if (ioc == null || !ioc.IsLocalFile()) return;

            byte[] pathHash = RecordStore.DbPathHash(ioc);
            if (m_skipNext.Remove(Convert.ToBase64String(pathHash))) return;

            UnlockRecord rec = RecordStore.TryLoad(pathHash);
            if (rec == null) return;
            if (!rec.IsCurrent(DateTime.UtcNow))
            {
                RecordStore.Delete(pathHash); // expired or Windows restarted
                return;
            }

            kpf.Shown += delegate { kpf.BeginInvoke(new Action(() => Run(kpf, ioc, rec))); };
        }

        /// <summary>
        /// KeePass has no public getter for the prompt's database. Read-only lookup of the
        /// single IOConnectionInfo field by type (not by name), fallback: the active locked
        /// document. If neither works, quick unlock is skipped and KeePass behaves normally.
        /// </summary>
        private IOConnectionInfo GetPromptConnection(KeyPromptForm kpf)
        {
            try
            {
                foreach (FieldInfo fi in typeof(KeyPromptForm).GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (fi.FieldType != typeof(IOConnectionInfo)) continue;
                    IOConnectionInfo ioc = fi.GetValue(kpf) as IOConnectionInfo;
                    if (ioc != null && !string.IsNullOrEmpty(ioc.Path)) return ioc;
                }
            }
            catch (Exception) { }

            PwDocument doc = m_host.MainWindow.DocumentManager.ActiveDocument;
            if (doc != null && doc.LockedIoc != null && !string.IsNullOrEmpty(doc.LockedIoc.Path))
                return doc.LockedIoc;
            return null;
        }

        private async void Run(KeyPromptForm kpf, IOConnectionInfo ioc, UnlockRecord rec)
        {
            byte[] sig = null, plain = null;
            KeySnapshot snap = null;
            try
            {
                KeyCredentialRetrievalResult kr = await KeyCredentialManager.OpenAsync(HelloKey.CredentialName);
                if (kr.Status == KeyCredentialStatus.NotFound)
                {
                    RecordStore.Delete(rec.DbPathHash); // Windows Hello was reset
                    return;
                }
                if (kr.Status != KeyCredentialStatus.Success) return;

                SignResult s = await HelloKey.SignAsync(kpf, kr.Credential, rec.Challenge);
                if (s.Signature == null) return; // cancelled: user types the password
                sig = s.Signature;

                if (kpf.IsDisposed || !kpf.Visible) return; // user already used the prompt

                try { plain = rec.Open(sig); }
                catch (CryptographicException)
                {
                    RecordStore.Delete(rec.DbPathHash);
                    return;
                }
                snap = KeySnapshot.Deserialize(plain);
                CompositeKey ck = snap.ToCompositeKey();
                byte[] rootUuid = (byte[])snap.RootGroupUuid.Clone();

                kpf.DialogResult = DialogResult.Cancel; // closes the prompt
                m_host.MainWindow.BeginInvoke(new Action(() =>
                    OpenWithKey(ioc, ck, rootUuid, rec.DbPathHash)));
            }
            catch (Exception ex)
            {
                Ui.Warn(kpf.IsDisposed ? (IWin32Window)m_host.MainWindow : kpf,
                    Strings.Format(Strings.UnlockFailed, ex.GetType().Name + " 0x" + ex.HResult.ToString("X8")));
            }
            finally
            {
                if (snap != null) snap.Clear();
                if (sig != null) Array.Clear(sig, 0, sig.Length);
                if (plain != null) Array.Clear(plain, 0, plain.Length);
            }
        }

        private void OpenWithKey(IOConnectionInfo ioc, CompositeKey ck, byte[] rootUuid,
            byte[] pathHash)
        {
            MainForm mf = m_host.MainWindow;
            mf.OpenDatabase(ioc, ck, false);

            PwDatabase db = mf.DocumentManager.ActiveDatabase;
            bool opened = db != null && db.IsOpen &&
                RecordCrypto.ConstantTimeEquals(RecordStore.DbPathHash(db.IOConnectionInfo), pathHash);

            if (!opened)
            {
                // Key no longer valid (e.g. master key changed): forget and fall back.
                RecordStore.Delete(pathHash);
                m_skipNext.Add(Convert.ToBase64String(pathHash));
                mf.OpenDatabase(ioc, null, false);
                return;
            }

            if (!RecordCrypto.ConstantTimeEquals(db.RootGroup.Uuid.UuidBytes, rootUuid))
            {
                RecordStore.Delete(pathHash);
                Ui.Warn(mf, Strings.Get(Strings.WrongDatabase));
            }
        }
    }
}
