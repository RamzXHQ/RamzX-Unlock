// Miftawerk Unlock - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Security.Cryptography;

namespace MiftawerkUnlock
{
    /// <summary>
    /// Building blocks for the record envelope: HKDF-SHA256 (RFC 5869),
    /// AES-256-CBC and HMAC-SHA256 (encrypt-then-MAC). .NET Framework 4.8 has
    /// neither HKDF nor AES-GCM, so both are composed from framework primitives.
    /// </summary>
    internal static class RecordCrypto
    {
        internal const int KeySize = 32;
        internal const int IvSize = 16;
        internal const int MacSize = 32;

        internal static byte[] RandomBytes(int n)
        {
            byte[] b = new byte[n];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                rng.GetBytes(b);
            return b;
        }

        /// <summary>HKDF-SHA256 extract + expand (RFC 5869).</summary>
        internal static byte[] Hkdf(byte[] ikm, byte[] salt, byte[] info, int length)
        {
            if (length <= 0 || length > 255 * 32) throw new ArgumentOutOfRangeException("length");

            byte[] prk;
            using (HMACSHA256 h = new HMACSHA256(salt)) prk = h.ComputeHash(ikm);

            byte[] okm = new byte[length];
            try
            {
                using (HMACSHA256 h = new HMACSHA256(prk))
                {
                    byte[] t = new byte[0];
                    int pos = 0;
                    for (byte i = 1; pos < length; ++i)
                    {
                        byte[] input = new byte[t.Length + info.Length + 1];
                        Buffer.BlockCopy(t, 0, input, 0, t.Length);
                        Buffer.BlockCopy(info, 0, input, t.Length, info.Length);
                        input[input.Length - 1] = i;

                        Array.Clear(t, 0, t.Length);
                        t = h.ComputeHash(input);
                        Array.Clear(input, 0, input.Length);

                        int n = Math.Min(t.Length, length - pos);
                        Buffer.BlockCopy(t, 0, okm, pos, n);
                        pos += n;
                    }
                    Array.Clear(t, 0, t.Length);
                }
            }
            finally { Array.Clear(prk, 0, prk.Length); }
            return okm;
        }

        internal static byte[] AesCbcEncrypt(byte[] key, byte[] iv, byte[] plain)
        {
            using (Aes aes = CreateAes(key, iv))
            using (ICryptoTransform t = aes.CreateEncryptor())
                return t.TransformFinalBlock(plain, 0, plain.Length);
        }

        internal static byte[] AesCbcDecrypt(byte[] key, byte[] iv, byte[] cipher)
        {
            using (Aes aes = CreateAes(key, iv))
            using (ICryptoTransform t = aes.CreateDecryptor())
                return t.TransformFinalBlock(cipher, 0, cipher.Length);
        }

        private static Aes CreateAes(byte[] key, byte[] iv)
        {
            Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;
            return aes;
        }

        internal static byte[] HmacSha256(byte[] key, byte[] data, int offset, int count)
        {
            using (HMACSHA256 h = new HMACSHA256(key)) return h.ComputeHash(data, offset, count);
        }

        internal static byte[] Sha256(byte[] data)
        {
            using (SHA256 sha = SHA256.Create()) return sha.ComputeHash(data);
        }

        internal static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; ++i) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
