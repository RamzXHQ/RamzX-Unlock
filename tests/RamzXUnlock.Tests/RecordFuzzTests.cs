// RamzX Unlock - tests
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.IO;
using System.Security.Cryptography;

using Xunit;

namespace RamzXUnlock.Tests
{
    /// <summary>
    /// Thousands of random and mutated record files: a record on disk can be changed by anyone with access to the
    /// user's profile. None may open, and parsing may only fail with the expected exception types (never crash
    /// KeePass with something unexpected such as OutOfMemory or IndexOutOfRange).
    /// </summary>
    public class RecordFuzzTests
    {
        private static bool Expected(Exception e)
        {
            return e is InvalidDataException || e is CryptographicException || e is EndOfStreamException ||
                   e is ArgumentException || e is FormatException;
        }

        private static void MustNotOpen(byte[] data, byte[] secret)
        {
            try
            {
                UnlockRecord r = UnlockRecord.FromBytes(data);
                byte[] plain = r.Open(secret);
                Assert.True(plain == null, "a random or mutated record opened");
            }
            catch (Exception e)
            {
                Assert.True(Expected(e), "unexpected " + e.GetType().Name + ": " + e.Message);
            }
        }

        [Fact]
        public void RandomBytes_NeverOpen_AndFailCleanly()
        {
            Random rnd = new Random(20261027);
            byte[] secret = RecordCrypto.RandomBytes(32);
            for (int i = 0; i < 3000; ++i)
            {
                byte[] data = new byte[rnd.Next(0, 700)];
                rnd.NextBytes(data);
                if (i % 3 == 0 && data.Length > 8) { data[0] = (byte)'M'; data[1] = (byte)'W'; data[2] = (byte)'U'; data[3] = (byte)'1'; data[4] = (byte)(1 + i % 2); }
                MustNotOpen(data, secret);
            }
        }

        [Fact]
        public void MutatedRealRecords_NeverOpen_WithTheRightSecret()
        {
            Random rnd = new Random(7);
            byte[] sig = RecordCrypto.RandomBytes(256);
            byte[] good = UnlockRecord.Seal(RecordCrypto.RandomBytes(32), TimeSpan.FromDays(7), RecordCrypto.RandomBytes(32),
                sig, new byte[] { 1, 2, 3, 4 }).ToBytes();
            for (int i = 0; i < 2000; ++i)
            {
                byte[] m = (byte[])good.Clone();
                int flips = 1 + rnd.Next(4);
                for (int f = 0; f < flips; ++f) m[rnd.Next(m.Length)] ^= (byte)(1 + rnd.Next(255));
                if (i % 10 == 0) Array.Resize(ref m, rnd.Next(m.Length));
                MustNotOpen(m, sig);
            }
        }
    }
}
