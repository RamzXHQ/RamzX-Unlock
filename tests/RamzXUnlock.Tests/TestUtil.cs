// RamzX Unlock - tests
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.IO;

using Xunit;

// RecordStore.DirectoryOverride is static: run tests one after another.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace RamzXUnlock.Tests
{
    internal static class Hex
    {
        internal static byte[] Parse(string hex)
        {
            byte[] b = new byte[hex.Length / 2];
            for (int i = 0; i < b.Length; ++i) b[i] = Convert.ToByte(hex.Substring(2 * i, 2), 16);
            return b;
        }

        internal static byte[] Range(int from, int toInclusive)
        {
            byte[] b = new byte[toInclusive - from + 1];
            for (int i = 0; i < b.Length; ++i) b[i] = (byte)(from + i);
            return b;
        }

        internal static byte[] Fill(byte v, int n)
        {
            byte[] b = new byte[n];
            for (int i = 0; i < n; ++i) b[i] = v;
            return b;
        }
    }

    /// <summary>Temporary folder that is deleted after the test.</summary>
    internal sealed class TempDir : IDisposable
    {
        internal readonly string Path;

        internal TempDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "RamzXUnlockTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        internal string File(string name) { return System.IO.Path.Combine(Path, name); }

        public void Dispose()
        {
            try { Directory.Delete(Path, true); }
            catch (IOException) { }
        }
    }
}
