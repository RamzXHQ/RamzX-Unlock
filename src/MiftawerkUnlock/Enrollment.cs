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
        private readonly IPluginHost m_host;
        private readonly Settings m_settings;
        private readonly HashSet<string> m_declined = new HashSet<string>(); // this session only

        internal Enrollment(IPluginHost host, Settings settings)
        {
            m_host = host;
            m_settings = settings;
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
                if (!db.IsOpen || !m_settings.OfferEnrollment) return;

                byte[] pathHash = RecordStore.DbPathHash(db.IOConnectionInfo);
                string id = Convert.ToBase64String(pathHash);
                if (m_declined.Contains(id)) return;

                UnlockRecord existing = RecordStore.TryLoad(pathHash);
                if (existing != null && existing.IsCurrent(DateTime.UtcNow)) return;

                // Unsupported key types (key providers, YubiKey) or opened via quick unlock.
                snap = KeySnapshot.TryCapture(db);
                if (snap == null) return;

                TimeSpan validity = m_settings.Validity;
                if (!Ui.Ask(owner, Strings.Format(Strings.EnrollQuestion,
                    db.IOConnectionInfo.GetDisplayName(), (int)validity.TotalDays)))
                {
                    m_declined.Add(id);
                    return;
                }

                if (!await KeyCredentialManager.IsSupportedAsync())
                {
                    Ui.Info(owner, Strings.Get(Strings.HelloNotSetUp));
                    return;
                }

                KeyCredentialRetrievalResult kr = await HelloKey.OpenOrCreateAsync(owner);
                if (kr.Status != KeyCredentialStatus.Success)
                {
                    Ui.Info(owner, Strings.Format(Strings.HelloKeyUnavailable, kr.Status));
                    return;
                }
                KeyCredential cred = kr.Credential;

                byte[] challenge = RecordCrypto.RandomBytes(32);

                // Requirement H5: two signatures must be identical and verifiable,
                // otherwise the record could never be opened again.
                SignResult s1 = await HelloKey.SignAsync(owner, cred, challenge);
                if (s1.Signature == null) { Ui.Info(owner, Strings.Format(Strings.HelloStatus, s1.Status)); return; }
                sig1 = s1.Signature;
                SignResult s2 = await HelloKey.SignAsync(owner, cred, challenge);
                if (s2.Signature == null) { Ui.Info(owner, Strings.Format(Strings.HelloStatus, s2.Status)); return; }
                sig2 = s2.Signature;

                if (!RecordCrypto.ConstantTimeEquals(sig1, sig2) ||
                    !HelloKey.VerifyPkcs1(cred, challenge, sig1))
                {
                    Ui.Info(owner, Strings.Get(Strings.NotRepeatable));
                    return;
                }

                if (!db.IsOpen) return;
                plain = snap.Serialize();
                UnlockRecord rec = UnlockRecord.Seal(pathHash, validity, challenge, sig1, plain);
                RecordStore.Save(rec);

                // Test-open before reporting success.
                UnlockRecord check = RecordStore.TryLoad(pathHash);
                byte[] back = check == null ? null : check.Open(sig1);
                bool ok = back != null && RecordCrypto.ConstantTimeEquals(back, plain);
                if (back != null) Array.Clear(back, 0, back.Length);
                if (!ok)
                {
                    RecordStore.Delete(pathHash);
                    Ui.Warn(owner, Strings.Get(Strings.SelfCheckFailed));
                    return;
                }

                Ui.Info(owner, Strings.Format(Strings.EnrollDone, rec.ExpiresUtc.ToLocalTime().ToString("g")));
            }
            catch (Exception ex)
            {
                Ui.Warn(owner, Strings.Format(Strings.EnrollFailed, ex.GetType().Name + " 0x" +
                    ex.HResult.ToString("X8") + "\r\n" + ex.Message));
            }
            finally
            {
                if (snap != null) snap.Clear();
                if (sig1 != null) Array.Clear(sig1, 0, sig1.Length);
                if (sig2 != null) Array.Clear(sig2, 0, sig2.Length);
                if (plain != null) Array.Clear(plain, 0, plain.Length);
            }
        }
    }
}
