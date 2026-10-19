# Feasibility of Windows Hello from a KeePass 2 plugin (.NET Framework 4.8), and the KeePass 2 plugin API

Research date: 2026-10-18. "Verified" means the fact was read on the cited page or source file during this session. "Inferred" means it is the researcher's reasoning or general platform knowledge that was not confirmed against a primary source in this session. GPL code (KeePass, KeePassXC) was read only to describe APIs and behavior. No code was copied.

## Q1. How can a .NET Framework 4.8 assembly call KeyCredentialManager / UserConsentVerifier (WinRT)? NuGet Contracts vs Windows.winmd, PLGX vs DLL, what existing plugins do

### Takeaway
Microsoft's documented route for .NET Framework 4.x is the `Microsoft.Windows.SDK.Contracts` NuGet package. It targets netstandard2.0, is compatible with net461 through net481, requires PackageReference, and was still being published in Aug 2026. Older plugins reference `Windows.winmd` from the Windows SDK UnionMetadata folder plus the `System.Runtime.WindowsRuntime` facade instead. Ship the plugin as a DLL. Whether the PLGX compiler resolves WinRT references could not be verified.

### Cited Findings
- **Verified.** Microsoft's "Call Windows Runtime APIs in desktop apps" page (ms.date 2026-10-03) says that apps targeting .NET Core 3.x or .NET Framework should install the `Microsoft.Windows.SDK.Contracts` NuGet package, picking the version that matches the minimum Windows target (for example 10.0.19041.xxxx for Windows 10 2004, 10.0.17763.xxxx for 1809). The same page says the package approach "applies to .NET Framework 4.x projects". For .NET 6+ it recommends a Windows TFM instead. — [Microsoft Learn: Call WinRT APIs in desktop apps](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/winrt-apis-desktop-apps)
- **Verified.** NuGet facts for `Microsoft.Windows.SDK.Contracts`:
  - Latest stable version: 10.0.28000.2705 (2026-08-26). A 10.0.29648.1000-preview was published 2026-08-20, and the 10.0.26100.x line is still maintained (10.0.26100.9169 on 2026-08-26). No deprecation notice.
  - Targets .NET Standard 2.0, with computed compatibility for .NET Framework 4.6.1 to 4.8.1.
  - Dependencies: System.Runtime.WindowsRuntime (>= 4.6.0), System.Runtime.WindowsRuntime.UI.Xaml (>= 4.6.0), System.Runtime.InteropServices.WindowsRuntime (>= 4.3.0).
  - Requires "default package management format set to PackageReference" and NuGet 4.0+.
  - License: the page links to Windows SDK license terms at https://aka.ms/WinSDKLicenseURL. This is a Microsoft proprietary license, not an OSI license, and no SPDX id is shown.
  — [NuGet: Microsoft.Windows.SDK.Contracts](https://www.nuget.org/packages/Microsoft.Windows.SDK.Contracts)
- **Verified.** Some older NuGet descriptions say ".NET Framework 4.6+" while the current page computes net461+ compatibility. Both include net48. — [NuGet: Microsoft.Windows.SDK.Contracts](https://packages.nuget.org/packages/Microsoft.Windows.SDK.Contracts)
- **Verified.** WinHelloUnlock (Angelelz, MIT), an existing KeePass Windows Hello plugin built on UWP KeyCredential/PasswordVault, has a .csproj that:
  - references `Windows.winmd` by HintPath `C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.15063.0\Windows.winmd`;
  - adds PackageReferences to System.Runtime.WindowsRuntime 4.6.0-preview, System.Runtime.WindowsRuntime.UI.Xaml 4.6.0-preview and Microsoft.Windows.SDK.Contracts 10.0.18362.2002-preview;
  - uses TargetFrameworkVersion v4.5.

  This mixes both approaches. — [WinHelloUnlock.csproj (GitHub)](https://raw.githubusercontent.com/Angelelz/WinHelloUnlock/master/WinHelloUnlock/WinHelloUnlock.csproj)
- **Verified.** WinHelloUnlock's README says it uses "Windows UWP APIs": Key Credential, Password Credential/Password Vault, CryptographicEngine and CryptographicKey. It is MIT licensed and was tested with KeePass 2.42.1. — [lazou/WinHelloUnlock README](https://github.com/lazou/WinHelloUnlock)
- **Verified.** KeePassWinHello (sirAndros, MIT) does not use WinRT. A Microsoft Q&A post attributes to it the use of NCrypt with the Microsoft Passport KSP and the undocumented `NgcCacheType` property (see Q4). Its release notes mention a `KeePassWinHelloPlugin.plgx` artifact (issue #92, Windows 11 22H2 error). — [KeePassWinHello releases](https://github.com/sirAndros/KeePassWinHello/releases); [MS Q&A 2100684](https://learn.microsoft.com/en-gb/answers/questions/2100684/undocumented-usage-of-ncrypt-api-for-microsoft-pas)
- **Verified.** KeePassXC (C++, GPL) uses `KeyCredentialManager::RequestCreateAsync` with `FailIfExists`, falls back to `OpenAsync`, then calls `RequestSignAsync`. — [KeePassXC WindowsHello.cpp mirror](https://gitverse.ru/soldim/keepassxc/content/develop/src/quickunlock/WindowsHello.cpp)
- **Verified.** KeePass PLGX limitations:
  - C# only, at most C# 5;
  - no project-to-project references, so custom assembly references must sit in the source directory or a subdirectory;
  - supports ".NET assembly references";
  - KeePass compiles a PLGX on the user's machine and caches the result;
  - PLGX cannot be Authenticode-signed, while a DLL can;
  - KeePass "recommends always providing a DLL".

  The page does not mention .winmd references. — [keepass.info Plugin Development](https://keepass.info/help/v2_dev/plg_index.html)

### Inferences
- Inferred: an SDK-style project with `<TargetFramework>net48</TargetFramework>` and a PackageReference to `Microsoft.Windows.SDK.Contracts` should compile `KeyCredentialManager` and `UserConsentVerifier` calls. The package's PackageReference requirement suits SDK-style projects.
- Inferred: the output DLL depends at runtime on the WinRT projection support in .NET Framework 4.5+ (`System.Runtime.WindowsRuntime.dll`, which ships in the framework). It does not need the .winmd files at runtime, because the metadata is resolved from the OS. Confirm with a test build.
- Inferred: to keep the plugin to a single DLL, check after building whether any facade DLLs from the NuGet dependency closure get copied to the output and are actually needed. If they are, KeePass would need them next to the plugin DLL in the Plugins folder.
- Inferred: `await` on `IAsyncOperation<T>` needs the `GetAwaiter`/`AsTask` extensions from `System.WindowsRuntimeSystemExtensions` (System.Runtime.WindowsRuntime). C# 5 supports async/await, so a C# 5 PLGX is not ruled out on language grounds. Whether KeePass's CodeDom-based PLGX compiler accepts a .winmd reference is unknown. Given that and the Authenticode advantage, DLL-only distribution is lower risk.
- Inferred: the KeePassWinHello approach (P/Invoke to ncrypt.dll) avoids WinRT references entirely and is PLGX-friendly, but it relies on undocumented properties (see Q4).

### Gaps
- Could not verify whether KeePass's PLGX compiler (2.60/2.61) can compile against `Windows.winmd` or the Contracts package. No documentation or plugin report was found.
- The exact license text behind aka.ms/WinSDKLicenseURL and whether it allows redistributing any facade DLLs from the package were not read.

## Q2. Window ownership / foreground: HWND-aware APIs, Windows 11 "prompt behind app" problems, workarounds

### Takeaway
Two documented HWND-aware entry points exist:
- `IUserConsentVerifierInterop::RequestVerificationForWindowAsync(HWND, …)`, Windows build 22000+, in `userconsentverifierinterop.h`.
- In newer SDKs, `KeyCredentialManager.RequestCreateForWindowAsync(WindowId, name, option)`, documented only for the winrt-26100 and winrt-28000 SDK monikers.

No window-bound variant of `OpenAsync` or `RequestSignAsync` was found. Several products report the Hello prompt opening behind their window or without focus. Known mitigations are heuristic: find the "Credential Dialog Xaml Host" window and call `SetForegroundWindow`, or use undocumented NCrypt window-handle properties.

### Cited Findings
- **Verified.** `IUserConsentVerifierInterop::RequestVerificationForWindowAsync(HWND appWindow, HSTRING message, REFIID riid, void **asyncOperation)` returns an `IAsyncOperation<UserConsentVerificationResult>`. Minimum supported client and server: Windows Build 22000. Header: `userconsentverifierinterop.h`. — [Microsoft Learn: RequestVerificationForWindowAsync](https://learn.microsoft.com/en-us/windows/win32/api/userconsentverifierinterop/nf-userconsentverifierinterop-iuserconsentverifierinterop-requestverificationforwindowasync)
- **Verified.** The `KeyCredentialManager` class page (updated 2026-09-30) lists these methods:
  - `DeleteAsync(String)`, `GetSecureId()`, `IsSupportedAsync()`, `OpenAsync(String)`, `RenewAttestationAsync()`, `RequestCreateAsync(String, KeyCredentialCreationOption)`;
  - new overloads `OpenAsync(String, ChallengeResponseKind, AttestationChallengeHandler)` and `RequestCreateAsync(String, KeyCredentialCreationOption, String, String, KeyCredentialCacheConfiguration, WindowId, ChallengeResponseKind, AttestationChallengeHandler)`;
  - `RequestCreateForWindowAsync(WindowId, String, KeyCredentialCreationOption)`.

  The class was introduced in 10.0.10240 (UniversalApiContract v1). — [Microsoft Learn: KeyCredentialManager](https://learn.microsoft.com/en-us/uwp/api/windows.security.credentials.keycredentialmanager)
- **Verified.** `RequestCreateForWindowAsync(WindowId window, string name, KeyCredentialCreationOption option)` returns `IAsyncOperation<KeyCredentialRetrievalResult>`. It is documented only under the winrt-26100 and winrt-28000 monikers. The page has no remarks. — [Microsoft Learn: RequestCreateForWindowAsync](https://learn.microsoft.com/en-us/uwp/api/windows.security.credentials.keycredentialmanager.requestcreateforwindowasync)
- **Verified.** Microsoft's "WinRT APIs not supported in desktop apps" page (2026-07-22) lists many `*Interop` COM interfaces and the IInitializeWithWindow pattern. It does not mention KeyCredentialManager or UserConsentVerifier. — [Microsoft Learn: WinRT API desktop support](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/winrt-api-desktop-app-support)
- **Verified.** A developer on Microsoft Q&A (2024-10-09, unanswered) reports two problems with calling KeyCredentialManager from a non-UWP app:
  - the prompt is not tied to the app window, so it adds an extra taskbar icon and does not focus;
  - the prompt text cannot be customized.

  Using NCrypt directly with `NCRYPT_USE_CONTEXT_PROPERTY` and `NCRYPT_WINDOW_HANDLE_PROPERTY` fixed both, but only after also setting the undocumented `NgcCacheType` property, which they say KeePassWinHello uses. — [MS Q&A 2100684](https://learn.microsoft.com/en-gb/answers/questions/2100684/undocumented-usage-of-ncrypt-api-for-microsoft-pas)
- **Verified.** KeePassXC's Windows Hello code does not pass an HWND. Shortly after starting the request, a timer looks for a window of class "Credential Dialog Xaml Host" and calls `SetForegroundWindow` on it, with a few retries. — [KeePassXC WindowsHello.cpp mirror](https://gitverse.ru/soldim/keepassxc/content/develop/src/quickunlock/WindowsHello.cpp)
- **Verified.** KeePassWinHello v3.3 (year not shown on the page) "lowers the probability" of issue #25 "Windows Hello Prompt Hidden" and #86 (Hello window not gaining focus when KeePass is opened via a shortcut). It also fixed #54 (dialog at top-left when the main window is hidden). The notes do not claim a full fix. — [KeePassWinHello releases](https://github.com/sirAndros/KeePassWinHello/releases)
- **Verified (secondary).** 1Password users reported the Windows Hello prompt opening behind other windows. Search snippets say it was fixed in 1Password for Windows 8.12.1, but the forum thread was not opened. — [1Password community thread](https://1password.community/discussions/1password/windows-hello-opens-in-background/166775)

### Inferences
- Inferred: the most robust documented path for the unlock-time prompt is a two-step flow:
  1. Get user presence with `UserConsentVerifier` through `IUserConsentVerifierInterop` and KeePass's HWND (Windows 11 only, build 22000+).
  2. Call `RequestSignAsync`.

  This has a drawback: `RequestSignAsync` may show its own Hello prompt as well, so the user could be prompted twice. Microsoft Account keys are documented as "transactional keys, which means the user is always prompted" (WHfB FAQ, see Q3). Test this.
- Inferred: in .NET Framework, the interop interface could be obtained by declaring the COM interface in C# with `[ComImport]` and the IID from the header, then casting the activation factory for `Windows.Security.Credentials.UI.UserConsentVerifier` (for example from `WindowsRuntimeMarshal.GetActivationFactory`). The IID was not retrieved in this session and must come from `userconsentverifierinterop.h` in the Windows SDK.
- Inferred: `RequestCreateForWindowAsync` needs a `Windows.UI.WindowId` built from the HWND. Using it from net48 requires a Contracts package version of at least 10.0.26100.x, and the call must be guarded at runtime (for example with `ApiInformation.IsMethodPresent`) because older Windows builds lack it. It only helps at enrollment, not at unlock.
- Inferred: a practical fallback for unlock is the KeePassXC-style "find Credential Dialog Xaml Host and SetForegroundWindow" heuristic. The window class name is undocumented and could change.

### Gaps
- No documented `IKeyCredentialManagerInterop` (HWND variant for `OpenAsync`/`RequestSignAsync`) was found. Absence is not proven.
- No Microsoft statement on the "prompt behind window" problem on Windows 11 was found.
- Whether `RequestCreateForWindowAsync` exists on Windows 11 23H2 or earlier at runtime is not documented beyond the SDK moniker.

## Q3. Security properties: TPM backing, non-exportability, signature determinism, PIN fallback, effect of PIN reset / Hello reset / feature updates, biometric privacy (GDPR-relevant)

### Takeaway
Microsoft documents that the TPM generates and protects the Hello private key when a TPM is present, with software protection otherwise, and that apps never get the private key. PIN is a mandatory fallback; it cannot be disabled while biometrics stay enabled. A PIN reset removes keys protected by the old material, so a plugin must handle `NotFound` and re-enroll. Biometric templates stay on the device. Signature determinism is not clearly documented: one Microsoft page says signatures are "PKCS #1 RSA PSS" (randomized), while the Windows Hello developer guide verifies with PKCS#1 v1.5 (deterministic). KeePassXC derives its key from SHA-256(signature), which works only if signatures are deterministic in practice.

### Cited Findings
- **Verified.** "The trusted platform module (TPM) generates and protects this private key. If the device does not have a TPM chip, the private key is encrypted and protected by software." On `RequestCreateAsync`: "If the device has the right TPM chip, the APIs will request the TPM chip to create the private and public key… if there is no TPM chip available, the OS will create the key pair in code. There is no way for the app to access the created private keys directly." — [Microsoft Learn: Windows Hello (apps)](https://learn.microsoft.com/en-us/windows/apps/develop/security/windows-hello)
- **Verified.** On access control, the same guide says: "An application can never use the keys from another application", and the key material "is not exposed to the requesting application". — [Microsoft Learn: Windows Hello (apps)](https://learn.microsoft.com/en-us/windows/apps/develop/security/windows-hello)
- **Verified, with conflicting empirical evidence.** A 2026-06 Microsoft Q&A thread disagrees with the "never use the keys from another application" claim for unpackaged apps:
  - The asker's test showed that an unpackaged app B could `OpenAsync` and `RequestSignAsync` a credential created by a packaged full-trust app A, using the same name, after Hello verification.
  - A Microsoft External Staff moderator (not an employee) said NGC scopes such credentials to the user account "by design" unless the app runs in an AppContainer.
  - Neither statement is backed by official documentation.

  — [MS Q&A 5912130](https://learn.microsoft.com/en-us/answers/questions/5912130/what-is-the-security-boundary-of-windows-hello-key)
- **Verified.** The `KeyCredentialManager` remarks say the keys are "RSA 2048-bit", with signature format "PKCS #1 RSA PSS with SHA256". — [Microsoft Learn: KeyCredentialManager](https://learn.microsoft.com/en-us/uwp/api/windows.security.credentials.keycredentialmanager). This is contradicted by the Windows Hello developer guide, which says: "We are using SHA256 as the hash algorithm and Pkcs1 for SignaturePadding", and verifies with `RSASignaturePadding.Pkcs1`. — [Microsoft Learn: Windows Hello (apps)](https://learn.microsoft.com/en-us/windows/apps/develop/security/windows-hello)
- **Verified.** `KeyCredential.RequestSignAsync(IBuffer)` "Prompts the user to cryptographcally sign data using their key credential" and returns `IAsyncOperation<KeyCredentialOperationResult>`. The page does not state the padding. — [Microsoft Learn: RequestSignAsync](https://learn.microsoft.com/en-us/uwp/api/windows.security.credentials.keycredential.requestsignasync)
- **Verified.** KeePassXC signs a random challenge stored next to the ciphertext and uses SHA-256(signature) as an AES-256-GCM key. Decryption re-signs the same challenge, so the design depends on deterministic signatures. The source has no comment explaining this. — [KeePassXC WindowsHello.cpp mirror](https://gitverse.ru/soldim/keepassxc/content/develop/src/quickunlock/WindowsHello.cpp); [KeePassXC PR #7384](https://github.com/keepassxreboot/keepassxc/pull/7384)
- **Verified.** On PIN fallback, the WHfB FAQ says "Can I disable the PIN while using Windows Hello for Business? No… The PIN is the fallback mechanism. Disabling or hiding the PIN credential provider will disable the use of biometrics." It also says biometrics require a PIN, and that after repeated biometric failures "the user must sign in using another authentication method, such as a PIN." — [Microsoft Learn: WHfB FAQ](https://learn.microsoft.com/en-us/windows/security/identity-protection/hello-for-business/faq) (updated 2026-10-08)
- **Verified.** On PIN reset: "Resetting the PIN means that all keys and certificates encrypted with the old key material will be removed." (WHfB FAQ and Windows Hello apps guide.) The apps guide sample treats `OpenAsync` returning `KeyCredentialStatus.NotFound` as "PIN reset has occurred somewhere else and key is lost. Repeat key registration". — [WHfB FAQ](https://learn.microsoft.com/en-us/windows/security/identity-protection/hello-for-business/faq); [Windows Hello (apps)](https://learn.microsoft.com/en-us/windows/apps/develop/security/windows-hello)
- **Verified.** `certutil.exe -deleteHelloContainer` deletes the Hello container. On recent Windows 11 it also clears passkeys stored in the same container. — [WHfB FAQ](https://learn.microsoft.com/en-us/windows/security/identity-protection/hello-for-business/faq)
- **Verified.** On key caching, "Microsoft Account sign-in keys are transactional keys, which means the user is always prompted when accessing the key", while Entra ID/AD keys "are cached under lock". — [WHfB FAQ](https://learn.microsoft.com/en-us/windows/security/identity-protection/hello-for-business/faq)
- **Verified.** On biometric privacy, the WHfB FAQ says:
  - "The enrollment profile biometrics data is device specific, is stored locally on the device, and does not leave the device or roam with the user."
  - The data is stored as "an encrypted template database".
  - "no user, or any process other than Windows Hello has access to it."
  - Diagnostic data "is pseudonymized, does not include biometric information".

  The apps guide says a Hello "doesn't sync across devices or share data with servers or apps". — [WHfB FAQ](https://learn.microsoft.com/en-us/windows/security/identity-protection/hello-for-business/faq); [Windows Hello (apps)](https://learn.microsoft.com/en-us/windows/apps/develop/security/windows-hello)
- **Verified.** "Attestation is only available for devices with a TPM chip that's version 2.0 or later." — [Windows Hello (apps)](https://learn.microsoft.com/en-us/windows/apps/develop/security/windows-hello)

### Inferences
- Inferred: a plugin can determine TPM backing only through `GetAttestationAsync()`. `NotSupported` suggests a software key, but this was not tested. The plugin cannot enforce TPM-only except by refusing to enroll when attestation fails.
- Inferred: a normal app cannot enforce biometric-only, because the PIN is the mandatory fallback. Biometric data is never accessible to the plugin, which only receives a signature or a verification result. This supports a GDPR statement that the plugin processes no biometric data, but the wording should cite Microsoft.
- Inferred: because determinism is undocumented and the documentation contradicts itself, the plugin should verify determinism at enrollment:
  1. Sign the same challenge twice and compare.
  2. Verify against `RetrievePublicKey()` with both PKCS#1 v1.5 and PSS.
  3. Refuse to enroll if the signatures differ.

  A design that avoids the dependency is a TPM-bound encrypt/decrypt key, such as the NCrypt approach in Q4, but that relies on undocumented properties.
- Inferred: a signature from a fixed challenge is a static secret. Any same-user process that can trigger `RequestSignAsync` on the same credential name (after a Hello gesture) can derive it (see the Q&A test). Expiry, a random per-enrollment challenge stored with the ciphertext, and wiping the key on lock limit exposure but do not remove it.
- Inferred: a PIN reset or Hello reset invalidates the plugin's credential, which is safe-fail: the user falls back to the master password and re-enrolls.

### Gaps
- No Microsoft document explicitly states whether KeyCredential signatures are deterministic. This needs an empirical test on the target Windows build.
- No source found on whether Windows feature updates (in-place upgrades) or TPM firmware updates or clears preserve KeyCredentials. Inferred: a TPM clear destroys TPM-bound keys. Feature updates are generally reported to preserve the Hello container, but this was not verified.
- No official documentation of the KeyCredential scope (per app vs per user) for unpackaged Win32 apps. The documentation and the Q&A test conflict.

## Q4. Alternatives: DPAPI (ProtectedData) and NCrypt Microsoft Passport KSP vs KeyCredentialManager

### Takeaway
DPAPI is documented and built into .NET Framework, but it involves no user-presence gesture: any code running as the user can decrypt CurrentUser-scope data. The NCrypt Microsoft Passport KSP route, used by KeePassWinHello, offers HWND ownership, a custom message and TPM encrypt/decrypt without the determinism question, but depends on properties Microsoft has not documented. KeyCredentialManager is the documented Hello API but has the window-ownership and determinism issues above.

### Cited Findings
- **Verified.** `ProtectedData` "provides access to the Data Protection API (DPAPI)… It provides protection using the user or machine credentials". It is Windows-only, available in .NET Framework 2.0 to 4.8.1, and keys are stored in the user profile. The sample says CurrentUser-scope data "can be decrypted only by the same current user". — [Microsoft Learn: ProtectedData](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata)
- **Verified.** KeePass itself uses DPAPI for in-memory protection ("KeePass uses the Windows DPAPI for encrypting sensitive data in memory") and offers a "Windows user account" key component. — [keepass.info Security](https://keepass.info/help/base/security.html)
- **Verified.** Credentials created by KeyCredentialManager are visible in the "Microsoft Passport Key Storage Provider", per `certutil -csp "Microsoft Passport Key Storage Provider" -user -key`. Using NCrypt with `NCRYPT_WINDOW_HANDLE_PROPERTY` and `NCRYPT_USE_CONTEXT_PROPERTY` solved window ownership and custom text, but needed the undocumented `NgcCacheType` property, which "appears only as a string in cryptngc.dll". There was no Microsoft answer as of the fetch. — [MS Q&A 2100684](https://learn.microsoft.com/en-gb/answers/questions/2100684/undocumented-usage-of-ncrypt-api-for-microsoft-pas)
- **Verified.** `NCryptOpenStorageProvider` is documented generically. The documented KSP list includes the Microsoft Platform Crypto Provider (`MS_PLATFORM_CRYPTO_PROVIDER`). No official page documenting Passport KSP usage for apps was found. — [Microsoft Learn: NCryptOpenStorageProvider](https://learn.microsoft.com/en-us/windows/win32/api/ncrypt/nf-ncrypt-ncryptopenstorageprovider); [CNG key storage providers](https://learn.microsoft.com/windows/win32/seccertenroll/cng-key-storage-providers)
- **Verified.** KeePassWinHello's history shows the maintenance cost of the NCrypt route. Fixes include NTE_INVALID_HANDLE, NTE_BAD_KEYSET, NTE_BAD_DATA, a retry on WINBIO_E_DATA_PROTECTION_FAILURE, and NCryptDecrypt 0x80098046. v3.1.1 also had a security fix (#38) for spoofing that replaced the permanent key in Windows Credential Manager. — [KeePassWinHello releases](https://github.com/sirAndros/KeePassWinHello/releases)
- **Verified.** KeePassWinHello's design:
  - by default, it keeps the encrypted master key in process memory only;
  - optionally, it stores master keys in Windows Credential Manager, encrypted with a persistent Hello-protected key;
  - the default validity period is 24 hours.

  — [KeePassWinHello README](https://github.com/sirAndros/KeePassWinHello)
- **Verified.** In a KeePassXC PR discussion, a contributor suggested that NCrypt-based encryption did not require a Hello prompt on initial use, possibly because of a DPAPI backend. — [KeePassXC PR #7384](https://github.com/keepassxreboot/keepassxc/pull/7384) (search snippet, not fully read)

### Inferences
- Inferred trade-offs:
  - **DPAPI:** documented and simple, no gesture, no TPM guarantee. It suits wrapping a per-boot secret, but by itself does not give "Windows Hello unlock".
  - **KeyCredentialManager:** documented and TPM-backed when available. Its drawbacks are determinism, window focus, and user-scope rather than app-scope.
  - **Passport KSP via NCrypt:** better UX and a real decrypt operation, but undocumented and at risk of breaking with Windows updates.
- Inferred: a defensible design wraps the KeePass composite-key material with AES-GCM. The AES key is derived from a KeyCredential signature over a random per-enrollment challenge, plus a per-boot nonce held only in memory or DPAPI-protected (see Q5 on reboot expiry).

### Gaps
- No official Microsoft documentation of `NgcCacheType`, `NCRYPT_WINDOW_HANDLE_PROPERTY` semantics for the Passport KSP, or support status.

## Q5. KeePass 2 plugin API facts (from keepass.info and the KeePass 2.x source)

### Takeaway
A plugin must be a .NET Framework class library named per KeePass rules. KeePass exposes the pieces Miftawerk Unlock needs:
- `KeyProvider`/`KeyProviderPool`;
- a public `MainForm.OpenDatabase(IOConnectionInfo, CompositeKey, bool)` and `MainForm.LockAllDocuments()`;
- the `FileClosingPre`, `FileClosingPost` and `FileClosed` events, which carry a locking flag;
- a built-in session-lock handler governed by `WorkspaceLocking.LockOnSessionSwitch`.

`KeyPromptForm` registers with `GlobalWindowManager`, which is how quick-unlock plugins typically detect the prompt. Its `ShowDialog` is internal and its `CompositeKey` is read-only.

### Cited Findings
- **Verified.** Plugin rules from keepass.info:
  - derive from `KeePass.Plugins.Plugin`;
  - `Initialize(IPluginHost host)` returns bool, and returning false unloads the plugin;
  - `Terminate()` is a notification only;
  - `GetMenuItem(PluginMenuType)` must return a new ToolStripMenuItem on each call;
  - `UpdateUrl` returns the version-file URL, with HTTPS recommended;
  - the namespace must equal the DLL name and the main class must be `<Namespace>Ext`;
  - the "Product name" version field must be exactly "KeePass Plugin";
  - "KeePass" may appear in a plugin name only with a non-numeric prefix or suffix;
  - build a .NET Framework (not .NET Standard/Core) class library against an official `KeePass.exe`.

  — [keepass.info Plugin Development](https://keepass.info/help/v2_dev/plg_index.html)
- **Verified.** Update-check file (KeePass ≥ 2.18):
  - UTF-8, ideally without BOM;
  - the first line is a separator character, each following line is `Name:Version`, and the file ends with a separator-only line;
  - optional `.gz`;
  - optional RSA/SHA-512 signature (KeePass ≥ 2.34) registered with `UpdateCheckEx.SetFileSigKey`;
  - the plugin is matched by the `AssemblyTitle` and `AssemblyFileVersion` attributes.

  — [keepass.info Plugin Development](https://keepass.info/help/v2_dev/plg_index.html)
- **Verified.** Key providers derive from `KeePassLib.Keys.KeyProvider`. Register with `host.KeyProviderPool.Add(...)` in `Initialize` and `Remove` in `Terminate`. Override `Name` and `byte[] GetKey(KeyProviderQueryContext ctx)`. — [keepass.info Key Providers](https://keepass.info/help/v2_dev/plg_keyprov.html)
- **Verified (source, © 2003-2025).** `KeyProvider` virtual members and defaults:
  - `Exclusive` (false);
  - `DirectKey` (false, "highly discouraged" to return true);
  - `GetKeyMightShowGui` (true);
  - `SecureDesktopCompatible` (false; the comment says secure desktop mode is "almost never the case").

  `KeyProviderPool` has `Add`, `Remove`, `Get(string)`, `IsKeyProvider(string)` and `Count`. — [KeyProvider.cs (mirror)](https://raw.githubusercontent.com/dlech/KeePass2.x/master/KeePassLib/Keys/KeyProvider.cs); [KeyProviderPool.cs (mirror)](https://raw.githubusercontent.com/dlech/KeePass2.x/master/KeePassLib/Keys/KeyProviderPool.cs)
- **Verified (source).** `KeyPromptForm`:
  - public constructor; `InitEx(IOConnectionInfo, bool bCanExit, bool bRedirectActivation[, string strCustomTitle])`;
  - public `SecureDesktopMode` (get/set), `CompositeKey` (get only), `HasClosedWithExit` and `InvokeAfterClose`;
  - static `ShowDialog(IOConnectionInfo, bool, string, out KeyPromptFormResult)` is internal;
  - it calls `GlobalWindowManager.InitializeForm` in the constructor, `GlobalWindowManager.AddWindow` in OnFormLoad and `RemoveWindow` in OnFormClosed;
  - secure desktop is used when `Program.Config.Security.MasterKeyOnSecureDesktop` is set and `ProtectedDialog.IsSupported` is true, and the flag is passed to `KeyUtil.KeyFromUI`.

  — [KeyPromptForm.cs (mirror)](https://raw.githubusercontent.com/dlech/KeePass2.x/master/KeePass/Forms/KeyPromptForm.cs)
- **Verified (source).** `MainForm`:
  - `public void OpenDatabase(IOConnectionInfo ioConnection, CompositeKey cmpKey, bool bOpenLocal)`; when no key is given, it shows the KeyPromptForm in a loop up to `Program.Config.Security.MasterKeyTries`;
  - `public bool IsFileLocked(PwDocument ds)`, `public bool IsAtLeastOneFileOpen()`, `public void LockAllDocuments()`.

  — [MainForm_Functions.cs (mirror)](https://raw.githubusercontent.com/dlech/KeePass2.x/master/KeePass/Forms/MainForm_Functions.cs)
- **Verified (source).** Locking flow:
  - `LockAllDocuments` aborts if interaction is blocked, or if open sub-windows cannot be closed (`GlobalWindowManager.CanCloseAllWindows`);
  - it exits instead of locking if `AlwaysExitInsteadOfLocking` is set;
  - per document, `CloseDocument` raises the `ClosingDatabaseFilePre` trigger and the `FileClosingPre` event (cancellable, with a Locking flag), then the save prompt, then `FileClosingPost` (cancellable), then `FileClosed`, which also carries the locking flag.

  — [MainForm_Functions.cs (mirror)](https://raw.githubusercontent.com/dlech/KeePass2.x/master/KeePass/Forms/MainForm_Functions.cs)
- **Verified (source).** Session lock: MainForm installs `m_sessionLockNotifier` with `OnSessionLock`. That handler calls `LockAllDocuments()` in these cases, all under `Program.Config.Security.WorkspaceLocking`:
  - Lock, Ending or UserSwitch, when `LockOnSessionSwitch` is enabled;
  - Suspend, when `LockOnSuspend` is enabled;
  - RemoteControlChange, when `LockOnRemoteControlChange` is enabled.

  Other options: `LockAfterTime`, `ExitInsteadOfLockingAfterTime`, `LockOnWindowMinimize`. — [MainForm.cs](https://raw.githubusercontent.com/dlech/KeePass2.x/master/KeePass/Forms/MainForm.cs) and [MainForm_Functions.cs](https://raw.githubusercontent.com/dlech/KeePass2.x/master/KeePass/Forms/MainForm_Functions.cs) (mirror)
- **Verified.** Secure desktop option ('Tools' → 'Options' → 'Security') is "turned off by default for compatibility reasons". Locking "closes the database file" and keeps only its path and some view state. If a sub-dialog is open, the workspace may not be locked. — [keepass.info Security](https://keepass.info/help/base/security.html)
- **Verified.** `Environment.TickCount64` applies only to .NET Core 3.0+ and .NET 5+, not .NET Framework 4.8. On Windows in .NET 10 and earlier, "including .NET Framework", tick counts include sleep time. — [Microsoft Learn: Environment.TickCount64](https://learn.microsoft.com/en-us/dotnet/api/system.environment.tickcount64)
- **Verified.** Existing quick-unlock designs:
  - KeePassWinHello shows the Hello prompt "over the normal KeePass unlock prompt" after a lock, supports the secure desktop option (since v2.1, reworked v3.1), and expires after a configurable period (default 24 h);
  - WinHelloUnlock claims Secure Desktop support, stores data in PasswordVault, and its credential does not expire.

  — [KeePassWinHello README](https://github.com/sirAndros/KeePassWinHello); [KeePassWinHello releases](https://github.com/sirAndros/KeePassWinHello/releases); [WinHelloUnlock](https://github.com/lazou/WinHelloUnlock)

### Inferences
- Inferred: the hook pattern for quick unlock:
  1. Subscribe to `GlobalWindowManager.WindowAdded` (the event name is commonly used by plugins but its declaration was not read in this session).
  2. When the added form is a `KeyPromptForm`, run the Hello flow.
  3. On success, close the prompt with Cancel and call `MainForm.OpenDatabase(ioc, compositeKey, false)` with the reconstructed key. Alternatively, set the key through non-public members, which is fragile across KeePass versions.

  The `IOConnectionInfo` is not publicly readable from `KeyPromptForm` according to the members seen, so the plugin may need the locked document's `PwDocument.LockedIoc` or similar. This was not verified.
- Inferred: capture the composite key after a normal unlock with the `FileOpened` event and `PwDatabase.MasterKey`. The `FileOpened` declaration was not seen in the fetched excerpts, but `FileClosingPre/Post`, `FileClosed`, `FileCreated`, `FileSaving*` and `MasterKeyChanged` were. Wipe the cached key in `FileClosed` when locking (or always), and on `MasterKeyChanged`.
- Inferred: for "lock when Windows locks", either rely on KeePass's own `LockOnSessionSwitch` option (check and suggest enabling it), or also subscribe to `Microsoft.Win32.SystemEvents.SessionSwitch` and call `LockAllDocuments()` on `SessionLock`. Either way, also wipe the plugin's quick-unlock state. SystemEvents requires a message loop, which KeePass's UI thread provides.
- Inferred: for reboot detection on net48, use P/Invoke `GetTickCount64` (kernel32) to compute boot time as `UtcNow - uptime` with tolerance, or read the WMI `Win32_OperatingSystem.LastBootUpTime`. Neither was verified in this session. Simpler: keep the unlock secret only in memory (expires on KeePass exit). For persistence across KeePass restarts but not reboots, store a boot identifier with the ciphertext.
- Inferred: when "Enter master key on secure desktop" is on, the KeyPromptForm runs on a separate desktop where the Hello UI from the normal desktop may not be visible. The plugin should run Hello before KeePass switches desktops, or detect `SecureDesktopMode` and degrade gracefully, as KeePassWinHello reworked in v3.1.

### Gaps
- The `GlobalWindowManager.WindowAdded` declaration, the `FileOpened`/`UIStateUpdated` event declarations and `KeyProviderQueryContext` members (including `IsOnSecureDesktop`) were not read in this session. The source mirror (dlech/KeePass2.x) may lag behind 2.61.1. Verify against the official 2.61.1 source ZIP from keepass.info or SourceForge.
- The mirror files are © 2003-2025, so they are likely 2.60-era or earlier. Exact parity with 2.61.1 is unverified.

## Q6. KeePass 2.60 / 2.61 / 2.61.1 release notes; latest version as of 2026-10

### Takeaway
As of 2026-10-18, the latest KeePass 2.x on keepass.info is 2.61.1 (2026-05-01). None of 2.60, 2.61 or 2.61.1 lists plugin-API-breaking changes. The relevant items are secure-desktop focus fixes, a new update banner inside the master key prompt (2.61), ProtectedUserKey.bin handling (2.60), and Authenticode-signed binaries (2.61).

### Cited Findings
- **Verified.** Release dates: 2.57 (2024-06-01), 2.57.1 (2024-10-08), 2.58 (2025-03-04), 2.59 (2025-07-09), 2.60 (2025-11-02), 2.61 (2026-03-04), 2.61.1 (2026-05-01, latest listed). — [keepass.info News archive](https://keepass.info/news/news_all.html)
- **Verified.** 2.60 changes:
  - improved plugin loading error messages;
  - ShInstUtil gets an improved .NET check and native ARM64 NGen;
  - if `ProtectedUserKey.bin` (Windows user account key) cannot be decrypted, KeePass shows an error and no longer overwrites the file;
  - workaround for a focus bug after returning from the secure desktop;
  - configuration is saved immediately on system shutdown;
  - bugfixes: "(None)".

  — [KeePass 2.60 release notes](https://keepass.info/news/n251102_2.60.html)
- **Verified.** 2.61 changes:
  - the startup update check runs asynchronously before a database opens, and a banner appears in the master key prompt if an update is found;
  - switching to the secure desktop plays the UAC sound at system volume;
  - binaries are Authenticode-signed with published hashes and signatures;
  - improved TopMost and activation workarounds.

  2.61.1: a "better workaround for focus problems after returning from the secure desktop" and more reliable "Always on Top" at startup. — [KeePass 2.61 / 2.61.1 release notes](https://keepass.info/news/n260304_2.61.html)

### Inferences
- Inferred: the 2.61 update banner means the KeyPromptForm layout changed slightly. Plugins that overlay or inspect KeyPromptForm controls by position should be tested on 2.61.x.
- Inferred: repeated secure-desktop focus fixes in 2.60 to 2.61.1 suggest focus and foreground handling is fragile. Test the Hello prompt with and without secure desktop on 2.61.1.

### Gaps
- The minimum .NET Framework version required by KeePass 2.60/2.61 was not stated on the fetched pages. Check keepass.info's "Requirements" or download page.
- No KeePass 2.62 or later was listed as of the fetch date.

## Q7. Building without Visual Studio: csc.exe vs .NET SDK targeting net48; WinRT in SDK-style projects

### Takeaway
Building with the .NET SDK (`dotnet build`) works:
- target `net48`;
- add `Microsoft.NETFramework.ReferenceAssemblies` (v1.0.3, covers net20 to net481) so no targeting pack is needed;
- add `Microsoft.Windows.SDK.Contracts`, which needs PackageReference and is therefore natural in SDK-style projects.

Set `LangVersion` 5 only if a PLGX is also planned. The in-box .NET Framework `csc.exe` is limited to old C# and was not verified here.

### Cited Findings
- **Verified.** `Microsoft.NETFramework.ReferenceAssemblies`: latest 1.0.3 (2022-08-19), with per-framework packages for 2.0 through 4.8.1 including 4.8. No deprecation. — [NuGet: Microsoft.NETFramework.ReferenceAssemblies](https://www.nuget.org/packages/Microsoft.NETFramework.ReferenceAssemblies)
- **Verified.** The Contracts package requires PackageReference and is compatible with net461–net481. — [NuGet: Microsoft.Windows.SDK.Contracts](https://www.nuget.org/packages/Microsoft.Windows.SDK.Contracts)
- **Verified.** KeePass plugins must target .NET Framework (not Standard/Core). PLGX compilation supports at most C# 5, so set `LangVersion` to 5 in the project file. — [keepass.info Plugin Development](https://keepass.info/help/v2_dev/plg_index.html)
- **Verified.** The Windows TFM approach (`net8.0-windows10.0.x`) applies only to .NET 6+, and the NuGet package approach "applies to .NET Framework 4.x projects". — [Microsoft Learn: Call WinRT APIs in desktop apps](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/winrt-apis-desktop-apps)
- **Verified.** KeePassWinHello v3.3.1 was a hotfix for "broken compatibility due to new development tools" (#105). Toolchain changes can break KeePass plugin compatibility. — [KeePassWinHello releases](https://github.com/sirAndros/KeePassWinHello/releases)

### Inferences
- Inferred minimal SDK-style setup:
  - `<Project Sdk="Microsoft.NET.Sdk">`;
  - `<TargetFramework>net48</TargetFramework>`;
  - PackageReferences to `Microsoft.NETFramework.ReferenceAssemblies` (1.0.3, PrivateAssets=all) and `Microsoft.Windows.SDK.Contracts`, where the version sets the WinRT API surface: 10.0.19041.x for broad Windows 10 support, 10.0.26100.x or later if `RequestCreateForWindowAsync` is wanted;
  - `<Reference Include="KeePass" HintPath="…\KeePass.exe" Private="false" />`;
  - assembly attributes AssemblyTitle, AssemblyProduct = "KeePass Plugin", AssemblyFileVersion;
  - `GenerateAssemblyInfo` set to false if the attributes are hand-written.
- Inferred: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` is the legacy compiler limited to C# 5. It could compile a WinRT-referencing plugin by passing `/r:` to `Windows.winmd` and `System.Runtime.WindowsRuntime.dll`, but it needs a Windows SDK UnionMetadata folder installed and is harder to reproduce. Not tested.
- Inferred: the .NET SDK on Windows can build net48 class libraries; WinForms references (System.Windows.Forms) are available from the reference assemblies. Not tested in this session.

### Gaps
- Not verified: an end-to-end `dotnet build` of a net48 plugin referencing Contracts, and whether the resulting plugin loads in KeePass 2.61.1 without extra DLLs.
- Not verified: the C# language version limit of the in-box .NET Framework `csc.exe`, which is commonly reported as C# 5.
