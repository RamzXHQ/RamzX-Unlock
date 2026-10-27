// RamzX Unlock - tests
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using KeePassLib.Serialization;

using Xunit;

namespace RamzXUnlock.Tests
{
    public class RecordStoreTests : IDisposable
    {
        private readonly TempDir m_tmp = new TempDir();

        public RecordStoreTests()
        {
            RecordStore.DirectoryOverride = m_tmp.File("records");
        }

        public void Dispose()
        {
            RecordStore.DirectoryOverride = null;
            m_tmp.Dispose();
        }

        private static UnlockRecord NewRecord(byte[] pathHash, byte[] sig)
        {
            return UnlockRecord.Seal(pathHash, TimeSpan.FromDays(7), RecordCrypto.RandomBytes(32),
                sig, Encoding.UTF8.GetBytes("payload"));
        }

        [Fact]
        public void DbPathHash_IsCaseInsensitive_AndNormalizesPath()
        {
            string p = m_tmp.File("Test.kdbx");
            byte[] a = RecordStore.DbPathHash(IOConnectionInfo.FromPath(p));
            byte[] b = RecordStore.DbPathHash(IOConnectionInfo.FromPath(p.ToUpperInvariant()));
            byte[] c = RecordStore.DbPathHash(IOConnectionInfo.FromPath(
                Path.Combine(m_tmp.Path, "sub", "..", "Test.kdbx")));
            byte[] other = RecordStore.DbPathHash(IOConnectionInfo.FromPath(m_tmp.File("Other.kdbx")));

            Assert.Equal(32, a.Length);
            Assert.Equal(a, b);
            Assert.Equal(a, c);
            Assert.NotEqual(a, other);
        }

        [Fact]
        public void SaveThenLoad_RoundTrip()
        {
            byte[] hash = RecordCrypto.RandomBytes(32), sig = RecordCrypto.RandomBytes(256);
            RecordStore.Save(NewRecord(hash, sig));

            UnlockRecord loaded = RecordStore.TryLoad(hash);

            Assert.NotNull(loaded);
            Assert.Equal(Encoding.UTF8.GetBytes("payload"), loaded.Open(sig));
        }

        [Fact]
        public void Save_OverwritesExistingRecord()
        {
            byte[] hash = RecordCrypto.RandomBytes(32);
            byte[] sig1 = RecordCrypto.RandomBytes(256), sig2 = RecordCrypto.RandomBytes(256);
            RecordStore.Save(NewRecord(hash, sig1));
            RecordStore.Save(NewRecord(hash, sig2));

            UnlockRecord loaded = RecordStore.TryLoad(hash);
            Assert.Equal(Encoding.UTF8.GetBytes("payload"), loaded.Open(sig2));
            Assert.Single(Directory.GetFiles(RecordStore.Directory));
        }

        [Fact]
        public void File_IsDpapiWrapped_NotRawRecord()
        {
            byte[] hash = RecordCrypto.RandomBytes(32), sig = RecordCrypto.RandomBytes(256);
            UnlockRecord r = NewRecord(hash, sig);
            RecordStore.Save(r);

            byte[] onDisk = File.ReadAllBytes(Directory.GetFiles(RecordStore.Directory)[0]);
            Assert.NotEqual(r.ToBytes(), onDisk);
            Assert.NotEqual((byte)'M', onDisk[0]); // no visible "MWU1" magic
        }

        [Fact]
        public void TryLoad_MissingOrCorrupt_ReturnsNull()
        {
            byte[] hash = RecordCrypto.RandomBytes(32);
            Assert.Null(RecordStore.TryLoad(hash));

            RecordStore.Save(NewRecord(hash, RecordCrypto.RandomBytes(256)));
            string f = Directory.GetFiles(RecordStore.Directory)[0];
            File.WriteAllBytes(f, new byte[] { 1, 2, 3 });
            Assert.Null(RecordStore.TryLoad(hash));
        }

