// RamzX Unlock - tests
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Security.Cryptography;
using System.Text;

using Xunit;

namespace MiftawerkUnlock.Tests
{
    public class RecordCryptoTests
    {
        // RFC 5869, Appendix A, test cases 1-3 (SHA-256)
        [Fact]
        public void Hkdf_Rfc5869_TestCase1()
        {
            byte[] okm = RecordCrypto.Hkdf(Hex.Fill(0x0b, 22), Hex.Range(0x00, 0x0c), Hex.Range(0xf0, 0xf9), 42);
            Assert.Equal(Hex.Parse("3cb25f25faacd57a90434f64d0362f2a2d2d0a90cf1a5a4c5db02d56ecc4c5bf34007208d5b887185865"), okm);
        }

        [Fact]
        public void Hkdf_Rfc5869_TestCase2_LongInputs()
        {
            byte[] okm = RecordCrypto.Hkdf(Hex.Range(0x00, 0x4f), Hex.Range(0x60, 0xaf), Hex.Range(0xb0, 0xff), 82);
            Assert.Equal(Hex.Parse(
                "b11e398dc80327a1c8e7f78c596a49344f012eda2d4efad8a050cc4c19afa97c" +
                "59045a99cac7827271cb41c65e590e09da3275600c2f09b8367793a9aca3db71" +
                "cc30c58179ec3e87c14c01d5c1f3434f1d87"), okm);
        }

        [Fact]
        public void Hkdf_Rfc5869_TestCase3_EmptySaltAndInfo()
        {
            byte[] okm = RecordCrypto.Hkdf(Hex.Fill(0x0b, 22), new byte[0], new byte[0], 42);
            Assert.Equal(Hex.Parse("8da4e775a563c18f715f802a063c5a31b8a11f5c5ee1879ec3454e5f3c738d2d9d201395faa4b61a96c8"), okm);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(255 * 32 + 1)]
        public void Hkdf_RejectsInvalidLength(int length)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RecordCrypto.Hkdf(new byte[32], new byte[32], new byte[0], length));
        }

        [Fact]
        public void Hkdf_MaximumLengthWorks()
        {
            Assert.Equal(255 * 32, RecordCrypto.Hkdf(new byte[32], new byte[32], new byte[0], 255 * 32).Length);
        }

        [Fact]
        public void AesCbc_RoundTrip_AndCiphertextDiffersFromPlaintext()
        {
            byte[] key = RecordCrypto.RandomBytes(32), iv = RecordCrypto.RandomBytes(16);
            byte[] plain = Encoding.UTF8.GetBytes("RamzX Unlock test payload");

            byte[] cipher = RecordCrypto.AesCbcEncrypt(key, iv, plain);

            Assert.NotEqual(plain, cipher);
            Assert.Equal(0, cipher.Length % 16);
            Assert.Equal(plain, RecordCrypto.AesCbcDecrypt(key, iv, cipher));
        }

        [Fact]
        public void AesCbc_WrongKeyFails()
        {
            byte[] iv = RecordCrypto.RandomBytes(16);
            byte[] cipher = RecordCrypto.AesCbcEncrypt(RecordCrypto.RandomBytes(32), iv, new byte[40]);
            byte[] result = null;
            try { result = RecordCrypto.AesCbcDecrypt(RecordCrypto.RandomBytes(32), iv, cipher); }
            catch (CryptographicException) { return; } // padding error: expected in most cases
            Assert.NotEqual(new byte[40], result);      // otherwise the output must be garbage
        }

        [Fact]
        public void HmacSha256_MatchesFramework_AndRespectsOffset()
        {
            byte[] key = RecordCrypto.RandomBytes(32);
            byte[] data = RecordCrypto.RandomBytes(100);
            byte[] expected;
            using (HMACSHA256 h = new HMACSHA256(key)) expected = h.ComputeHash(data, 10, 50);

            Assert.Equal(expected, RecordCrypto.HmacSha256(key, data, 10, 50));
        }

        [Fact]
        public void Sha256_KnownValue()
        {
            Assert.Equal(Hex.Parse("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"),
                RecordCrypto.Sha256(Encoding.ASCII.GetBytes("abc")));
        }

        [Fact]
        public void RandomBytes_HasRequestedLength_AndVaries()
        {
            Assert.Equal(32, RecordCrypto.RandomBytes(32).Length);
            Assert.NotEqual(RecordCrypto.RandomBytes(32), RecordCrypto.RandomBytes(32));
        }

        [Fact]
        public void ConstantTimeEquals_Cases()
        {
            Assert.True(RecordCrypto.ConstantTimeEquals(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 3 }));
            Assert.True(RecordCrypto.ConstantTimeEquals(new byte[0], new byte[0]));
            Assert.False(RecordCrypto.ConstantTimeEquals(new byte[] { 1, 2, 3 }, new byte[] { 1, 2, 4 }));
            Assert.False(RecordCrypto.ConstantTimeEquals(new byte[] { 1, 2, 3 }, new byte[] { 1, 2 }));
            Assert.False(RecordCrypto.ConstantTimeEquals(null, new byte[0]));
            Assert.False(RecordCrypto.ConstantTimeEquals(new byte[0], null));
        }
    }
}
