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

namespace RamzXUnlock
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
        // KeePass's main window handle, read on the UI thread. On KeePass's secure desktop the
        // key prompt lives on another desktop; WebAuthn refuses an owner window there
        // (RPC_E_ACCESS_DENIED, measured), but accepts the main window on the user's desktop.
        private IntPtr m_mainHandle = IntPtr.Zero;
        private const int RpcAccessDenied = unchecked((int)0x8001011B);

        internal QuickUnlock(IPluginHost host)
        {
            m_host = host;
            if (host.MainWindow.IsHandleCreated) m_mainHandle = host.MainWindow.Handle;
            else host.MainWindow.HandleCreated += delegate { m_mainHandle = host.MainWindow.Handle; };
        }

        internal void OnWindowAdded(object sender, GwmWindowEventArgs e)
        {
            KeyPromptForm kpf = e.Form as KeyPromptForm;
            if (kpf == null) return;
            IOConnectionInfo ioc = GetPromptConnection(kpf);
            if (ioc == null || !ioc.IsLocalFile()) return;

            byte[] pathHash = RecordStore.DbPathHash(ioc);
            if (m_skipNext.Remove(Convert.ToBase64String(pathHash))) return;

            List<UnlockRecord> usable = new List<UnlockRecord>();
            DateTime now = DateTime.UtcNow;
            foreach (UnlockRecord r in RecordStore.LoadAll(pathHash))
            {
                if (!r.IsCurrent(now)) { RecordStore.Delete(r); continue; } // expired or Windows restarted
                if (CanUseHere(r, kpf.SecureDesktopMode)) usable.Add(r);
            }
            if (usable.Count == 0) return;

            kpf.Shown += delegate { kpf.BeginInvoke(new Action(() => Run(kpf, ioc, usable))); };
        }

        /// <summary>
        /// Windows Hello key records need this PC's own Windows Hello: not over Remote Desktop (H12)
        /// and not on KeePass's secure desktop, where its dialog would be hidden (H14).
        /// Passkey records work locally and over Remote Desktop (the prompt then appears on the
        /// user's own PC, so the remote secure desktop does not hide it).
        /// </summary>
        private static bool CanUseHere(UnlockRecord r, bool secureDesktop)
        {
            bool remote = SystemInformation.TerminalServerSession;
            if (r.Kind == RecordKind.HelloKey) return !remote && !secureDesktop;
            return remote || !secureDesktop;
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

        private async void Run(KeyPromptForm kpf, IOConnectionInfo ioc, List<UnlockRecord> usable)
        {
            byte[] sig = null, plain = null;
            KeySnapshot snap = null;
            UnlockRecord rec = null;
            try
            {
                List<UnlockRecord> passkeys = usable.FindAll(r => r.Kind == RecordKind.WebAuthnPrf);
                UnlockRecord hello = usable.Find(r => r.Kind == RecordKind.HelloKey);

                if (passkeys.Count > 0)
                {
                    // One prompt for all devices: Windows uses the passkey of the PC the user sits at.
                    List<KeyValuePair<byte[], byte[]>> ask = new List<KeyValuePair<byte[], byte[]>>();
                    foreach (UnlockRecord r in passkeys) ask.Add(new KeyValuePair<byte[], byte[]>(r.CredentialId, r.Challenge));
                    IntPtr owner = kpf.SecureDesktopMode && m_mainHandle != IntPtr.Zero ? m_mainHandle : kpf.Handle;
                    PrfResult p = await WebAuthnPrf.GetSecretAsync(owner, ask);
                    if (p.Status == PrfStatus.Error && p.HResult == RpcAccessDenied && owner != IntPtr.Zero)
                        p = await WebAuthnPrf.GetSecretAsync(IntPtr.Zero, ask); // no owner window at all

                    if (p.Status == PrfStatus.Success)
                    {
                        rec = passkeys.Find(r => p.CredentialId != null && RecordCrypto.ConstantTimeEquals(r.CredentialId, p.CredentialId));
                        if (rec == null) { Array.Clear(p.Data, 0, p.Data.Length); return; }
                        sig = p.Data;
                    }
                    else if (p.Status == PrfStatus.Cancelled) return; // the user types the password
                    else if (p.Status == PrfStatus.Error || p.Status == PrfStatus.NoSecret)
                    {
                        if (hello == null)
                        {
                            Ui.Warn(kpf.IsDisposed ? (IWin32Window)m_host.MainWindow : kpf,
                                Strings.Format(Strings.UnlockFailed, "WebAuthn " + p.Status + " 0x" + p.HResult.ToString("X8")));
                            return;
                        }
                    }
                    // NotFound (no linked passkey on this PC): try the Windows Hello key below, if any.
                }

                if (sig == null)
                {
                    if (hello == null) return; // records are kept: another device may be used next time
                    rec = hello;
                    KeyCredentialRetrievalResult kr = await KeyCredentialManager.OpenAsync(HelloKey.CredentialName);
                    if (kr.Status == KeyCredentialStatus.NotFound)
                    {
                        RecordStore.Delete(rec); // Windows Hello was reset
                        return;
                    }
                    if (kr.Status != KeyCredentialStatus.Success) return;

                    SignResult s = await HelloKey.SignAsync(kpf, kr.Credential, rec.Challenge);
                    if (s.Signature == null) return; // cancelled: user types the password
                    sig = s.Signature;
                }

                if (kpf.IsDisposed || !kpf.Visible) return; // user already used the prompt

                try { plain = rec.Open(sig); }
                catch (CryptographicException)
                {
                    RecordStore.Delete(rec);
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
            KeePass.App.Configuration.AceKeyAssoc before = KeySources.Capture(ioc);
            mf.OpenDatabase(ioc, ck, false);
            KeySources.Restore(before);

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