        [Fact]
        public void TryLoad_ValidDpapiButInvalidRecord_ReturnsNull()
        {
            byte[] hash = RecordCrypto.RandomBytes(32);
            UnlockRecord r = NewRecord(hash, RecordCrypto.RandomBytes(256));
            RecordStore.Save(r);
            string f = Directory.GetFiles(RecordStore.Directory)[0];
            byte[] entropy = Encoding.ASCII.GetBytes("SeeColors.MiftawerkUnlock.Store.v1");

            // Unknown format inside a correct DPAPI wrapper
            File.WriteAllBytes(f, ProtectedData.Protect(Encoding.ASCII.GetBytes("XXXX-not-a-record"),
                entropy, DataProtectionScope.CurrentUser));
            Assert.Null(RecordStore.TryLoad(hash));

            // Truncated record inside a correct DPAPI wrapper
            byte[] cut = new byte[20];
            Array.Copy(r.ToBytes(), cut, cut.Length);
            File.WriteAllBytes(f, ProtectedData.Protect(cut, entropy, DataProtectionScope.CurrentUser));
            Assert.Null(RecordStore.TryLoad(hash));
        }

        [Fact]
        public void TryLoad_RecordForOtherDatabase_ReturnsNull()
        {
            byte[] hashA = RecordCrypto.RandomBytes(32), hashB = RecordCrypto.RandomBytes(32);
            RecordStore.Save(NewRecord(hashA, RecordCrypto.RandomBytes(256)));

            // Copy A's file to B's name: must not be accepted for B.
            string fileA = Directory.GetFiles(RecordStore.Directory)[0];
            RecordStore.Save(NewRecord(hashB, RecordCrypto.RandomBytes(256)));
            string fileB = Array.Find(Directory.GetFiles(RecordStore.Directory), x => x != fileA);
            File.Copy(fileA, fileB, true);

            Assert.Null(RecordStore.TryLoad(hashB));
            Assert.NotNull(RecordStore.TryLoad(hashA));
        }

        [Fact]
        public void Delete_AndDeleteAll()
        {
            byte[] h1 = RecordCrypto.RandomBytes(32), h2 = RecordCrypto.RandomBytes(32), h3 = RecordCrypto.RandomBytes(32);
            foreach (byte[] h in new[] { h1, h2, h3 }) RecordStore.Save(NewRecord(h, RecordCrypto.RandomBytes(256)));

            RecordStore.Delete(h1);
            RecordStore.Delete(RecordCrypto.RandomBytes(32)); // not existing: no exception
            Assert.Null(RecordStore.TryLoad(h1));

            Assert.Equal(2, RecordStore.DeleteAll());
            Assert.Null(RecordStore.TryLoad(h2));
            Assert.Equal(0, RecordStore.DeleteAll());
        }

        [Fact]
        public void DeleteAll_WithoutDirectory_ReturnsZero()
        {
            Assert.Equal(0, RecordStore.DeleteAll());
        }

        private static UnlockRecord NewPasskeyRecord(byte[] pathHash, byte[] credId, byte[] prf)
        {
            return UnlockRecord.SealPrf(credId, pathHash, TimeSpan.FromDays(7), RecordCrypto.RandomBytes(32),
                prf, Encoding.UTF8.GetBytes("payload"));
        }

