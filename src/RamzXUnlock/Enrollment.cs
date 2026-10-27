// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

using KeePass.Forms;
using KeePass.Plugins;
using KeePassLib;

using Windows.Security.Credentials;

namespace RamzXUnlock
{
    /// <summary>
    /// After a manual unlock (master password etc.), offers to enable Windows Hello quick unlock
    /// for that database - on the device the user is working from. Opt-in; nothing happens
    /// without "Yes".
    ///
    /// One method for the user, chosen automatically:
    ///   1. A passkey (WebAuthn PRF) in Windows Hello of the PC the user sits at. Works at this
    ///      computer and over Remote Desktop; several devices can be linked to one database.
    ///   2. Fallback on Windows without passkey secrets (PRF): the plugin's Windows Hello key
    ///      (KeyCredentialManager), only at this computer.
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
            byte[] plain = null;
            try
            {
                if (!db.IsOpen || !m_settings.OfferEnrollment) return;

                bool remote = SystemInformation.TerminalServerSession;
                bool passkeys = WebAuthnPrf.IsAvailable();
                // Over Remote Desktop only passkeys work (H12); without them do not ask (no nagging).
                if (remote && !passkeys) return;

                byte[] pathHash = RecordStore.DbPathHash(db.IOConnectionInfo);
                string id = Convert.ToBase64String(pathHash);
                if (m_declined.Contains(id)) return;

                // Unsupported key types (key providers, YubiKey) or opened via quick unlock.
                snap = KeySnapshot.TryCapture(db);
                if (snap == null) return;

                // The master password was typed although quick unlock is enabled: probably another
                // device (or the prompt was cancelled). Then ask to link this device as well.
                bool alreadyEnabled = false;
                foreach (UnlockRecord r in RecordStore.LoadAll(pathHash))
                {
                    if (!r.IsCurrent(DateTime.UtcNow)) continue;
                    // A current Windows Hello key record at this computer: nothing to add.
                    if (r.Kind == RecordKind.HelloKey && !remote) return;
                    alreadyEnabled = true;
                }

                TimeSpan validity = m_settings.Validity;
                string question = alreadyEnabled
                    ? Strings.Format(Strings.EnrollAnotherDevice, db.IOConnectionInfo.GetDisplayName(), (int)validity.TotalDays)
                    : Strings.Format(Strings.EnrollQuestion, db.IOConnectionInfo.GetDisplayName(), (int)validity.TotalDays);
                if (!EnrollForm.Ask(owner, question))
                {
                    m_declined.Add(id);
                    return;
                }

                plain = snap.Serialize();
                if (passkeys)
                {
                    EnrollResult r = await EnrollPasskeyAsync(owner, db, pathHash, validity, plain);
                    if (r == EnrollResult.Done || r == EnrollResult.Stop) return;
                    if (remote) return; // NoSecret over Remote Desktop: nothing else can work
                    // NoSecret at this computer (older Windows): fall back to the Windows Hello key.
                }
                await EnrollHelloKeyAsync(owner, db, pathHash, validity, plain);
            }
            catch (Exception ex)
            {
                Ui.Warn(owner, Strings.Format(Strings.EnrollFailed, ex.GetType().Name + " 0x" +
                    ex.HResult.ToString("X8") + "\r\n" + ex.Message));
            }
            finally
            {
                if (snap != null) snap.Clear();
                if (plain != null) Array.Clear(plain, 0, plain.Length);
            }
        }

        private enum EnrollResult { Done, Stop, NoSecret }

        /// <summary>
        /// Passkey in Windows Hello of the PC the user sits at (locally or over Remote Desktop).
        /// If one of the known passkeys is on this PC it is reused, otherwise a new one is created.
        /// The record key comes from the passkey's PRF value for a random per-record challenge.
        /// </summary>
        private async Task<EnrollResult> EnrollPasskeyAsync(Form owner, PwDatabase db, byte[] pathHash,
            TimeSpan validity, byte[] plain)
        {
            IntPtr hwnd = owner.Handle;
            byte[] challenge = RecordCrypto.RandomBytes(32);
            byte[] s1 = null, s2 = null;
            try
            {
                byte[] credId = null;
                PrfResult p1 = null;
                List<byte[]> known = RecordStore.LoadPasskeyIds();
                if (known.Count > 0)
                {
                    List<KeyValuePair<byte[], byte[]>> ask = new List<KeyValuePair<byte[], byte[]>>();
                    foreach (byte[] k in known) ask.Add(new KeyValuePair<byte[], byte[]>(k, challenge));
                    p1 = await WebAuthnPrf.GetSecretAsync(hwnd, ask);
                    if (p1.Status == PrfStatus.Cancelled) return EnrollResult.Stop;
                    if (p1.Status == PrfStatus.Success) credId = p1.CredentialId;
                    // NotFound/NoSecret/Error: none of the known passkeys works on this PC.
                }
                if (credId == null)
                {
                    PrfResult c = await WebAuthnPrf.CreateAsync(hwnd);
                    if (c.Status == PrfStatus.Cancelled) return EnrollResult.Stop;
                    if (c.Status != PrfStatus.Success) return EnrollResult.NoSecret;
                    credId = c.Data;
                    p1 = await WebAuthnPrf.GetSecretAsync(hwnd, credId, challenge);
                    if (p1.Status == PrfStatus.Cancelled) return EnrollResult.Stop;
                    if (p1.Status != PrfStatus.Success) return EnrollResult.NoSecret;
                    RecordStore.AddPasskeyId(credId);
                }
                s1 = p1.Data;

                // Same requirement as for the Windows Hello key (H5): the secret must be repeatable.
                PrfResult p2 = await WebAuthnPrf.GetSecretAsync(hwnd, credId, challenge);
                if (p2.Status == PrfStatus.Cancelled) return EnrollResult.Stop;
                if (p2.Status != PrfStatus.Success) return EnrollResult.NoSecret;
                s2 = p2.Data;
                if (!RecordCrypto.ConstantTimeEquals(s1, s2)) return EnrollResult.NoSecret;

                if (!db.IsOpen) return EnrollResult.Stop;
                // Replace an older record of this database for the same passkey (also 0.4.0's).
                foreach (UnlockRecord old in RecordStore.LoadAll(pathHash))
                    if (old.Kind == RecordKind.WebAuthnPrf && RecordCrypto.ConstantTimeEquals(old.CredentialId, credId))
                        RecordStore.Delete(old);

                UnlockRecord rec = UnlockRecord.SealPrf(credId, pathHash, validity, challenge, s1, plain);
                RecordStore.Save(rec);
                if (!SelfCheck(pathHash, r => r.Kind == RecordKind.WebAuthnPrf &&
                    RecordCrypto.ConstantTimeEquals(r.CredentialId, credId), s1, plain))
                {
                    RecordStore.Delete(rec);
                    Ui.Warn(owner, Strings.Get(Strings.SelfCheckFailed));
                    return EnrollResult.Stop;
                }

                Ui.Info(owner, Strings.Format(Strings.EnrollDone, rec.ExpiresUtc.ToLocalTime().ToString("g")));
                return EnrollResult.Done;
            }
            finally
            {
                if (s1 != null) Array.Clear(s1, 0, s1.Length);
                if (s2 != null) Array.Clear(s2, 0, s2.Length);
            }
        }

        /// <summary>Fallback: the plugin's Windows Hello key at this computer (as in 0.1.0-0.3.0).</summary>
        private async Task EnrollHelloKeyAsync(Form owner, PwDatabase db, byte[] pathHash, TimeSpan validity, byte[] plain)
        {
            byte[] sig1 = null, sig2 = null;
            try
            {
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
                UnlockRecord rec = UnlockRecord.Seal(pathHash, validity, challenge, sig1, plain);
                RecordStore.Save(rec);

                if (!SelfCheck(pathHash, r => r.Kind == RecordKind.HelloKey, sig1, plain))
                {
                    RecordStore.Delete(rec);
                    Ui.Warn(owner, Strings.Get(Strings.SelfCheckFailed));
                    return;
                }

                Ui.Info(owner, Strings.Format(Strings.EnrollDone, rec.ExpiresUtc.ToLocalTime().ToString("g")));
            }
            finally
            {
                if (sig1 != null) Array.Clear(sig1, 0, sig1.Length);
                if (sig2 != null) Array.Clear(sig2, 0, sig2.Length);
            }
        }

        /// <summary>Test-open the stored record before reporting success.</summary>
        private static bool SelfCheck(byte[] pathHash, Predicate<UnlockRecord> which, byte[] ikm, byte[] plain)
        {
            UnlockRecord check = RecordStore.LoadAll(pathHash).Find(which);
            byte[] back = check == null ? null : check.Open(ikm);
            bool ok = back != null && RecordCrypto.ConstantTimeEquals(back, plain);
            if (back != null) Array.Clear(back, 0, back.Length);
            return ok;
        }
    }
}
