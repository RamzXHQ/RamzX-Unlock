// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Collections.Generic;
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

        private static string Hex(byte[] b, int count)
        {
            StringBuilder sb = new StringBuilder(2 * count);
            for (int i = 0; i < count && i < b.Length; ++i) sb.Append(b[i].ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// {sha256(path)}.mwu for the Windows Hello key record (one per database, as before);
        /// {sha256(path)}-{sha256(passkey id)[0..8]}.mwu for passkey records (one per database and
        /// device passkey, so a database can be quick-unlocked from several PCs).
        /// </summary>
        private static string FileFor(UnlockRecord r)
        {
            string name = Hex(r.DbPathHash, 32);
            if (r.Kind == RecordKind.WebAuthnPrf) name += "-" + Hex(RecordCrypto.Sha256(r.CredentialId), 8);
            return Path.Combine(Directory, name + ".mwu");
        }

        private static string[] FilesFor(byte[] dbPathHash)
        {
            if (!System.IO.Directory.Exists(Directory)) return new string[0];
            return System.IO.Directory.GetFiles(Directory, Hex(dbPathHash, 32) + "*.mwu");
        }

        internal static void Save(UnlockRecord r)
        {
            System.IO.Directory.CreateDirectory(Directory);
            byte[] wrapped = ProtectedData.Protect(r.ToBytes(), DpapiEntropy,
                DataProtectionScope.CurrentUser);

            string target = FileFor(r);
            string tmp = target + ".tmp";
            File.WriteAllBytes(tmp, wrapped);
            if (File.Exists(target)) File.Replace(tmp, target, null);
            else File.Move(tmp, target);
        }

        /// <summary>All readable records of this database (any kind, any device).</summary>
        internal static List<UnlockRecord> LoadAll(byte[] dbPathHash)
        {
            List<UnlockRecord> list = new List<UnlockRecord>();
            foreach (string f in FilesFor(dbPathHash))
            {
                UnlockRecord r = TryRead(f, dbPathHash);
                if (r != null) list.Add(r);
            }
            return list;
        }

        /// <summary>The Windows Hello key record of this database, or null (kept for callers and tests).</summary>
        internal static UnlockRecord TryLoad(byte[] dbPathHash)
        {
            List<UnlockRecord> all = LoadAll(dbPathHash);
            foreach (UnlockRecord r in all) if (r.Kind == RecordKind.HelloKey) return r;
            return all.Count > 0 ? all[0] : null;
        }

        private static UnlockRecord TryRead(string f, byte[] dbPathHash)
        {
            try
            {
                byte[] raw = ProtectedData.Unprotect(File.ReadAllBytes(f), DpapiEntropy,
                    DataProtectionScope.CurrentUser);
                UnlockRecord r = UnlockRecord.FromBytes(raw);
                if (!RecordCrypto.ConstantTimeEquals(r.DbPathHash, dbPathHash)) return null;
                r.SourceFile = f; // 0.4.0 stored its passkey record under the plain name
                return r;
            }
            catch (CryptographicException) { return null; }
            catch (InvalidDataException) { return null; }
            catch (EndOfStreamException) { return null; }
            catch (IOException) { return null; }
        }

        /// <summary>Deletes every record of this database (all devices).</summary>
        internal static void Delete(byte[] dbPathHash)
        {
            foreach (string f in FilesFor(dbPathHash)) File.Delete(f);
        }

        /// <summary>Deletes one record (e.g. an expired one), keeps the others.</summary>
        internal static void Delete(UnlockRecord r)
        {
            string f = r.SourceFile ?? FileFor(r);
            if (File.Exists(f)) File.Delete(f);
        }

        // ---- passkey for WebAuthn PRF records (one per Windows user, reused for all databases) ----

        private static string PasskeyFile { get { return Path.Combine(Directory, "passkey.id"); } }

        private static readonly byte[] PasskeyListMagic = Encoding.ASCII.GetBytes("PKL1");
        private const int MaxPasskeys = 32;

        /// <summary>
        /// Credential IDs of the passkeys created for quick unlock (one per device the user works
        /// from), newest last. Not secret (the secrets stay in Windows Hello of each device);
        /// kept DPAPI-wrapped anyway. Reads the single-ID file of 0.4.0 as a list of one.
        /// </summary>
        internal static List<byte[]> LoadPasskeyIds()
        {
            List<byte[]> ids = new List<byte[]>();
            try
            {
                if (!File.Exists(PasskeyFile)) return ids;
                byte[] raw = ProtectedData.Unprotect(File.ReadAllBytes(PasskeyFile), DpapiEntropy,
                    DataProtectionScope.CurrentUser);
                if (raw.Length >= 4 && RecordCrypto.ConstantTimeEquals(Sub(raw, 0, 4), PasskeyListMagic))
                {
                    using (BinaryReader rd = new BinaryReader(new MemoryStream(raw, 4, raw.Length - 4)))
                    {
                        int n = rd.ReadByte();
                        for (int i = 0; i < n && i < MaxPasskeys; ++i)
                        {
                            int len = rd.ReadUInt16();
                            if (len == 0 || len > 1024) break;
                            byte[] id = rd.ReadBytes(len);
                            if (id.Length != len) break;
                            ids.Add(id);
                        }
                    }
                }
                else if (raw.Length > 0 && raw.Length <= 1024) ids.Add(raw); // 0.4.0: one raw ID
            }
            catch (CryptographicException) { }
            catch (IOException) { }
            return ids;
        }

        /// <summary>Adds a passkey to the list (kept unique; the oldest are dropped beyond 32).</summary>
        internal static void AddPasskeyId(byte[] credentialId)
        {
            List<byte[]> ids = LoadPasskeyIds();
            ids.RemoveAll(x => RecordCrypto.ConstantTimeEquals(x, credentialId));
            ids.Add(credentialId);
            while (ids.Count > MaxPasskeys) ids.RemoveAt(0);

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms))
            {
                w.Write(PasskeyListMagic);
                w.Write((byte)ids.Count);
                foreach (byte[] id in ids) { w.Write((ushort)id.Length); w.Write(id); }
                w.Flush();
                System.IO.Directory.CreateDirectory(Directory);
                File.WriteAllBytes(PasskeyFile, ProtectedData.Protect(ms.ToArray(), DpapiEntropy,
                    DataProtectionScope.CurrentUser));
            }
        }

        private static byte[] Sub(byte[] b, int start, int count)
        {
            byte[] r = new byte[count];
            Buffer.BlockCopy(b, start, r, 0, count);
            return r;
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