        [Fact]
        public void SeveralDevices_PerDatabase()
        {
            byte[] hash = RecordCrypto.RandomBytes(32), sig = RecordCrypto.RandomBytes(256);
            byte[] idA = RecordCrypto.RandomBytes(32), idB = RecordCrypto.RandomBytes(32), prf = RecordCrypto.RandomBytes(32);
            RecordStore.Save(NewRecord(hash, sig));
            RecordStore.Save(NewPasskeyRecord(hash, idA, prf));
            RecordStore.Save(NewPasskeyRecord(hash, idB, prf));
            RecordStore.Save(NewPasskeyRecord(hash, idB, prf)); // same device again: replaces
            RecordStore.Save(NewPasskeyRecord(RecordCrypto.RandomBytes(32), idA, prf)); // other database

            var all = RecordStore.LoadAll(hash);
            Assert.Equal(3, all.Count);
            Assert.Equal(RecordKind.HelloKey, RecordStore.TryLoad(hash).Kind);
            Assert.Equal(2, all.FindAll(r => r.Kind == RecordKind.WebAuthnPrf).Count);

            RecordStore.Delete(all.Find(r => r.Kind == RecordKind.WebAuthnPrf && RecordCrypto.ConstantTimeEquals(r.CredentialId, idA)));
            Assert.Equal(2, RecordStore.LoadAll(hash).Count);

            RecordStore.Delete(hash); // all devices of this database
            Assert.Empty(RecordStore.LoadAll(hash));
            Assert.Equal(1, RecordStore.DeleteAll()); // the other database
        }

        [Fact]
        public void PasskeyRecordOf040_UnderThePlainName_IsReadAndDeleted()
        {
            // 0.4.0 saved its passkey record as {hash}.mwu (like the Windows Hello key record).
            byte[] hash = RecordCrypto.RandomBytes(32), id = RecordCrypto.RandomBytes(32), prf = RecordCrypto.RandomBytes(32);
            UnlockRecord r = NewPasskeyRecord(hash, id, prf);
            StringBuilder name = new StringBuilder();
            foreach (byte b in hash) name.Append(b.ToString("x2"));
            Directory.CreateDirectory(RecordStore.Directory);
            string legacy = Path.Combine(RecordStore.Directory, name + ".mwu");
            File.WriteAllBytes(legacy, ProtectedData.Protect(r.ToBytes(),
                Encoding.ASCII.GetBytes("SeeColors.MiftawerkUnlock.Store.v1"), DataProtectionScope.CurrentUser));

            UnlockRecord loaded = RecordStore.LoadAll(hash).Find(x => x.Kind == RecordKind.WebAuthnPrf);
            Assert.NotNull(loaded);
            Assert.Equal(Encoding.UTF8.GetBytes("payload"), loaded.Open(prf));
            RecordStore.Delete(loaded);
            Assert.False(File.Exists(legacy));
        }

        [Fact]
        public void PasskeyList_KeepsOrder_Unique_AndReads040File()
        {
            Assert.Empty(RecordStore.LoadPasskeyIds());

            // 0.4.0 wrote one raw credential ID.
            byte[] old = RecordCrypto.RandomBytes(32);
            Directory.CreateDirectory(RecordStore.Directory);
            File.WriteAllBytes(Path.Combine(RecordStore.Directory, "passkey.id"), ProtectedData.Protect(old,
                Encoding.ASCII.GetBytes("SeeColors.MiftawerkUnlock.Store.v1"), DataProtectionScope.CurrentUser));
            Assert.Equal(old, Assert.Single(RecordStore.LoadPasskeyIds()));

            byte[] b = RecordCrypto.RandomBytes(48);
            RecordStore.AddPasskeyId(b);
            RecordStore.AddPasskeyId(old); // again: moves to the end, no duplicate
            var ids = RecordStore.LoadPasskeyIds();
            Assert.Equal(2, ids.Count);
            Assert.Equal(b, ids[0]);
            Assert.Equal(old, ids[1]);

            for (int i = 0; i < 40; ++i) RecordStore.AddPasskeyId(RecordCrypto.RandomBytes(16));
            Assert.Equal(32, RecordStore.LoadPasskeyIds().Count);
        }

        [Fact]
        public void DefaultDirectory_IsUnderLocalAppData()
        {
            RecordStore.DirectoryOverride = null;
            string expected = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "SeeColors", "MiftawerkUnlock", "records");
            Assert.Equal(expected, RecordStore.Directory);
        }
    }
}
