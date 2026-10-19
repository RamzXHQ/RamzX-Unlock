// Miftawerk Unlock - tests
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.IO;
using System.Linq;

using KeePassLib;
using KeePassLib.Cryptography.KeyDerivation;
using KeePassLib.Keys;
using KeePassLib.Serialization;

using Xunit;

namespace MiftawerkUnlock.Tests
{
    public class KeySnapshotTests
    {
        private static PwDatabase DbWithKey(CompositeKey ck)
        {
            PwDatabase db = new PwDatabase();
            db.MasterKey = ck;
            db.RootGroup = new PwGroup(true, true);
            return db;
        }

        private static byte[] DeriveKey(CompositeKey ck, KdfParameters p)
        {
            return ck.GenerateKey32(p).ReadData();
        }

        private static KdfParameters FastAesKdf()
        {
            AesKdf kdf = new AesKdf();
            KdfParameters p = kdf.GetDefaultParameters();
            kdf.Randomize(p);
            p.SetUInt64(AesKdf.ParamRounds, 16);
            return p;
        }

        [Fact]
        public void RebuiltKey_DerivesSameKeyAsOriginal_PasswordAndKeyFile()
        {
            using (TempDir tmp = new TempDir())
            {
                File.WriteAllBytes(tmp.File("test.key"), RecordCrypto.RandomBytes(64));
                CompositeKey original = new CompositeKey();
                original.AddUserKey(new KcpPassword("correct horse battery staple"));
                original.AddUserKey(new KcpKeyFile(tmp.File("test.key")));

                KeySnapshot snap = KeySnapshot.TryCapture(DbWithKey(original));
                Assert.NotNull(snap);

                CompositeKey rebuilt = KeySnapshot.Deserialize(snap.Serialize()).ToCompositeKey();

                KdfParameters p = FastAesKdf();
                Assert.Equal(DeriveKey(original, p), DeriveKey(rebuilt, p));
                Assert.All(rebuilt.UserKeys, k => Assert.IsType<KcpCustomKey>(k));
                Assert.Equal(2, rebuilt.UserKeys.Count());
            }
        }

        [Fact]
        public void RebuiltKey_DiffersFromOtherPassword()
        {
            CompositeKey original = new CompositeKey();
            original.AddUserKey(new KcpPassword("password A"));
            CompositeKey other = new CompositeKey();
            other.AddUserKey(new KcpPassword("password B"));

            CompositeKey rebuilt = KeySnapshot.TryCapture(DbWithKey(original)).ToCompositeKey();

            KdfParameters p = FastAesKdf();
            Assert.NotEqual(DeriveKey(other, p), DeriveKey(rebuilt, p));
        }

        [Fact]
        public void Serialize_DoesNotContainPlainPassword_AndKeepsRootUuid()
        {
            CompositeKey ck = new CompositeKey();
            ck.AddUserKey(new KcpPassword("PlainTextMustNotAppear"));
            PwDatabase db = DbWithKey(ck);

            KeySnapshot snap = KeySnapshot.TryCapture(db);
            byte[] data = snap.Serialize();

            Assert.DoesNotContain("PlainTextMustNotAppear", System.Text.Encoding.UTF8.GetString(data));
            Assert.Equal(db.RootGroup.Uuid.UuidBytes, KeySnapshot.Deserialize(data).RootGroupUuid);
        }

        [Fact]
        public void UnsupportedComponents_AreNotCaptured()
        {
            // Key providers (e.g. YubiKey plugins) and an existing quick-unlock key
            CompositeKey ck = new CompositeKey();
            ck.AddUserKey(new KcpPassword("x"));
            ck.AddUserKey(new KcpCustomKey("Some provider", new byte[32], false));
            Assert.Null(KeySnapshot.TryCapture(DbWithKey(ck)));

            Assert.Null(KeySnapshot.TryCapture(DbWithKey(new CompositeKey())));
        }

        [Fact]
        public void Deserialize_RejectsOversizedPart()
        {
            byte[] data = new byte[16 + 1 + 1 + 4];
            data[16] = 1;                              // one part
            data[17] = 1;                              // tag
            BitConverter.GetBytes(5000).CopyTo(data, 18); // length > 1024
            Assert.Throws<InvalidDataException>(() => KeySnapshot.Deserialize(data));
        }

        [Fact]
        public void Clear_WipesKeyData()
        {
            CompositeKey ck = new CompositeKey();
            ck.AddUserKey(new KcpPassword("wipe me"));
            KeySnapshot snap = KeySnapshot.TryCapture(DbWithKey(ck));

            snap.Clear();

            byte[] after = snap.Serialize();
            Assert.All(after.Skip(16 + 1 + 1 + 4), b => Assert.Equal(0, b));
        }
    }
}
