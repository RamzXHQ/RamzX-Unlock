// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MiftawerkUnlock
{
    /// <summary>Where the key material of a record comes from.</summary>
    internal enum RecordKind : byte
    {
        /// <summary>Signature of the plugin's KeyCredentialManager key (this PC's Windows Hello).</summary>
        HelloKey = 1,
        /// <summary>
        /// WebAuthn PRF value of a passkey (Windows Hello of the PC the user sits at; also over
        /// Remote Desktop with WebAuthn redirection).
        /// </summary>
        WebAuthnPrf = 2
    }

    /// <summary>
    /// Encrypted quick-unlock record for one database.
    ///
    /// Layout (little-endian), all of it authenticated by the trailing HMAC:
    ///   "MWU1" | version(1)
    ///   [version 2 only: kind(1) | credential id length(2) | credential id]
    ///   | created ticks(8) | expires ticks(8) | boot time ticks(8)
    ///   | db path hash(32) | salt(32) | challenge(32) | iv(16) | cipher length(4) | cipher
    ///   | HMAC-SHA256(32)
    ///
    /// Version 1 = Windows Hello key (unchanged since 0.1.0), version 2 = WebAuthn PRF.
    /// Keys: HKDF-SHA256(ikm = Windows Hello signature over the challenge, or the 32-byte PRF
    /// value for the challenge, salt, info = label | db path hash) -> AES-256-CBC key | HMAC key.
    /// Without a fresh Windows Hello confirmation the record cannot be decrypted.
    /// </summary>
    internal sealed class UnlockRecord
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("MWU1");
        private const byte VersionHelloKey = 1;
        private const byte VersionWebAuthn = 2;
        private const int MaxCredentialIdLength = 1024;
        private static readonly byte[] KdfLabel = Encoding.ASCII.GetBytes("SeeColors.MiftawerkUnlock.Record.v1");
        private static readonly byte[] KdfLabelPrf = Encoding.ASCII.GetBytes("RamzX.Unlock.Record.WebAuthnPrf.v1");

        internal RecordKind Kind { get; private set; }
        /// <summary>Passkey credential ID (WebAuthnPrf records only, otherwise null).</summary>
        internal byte[] CredentialId { get; private set; }
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

        /// <summary>Record for the Windows Hello key (KeyCredentialManager); ikm = signature.</summary>
        internal static UnlockRecord Seal(byte[] dbPathHash, TimeSpan validity, byte[] challenge,
            byte[] signature, byte[] plaintext)
        {
            return Seal(RecordKind.HelloKey, null, dbPathHash, validity, challenge, signature, plaintext);
        }

        /// <summary>Record for a WebAuthn passkey; ikm = PRF value of the challenge.</summary>
        internal static UnlockRecord SealPrf(byte[] credentialId, byte[] dbPathHash, TimeSpan validity,
            byte[] challenge, byte[] prfSecret, byte[] plaintext)
        {
            if (credentialId == null || credentialId.Length == 0 || credentialId.Length > MaxCredentialIdLength)
                throw new ArgumentException("Invalid credential ID.", "credentialId");
            return Seal(RecordKind.WebAuthnPrf, (byte[])credentialId.Clone(), dbPathHash, validity, challenge,
                prfSecret, plaintext);
        }

        private static UnlockRecord Seal(RecordKind kind, byte[] credentialId, byte[] dbPathHash,
            TimeSpan validity, byte[] challenge, byte[] ikm, byte[] plaintext)
        {
            DateTime now = DateTime.UtcNow;
            UnlockRecord r = new UnlockRecord();
            r.Kind = kind;
            r.CredentialId = credentialId;
            r.CreatedUtcTicks = now.Ticks;
            r.ExpiresUtcTicks = (now + validity).Ticks;
            r.BootTimeUtcTicks = BootStamp.CurrentBootTimeUtcTicks();
            r.DbPathHash = dbPathHash;
            r.Challenge = challenge;
            r.m_salt = RecordCrypto.RandomBytes(32);
            r.m_iv = RecordCrypto.RandomBytes(RecordCrypto.IvSize);

            byte[] encKey, macKey;
            r.DeriveKeys(ikm, out encKey, out macKey);
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

        /// <summary>
        /// Verifies the HMAC and decrypts. <paramref name="ikm"/> is the Windows Hello signature
        /// (HelloKey) or the PRF value (WebAuthnPrf). Throws CryptographicException on mismatch.
        /// </summary>
        internal byte[] Open(byte[] ikm)
        {
            byte[] encKey, macKey;
            DeriveKeys(ikm, out encKey, out macKey);
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

        private void DeriveKeys(byte[] ikm, out byte[] encKey, out byte[] macKey)
        {
            byte[] label = Kind == RecordKind.WebAuthnPrf ? KdfLabelPrf : KdfLabel;
            byte[] info = new byte[label.Length + DbPathHash.Length];
            Buffer.BlockCopy(label, 0, info, 0, label.Length);
            Buffer.BlockCopy(DbPathHash, 0, info, label.Length, DbPathHash.Length);

            byte[] okm = RecordCrypto.Hkdf(ikm, m_salt, info, 2 * RecordCrypto.KeySize);
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
                if (Kind == RecordKind.WebAuthnPrf)
                {
                    w.Write(VersionWebAuthn);
                    w.Write((byte)Kind);
                    w.Write((ushort)CredentialId.Length);
                    w.Write(CredentialId);
                }
                else w.Write(VersionHelloKey); // byte-identical to records written by 0.1.0-0.3.0
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
                if (!RecordCrypto.ConstantTimeEquals(rd.ReadBytes(4), Magic))
                    throw new InvalidDataException("Unknown record format.");

                UnlockRecord r = new UnlockRecord();
                byte version = rd.ReadByte();
                if (version == VersionHelloKey) r.Kind = RecordKind.HelloKey;
                else if (version == VersionWebAuthn)
                {
                    if (rd.ReadByte() != (byte)RecordKind.WebAuthnPrf) throw new InvalidDataException("Unknown record kind.");
                    r.Kind = RecordKind.WebAuthnPrf;
                    int idLen = rd.ReadUInt16();
                    if (idLen == 0 || idLen > MaxCredentialIdLength) throw new InvalidDataException("Bad credential ID.");
                    r.CredentialId = rd.ReadBytes(idLen);
                    if (r.CredentialId.Length != idLen) throw new InvalidDataException("Truncated record.");
                }
                else throw new InvalidDataException("Unknown record format.");

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
