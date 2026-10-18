// Miftawerk Unlock - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Collections.Generic;
using System.Windows.Forms;

using KeePass.Forms;
using KeePass.Plugins;
using KeePassLib;

using Windows.Security.Credentials;

namespace MiftawerkUnlock
{
    /// <summary>
    /// After a manual unlock (master password etc.), offers to enable Windows Hello
    /// quick unlock for that database. Opt-in per database; nothing happens without "Yes".
    /// </summary>
    internal sealed class Enrollment
    {
        internal static readonly TimeSpan Validity = TimeSpan.FromDays(7);
        private const string Title = "Miftawerk Unlock";

        private readonly IPluginHost m_host;
        private readonly HashSet<string> m_declined = new HashSet<string>(); // this session only

        internal Enrollment(IPluginHost host)
        {
            m_host = host;
        }

        internal void OnFileOpened(object sender, FileOpenedEventArgs e)
        {
            PwDatabase db = e.Database;
            if (db == null || !db.IsOpen || !db.IOConnectionInfo.IsLocalFile()) return;

            // Let KeePass finish opening and drawing before asking anything.
            m_host.MainWindow.BeginInvoke(new Action(() => Offer(db)));
        }

        private async void Offer(PwDatabase db)
        {
            Form owner = m_host.MainWindow;
            KeySnapshot snap = null;
            byte[] sig1 = null, sig2 = null, plain = null;
            try
            {
                if (!db.IsOpen) return;

                byte[] pathHash = RecordStore.DbPathHash(db.IOConnectionInfo);
                string id = Convert.ToBase64String(pathHash);
                if (m_declined.Contains(id)) return;

                UnlockRecord existing = RecordStore.TryLoad(pathHash);
                if (existing != null && existing.IsCurrent(DateTime.UtcNow)) return;

                // Unsupported key types (key providers, YubiKey) or opened via quick unlock.
                snap = KeySnapshot.TryCapture(db);
                if (snap == null) return;

                DialogResult answer = MessageBox.Show(owner,
                    "Enable Windows Hello quick unlock for this database?\r\n\r\n" +
                    db.IOConnectionInfo.GetDisplayName() + "\r\n\r\n" +
                    "You can then open it with face, fingerprint or your Windows Hello PIN for " +
                    (int)Validity.TotalDays + " days or until Windows restarts. After that, the " +
                    "master password is required again.\r\n\r\n" +
                    "Important: Windows Hello always accepts the PIN. Anyone who knows your " +
                    "Windows Hello PIN and can use your Windows session can open this database " +
                    "during that time. This is a convenience feature, not a second factor.",
                    Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes)
                {
                    m_declined.Add(id);
                    return;
                }

                if (!await KeyCredentialManager.IsSupportedAsync())
                {
                    Info(owner, "Windows Hello is not set up on this computer. Nothing was changed.");
                    return;
                }

                KeyCredentialRetrievalResult kr = await HelloKey.OpenOrCreateAsync(owner);
                if (kr.Status != KeyCredentialStatus.Success)
                {
                    Info(owner, "Windows Hello key not available (" + kr.Status + "). Nothing was changed.");
                    return;
                }
                KeyCredential cred = kr.Credential;

                byte[] challenge = RecordCrypto.RandomBytes(32);

                // Requirement H5: two signatures must be identical and verifiable,
                // otherwise the record could never be opened again.
                SignResult s1 = await HelloKey.SignAsync(owner, cred, challenge);
                if (s1.Signature == null) { Info(owner, "Windows Hello: " + s1.Status + ". Nothing was changed."); return; }
                sig1 = s1.Signature;
                SignResult s2 = await HelloKey.SignAsync(owner, cred, challenge);
                if (s2.Signature == null) { Info(owner, "Windows Hello: " + s2.Status + ". Nothing was changed."); return; }
                sig2 = s2.Signature;

                if (!RecordCrypto.ConstantTimeEquals(sig1, sig2) ||
                    !HelloKey.VerifyPkcs1(cred, challenge, sig1))
                {
                    Info(owner, "Windows Hello signatures are not repeatable on this computer. " +
                        "Quick unlock cannot be used safely. Nothing was changed.");
                    return;
                }

                if (!db.IsOpen) return;
                plain = snap.Serialize();
                UnlockRecord rec = UnlockRecord.Seal(pathHash, Validity, challenge, sig1, plain);
                RecordStore.Save(rec);

                // Test-open before reporting success.
                UnlockRecord check = RecordStore.TryLoad(pathHash);
                byte[] back = check == null ? null : check.Open(sig1);
                bool ok = back != null && RecordCrypto.ConstantTimeEquals(back, plain);
                if (back != null) Array.Clear(back, 0, back.Length);
                if (!ok)
                {
                    RecordStore.Delete(pathHash);
                    Info(owner, "Self-check of the stored data failed. Nothing was kept.");
                    return;
                }

                Info(owner, "Windows Hello quick unlock is enabled for this database until " +
                    rec.ExpiresUtc.ToLocalTime().ToString("g") + " or the next Windows restart.");
            }
            catch (Exception ex)
            {
                Info(owner, "Windows Hello setup failed: " + ex.GetType().Name + " 0x" +
                    ex.HResult.ToString("X8") + "\r\n" + ex.Message);
            }
            finally
            {
                if (snap != null) snap.Clear();
                if (sig1 != null) Array.Clear(sig1, 0, sig1.Length);
                if (sig2 != null) Array.Clear(sig2, 0, sig2.Length);
                if (plain != null) Array.Clear(plain, 0, plain.Length);
            }
        }

        private static void Info(Form owner, string text)
        {
            MessageBox.Show(owner, text, Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
