// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace MiftawerkUnlock
{
    /// <summary>
    /// Phase 0 experiments 1-3 (see reports/): does the Windows Hello prompt work inside
    /// KeePass.exe on this machine, are KeyCredential signatures deterministic, and
    /// which window holds the foreground while the prompt is open.
    /// Logs only status codes, timings and booleans - never challenge or signature bytes.
    /// </summary>
    internal static class HelloSelfTest
    {
        internal const string CredentialName = "MiftawerkUnlock.SelfTest";
        private const int SignRounds = 3;
        private const string LogFileName = "MiftawerkUnlock-selftest.log";

        internal static async Task<string> RunAsync(Form owner)
        {
            StringBuilder log = new StringBuilder();
            byte[] challenge = new byte[32];
            byte[][] sigs = new byte[SignRounds][];

            try
            {
                Log(log, "=== Windows Hello self-test " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                Log(log, "OS: " + Environment.OSVersion.VersionString +
                    ", 64-bit process: " + Environment.Is64BitProcess +
                    ", KeePass: " + KeePassLib.PwDefs.VersionString);

                bool supported = await KeyCredentialManager.IsSupportedAsync();
                Log(log, "IsSupported: " + supported);
                Log(log, "WebAuthn API: " + WebAuthnPrf.ApiVersion() + ", passkey unlock available: " +
                    WebAuthnPrf.IsAvailable() + ", stored passkey: " + (RecordStore.TryLoadPasskeyId() != null));
                if (SystemInformation.TerminalServerSession)
                    Log(log, "Remote Desktop session: the Windows Hello key is not available over RDP; " +
                        "quick unlock uses a passkey on your local PC instead (WebAuthn redirection).");
                if (!supported) return Finish(log);

                KeyCredentialRetrievalResult kr = await WithForegroundProbe(owner, log, "create",
                    () => KeyCredentialManager.RequestCreateAsync(CredentialName,
                        KeyCredentialCreationOption.FailIfExists).AsTask());
                Log(log, "RequestCreate: " + kr.Status);

                if (kr.Status == KeyCredentialStatus.CredentialAlreadyExists)
                {
                    kr = await KeyCredentialManager.OpenAsync(CredentialName);
                    Log(log, "Open: " + kr.Status);
                }
                if (kr.Status != KeyCredentialStatus.Success) return Finish(log);

                KeyCredential cred = kr.Credential;

                KeyCredentialAttestationResult att = await cred.GetAttestationAsync();
                Log(log, "Attestation (TPM-backed if Success): " + att.Status);

                byte[] pubKeyHash = HashPublicKey(cred);
                SelfTestState prev = SelfTestState.Load();
                bool reuse = prev != null && ByteArraysEqual(prev.PublicKeyHash, pubKeyHash);
                if (reuse) Array.Copy(prev.Challenge, challenge, challenge.Length);
                else
                {
                    using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                        rng.GetBytes(challenge);
                }
                Log(log, "Challenge: " + (reuse ? "reused from previous run" : "new (first run with this key)"));
                IBuffer challengeBuf = CryptographicBuffer.CreateFromByteArray(challenge);

                for (int i = 0; i < SignRounds; ++i)
                {
                    KeyCredentialOperationResult sr = await WithForegroundProbe(owner, log,
                        "sign " + (i + 1), () => cred.RequestSignAsync(challengeBuf).AsTask());
                    Log(log, "Sign " + (i + 1) + ": " + sr.Status);
                    if (sr.Status != KeyCredentialStatus.Success) return Finish(log);

                    byte[] sig;
                    CryptographicBuffer.CopyToByteArray(sr.Result, out sig);
                    sigs[i] = sig;
                }

                Log(log, "Signature length: " + sigs[0].Length + " bytes");
                bool deterministic = true;
                for (int i = 1; i < SignRounds; ++i)
                    deterministic &= ByteArraysEqual(sigs[0], sigs[i]);
                Log(log, "Deterministic (all signatures identical): " + deterministic);

                byte[] fp = Fingerprint(sigs[0]);
                if (reuse)
                    Log(log, "Same signature as previous run (KeePass restart / reboot test): " +
                        ByteArraysEqual(prev.SignatureFingerprint, fp));
                else
                    new SelfTestState(challenge, pubKeyHash, fp).Save();

                VerifySignatures(cred, challenge, sigs, log);
            }
            catch (Exception ex)
            {
                Log(log, "EXCEPTION " + ex.GetType().Name + " 0x" +
                    ex.HResult.ToString("X8") + ": " + ex.Message);
            }
            finally
            {
                Array.Clear(challenge, 0, challenge.Length);
                foreach (byte[] s in sigs)
                    if (s != null) Array.Clear(s, 0, s.Length);
            }

            return Finish(log);
        }

        internal static async Task<string> DeleteKeyAsync()
        {
            StringBuilder log = new StringBuilder();
            try
            {
                await KeyCredentialManager.DeleteAsync(CredentialName);
                SelfTestState.Delete();
                Log(log, "Self-test key deleted.");
            }
            catch (Exception ex)
            {
                Log(log, "Delete failed " + ex.GetType().Name + " 0x" +
                    ex.HResult.ToString("X8") + ": " + ex.Message);
            }
            return Finish(log);
        }

        private static void VerifySignatures(KeyCredential cred, byte[] data, byte[][] sigs,
            StringBuilder log)
        {
            RSAParameters p;
            try { p = HelloKey.ExportPublicKey(cred); }
            catch (Exception ex)
            {
                Log(log, "Public key export failed: " + ex.GetType().Name + " 0x" +
                    ex.HResult.ToString("X8"));
                return;
            }
            Log(log, "Public key: RSA " + (p.Modulus.Length * 8) + " bit");

            using (RSACng rsa = new RSACng())
            {
                rsa.ImportParameters(p);
                for (int i = 0; i < sigs.Length; ++i)
                {
                    bool v15 = rsa.VerifyData(data, sigs[i], HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);
                    bool pss = rsa.VerifyData(data, sigs[i], HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pss);
                    Log(log, "Verify sig " + (i + 1) + ": PKCS#1 v1.5=" + v15 + ", PSS=" + pss);
                }
            }
        }

        /// <summary>
        /// Activates KeePass, runs the Hello operation and samples the foreground window
        /// every 150 ms, so the log shows whether the prompt really came to the front.
        /// Foreground fix: while KeePass itself holds the foreground and the Hello dialog
        /// exists, the dialog is brought to the front. Never steals focus from other apps.
        /// </summary>
        private static async Task<T> WithForegroundProbe<T>(Form owner, StringBuilder log,
            string label, Func<Task<T>> op)
        {
            if (owner != null && owner.Visible) owner.Activate();

            List<string> seen = new List<string>();
            int pushes = 0;
            Timer timer = new Timer();
            timer.Interval = 150;
            timer.Tick += delegate
            {
                if (HelloForeground.TryBringDialogToFront()) ++pushes;

                string fg = DescribeForegroundWindow();
                if (seen.Count == 0 || seen[seen.Count - 1] != fg) seen.Add(fg);
            };

            Stopwatch sw = Stopwatch.StartNew();
            timer.Start();
            try { return await op(); }
            finally
            {
                timer.Stop();
                timer.Dispose();
                Log(log, "  [" + label + "] " + sw.ElapsedMilliseconds + " ms, pushed to front: " +
                    pushes + "x, foreground: " +
                    (seen.Count == 0 ? "(no sample)" : string.Join(" -> ", seen)));
            }
        }

        private static byte[] HashPublicKey(KeyCredential cred)
        {
            byte[] raw;
            CryptographicBuffer.CopyToByteArray(cred.RetrievePublicKey(), out raw);
            using (SHA256 sha = SHA256.Create()) return sha.ComputeHash(raw);
        }

        /// <summary>
        /// Short one-way tag of a signature, only to compare runs. The self-test key
        /// protects nothing; the production code will never persist anything like this.
        /// </summary>
        private static byte[] Fingerprint(byte[] sig)
        {
            using (HMACSHA256 h = new HMACSHA256(sig))
            {
                byte[] full = h.ComputeHash(Encoding.UTF8.GetBytes("MiftawerkUnlock self-test fingerprint v1"));
                byte[] tag = new byte[8];
                Array.Copy(full, tag, tag.Length);
                return tag;
            }
        }

        /// <summary>
        /// Non-secret state for the restart/reboot test: the challenge, the public key hash
        /// and the 8-byte fingerprint. Stored next to the isolated test KeePass.
        /// </summary>
        private sealed class SelfTestState
        {
            private const string FileName = "MiftawerkUnlock-selftest.state";

            internal readonly byte[] Challenge;
            internal readonly byte[] PublicKeyHash;
            internal readonly byte[] SignatureFingerprint;

            internal SelfTestState(byte[] challenge, byte[] pubKeyHash, byte[] fp)
            {
                Challenge = (byte[])challenge.Clone();
                PublicKeyHash = pubKeyHash;
                SignatureFingerprint = fp;
            }

            private static string FilePath
            {
                get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName); }
            }

            internal static SelfTestState Load()
            {
                try
                {
                    string[] l = File.ReadAllLines(FilePath);
                    if (l.Length < 3) return null;
                    return new SelfTestState(Convert.FromBase64String(l[0]),
                        Convert.FromBase64String(l[1]), Convert.FromBase64String(l[2]));
                }
                catch (Exception) { return null; }
            }

            internal void Save()
            {
                File.WriteAllLines(FilePath, new string[] {
                    Convert.ToBase64String(Challenge), Convert.ToBase64String(PublicKeyHash),
                    Convert.ToBase64String(SignatureFingerprint) });
            }

            internal static void Delete()
            {
                try { File.Delete(FilePath); }
                catch (Exception) { }
            }
        }

        private static string DescribeForegroundWindow()
        {
            IntPtr h = NativeMethods.GetForegroundWindow();
            if (h == IntPtr.Zero) return "(none)";

            StringBuilder cls = new StringBuilder(256);
            NativeMethods.GetClassName(h, cls, cls.Capacity);

            uint pid;
            NativeMethods.GetWindowThreadProcessId(h, out pid);
            string proc;
            try { using (Process p = Process.GetProcessById((int)pid)) proc = p.ProcessName; }
            catch (ArgumentException) { proc = "pid " + pid; }

            return proc + "/" + cls;
        }

        private static bool ByteArraysEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; ++i) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static void Log(StringBuilder log, string line)
        {
            log.AppendLine(line);
        }

        private static string Finish(StringBuilder log)
        {
            string text = log.ToString();
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogFileName);
                File.AppendAllText(path, text + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception) { } // log file is a convenience only
            return text;
        }
    }
}
