// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MiftawerkUnlock
{
    /// <summary>
    /// Encrypted quick-unlock record for one database.
    ///
    /// Layout (little-endian), all of it authenticated by the trailing HMAC:
    ///   "MWU1" | version(1) | created ticks(8) | expires ticks(8) | boot time ticks(8)
    ///   | db path hash(32) | salt(32) | challenge(32) | iv(16) | cipher length(4) | cipher
    ///   | HMAC-SHA256(32)
    ///
    /// Keys: HKDF-SHA256(ikm = Windows Hello signature over the challenge, salt,
    /// info = label | db path hash) -> 32 bytes AES-256-CBC key | 32 bytes HMAC key.
    /// Without a fresh Windows Hello signature the record cannot be decrypted.
    /// </summary>
    internal sealed class UnlockRecord
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("MWU1");
        private const byte FormatVersion = 1;
        private static readonly byte[] KdfLabel = Encoding.ASCII.GetBytes("SeeColors.MiftawerkUnlock.Record.v1");

        internal long CreatedUtcTicks { get; private set; }
        internal long ExpiresUtcTicks { get; private set; }
        internal long BootTimeUtcTicks { get; private set; }
        internal byte[] DbPathHash { get; private set; }
        internal byte[] Challenge { get; private set; }

        private byte[] m_salt, m_iv, m_cipher, m_mac;

        private UnlockRecord() { }

        internal DateTime ExpiresUtc { get { return new DateTime(ExpiresUtcTicks, DateTimeKind.Utc); } }

        /// <summary>True if the record is still usable (not expired, same Windows boot).</summary>
        internal bool IsCurrent(DateTime utcNow)
        {
            return utcNow.Ticks < ExpiresUtcTicks && utcNow.Ticks >= CreatedUtcTicks &&
                BootStamp.IsSameBoot(BootTimeUtcTicks);
        }

        internal static UnlockRecord Seal(byte[] dbPathHash, TimeSpan validity, byte[] challenge,
            byte[] signature, byte[] plaintext)
        {
            DateTime now = DateTime.UtcNow;
            UnlockRecord r = new UnlockRecord();
            r.CreatedUtcTicks = now.Ticks;
            r.ExpiresUtcTicks = (now + validity).Ticks;
            r.BootTimeUtcTicks = BootStamp.CurrentBootTimeUtcTicks();
            r.DbPathHash = dbPathHash;
            r.Challenge = challenge;
            r.m_salt = RecordCrypto.RandomBytes(32);
            r.m_iv = RecordCrypto.RandomBytes(RecordCrypto.IvSize);

            byte[] encKey, macKey;
            r.DeriveKeys(signature, out encKey, out macKey);
            try
            {
                r.m_cipher = RecordCrypto.AesCbcEncrypt(encKey, r.m_iv, plaintext);
                byte[] header = r.WriteAuthenticatedPart();
                r.m_mac = RecordCrypto.HmacSha256(macKey, header, 0, header.Length);
            }
            finally
            {
                Array.Clear(encKey, 0, encKey.Length);
                Array.Clear(macKey, 0, macKey.Length);
            }
            return r;
        }

        /// <summary>Verifies the HMAC and decrypts. Throws CryptographicException on mismatch.</summary>
        internal byte[] Open(byte[] signature)
        {
            byte[] encKey, macKey;
            DeriveKeys(signature, out encKey, out macKey);
            try
            {
                byte[] header = WriteAuthenticatedPart();
                byte[] mac = RecordCrypto.HmacSha256(macKey, header, 0, header.Length);
                if (!RecordCrypto.ConstantTimeEquals(mac, m_mac))
                    throw new CryptographicException("Record authentication failed.");
                return RecordCrypto.AesCbcDecrypt(encKey, m_iv, m_cipher);
            }
            finally
            {
                Array.Clear(encKey, 0, encKey.Length);
                Array.Clear(macKey, 0, macKey.Length);
            }
        }

        private void DeriveKeys(byte[] signature, out byte[] encKey, out byte[] macKey)
        {
            byte[] info = new byte[KdfLabel.Length + DbPathHash.Length];
            Buffer.BlockCopy(KdfLabel, 0, info, 0, KdfLabel.Length);
            Buffer.BlockCopy(DbPathHash, 0, info, KdfLabel.Length, DbPathHash.Length);

            byte[] okm = RecordCrypto.Hkdf(signature, m_salt, info, 2 * RecordCrypto.KeySize);
            encKey = new byte[RecordCrypto.KeySize];
            macKey = new byte[RecordCrypto.KeySize];
            Buffer.BlockCopy(okm, 0, encKey, 0, RecordCrypto.KeySize);
            Buffer.BlockCopy(okm, RecordCrypto.KeySize, macKey, 0, RecordCrypto.KeySize);
            Array.Clear(okm, 0, okm.Length);
        }

        private byte[] WriteAuthenticatedPart()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms))
            {
                w.Write(Magic);
                w.Write(FormatVersion);
                w.Write(CreatedUtcTicks);
                w.Write(ExpiresUtcTicks);
                w.Write(BootTimeUtcTicks);
                w.Write(DbPathHash);
                w.Write(m_salt);
                w.Write(Challenge);
                w.Write(m_iv);
                w.Write(m_cipher.Length);
                w.Write(m_cipher);
                w.Flush();
                return ms.ToArray();
            }
        }

        internal byte[] ToBytes()
        {
            byte[] header = WriteAuthenticatedPart();
            byte[] all = new byte[header.Length + m_mac.Length];
            Buffer.BlockCopy(header, 0, all, 0, header.Length);
            Buffer.BlockCopy(m_mac, 0, all, header.Length, m_mac.Length);
            return all;
        }

        internal static UnlockRecord FromBytes(byte[] data)
        {
            using (BinaryReader rd = new BinaryReader(new MemoryStream(data, false)))
            {
                if (!RecordCrypto.ConstantTimeEquals(rd.ReadBytes(4), Magic) ||
                    rd.ReadByte() != FormatVersion)
                    throw new InvalidDataException("Unknown record format.");

                UnlockRecord r = new UnlockRecord();
                r.CreatedUtcTicks = rd.ReadInt64();
                r.ExpiresUtcTicks = rd.ReadInt64();
                r.BootTimeUtcTicks = rd.ReadInt64();
                r.DbPathHash = rd.ReadBytes(32);
                r.m_salt = rd.ReadBytes(32);
                r.Challenge = rd.ReadBytes(32);
                r.m_iv = rd.ReadBytes(RecordCrypto.IvSize);
                int n = rd.ReadInt32();
                if (n <= 0 || n > 64 * 1024) throw new InvalidDataException("Bad record length.");
                r.m_cipher = rd.ReadBytes(n);
                r.m_mac = rd.ReadBytes(RecordCrypto.MacSize);
                if (r.m_cipher.Length != n || r.m_mac.Length != RecordCrypto.MacSize)
                    throw new InvalidDataException("Truncated record.");
                return r;
            }
        }
    }
}
