// Miftawerk Unlock - tests
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.IO;

using KeePassLib;
using KeePassLib.Cryptography.KeyDerivation;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;

using Xunit;

namespace MiftawerkUnlock.Tests
{
    /// <summary>
    /// The full quick-unlock path with a real KDBX file and KeePassLib, without Windows Hello:
    /// a random byte array stands in for the Hello signature. Proves that
    /// (1) the rebuilt key opens the database and
    /// (2) saving after a quick unlock keeps the file openable with the original master key.
    /// </summary>
    public class EndToEndTests : IDisposable
    {
        private readonly TempDir m_tmp = new TempDir();

        public EndToEndTests()
        {
            RecordStore.DirectoryOverride = m_tmp.File("records");
        }

        public void Dispose()
        {
            RecordStore.DirectoryOverride = null;
            m_tmp.Dispose();
        }

        private static CompositeKey MasterKey(string keyFile)
        {
            CompositeKey ck = new CompositeKey();
            ck.AddUserKey(new KcpPassword("test password, not a real one"));
            ck.AddUserKey(new KcpKeyFile(keyFile));
            return ck;
        }

        private static PwDatabase CreateDatabase(string path, CompositeKey key)
        {
            PwDatabase db = new PwDatabase();
            db.New(IOConnectionInfo.FromPath(path), key);

            AesKdf kdf = new AesKdf();
            KdfParameters p = kdf.GetDefaultParameters();
            kdf.Randomize(p);
            p.SetUInt64(AesKdf.ParamRounds, 1000); // fast for tests
            db.KdfParameters = p;

            PwEntry e = new PwEntry(true, true);
            e.Strings.Set(PwDefs.TitleField, new ProtectedString(false, "Entry 1"));
            db.RootGroup.AddEntry(e, true);
            db.Save(null);
            return db;
        }

        private static PwDatabase Open(string path, CompositeKey key)
        {
            PwDatabase db = new PwDatabase();
            db.Open(IOConnectionInfo.FromPath(path), key, null);
            return db;
        }

        [Fact]
        public void QuickUnlockKey_OpensDatabase_AndSaveKeepsMasterKeyValid()
        {
            string dbPath = m_tmp.File("e2e.kdbx"), keyFile = m_tmp.File("e2e.key");
            File.WriteAllBytes(keyFile, RecordCrypto.RandomBytes(64));

            // 1. Create and close a database (manual unlock situation)
            PwDatabase db = CreateDatabase(dbPath, MasterKey(keyFile));
            byte[] rootUuid = db.RootGroup.Uuid.UuidBytes;

            // 2. Enrollment: snapshot + record sealed with a stand-in "signature"
            byte[] signature = RecordCrypto.RandomBytes(256);
            byte[] pathHash = RecordStore.DbPathHash(db.IOConnectionInfo);
            KeySnapshot snap = KeySnapshot.TryCapture(db);
            RecordStore.Save(UnlockRecord.Seal(pathHash, TimeSpan.FromDays(7),
                RecordCrypto.RandomBytes(32), signature, snap.Serialize()));
            db.Close();
            byte[] fileBefore = File.ReadAllBytes(dbPath);

            // 3. Quick unlock: load record, open with the signature, rebuild key, open database
            UnlockRecord rec = RecordStore.TryLoad(pathHash);
            Assert.NotNull(rec);
            Assert.True(rec.IsCurrent(DateTime.UtcNow));
            KeySnapshot restored = KeySnapshot.Deserialize(rec.Open(signature));
            Assert.Equal(rootUuid, restored.RootGroupUuid);

            PwDatabase quick = Open(dbPath, restored.ToCompositeKey());
            Assert.True(quick.IsOpen);
            Assert.Equal(rootUuid, quick.RootGroup.Uuid.UuidBytes);
            Assert.Equal(fileBefore, File.ReadAllBytes(dbPath)); // opening never writes

            // 4. Edit and save while unlocked via quick unlock
            PwEntry e = new PwEntry(true, true);
            e.Strings.Set(PwDefs.TitleField, new ProtectedString(false, "Added after quick unlock"));
            quick.RootGroup.AddEntry(e, true);
            quick.Save(null);
            quick.Close();

            // 5. The original master key (password + key file) still opens the saved file
            PwDatabase manual = Open(dbPath, MasterKey(keyFile));
            Assert.Equal(2u, manual.RootGroup.GetEntriesCount(true));
            manual.Close();
        }

        [Fact]
        public void WrongSignature_CannotOpenRecord()
        {
            byte[] pathHash = RecordCrypto.RandomBytes(32);
            RecordStore.Save(UnlockRecord.Seal(pathHash, TimeSpan.FromDays(7), RecordCrypto.RandomBytes(32),
                RecordCrypto.RandomBytes(256), new byte[] { 1, 2, 3 }));

            Assert.Throws<System.Security.Cryptography.CryptographicException>(() =>
                RecordStore.TryLoad(pathHash).Open(RecordCrypto.RandomBytes(256)));
        }

        [Fact]
        public void KeyFromOtherDatabase_DoesNotOpen()
        {
            string pathA = m_tmp.File("a.kdbx"), pathB = m_tmp.File("b.kdbx"), keyFile = m_tmp.File("k.key");
            File.WriteAllBytes(keyFile, RecordCrypto.RandomBytes(64));

            PwDatabase a = CreateDatabase(pathA, MasterKey(keyFile));
            CompositeKey rebuiltA = KeySnapshot.TryCapture(a).ToCompositeKey();
            a.Close();

            CompositeKey keyB = new CompositeKey();
            keyB.AddUserKey(new KcpPassword("different"));
            CreateDatabase(pathB, keyB).Close();

            Assert.ThrowsAny<Exception>(() => Open(pathB, rebuiltA));
        }
    }
}
