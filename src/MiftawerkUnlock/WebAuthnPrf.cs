// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MiftawerkUnlock
{
    internal enum PrfStatus
    {
        Success,
        /// <summary>The user cancelled the Windows Hello prompt (or it timed out).</summary>
        Cancelled,
        /// <summary>The passkey no longer exists (deleted on the client PC, other PC, ...).</summary>
        NotFound,
        /// <summary>Windows Hello answered, but without the PRF secret.</summary>
        NoSecret,
        Error
    }

    internal sealed class PrfResult
    {
        internal readonly PrfStatus Status;
        internal readonly int HResult;
        internal readonly byte[] Data; // credential ID (create) or 32-byte secret (get)

        internal PrfResult(PrfStatus status, int hr, byte[] data)
        {
            Status = status;
            HResult = hr;
            Data = data;
        }
    }

    /// <summary>
    /// Windows Hello through the WebAuthn API (webauthn.dll) with the PRF extension
    /// (hmac-secret). Unlike KeyCredentialManager this also works inside a Remote Desktop
    /// session: with "WebAuthn redirection" (redirectwebauthn:i:1, Windows 11 22H2+) the
    /// request goes to Windows Hello of the PC the user sits at. The passkey and its secret
    /// stay in that PC's Windows Hello (TPM); this plugin only receives a 32-byte PRF value
    /// for a salt it chooses. Same passkey + same salt = same value.
    /// Measured on Windows 11 22621 (API 7) over RDP from 26300 (API 9), see docs.
    /// </summary>
    internal static class WebAuthnPrf
    {
        /// <summary>Relying party ID shown in Windows' passkey list on the client PC.</summary>
        internal const string RpId = "unlock.ramzx.de";

        /// <summary>API 6 brings bEnablePrf (MakeCredential options 6) and PRF salts in GetAssertion.</summary>
        internal const uint MinApiVersion = 6;

        internal const int SecretLength = 32;
        private const int TimeoutMs = 120000;

        private const int NTE_NOT_FOUND = unchecked((int)0x80090011);
        private const int NTE_DEVICE_NOT_FOUND = unchecked((int)0x80090035);
        private const int NTE_USER_CANCELLED = unchecked((int)0x80090036);
        private const int ERROR_CANCELLED_HR = unchecked((int)0x800704C7);
        private const int ERROR_TIMEOUT_HR = unchecked((int)0x800705B4);

        internal static uint ApiVersion()
        {
            try { return Native.WebAuthNGetApiVersionNumber(); }
            catch (DllNotFoundException) { return 0; }
            catch (EntryPointNotFoundException) { return 0; }
        }

        /// <summary>True if Windows Hello (local or redirected over Remote Desktop) can be asked.</summary>
        internal static bool IsAvailable()
        {
            if (ApiVersion() < MinApiVersion) return false;
            try
            {
                int available;
                return Native.WebAuthNIsUserVerifyingPlatformAuthenticatorAvailable(out available) == 0 && available != 0;
            }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
        }

        /// <summary>Creates the passkey (Windows Hello prompt). Data = credential ID.</summary>
        internal static Task<PrfResult> CreateAsync(IntPtr owner)
        {
            return Task.Run(() => Create(owner));
        }

        /// <summary>Asks Windows Hello for the PRF value of <paramref name="salt"/>. Data = 32-byte secret.</summary>
        internal static Task<PrfResult> GetSecretAsync(IntPtr owner, byte[] credentialId, byte[] salt)
        {
            if (credentialId == null || credentialId.Length == 0) throw new ArgumentException("No credential.", "credentialId");
            if (salt == null || salt.Length != SecretLength) throw new ArgumentException("Salt must be 32 bytes.", "salt");
            return Task.Run(() => GetSecret(owner, (byte[])credentialId.Clone(), (byte[])salt.Clone()));
        }

        internal static PrfStatus Classify(int hr)
        {
            switch (hr)
            {
                case 0: return PrfStatus.Success;
                case NTE_USER_CANCELLED:
                case ERROR_CANCELLED_HR:
                case ERROR_TIMEOUT_HR: return PrfStatus.Cancelled;
                case NTE_NOT_FOUND:
                case NTE_DEVICE_NOT_FOUND: return PrfStatus.NotFound;
                default: return PrfStatus.Error;
            }
        }

        private static PrfResult Create(IntPtr owner)
        {
            using (Unmanaged mem = new Unmanaged())
            {
                RpInfo rp = new RpInfo { dwVersion = 1, pwszId = mem.String(RpId), pwszName = mem.String("RamzX Unlock (KeePass)") };
                byte[] userId = RecordCrypto.RandomBytes(16);
                UserInfo user = new UserInfo
                {
                    dwVersion = 1, cbId = (uint)userId.Length, pbId = mem.Bytes(userId),
                    pwszName = mem.String("keepass@" + Environment.MachineName),
                    pwszDisplayName = mem.String("KeePass on " + Environment.MachineName)
                };

                IntPtr type = mem.String("public-key");
                IntPtr cose = mem.Alloc(2 * Marshal.SizeOf(typeof(CoseParam)));
                Marshal.StructureToPtr(new CoseParam { dwVersion = 1, pwszCredentialType = type, lAlg = -7 }, cose, false);
                Marshal.StructureToPtr(new CoseParam { dwVersion = 1, pwszCredentialType = type, lAlg = -257 },
                    cose + Marshal.SizeOf(typeof(CoseParam)), false);
                CoseParams coses = new CoseParams { cCredentialParameters = 2, pCredentialParameters = cose };

                MakeOptions opt = new MakeOptions
                {
                    dwVersion = 6, // up to bEnablePrf
                    dwTimeoutMilliseconds = TimeoutMs,
                    dwUserVerificationRequirement = 1,     // required: face, fingerprint or PIN
                    dwAttestationConveyancePreference = 1, // none
                    bPreferResidentKey = 1,
                    bEnablePrf = 1
                };

                IntPtr pAtt;
                int hr = Native.WebAuthNAuthenticatorMakeCredential(owner, mem.Struct(rp), mem.Struct(user),
                    mem.Struct(coses), mem.Struct(ClientData(mem, "webauthn.create")), mem.Struct(opt), out pAtt);
                if (hr != 0) return new PrfResult(Classify(hr), hr, null);
                try
                {
                    AttestationPrefix att = (AttestationPrefix)Marshal.PtrToStructure(pAtt, typeof(AttestationPrefix));
                    byte[] id = Copy(att.pbCredentialId, (int)att.cbCredentialId);
                    return id.Length == 0 ? new PrfResult(PrfStatus.Error, hr, null) : new PrfResult(PrfStatus.Success, 0, id);
                }
                finally { Native.WebAuthNFreeCredentialAttestation(pAtt); }
            }
        }

        private static PrfResult GetSecret(IntPtr owner, byte[] credentialId, byte[] salt)
        {
            using (Unmanaged mem = new Unmanaged())
            {
                try
                {
                    Credential cred = new Credential
                    {
                        dwVersion = 1, cbId = (uint)credentialId.Length, pbId = mem.Bytes(credentialId),
                        pwszCredentialType = mem.String("public-key")
                    };
                    HmacSalt s = new HmacSalt { cbFirst = (uint)salt.Length, pbFirst = mem.Bytes(salt) };
                    HmacSaltValues sv = new HmacSaltValues { pGlobalHmacSalt = mem.Struct(s) };
                    GetOptions opt = new GetOptions
                    {
                        dwVersion = 6, // up to pHmacSecretSaltValues
                        dwTimeoutMilliseconds = TimeoutMs,
                        CredentialList = new Credentials { cCredentials = 1, pCredentials = mem.Struct(cred) },
                        dwUserVerificationRequirement = 1,
                        pHmacSecretSaltValues = mem.Struct(sv)
                    };

                    IntPtr pAssertion;
                    int hr = Native.WebAuthNAuthenticatorGetAssertion(owner, mem.String(RpId),
                        mem.Struct(ClientData(mem, "webauthn.get")), mem.Struct(opt), out pAssertion);
                    if (hr != 0) return new PrfResult(Classify(hr), hr, null);
                    try
                    {
                        if (Marshal.ReadInt32(pAssertion) < 3) return new PrfResult(PrfStatus.NoSecret, 0, null);
                        AssertionPrefix a = (AssertionPrefix)Marshal.PtrToStructure(pAssertion, typeof(AssertionPrefix));
                        if (a.pHmacSecret == IntPtr.Zero) return new PrfResult(PrfStatus.NoSecret, 0, null);
                        HmacSalt outSalt = (HmacSalt)Marshal.PtrToStructure(a.pHmacSecret, typeof(HmacSalt));
                        if (outSalt.cbFirst != SecretLength) return new PrfResult(PrfStatus.NoSecret, 0, null);
                        return new PrfResult(PrfStatus.Success, 0, Copy(outSalt.pbFirst, SecretLength));
                    }
                    finally { Native.WebAuthNFreeAssertion(pAssertion); }
                }
                finally { Array.Clear(salt, 0, salt.Length); }
            }
        }

        /// <summary>
        /// The client data only has to be well-formed: no server verifies it here. The secret
        /// comes from the PRF output, not from the signature over this data.
        /// </summary>
        private static ClientDataInfo ClientData(Unmanaged mem, string type)
        {
            string challenge = Convert.ToBase64String(RecordCrypto.RandomBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            byte[] json = Encoding.UTF8.GetBytes("{\"type\":\"" + type + "\",\"challenge\":\"" + challenge +
                "\",\"origin\":\"https://" + RpId + "\"}");
            return new ClientDataInfo { dwVersion = 1, cbClientDataJSON = (uint)json.Length, pbClientDataJSON = mem.Bytes(json),
                pwszHashAlgId = mem.String("SHA-256") };
        }

        private static byte[] Copy(IntPtr p, int n)
        {
            byte[] b = new byte[Math.Max(0, n)];
            if (n > 0 && p != IntPtr.Zero) Marshal.Copy(p, b, 0, n);
            return b;
        }

        /// <summary>Unmanaged memory for one call; secrets are zeroed before freeing.</summary>
        private sealed class Unmanaged : IDisposable
        {
            private readonly List<KeyValuePair<IntPtr, int>> m_blocks = new List<KeyValuePair<IntPtr, int>>();

            internal IntPtr Alloc(int size)
            {
                IntPtr p = Marshal.AllocHGlobal(size);
                for (int i = 0; i < size; ++i) Marshal.WriteByte(p, i, 0);
                m_blocks.Add(new KeyValuePair<IntPtr, int>(p, size));
                return p;
            }

            internal IntPtr Bytes(byte[] b)
            {
                IntPtr p = Alloc(Math.Max(1, b.Length));
                Marshal.Copy(b, 0, p, b.Length);
                return p;
            }

            internal IntPtr String(string s)
            {
                byte[] b = Encoding.Unicode.GetBytes(s + "\0");
                return Bytes(b);
            }

            internal IntPtr Struct<T>(T value) where T : struct
            {
                IntPtr p = Alloc(Marshal.SizeOf(typeof(T)));
                Marshal.StructureToPtr(value, p, false);
                return p;
            }

            public void Dispose()
            {
                foreach (KeyValuePair<IntPtr, int> b in m_blocks)
                {
                    for (int i = 0; i < b.Value; ++i) Marshal.WriteByte(b.Key, i, 0);
                    Marshal.FreeHGlobal(b.Key);
                }
                m_blocks.Clear();
            }
        }

        // ---- webauthn.h structures (only the versions this plugin uses; IntPtr for pointers) ----

        [StructLayout(LayoutKind.Sequential)]
        internal struct RpInfo { public uint dwVersion; public IntPtr pwszId, pwszName, pwszIcon; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct UserInfo
        {
            public uint dwVersion, cbId; public IntPtr pbId, pwszName, pwszIcon, pwszDisplayName;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CoseParam { public uint dwVersion; public IntPtr pwszCredentialType; public int lAlg; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CoseParams { public uint cCredentialParameters; public IntPtr pCredentialParameters; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ClientDataInfo
        {
            public uint dwVersion, cbClientDataJSON; public IntPtr pbClientDataJSON, pwszHashAlgId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Credential
        {
            public uint dwVersion, cbId; public IntPtr pbId, pwszCredentialType;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Credentials { public uint cCredentials; public IntPtr pCredentials; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Extensions { public uint cExtensions; public IntPtr pExtensions; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HmacSalt { public uint cbFirst; public IntPtr pbFirst; public uint cbSecond; public IntPtr pbSecond; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HmacSaltValues
        {
            public IntPtr pGlobalHmacSalt; public uint cCredWithHmacSecretSaltList; public IntPtr pCredWithHmacSecretSaltList;
        }

        /// <summary>WEBAUTHN_AUTHENTICATOR_MAKE_CREDENTIAL_OPTIONS up to version 6.</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct MakeOptions
        {
            public uint dwVersion, dwTimeoutMilliseconds;
            public Credentials CredentialList;
            public Extensions Extensions;
            public uint dwAuthenticatorAttachment;
            public int bRequireResidentKey;
            public uint dwUserVerificationRequirement, dwAttestationConveyancePreference, dwFlags;
            public IntPtr pCancellationId;                      // v2
            public IntPtr pExcludeCredentialList;               // v3
            public uint dwEnterpriseAttestation, dwLargeBlobSupport; // v4
            public int bPreferResidentKey;                      // v4
            public int bBrowserInPrivateMode;                   // v5
            public int bEnablePrf;                              // v6
        }

        /// <summary>WEBAUTHN_AUTHENTICATOR_GET_ASSERTION_OPTIONS up to version 6.</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct GetOptions
        {
            public uint dwVersion, dwTimeoutMilliseconds;
            public Credentials CredentialList;
            public Extensions Extensions;
            public uint dwAuthenticatorAttachment, dwUserVerificationRequirement, dwFlags;
            public IntPtr pwszU2fAppId, pbU2fAppId;             // v2
            public IntPtr pCancellationId;                      // v3
            public IntPtr pAllowCredentialList;                 // v4
            public uint dwCredLargeBlobOperation, cbCredLargeBlob; // v5
            public IntPtr pbCredLargeBlob;                      // v5
            public IntPtr pHmacSecretSaltValues;                // v6
            public int bBrowserInPrivateMode;                   // v6
        }

        /// <summary>WEBAUTHN_CREDENTIAL_ATTESTATION, version 1 fields (all versions start like this).</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct AttestationPrefix
        {
            public uint dwVersion; public IntPtr pwszFormatType;
            public uint cbAuthenticatorData; public IntPtr pbAuthenticatorData;
            public uint cbAttestation; public IntPtr pbAttestation;
            public uint dwAttestationDecodeType; public IntPtr pvAttestationDecode;
            public uint cbAttestationObject; public IntPtr pbAttestationObject;
            public uint cbCredentialId; public IntPtr pbCredentialId;
        }

        /// <summary>WEBAUTHN_ASSERTION up to version 3 (pHmacSecret).</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct AssertionPrefix
        {
            public uint dwVersion, cbAuthenticatorData; public IntPtr pbAuthenticatorData;
            public uint cbSignature; public IntPtr pbSignature;
            public Credential Credential;
            public uint cbUserId; public IntPtr pbUserId;
            public Extensions Extensions;                       // v2
            public uint cbCredLargeBlob; public IntPtr pbCredLargeBlob; public uint dwCredLargeBlobStatus; // v2
            public IntPtr pHmacSecret;                          // v3
        }

        private static class Native
        {
            [DllImport("webauthn.dll")]
            internal static extern uint WebAuthNGetApiVersionNumber();

            [DllImport("webauthn.dll")]
            internal static extern int WebAuthNIsUserVerifyingPlatformAuthenticatorAvailable(out int available);

            [DllImport("webauthn.dll")]
            internal static extern int WebAuthNAuthenticatorMakeCredential(IntPtr hWnd, IntPtr rp, IntPtr user,
                IntPtr pubKeyCredParams, IntPtr clientData, IntPtr options, out IntPtr attestation);

            [DllImport("webauthn.dll")]
            internal static extern int WebAuthNAuthenticatorGetAssertion(IntPtr hWnd, IntPtr rpId, IntPtr clientData,
                IntPtr options, out IntPtr assertion);

            [DllImport("webauthn.dll")]
            internal static extern void WebAuthNFreeCredentialAttestation(IntPtr attestation);

            [DllImport("webauthn.dll")]
            internal static extern void WebAuthNFreeAssertion(IntPtr assertion);
        }
    }
}
