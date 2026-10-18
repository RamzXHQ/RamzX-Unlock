// Miftawerk Unlock - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;

namespace MiftawerkUnlock
{
    internal sealed class SignResult
    {
        internal readonly KeyCredentialStatus Status;
        internal readonly byte[] Signature; // null unless Status == Success

        internal SignResult(KeyCredentialStatus status, byte[] signature)
        {
            Status = status;
            Signature = signature;
        }
    }

    /// <summary>
    /// The plugin's Windows Hello key (KeyCredentialManager). The private key stays in
    /// Windows (TPM where present); the plugin only ever sees signatures.
    /// One key per Windows user; each database record uses its own random challenge.
    /// </summary>
    internal static class HelloKey
    {
        internal const string CredentialName = "SeeColors.MiftawerkUnlock.v1";

        /// <summary>
        /// Opens the existing key, or creates it (Hello prompt) if Windows has none.
        /// Never replaces an existing key: that would orphan every stored record.
        /// </summary>
        internal static async Task<KeyCredentialRetrievalResult> OpenOrCreateAsync(Form owner)
        {
            KeyCredentialRetrievalResult r = await KeyCredentialManager.OpenAsync(CredentialName);
            if (r.Status != KeyCredentialStatus.NotFound) return r;

            return await HelloForeground.RunAsync(owner, () =>
                KeyCredentialManager.RequestCreateAsync(CredentialName,
                    KeyCredentialCreationOption.FailIfExists).AsTask());
        }

        internal static async Task<SignResult> SignAsync(Form owner, KeyCredential cred,
            byte[] challenge)
        {
            KeyCredentialOperationResult r = await HelloForeground.RunAsync(owner, () =>
                cred.RequestSignAsync(CryptographicBuffer.CreateFromByteArray(challenge)).AsTask());
            if (r.Status != KeyCredentialStatus.Success) return new SignResult(r.Status, null);

            byte[] sig;
            CryptographicBuffer.CopyToByteArray(r.Result, out sig);
            return new SignResult(r.Status, sig);
        }

        /// <summary>Signatures are RSA PKCS#1 v1.5 with SHA-256 (measured, see docs/phase0).</summary>
        internal static bool VerifyPkcs1(KeyCredential cred, byte[] data, byte[] signature)
        {
            using (RSACng rsa = new RSACng())
            {
                rsa.ImportParameters(ExportPublicKey(cred));
                return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
        }

        internal static RSAParameters ExportPublicKey(KeyCredential cred)
        {
            byte[] raw;
            CryptographicBuffer.CopyToByteArray(
                cred.RetrievePublicKey(CryptographicPublicKeyBlobType.BCryptPublicKey), out raw);
            return ParseBCryptRsaPublicBlob(raw);
        }

        // BCRYPT_RSAKEY_BLOB: Magic, BitLength, cbPublicExp, cbModulus, cbPrime1, cbPrime2
        // (6 x ULONG little-endian), then exponent and modulus as big-endian bytes.
        private static RSAParameters ParseBCryptRsaPublicBlob(byte[] b)
        {
            const uint RsaPublicMagic = 0x31415352; // "RSA1"
            if (b.Length < 24 || BitConverter.ToUInt32(b, 0) != RsaPublicMagic)
                throw new FormatException("Not a BCRYPT_RSAPUBLIC_BLOB.");

            int cbExp = (int)BitConverter.ToUInt32(b, 8);
            int cbMod = (int)BitConverter.ToUInt32(b, 12);
            if (b.Length < 24 + cbExp + cbMod) throw new FormatException("Truncated blob.");

            RSAParameters p = new RSAParameters();
            p.Exponent = new byte[cbExp];
            p.Modulus = new byte[cbMod];
            Array.Copy(b, 24, p.Exponent, 0, cbExp);
            Array.Copy(b, 24 + cbExp, p.Modulus, 0, cbMod);
            return p;
        }
    }
}
