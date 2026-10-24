// RamzX Unlock - tests
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using Xunit;

namespace MiftawerkUnlock.Tests
{
    public class UnlockRecordTests
    {
        private static readonly byte[] Payload = Encoding.UTF8.GetBytes("database key material (test)");

        private static UnlockRecord NewRecord(out byte[] signature, out byte[] pathHash)
        {
            signature = RecordCrypto.RandomBytes(256); // stands in for a Windows Hello RSA signature
            pathHash = RecordCrypto.RandomBytes(32);
            return UnlockRecord.Seal(pathHash, TimeSpan.FromDays(7), RecordCrypto.RandomBytes(32),
                signature, Payload);
        }

        [Fact]
        public void SealThenOpen_ReturnsPayload()
        {
            byte[] sig, hash;
            UnlockRecord r = NewRecord(out sig, out hash);
            Assert.Equal(Payload, r.Open(sig));
        }

        [Fact]
        public void Serialize_RoundTrip_PreservesHeaderAndPayload()
        {
            byte[] sig, hash;
            UnlockRecord r = NewRecord(out sig, out hash);

            UnlockRecord back = UnlockRecord.FromBytes(r.ToBytes());

            Assert.Equal(r.CreatedUtcTicks, back.CreatedUtcTicks);
            Assert.Equal(r.ExpiresUtcTicks, back.ExpiresUtcTicks);
            Assert.Equal(r.BootTimeUtcTicks, back.BootTimeUtcTicks);
            Assert.Equal(hash, back.DbPathHash);
            Assert.Equal(r.Challenge, back.Challenge);
            Assert.Equal(Payload, back.Open(sig));
            Assert.Equal(r.ToBytes(), back.ToBytes());
        }

        [Fact]
        public void Payload_IsNotStoredInPlaintext()
        {
            byte[] sig, hash;
            byte[] bytes = NewRecord(out sig, out hash).ToBytes();
            string asText = Encoding.UTF8.GetString(bytes);
            Assert.DoesNotContain("database key material", asText);
        }

        [Fact]
        public void SameInputs_ProduceDifferentRecords()
        {
            byte[] sig = RecordCrypto.RandomBytes(256), hash = new byte[32], ch = new byte[32];
            byte[] a = UnlockRecord.Seal(hash, TimeSpan.FromDays(1), ch, sig, Payload).ToBytes();
            byte[] b = UnlockRecord.Seal(hash, TimeSpan.FromDays(1), ch, sig, Payload).ToBytes();
            Assert.NotEqual(a, b); // random salt and IV
        }

        [Fact]
        public void WrongSignature_IsRejected()
        {
            byte[] sig, hash;
            UnlockRecord r = NewRecord(out sig, out hash);
            byte[] wrong = (byte[])sig.Clone();
            wrong[0] ^= 1;
            Assert.Throws<CryptographicException>(() => r.Open(wrong));
        }

        [Fact]
        public void EveryFlippedByte_IsDetected()
        {
            byte[] sig, hash;
            byte[] good = NewRecord(out sig, out hash).ToBytes();

            for (int i = 0; i < good.Length; ++i)
            {
                byte[] bad = (byte[])good.Clone();
                bad[i] ^= 0x01;

                bool rejected;
                try { UnlockRecord.FromBytes(bad).Open(sig); rejected = false; }
                catch (CryptographicException) { rejected = true; }
                catch (InvalidDataException) { rejected = true; }
                catch (EndOfStreamException) { rejected = true; }

                Assert.True(rejected, "Tampered byte at offset " + i + " was not detected.");
            }
        }

        [Fact]
        public void Truncated_IsRejected()
        {
            byte[] sig, hash;
            byte[] good = NewRecord(out sig, out hash).ToBytes();
            foreach (int len in new[] { 0, 3, 5, 60, good.Length - 33, good.Length - 1 })
            {
                byte[] cut = new byte[len];
                Array.Copy(good, cut, len);
                Assert.ThrowsAny<Exception>(() => UnlockRecord.FromBytes(cut).Open(sig));
            }
        }

        [Fact]
        public void UnknownMagicOrVersion_IsRejected()
        {
            byte[] sig, hash;
            byte[] good = NewRecord(out sig, out hash).ToBytes();

            byte[] badMagic = (byte[])good.Clone(); badMagic[0] = (byte)'X';
            Assert.Throws<InvalidDataException>(() => UnlockRecord.FromBytes(badMagic));

            byte[] badVersion = (byte[])good.Clone(); badVersion[4] = 2;
            Assert.Throws<InvalidDataException>(() => UnlockRecord.FromBytes(badVersion));
        }

        [Fact]
        public void IsCurrent_RespectsValidityWindow()
        {
            byte[] sig, hash;
            UnlockRecord r = NewRecord(out sig, out hash);
            DateTime created = new DateTime(r.CreatedUtcTicks, DateTimeKind.Utc);

            Assert.True(r.IsCurrent(created.AddMinutes(1)));
            Assert.True(r.IsCurrent(created.AddDays(7).AddMinutes(-1)));
            Assert.False(r.IsCurrent(created.AddDays(7).AddMinutes(1)));   // expired
            Assert.False(r.IsCurrent(created.AddMinutes(-1)));            // clock set back
            Assert.Equal(created.AddDays(7), r.ExpiresUtc);
        }

        [Fact]
        public void BootTime_IsTakenFromCurrentBoot()
        {
            byte[] sig, hash;
            UnlockRecord r = NewRecord(out sig, out hash);
            Assert.True(BootStamp.IsSameBoot(r.BootTimeUtcTicks));
        }
    }

    public class BootStampTests
    {
        [Fact]
        public void CurrentBoot_IsInThePast_AndStable()
        {
            long a = BootStamp.CurrentBootTimeUtcTicks();
            Assert.True(a < DateTime.UtcNow.Ticks);
            Assert.True(BootStamp.IsSameBoot(a));
        }

        [Fact]
        public void DifferentBoot_IsDetected()
        {
            long now = BootStamp.CurrentBootTimeUtcTicks();
            Assert.False(BootStamp.IsSameBoot(now - TimeSpan.FromMinutes(10).Ticks));
            Assert.False(BootStamp.IsSameBoot(now + TimeSpan.FromMinutes(10).Ticks));
            Assert.True(BootStamp.IsSameBoot(now + TimeSpan.FromSeconds(30).Ticks)); // NTP tolerance
        }
    }
}
