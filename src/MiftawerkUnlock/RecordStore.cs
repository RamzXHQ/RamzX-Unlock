// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using KeePassLib.Serialization;

namespace MiftawerkUnlock
{
    /// <summary>
    /// Local, non-roaming, per-user storage of unlock records:
    /// %LOCALAPPDATA%\SeeColors\MiftawerkUnlock\records\{sha256(path)}.mwu
    /// Each file is additionally wrapped with DPAPI (CurrentUser). Never Credential
    /// Manager or PasswordVault (those roam / have size limits, see report H7).
    /// The plugin never writes to the database file itself.
    /// </summary>
    internal static class RecordStore
    {
        private static readonly byte[] DpapiEntropy =
            Encoding.ASCII.GetBytes("SeeColors.MiftawerkUnlock.Store.v1");

        // Tests point this to a temporary folder; null = the per-user default below.
        internal static string DirectoryOverride;

        internal static string Directory
        {
            get
            {
                if (DirectoryOverride != null) return DirectoryOverride;
                return Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                    "SeeColors", "MiftawerkUnlock", "records");
            }
        }

        /// <summary>SHA-256 of the normalized local path. Moving the file requires re-enrollment.</summary>
        internal static byte[] DbPathHash(IOConnectionInfo ioc)
        {
            string norm = Path.GetFullPath(ioc.Path).ToUpperInvariant();
            return RecordCrypto.Sha256(Encoding.UTF8.GetBytes(norm));
        }

        private static string FileFor(byte[] dbPathHash)
        {
            StringBuilder sb = new StringBuilder(64);
            foreach (byte b in dbPathHash) sb.Append(b.ToString("x2"));
            return Path.Combine(Directory, sb + ".mwu");
        }

        internal static void Save(UnlockRecord r)
        {
            System.IO.Directory.CreateDirectory(Directory);
            byte[] wrapped = ProtectedData.Protect(r.ToBytes(), DpapiEntropy,
                DataProtectionScope.CurrentUser);

            string target = FileFor(r.DbPathHash);
            string tmp = target + ".tmp";
            File.WriteAllBytes(tmp, wrapped);
            if (File.Exists(target)) File.Replace(tmp, target, null);
            else File.Move(tmp, target);
        }

        /// <summary>Returns null if there is no readable record for this database.</summary>
        internal static UnlockRecord TryLoad(byte[] dbPathHash)
        {
            string f = FileFor(dbPathHash);
            if (!File.Exists(f)) return null;
            try
            {
                byte[] raw = ProtectedData.Unprotect(File.ReadAllBytes(f), DpapiEntropy,
                    DataProtectionScope.CurrentUser);
                UnlockRecord r = UnlockRecord.FromBytes(raw);
                return RecordCrypto.ConstantTimeEquals(r.DbPathHash, dbPathHash) ? r : null;
            }
            catch (CryptographicException) { return null; }
            catch (InvalidDataException) { return null; }
            catch (EndOfStreamException) { return null; }
        }

        internal static void Delete(byte[] dbPathHash)
        {
            string f = FileFor(dbPathHash);
            if (File.Exists(f)) File.Delete(f);
        }

        // ---- passkey for WebAuthn PRF records (one per Windows user, reused for all databases) ----

        private static string PasskeyFile { get { return Path.Combine(Directory, "passkey.id"); } }

        /// <summary>
        /// Credential ID of the passkey created for WebAuthn quick unlock, or null. Not secret
        /// (the secret stays in Windows Hello of the client PC); kept DPAPI-wrapped anyway.
        /// </summary>
        internal static byte[] TryLoadPasskeyId()
        {
            try
            {
                if (!File.Exists(PasskeyFile)) return null;
                byte[] id = ProtectedData.Unprotect(File.ReadAllBytes(PasskeyFile), DpapiEntropy,
                    DataProtectionScope.CurrentUser);
                return id.Length > 0 && id.Length <= 1024 ? id : null;
            }
            catch (CryptographicException) { return null; }
            catch (IOException) { return null; }
        }

        internal static void SavePasskeyId(byte[] credentialId)
        {
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllBytes(PasskeyFile, ProtectedData.Protect(credentialId, DpapiEntropy,
                DataProtectionScope.CurrentUser));
        }

        /// <returns>Number of deleted records.</returns>
        internal static int DeleteAll()
        {
            if (!System.IO.Directory.Exists(Directory)) return 0;
            int n = 0;
            foreach (string f in System.IO.Directory.GetFiles(Directory, "*.mwu"))
            {
                File.Delete(f);
                ++n;
            }
            return n;
        }
    }
}
