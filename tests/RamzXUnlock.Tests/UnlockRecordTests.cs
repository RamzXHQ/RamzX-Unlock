// RamzX Unlock - tests
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using Xunit;

namespace RamzXUnlock.Tests
{
    public partial class UnlockRecordTests
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

            byte[] badVersion = (byte[])good.Clone(); badVersion[4] = 3;
            Assert.Throws<InvalidDataException>(() => UnlockRecord.FromBytes(badVersion));
        }

        [Fact]
        public void HelloKeyRecords_KeepTheVersion1Layout()
        {
            byte[] sig, hash;
            UnlockRecord r = NewRecord(out sig, out hash);
            byte[] bytes = r.ToBytes();
            Assert.Equal(1, bytes[4]);
            Assert.Equal(RecordKind.HelloKey, r.Kind);
            Assert.Null(r.CredentialId);
            // 4 magic + 1 version + 3x8 ticks + 3x32 + 16 iv + 4 length + cipher + 32 mac
            Assert.Equal(4 + 1 + 24 + 96 + 16 + 4 + 32, bytes.Length - CipherLength(bytes, 4 + 1 + 24 + 96 + 16));
        }

        private static int CipherLength(byte[] b, int offset) { return BitConverter.ToInt32(b, offset); }
    }

    public class WebAuthnRecordTests
    {
        private static readonly byte[] Payload = Encoding.UTF8.GetBytes("database key material (test)");

        private static UnlockRecord NewPrfRecord(out byte[] prf, out byte[] credId)
        {
            prf = RecordCrypto.RandomBytes(32); // stands in for the WebAuthn PRF value
            credId = RecordCrypto.RandomBytes(32);
            return UnlockRecord.SealPrf(credId, RecordCrypto.RandomBytes(32), TimeSpan.FromDays(7),
                RecordCrypto.RandomBytes(32), prf, Payload);
        }

        [Fact]
        public void SealThenOpen_AndRoundTrip()
        {
            byte[] prf, credId;
            UnlockRecord r = NewPrfRecord(out prf, out credId);
            Assert.Equal(RecordKind.WebAuthnPrf, r.Kind);
            Assert.Equal(Payload, r.Open(prf));

            byte[] bytes = r.ToBytes();
            Assert.Equal(2, bytes[4]);
            UnlockRecord back = UnlockRecord.FromBytes(bytes);
            Assert.Equal(RecordKind.WebAuthnPrf, back.Kind);
            Assert.Equal(credId, back.CredentialId);
            Assert.Equal(r.Challenge, back.Challenge);
            Assert.Equal(Payload, back.Open(prf));
            Assert.Equal(bytes, back.ToBytes());
        }

        [Fact]
        public void WrongSecret_IsRejected()
        {
            byte[] prf, credId;
            UnlockRecord r = NewPrfRecord(out prf, out credId);
            byte[] wrong = (byte[])prf.Clone();
            wrong[31] ^= 0x80;
            Assert.Throws<CryptographicException>(() => r.Open(wrong));
        }

        [Fact]
        public void SameSecret_DifferentKind_DoesNotOpen()
        {
            // Key separation: a PRF value can never open a Windows-Hello-key record and vice versa.
            byte[] ikm = RecordCrypto.RandomBytes(32), hash = RecordCrypto.RandomBytes(32), ch = RecordCrypto.RandomBytes(32);
            UnlockRecord hello = UnlockRecord.Seal(hash, TimeSpan.FromDays(1), ch, ikm, Payload);
            UnlockRecord prf = UnlockRecord.SealPrf(new byte[] { 1, 2, 3 }, hash, TimeSpan.FromDays(1), ch, ikm, Payload);
            Assert.Equal(Payload, hello.Open(ikm));
            Assert.Equal(Payload, prf.Open(ikm));

            // Re-label a PRF record as a version-1 record: the authenticated header changes.
            byte[] bytes = prf.ToBytes();
            byte[] asV1 = new byte[bytes.Length - 3 - 3];
            Buffer.BlockCopy(bytes, 0, asV1, 0, 4);
            asV1[4] = 1;
            Buffer.BlockCopy(bytes, 4 + 1 + 1 + 2 + 3, asV1, 5, bytes.Length - (4 + 1 + 1 + 2 + 3));
            Assert.Throws<CryptographicException>(() => UnlockRecord.FromBytes(asV1).Open(ikm));
        }

        [Fact]
        public void EveryFlippedByte_IsDetected()
        {
            byte[] prf, credId;
            byte[] good = NewPrfRecord(out prf, out credId).ToBytes();
            for (int i = 0; i < good.Length; ++i)
            {
                byte[] bad = (byte[])good.Clone();
                bad[i] ^= 0x01;
                bool rejected;
                try { UnlockRecord.FromBytes(bad).Open(prf); rejected = false; }
                catch (CryptographicException) { rejected = true; }
                catch (InvalidDataException) { rejected = true; }
                catch (EndOfStreamException) { rejected = true; }
                Assert.True(rejected, "Tampered byte at offset " + i + " was not detected.");
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1025)]
        public void CredentialIdLength_IsChecked(int length)
        {
            Assert.Throws<ArgumentException>(() => UnlockRecord.SealPrf(new byte[length], new byte[32],
                TimeSpan.FromDays(1), new byte[32], new byte[32], Payload));

            byte[] prf, credId;
            byte[] bytes = NewPrfRecord(out prf, out credId).ToBytes();
            byte[] len = BitConverter.GetBytes((ushort)length);
            bytes[6] = len[0]; bytes[7] = len[1];
            Assert.ThrowsAny<Exception>(() => UnlockRecord.FromBytes(bytes).Open(prf));
        }

        [Fact]
        public void UnknownKind_IsRejected()
        {
            byte[] prf, credId;
            byte[] bytes = NewPrfRecord(out prf, out credId).ToBytes();
            bytes[5] = 9;
            Assert.Throws<InvalidDataException>(() => UnlockRecord.FromBytes(bytes));
        }
    }

    /// <summary>The P/Invoke structures must match webauthn.h exactly (x64 offsets from the header).</summary>
    public class WebAuthnLayoutTests
    {
        private static int Off<T>(string field) { return System.Runtime.InteropServices.Marshal.OffsetOf(typeof(T), field).ToInt32(); }
        private static int Size<T>() { return System.Runtime.InteropServices.Marshal.SizeOf(typeof(T)); }

        [Fact]
        public void Structures_MatchTheNativeLayout()
        {
            if (IntPtr.Size != 8) return; // KeePass runs as 64-bit process on 64-bit Windows; offsets below are x64

            Assert.Equal(96, Off<WebAuthnPrf.MakeOptions>("bEnablePrf"));
            Assert.Equal(104, Size<WebAuthnPrf.MakeOptions>());
            Assert.Equal(88, Off<WebAuthnPrf.MakeOptions>("bPreferResidentKey"));
            Assert.Equal(104, Off<WebAuthnPrf.GetOptions>("pHmacSecretSaltValues"));
            Assert.Equal(120, Size<WebAuthnPrf.GetOptions>());
            Assert.Equal(88, Off<WebAuthnPrf.AttestationPrefix>("pbCredentialId"));
            Assert.Equal(112, Off<WebAuthnPrf.AssertionPrefix>("pHmacSecret"));
            Assert.Equal(32, Size<WebAuthnPrf.HmacSalt>());
            Assert.Equal(24, Size<WebAuthnPrf.CoseParam>());
            Assert.Equal(24, Size<WebAuthnPrf.Credential>());
        }

        [Theory]
        [InlineData(0, "Success")]
        [InlineData(unchecked((int)0x80090036), "Cancelled")]
        [InlineData(unchecked((int)0x800704C7), "Cancelled")]
        [InlineData(unchecked((int)0x80090011), "NotFound")]
        [InlineData(unchecked((int)0x80004005), "Error")]
        public void ErrorCodes_AreClassified(int hr, string expected)
        {
            Assert.Equal(expected, WebAuthnPrf.Classify(hr).ToString());
        }
    }

    public partial class UnlockRecordTests
    {
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
