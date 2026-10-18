# KeePassXC Windows Hello "Quick Unlock": design and user complaints

Research date: 2026-10-18. Scope: KeePassXC (GPL-2.0/3.0) Windows Hello Quick Unlock. These notes describe the design **in prose only**. No KeePassXC code is reproduced, and none should be copied into "Miftawerk Unlock".

Verification legend: **[V-src]** = read the source myself (shallow clone of `develop` at commit d608efc1, dated 2026-10-18, plus the `2.7.12` tag). **[V-page]** = read the issue/PR page. **[V-commit]** = read the commit message/diff via the GitHub API. **[Title-only]** = only the title and date were seen in GitHub search results, not the content.

Source shorthand:
- WH-src = https://github.com/keepassxreboot/keepassxc/blob/develop/src/quickunlock/WindowsHello.cpp
- WH-2.7.12 = https://github.com/keepassxreboot/keepassxc/blob/2.7.12/src/winhello/WindowsHello.cpp
- DOW-src = https://github.com/keepassxreboot/keepassxc/blob/develop/src/gui/DatabaseOpenWidget.cpp
- CHANGELOG = https://github.com/keepassxreboot/keepassxc/blob/develop/CHANGELOG.md
- UserGuide = https://github.com/keepassxreboot/keepassxc/blob/develop/docs/topics/DatabaseOperations.adoc (published as the "Quick Unlock" section of https://keepassxc.org/docs/KeePassXC_UserGuide)

---

## Q1. Design: Windows APIs, key derivation, cipher, storage, restart/reboot, timeouts, Windows lock

### Takeaway
KeePassXC uses the WinRT **KeyCredentialManager / KeyCredential** API (Windows Hello / "Passport"). It does not use UserConsentVerifier for the unlock itself. One app-wide Hello key pair named `keepassxc_winhello` is created on first use, or opened if it already exists. For each database, a random 16-byte challenge is signed with that key. The SHA-256 of the signature becomes an AES-256-GCM key, which encrypts the serialized composite key (the database's master key material), and the challenge doubles as the IV. The ciphertext lives **only in process memory**, in a hash keyed by the database. It is lost when KeePassXC exits, the database tab is closed, the database key changes, or the user presses Cancel/Esc on the quick-unlock screen. It survives a database lock, including a lock triggered by Windows screen lock. There is **no timeout/expiry**. Maintainers deliberately removed timeouts in 2022 and have repeatedly rejected requests to add them.

### Cited Findings

**API calls (what is used) [V-src]**
- Availability is checked with `KeyCredentialManager::IsSupportedAsync()`. In the `develop` refactor this check runs on every call, not once at startup. — [WH-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/quickunlock/WindowsHello.cpp); the original version cached it in the constructor — [commit f93adaa8](https://github.com/keepassxreboot/keepassxc/commit/f93adaa8)
- Credential setup tries `KeyCredentialManager::RequestCreateAsync(name, FailIfExists)`. If the status is `CredentialAlreadyExists`, it calls `KeyCredentialManager::OpenAsync(name)` instead. Any other non-success status is an error ("Failed to create Windows Hello credential"). The credential name is the fixed string `keepassxc_winhello`, so **all databases share one Hello key pair**. — [WH-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/quickunlock/WindowsHello.cpp)
- The user-facing prompt comes from `KeyCredential::RequestSignAsync(challenge)`. A `UserCanceled` status is treated as a silent failure with no error text. Other statuses produce "Failed to sign challenge using Windows Hello". WinRT `hresult_error` exceptions are caught and their message is shown. — [WH-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/quickunlock/WindowsHello.cpp)
- The file includes the `Userconsentverifierinterop.h` header, but no UserConsentVerifier call is made. The whole flow is KeyCredential-based. — [WH-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/quickunlock/WindowsHello.cpp)
- The async WinRT calls run on a worker via KeePassXC's own `AsyncTask::runAndWaitForFuture` helper, which keeps the Qt UI thread responsive. — [WH-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/quickunlock/WindowsHello.cpp)
- Maintainer rationale for the sign-based design: the Windows Hello API "only supports signing", not direct encrypt/decrypt, so the private key is used to sign and the prompt appears at initial encryption. (droidmonkey, 2022-08-14) — [#8365](https://github.com/keepassxreboot/keepassxc/issues/8365) [V-page]

**Key derivation and cipher [V-src, V-commit]**
- When quick unlock is set up (`setKey`), KeePassXC generates a random challenge with the same length as the AES-256-GCM IV. In KeePassXC's cipher wrapper, `defaultIvSize(Aes256_GCM)` returns **16 bytes**. Windows Hello signs the challenge, and **SHA-256(signature)** becomes the 32-byte AES key. The data is encrypted with **AES-256-GCM** using the challenge as the IV. The stored blob is the challenge (IV) prepended to the ciphertext and tag. — [WH-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/quickunlock/WindowsHello.cpp); [SymmetricCipher.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/crypto/SymmetricCipher.cpp)
- At unlock (`getKey`), the 16-byte challenge is split off the stored blob and signed again with Hello (a second prompt). The key is re-derived and the blob decrypted. GCM tag failure produces "Failed to decrypt key data". — [WH-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/quickunlock/WindowsHello.cpp)
- A source comment says "AES-256-CBC", but the actual mode is AES-256-GCM. The 2022 commit message also says GCM: "The signed challenge is hashed using SHA-256 and then used as the encryption key … Credentials are encrypted using AES-256/GCM." — [commit 4f071035 (2022-02-22)](https://github.com/keepassxreboot/keepassxc/commit/4f071035)
- **The design depends on deterministic signatures.** Signing the same challenge twice must give byte-identical output, or the re-derived key will not match. Microsoft documents that Windows Hello keys are **RSA** and that signatures use **SHA-256 with PKCS#1 (v1.5) padding** ("We are using SHA256 as the hash algorithm and Pkcs1 for SignaturePadding"). — [Microsoft Learn: Windows Hello (apps)](https://learn.microsoft.com/en-us/windows/apps/develop/security/windows-hello)
- RSASSA-PKCS1-v1_5 encoding (EMSA-PKCS1-v1_5) contains no random salt, unlike RSASSA-PSS, so for a fixed key and message the signature is deterministic. — [RFC 8017 §8.2 / §9.2](https://www.rfc-editor.org/rfc/rfc8017#section-8.2)
- Microsoft: the Hello private key is generated and protected by the TPM where available, with a software fallback otherwise. "An application can never use the keys from another application". Signing APIs "require explicit validation through a user gesture". A PIN reset (TPM lockout) removes all keys, and `OpenAsync` returning `NotFound` means "PIN reset has occurred somewhere else and key is lost." — [Microsoft Learn: Windows Hello (apps)](https://learn.microsoft.com/en-us/windows/apps/develop/security/windows-hello)
- `RequestSignAsync` is documented only as "Prompts the user to cryptographcally sign data using their key credential". It takes no window-handle parameter. — [Microsoft Learn: KeyCredential.RequestSignAsync](https://learn.microsoft.com/en-us/uwp/api/windows.security.credentials.keycredential.requestsignasync)

**What is encrypted [V-src]**
- The plaintext is KeePassXC's serialized **CompositeKey**. Each sub-key contributes its UUID and raw key: the *hashed* password key, the key-file raw key plus type plus file path, and, for challenge-response (YubiKey) keys, **only the slot identifier**. So a hardware key must still be present at quick-unlock time. — [CompositeKey.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/keys/CompositeKey.cpp), [PasswordKey.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/keys/PasswordKey.cpp), [FileKey.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/keys/FileKey.cpp), [ChallengeResponseKey.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/keys/ChallengeResponseKey.cpp); the User Guide also says a hardware key "must be connected to your computer to complete the unlock" — [UserGuide](https://github.com/keepassxreboot/keepassxc/blob/develop/docs/topics/DatabaseOperations.adoc)
- On unlock, the decrypted blob is loaded back as a raw composite key and the database is opened normally. — [DOW-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/gui/DatabaseOpenWidget.cpp)

**Where it is stored, and per-database identity [V-src]**
- The encrypted blob is held in an in-memory hash in a process-wide singleton. Nothing is written to disk, the registry, or the Windows Credential Manager. — [WindowsHello.h](https://github.com/keepassxreboot/keepassxc/blob/develop/src/quickunlock/WindowsHello.h), [QuickUnlockInterface.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/quickunlock/QuickUnlockInterface.cpp)
- **2.7.x (current stable 2.7.12)** keys the in-memory hash by **database file path**. — [WH-2.7.12](https://github.com/keepassxreboot/keepassxc/blob/2.7.12/src/winhello/WindowsHello.cpp)
- **`develop` / 2.8** keys it by a **public UUID** stored unencrypted in KDBX4 public custom data (`KPXC_PUBLIC_UUID`), which is created on first use. KDBX3 falls back to a SHA-256 of the file path. — [Database.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Database.cpp). The commit says the UUID "will eventually support persistent Quick Unlock." — [commit f93adaa8](https://github.com/keepassxreboot/keepassxc/commit/f93adaa8) [V-commit]

**Lifecycle (set, use, reset) [V-src unless noted]**
- **Setup:** after a *successful full-credential unlock*, if quick unlock is enabled and available, KeePassXC calls `setKey`, which shows the Hello prompt. If the user cancels, the database still opens and quick unlock is simply not armed. — [DOW-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/gui/DatabaseOpenWidget.cpp); the User Guide says "you will be prompted to authenticate to Windows Hello after unlocking your database with full credentials … If you cancel this prompt then Quick Unlock will not be enabled and your database will continue to unlock." — [UserGuide](https://github.com/keepassxreboot/keepassxc/blob/develop/docs/topics/DatabaseOperations.adoc)
- **Not armed** when the unlock came from the command line/AutoOpen (`enterKey` sets a block flag) or when the unlock was itself a quick unlock. — [DOW-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/gui/DatabaseOpenWidget.cpp); [commit 4f071035](https://github.com/keepassxreboot/keepassxc/commit/4f071035) ("Prevent quick unlock when using AutoOpen or opening from the command line")
- **Use:** when the unlock widget is shown or its window activates and a key exists for this database, a separate "quick unlock" page with a big Unlock button is displayed. Since 2.7.6 (2023-08-15) the Hello prompt is triggered automatically when the unlock dialog opens. — [DOW-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/gui/DatabaseOpenWidget.cpp); [CHANGELOG 2.7.6: "Automatically prompt for Quick Unlock when showing unlock dialog [#9697]"](https://github.com/keepassxreboot/keepassxc/blob/develop/CHANGELOG.md)
- **Reset triggers:** (a) Cancel button on the quick-unlock page, bound to **Esc**. (b) Closing the database tab (`DatabaseWidget::closeEvent`). (c) Changing the database key in Database Settings. (d) The widget being shown or activated while no key exists. (e) App exit, since memory is lost. — [DOW-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/gui/DatabaseOpenWidget.cpp), [DatabaseWidget.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/gui/DatabaseWidget.cpp), [DatabaseSettingsWidgetDatabaseKey.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/gui/dbsettings/DatabaseSettingsWidgetDatabaseKey.cpp). The 2022 commit says quick unlock "is automatically reset only when the database key is changed". — [commit 4f071035](https://github.com/keepassxreboot/keepassxc/commit/4f071035)
- In early 2023, a commit made KeePassXC reset quick unlock after an "unrecoverable" Hello error but not after a user cancel. — [commit b84d38e7 (2023-01-19)](https://github.com/keepassxreboot/keepassxc/commit/b84d38e7). In current `develop`, `buildDatabaseKey` only shows "Failed to authenticate with Quick Unlock: %1" and returns. I did not see an explicit reset there. — [DOW-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/gui/DatabaseOpenWidget.cpp)
- **Restart/reboot:** quick unlock is lost. Maintainer (2022-03-22): this is "by design: we don't remember your quick unlock if you close the database or keepassxc." — [#7570](https://github.com/keepassxreboot/keepassxc/issues/7570) [V-page]. Again on 2023-10-03: quick unlock "only applies to the current application session". — [#8365](https://github.com/keepassxreboot/keepassxc/issues/8365) [V-page]
- **Windows lock/screen lock:** the default setting `Security/LockDatabaseScreenLock = true` locks databases when the screen locks. Locking does not call the quick-unlock reset, so the encrypted blob survives and the next unlock offers Hello. — [Config.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Config.cpp); reset call sites in [DOW-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/gui/DatabaseOpenWidget.cpp)
- **Hibernate:** users report "Handle Invalid" errors from Hello after hibernation. The maintainer said this cannot be fixed while the auth data is kept in app memory and pointed to PR #11520. Closed as not planned (2026-03-20). — [#13171](https://github.com/keepassxreboot/keepassxc/issues/13171) [V-page]
- **Settings:** a single application-wide checkbox, "Enable database quick unlock (Touch ID / Windows Hello)". It is stored as `Security/QuickUnlock`, **default true**, in the *local* (non-roaming) config. The checkbox is greyed out when `isAvailable()` is false. — [Config.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Config.cpp), [ApplicationSettingsWidget.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/gui/ApplicationSettingsWidget.cpp). The User Guide says it "is enabled by default on Windows using _Windows Hello_". — [UserGuide](https://github.com/keepassxreboot/keepassxc/blob/develop/docs/topics/DatabaseOperations.adoc)
- **Timeouts:** none. The 2022 Windows Hello commit says it will "Remove all timeout features of Touch ID as they are unnecessary and complicate the feature for no security gain." — [commit 4f071035](https://github.com/keepassxreboot/keepassxc/commit/4f071035) [V-commit]
- **RDP:** Hello can be available at first and then become unavailable, for example inside a Remote Desktop session. `RequestCreateAsync` was moved into the try/catch to prevent a crash (#7890). — [commit b84d38e7](https://github.com/keepassxreboot/keepassxc/commit/b84d38e7); [CHANGELOG 2.7.5: "Fix crash when using Windows Hello in a Remote Desktop session [#9006]"](https://github.com/keepassxreboot/keepassxc/blob/develop/CHANGELOG.md)

**History of the Windows implementation [V-commit / CHANGELOG]**
- 2022-02-22: "Add support for Windows Hello" (PR #7384, credits @HexF and @smlu), shipped in 2.7.0. — [commit 4f071035](https://github.com/keepassxreboot/keepassxc/commit/4f071035); [CHANGELOG](https://github.com/keepassxreboot/keepassxc/blob/develop/CHANGELOG.md)
- 2022-08-12: "Fix Windows Hello bugs" (#8354), which added try/catch around signing (#7977) and a 3-retry focus loop (#8120). Shipped in 2.7.3 (2022-10-23). — [commit ed693e14](https://github.com/keepassxreboot/keepassxc/commit/ed693e14)
- 2023-01-19: "Properly handle Windows Hello errors" (RDP crash #7890, ignore UserCanceled). — [commit b84d38e7](https://github.com/keepassxreboot/keepassxc/commit/b84d38e7)
- 2023-09-05 (author date): Polkit refactor moved `src/winhello` to `src/quickunlock` behind a common `QuickUnlockInterface` and switched to public-UUID keying. This is on `develop`/2.8 only. 2.7.12 still has `src/winhello` with path keying. — [commit f93adaa8](https://github.com/keepassxreboot/keepassxc/commit/f93adaa8); [WH-2.7.12](https://github.com/keepassxreboot/keepassxc/blob/2.7.12/src/winhello/WindowsHello.cpp)
- The GitHub commits API lists no changes to `src/quickunlock/WindowsHello.cpp` after f93adaa8, as of `develop` on 2026-10-18. — [GitHub commits API for path](https://api.github.com/repos/keepassxreboot/keepassxc/commits?path=src/quickunlock/WindowsHello.cpp)

### Inferences
- Design pattern a clean-room plugin could reimplement independently: a Hello `KeyCredential` (RSA, PKCS#1 v1.5, TPM-backed) used as a deterministic keyed PRF via sign(challenge), then a hash, then a symmetric key that wraps the master key with AEAD. The technique is standard and not KeePassXC-specific, but the plugin should implement it from Microsoft docs, not from this code.
- The scheme is only as stable as the signature. If Windows ever switched Hello keys to a randomized padding (PSS), or the Hello key is replaced (PIN reset, re-enrollment, TPM clear), the derived key changes and all wrapped blobs become undecryptable. This is harmless for in-memory blobs but matters for any *persisted* blob, which would need a full-password fallback and a re-wrap path.
- A single shared credential name for all databases means one Hello key protects every database's wrapped key. Per-database separation comes only from different random challenges.
- Using a 16-byte challenge as the GCM nonce works but is non-standard (GCM's natural nonce is 12 bytes). A new plugin can choose its own parameters, such as a separate random challenge and a 12-byte nonce, plus associated data binding the database ID.
- Because quick unlock survives screen lock but not app restart, the KeePassXC model is "re-auth by biometric for the lifetime of the process". That is similar in spirit to KeePass 2's existing WinKeyProvider plugins. Persistence is the main delta users ask for (see Q3).

### Gaps
- I did not find Microsoft documentation that explicitly *guarantees* deterministic signatures for KeyCredential. Determinism is inferred from the documented PKCS#1 (v1.5) padding plus RFC 8017.
- Whether KeyCredential keys are scoped per calling executable for unpackaged Win32 apps (relevant to a plugin running inside KeePass.exe) was not verified. Microsoft only says "An application can never use the keys from another application."
- I did not read PR #7384's review thread (title/number only via CHANGELOG and commit).

---

## Q2. Bringing the Hello prompt to the foreground / owner window on Windows 11

### Takeaway
KeePassXC passes no owner window (KeyCredential APIs take no HWND). Instead it uses a **timer hack**: 500 ms after starting a Hello operation, it looks up a top-level window by the class name "Credential Dialog Xaml Host" and calls `SetForegroundWindow` on it. If the window is not found, it retries up to 3 more times at 500 ms intervals (about 2 s total), then gives up. It also stopped making the unlock dialog "always on top" so Hello can appear above it. Users on Windows 10 and 11 keep reporting the prompt stuck behind the window. Maintainers close these reports as not reproducible or blame Hello enrollment.

### Cited Findings
- The focus routine waits ~500 ms, finds the window with class "Credential Dialog Xaml Host", foregrounds it, and otherwise retries (3 retries added in 2022-08). It runs before both setKey and getKey. — [WH-src](https://github.com/keepassxreboot/keepassxc/blob/develop/src/quickunlock/WindowsHello.cpp) [V-src]; [commit ed693e14: "try 3 times to bring Windows Hello prompt to front … older machines that are slow to bring up the prompt window"](https://github.com/keepassxreboot/keepassxc/commit/ed693e14) [V-commit]
- The original commit made the unlock dialog *not* always-on-top "to allow Touch ID and Windows Hello prompts to appear above the dialog properly." — [commit 4f071035](https://github.com/keepassxreboot/keepassxc/commit/4f071035) [V-commit]
- Maintainer explanation (2024): "we do attempt to move the fingerprint window to the front. If it takes longer than 2 seconds to appear for you, then we will give up trying to find it". The failure could be "either timing or Microsoft renamed the unlock dialog so we cannot find it anymore with our hack." The issue (2.7.6, Windows 11 23H2; repro after screen lock via the browser extension) was closed as not planned on 2025-02-09. — [#10175 (opened 2024-01-11)](https://github.com/keepassxreboot/keepassxc/issues/10175) [V-page]
- Windows 11 24H2 / 2.7.10: the prompt stays behind KeePassXC and flashes in the taskbar when "reopen database" is triggered from the browser. Fingerprint doesn't work until the prompt is focused, and it sometimes appears on another monitor. "Always on Top" helped partially. The maintainer replied: "We already move the hello dialog to the front and try to do so many times." Closed as not planned on 2025-07-06. — [#11864 (2025-03-10)](https://github.com/keepassxreboot/keepassxc/issues/11864) [V-page]
- After a wrong PIN, the Hello prompt drops behind the KeePassXC window and the app looks hung. The maintainer had "noticed the issue too". The issue is still **open**, with its milestone moved to v2.9.0 on 2026-09-13. — [#9785 (2023-08-23)](https://github.com/keepassxreboot/keepassxc/issues/9785) [V-page]
- "Unlock Database" button doesn't always bring up the Hello PIN dialog, which led to the 3-retry fix. — [#8120 (2022-05-27)](https://github.com/keepassxreboot/keepassxc/issues/8120) [Title-only; fix confirmed via commit ed693e14]
- Microsoft offers a window-parented *UserConsentVerifier* interop, `IUserConsentVerifierInterop::RequestVerificationForWindowAsync(HWND, …)`, which requires Windows build 22000 or later. It returns only verified/not-verified and gives no signature. — [Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/userconsentverifierinterop/nf-userconsentverifierinterop-iuserconsentverifierinterop-requestverificationforwindowasync)
- A developer forum report says KeyCredentialManager popups from non-UWP apps appear behind the app, while `UserConsentVerifier` does not. This is an unofficial forum claim. — [1Password Community thread](https://1password.community/discussion/122361/security-prompts) (via search snippet; not independently verified)

### Inferences
- A KeePass 2 plugin using KeyCredential will probably hit the same z-order problem. KeePass's own key prompt is modal and may be TopMost, so the plugin should plan for a foreground workaround (finding the Hello host window and foregrounding it, `AllowSetForegroundWindow`, temporarily dropping TopMost from the KeePass key dialog), or check for an HWND-aware interop for KeyCredentialManager in newer SDKs. The window class name KeePassXC targets is an undocumented Windows detail that could change.
- A hybrid (UserConsentVerifier for-window as the gesture, plus a key that is not Hello-bound) would fix z-order but loses the cryptographic binding to the Hello key. That is a security trade-off to document, not a free win.

### Gaps
- I did not find an official Microsoft HWND-based interop for `KeyCredentialManager`/`RequestSignAsync`. Its existence in recent SDKs is unverified.
- No root cause for the Windows 11 z-order failures has been confirmed by KeePassXC or Microsoft.

---

## Q3. Complaints and feature requests (grouped by theme)

### Takeaway
I identified roughly **70** Windows-relevant quick-unlock and Windows Hello issues or PRs from 2018 to 2026 (GitHub search over issues/PRs; Discussions not searched). They fall into seven themes:
- Users who believe cancelling the setup prompt is a "bypass": the largest cluster, about 14.
- Requests to persist quick unlock across restart, or to add a PIN: about 12. The main open item is PR #11520, now slipped to v2.9.0.
- Prompt hidden behind the window: about 7.
- Requests for timeouts/expiry: about 7, all rejected.
- Default-on, greyed-out-setting and enterprise-deployment friction: about 8.
- RDP, hibernate and crash errors: about 12.
- Hello-method and UX wishes: about 10.

Counts are approximate. They are based on titles from GitHub search, with key items read in full.

### Cited Findings

**Theme A. "Bypass": cancelling the Hello prompt still opens the DB (misunderstood setup prompt), about 14 issues**
- #7886 "Insecure quick unlock feature using Windows Hello" (2022-04-13, 2.7.1). The maintainer asked, "How do you bypass authentication by providing your credentials?" phoerious (2022-04-14) said "The first prompt keeps confusing users". — [#7886](https://github.com/keepassxreboot/keepassxc/issues/7886) [V-page]
- #8365 "Trivial Bypass for Windows Security popup" (2022-08-14), closed as not planned. The maintainer said quick unlock "will still be on by default." — [#8365](https://github.com/keepassxreboot/keepassxc/issues/8365) [V-page]
- #12376 "Potential Security Vulnerability in Windows Hello Unlock" (2025-08-11). Maintainer: "The windows hello dialog is NOT protecting your database." A community member asked for clearer wording. — [#12376](https://github.com/keepassxreboot/keepassxc/issues/12376) [V-page]
- #12870 "Windows Hello only shown after I enter with full login. Quick Login never available." (2025-12-24), resolved as a cancelled first prompt or a Store-build issue. — [#12870](https://github.com/keepassxreboot/keepassxc/issues/12870) [V-page]
- Title-only, same theme: [#8141](https://github.com/keepassxreboot/keepassxc/issues/8141) "Windows Hello will Unlock Database when Pressing Cancel" (2022-06-12); [#8344](https://github.com/keepassxreboot/keepassxc/issues/8344) "KeePassXC ignore Windows Security" (2022-08-09); [#8422](https://github.com/keepassxreboot/keepassxc/issues/8422) (2022-08-29); [#8562](https://github.com/keepassxreboot/keepassxc/issues/8562) (2022-10-10); [#8883](https://github.com/keepassxreboot/keepassxc/issues/8883) "2.7.4 Windows Hello bypass" (2022-12-11); [#8897](https://github.com/keepassxreboot/keepassxc/issues/8897) "'Making sure it's you' can be skipped" (2022-12-17); [#9405](https://github.com/keepassxreboot/keepassxc/issues/9405) "Windows Security not respected" (2023-05-08); [#9533](https://github.com/keepassxreboot/keepassxc/issues/9533) "Bypass biometrics by killing the window" (2023-06-06); [#9749](https://github.com/keepassxreboot/keepassxc/issues/9749) "Able to bypass 2FA on 2.7.5" (2023-08-16); [#10042](https://github.com/keepassxreboot/keepassxc/issues/10042) "Bypass of windows hello auth" (2023-11-20); [#10646](https://github.com/keepassxreboot/keepassxc/issues/10646) "Windows Hello dialog can be easily skipped" (2024-04-30). — [GitHub issue search](https://github.com/keepassxreboot/keepassxc/issues?q=%22windows+hello%22)

**Theme B. Not available after restart / requests to persist across restart, full fingerprint unlock, or PIN, about 12**
- #7570 "Windows Hello verification window does not appear upon first time unlocking after start" (2022-03-22, 2.7.0, Win 10.0.22579). "By design". In December 2025, phoerious said passwordless unlock would not come "until 2.8 comes out". — [#7570](https://github.com/keepassxreboot/keepassxc/issues/7570) [V-page]
- #7020 "Optional FULL unlock using fingerprint" (opened 2021-10-05, **open**, labelled high priority). Roughly 70+ timeline items. Milestone moved from v2.8.0 to **v2.9.0 on 2026-09-13**. Duplicate #12806 (2025-12-10). Maintainer positions:
  - phoerious (2022-05-02) opposed it ("easy to steal and impossible to change").
  - droidmonkey (2022-05-02) planned to store the encrypted payload in the OS credential store.
  - droidmonkey (2023-01-18): Hello "is not a passwordless workflow and does not move with you between machines".
  - droidmonkey (2025-12-07): "This feature isnt released yet".
  — [#7020](https://github.com/keepassxreboot/keepassxc/issues/7020) [V-page]
- PR #11520 "Add Pin Quick Unlock and Remember Quick Unlock for Win/Mac" (droidmonkey, opened 2024-12-02, **open**, milestone v2.9.0, closes #7020):
  - "Remember Quick Unlock" persists the encrypted credentials in the Windows Credential Store / macOS Keychain. AI review comments say PasswordVault on Windows, which is unconfirmed.
  - It adds a PIN fallback and changes the setting to "Use Quick Unlock by default".
  - phoerious requested changes (2025-01-04) and said that in critical environments "you definitely don't want to have quick unlock".
  - A contributor (2025-01-18) called the PIN feature "purely a UI level protection". droidmonkey (2025-07-20) called that "completely false".
  - No expiry setting is described.
  — [PR #11520](https://github.com/keepassxreboot/keepassxc/pull/11520) [V-page]
- PR #13564 "Add persistent Windows Hello unlock prototype" (tqhaas, 2026-08-04): empty description, closed the same day, no maintainer comments. PR #13565 "Port remembered Windows Quick Unlock to Qt 6" (2026-08-04, closed) is title-only. — [#13564](https://github.com/keepassxreboot/keepassxc/pull/13564) [V-page]; [#13565](https://github.com/keepassxreboot/keepassxc/pull/13565) [Title-only]
- Title-only, same theme: [#8547](https://github.com/keepassxreboot/keepassxc/issues/8547) "Use Windows Hello for Initial Unlock" (2022-10-06); [#9211](https://github.com/keepassxreboot/keepassxc/issues/9211) "Add QuickUnlock using pin" (2023-03-08, open); [#11190](https://github.com/keepassxreboot/keepassxc/issues/11190) "Reopening Database with PIN after automatically closed" (2024-08-21); [#13152](https://github.com/keepassxreboot/keepassxc/issues/13152) "Quick unlock with pin code" (2026-03-15); [#13637](https://github.com/keepassxreboot/keepassxc/issues/13637) "Biometric/Windows Hello-gated keyfile storage (app-managed, encrypted at rest)" (2026-09-02); [PR #13558](https://github.com/keepassxreboot/keepassxc/pull/13558) "docs: document PIN Quick Unlock fallback" (2026-08-01, closed).

**Theme C. Prompt hidden behind KeePassXC / not appearing, about 7** (details in Q2)
- [#8120](https://github.com/keepassxreboot/keepassxc/issues/8120) (2022-05-27, fixed by retries) [Title-only + commit]; [#9785](https://github.com/keepassxreboot/keepassxc/issues/9785) (2023-08-23, open) [V-page]; [#10175](https://github.com/keepassxreboot/keepassxc/issues/10175) (2024-01-11) [V-page]; [#11864](https://github.com/keepassxreboot/keepassxc/issues/11864) (2025-03-10, Win 11 24H2) [V-page].
- Title-only: [#7925](https://github.com/keepassxreboot/keepassxc/issues/7925) "Windows Security (Key Confirmation) window below KeePassXC" (2022-04-20); [#8831](https://github.com/keepassxreboot/keepassxc/issues/8831) "Windows Hello prompt does not appear after hibernate" (2022-11-22); [#9789](https://github.com/keepassxreboot/keepassxc/issues/9789) "Windows Hello not doing anything" (2023-08-25).

**Theme D. Timeout / expiry / usage-limit requests (all rejected), about 7**
- #8013 "Usage Limit for Quick Unlock" (2022-05-05). droidmonkey: "This just appears counter to usability" (2022-05-05). Per-database limits would need unencrypted public data (2024-10-14). "I've decided to not implement this" (2025-06-15). Closed as not planned. — [#8013](https://github.com/keepassxreboot/keepassxc/issues/8013) [V-page]
- #11362 "Quick Unlock expiration, configurable" (2024-10-14), 1–7 days requested. "We have chosen to not implement this until 'remember quick unlock' is implemented." — [#11362](https://github.com/keepassxreboot/keepassxc/issues/11362) [V-page]
- #12853 "Add configurable timeout to 'forget' Quick Unlock / Windows Hello" (2025-12-20). "We have rejected this request in the past." — [#12853](https://github.com/keepassxreboot/keepassxc/issues/12853) [V-page]
- Title-only: [#12863](https://github.com/keepassxreboot/keepassxc/issues/12863) "Setting to automatically lock the database after first unlock (to prime Quick Unlock)" (2025-12-23); [#9535](https://github.com/keepassxreboot/keepassxc/issues/9535) "Windows Hello prompt within short amount of time for different database unlockings" (2023-06-06); [#8027](https://github.com/keepassxreboot/keepassxc/issues/8027) "Windows Hello Quick Unlock - multiple databases in once" (2022-05-09); [#8088](https://github.com/keepassxreboot/keepassxc/issues/8088) "unlock all dbs at once with pin" (2022-05-12).

**Theme E. Default-on, greyed-out setting, enterprise/GPO deployment, about 8**
- #10369 "Windows Hello prompt still appears even though application settings thinks it is unavailable" (2024-03-10, **open**):
  - The checkbox is checked and greyed out, so it cannot be disabled in the GUI.
  - An enterprise deployer with hundreds of PCs said they were dropping KeePassXC.
  - Workaround: `[Security] QuickUnlock=false` in keepassxc.ini. A user noted the Local vs Roaming AppData .ini confusion.
  - droidmonkey (2024-03-11): "we'll fix it". Last comment 2026-07-13 ("could be a disaster in remote control scenario").
  — [#10369](https://github.com/keepassxreboot/keepassxc/issues/10369) [V-page]
- Title-only: [#7646](https://github.com/keepassxreboot/keepassxc/issues/7646) "Upgrading to 2.7.0 enables Quick Unlock without asking or notifying" (2022-03-25); [#9554](https://github.com/keepassxreboot/keepassxc/issues/9554) "Always show Quick Unlock settings checkbox on Windows and MacOS" (2023-06-14); [#9800](https://github.com/keepassxreboot/keepassxc/issues/9800) "'Enable database quick unlock' is greyed out" (2023-08-28); [#10829](https://github.com/keepassxreboot/keepassxc/issues/10829) "QuickUnlock feature is broken in 2.7.8 (availability detection)" (2024-05-29); [PR #11223](https://github.com/keepassxreboot/keepassxc/pull/11223) "Add support for configuration overrides via GPOs in Windows" (2024-09-01, open); [#2189](https://github.com/keepassxreboot/keepassxc/issues/2189) "allow enforced defaults from the Windows registry" (2018-08-07, open); [#8150](https://github.com/keepassxreboot/keepassxc/issues/8150) "Force to use Windows Hello" (2022-06-14).

**Theme F. RDP / hibernate / errors / crashes, about 12**
- RDP crash [#7890](https://github.com/keepassxreboot/keepassxc/issues/7890) (2022-04-14), fixed via [commit b84d38e7](https://github.com/keepassxreboot/keepassxc/commit/b84d38e7) and [PR #9006](https://github.com/keepassxreboot/keepassxc/pull/9006) (2.7.5) [V-commit].
- Crash when providing fingerprint [#7977](https://github.com/keepassxreboot/keepassxc/issues/7977) (2022-04-27), fixed in [commit ed693e14](https://github.com/keepassxreboot/keepassxc/commit/ed693e14) [V-commit].
- #13171 "Handle Invalid" after hibernate (2026-03-20, 2.7.11, Win 11 26200), not planned and deferred to #11520. — [#13171](https://github.com/keepassxreboot/keepassxc/issues/13171) [V-page]
- #13609 "KeePassXC non-interactive on RDS hosts" (2026-08-21). The reporter notes "Microsoft blocks Windows Hello on RDS hosts", but the bug was not tied to quick unlock. — [#13609](https://github.com/keepassxreboot/keepassxc/issues/13609) [V-page]
- Title-only: [#8313](https://github.com/keepassxreboot/keepassxc/issues/8313) crash unlocking with Hello (2022-07-28); [#8799](https://github.com/keepassxreboot/keepassxc/issues/8799) invalid memory reference (2022-11-16); [#10125](https://github.com/keepassxreboot/keepassxc/issues/10125) "does not finish setup in Windows 11" (2023-12-24); [#11764](https://github.com/keepassxreboot/keepassxc/issues/11764) untranslated error text (2025-02-16); [#12012](https://github.com/keepassxreboot/keepassxc/issues/12012) "2.7.10 timeouts unlocking database if windows hello and yubikey used" (2025-04-14); [#13595](https://github.com/keepassxreboot/keepassxc/issues/13595) "Windows Hello unlock no longer works" (2026-08-17); [#13707](https://github.com/keepassxreboot/keepassxc/issues/13707) "Windows Hello / TPM fails repeatedly" (2026-09-25); [#9703](https://github.com/keepassxreboot/keepassxc/issues/9703) "Quick Unlock makes opening database slow" (2023-08-06).

**Theme G. Hello method / PIN-vs-biometric / UX wishes, about 10**
- #13734 "Preferred Windows Hello authentication method for Quick Unlock" (2026-10-01). Maintainer: "We cannot dictate which to use through the api for windows hello", and "The dialog defaults to what you last used and/or what you used to unlock your computer with." Closed as External. — [#13734](https://github.com/keepassxreboot/keepassxc/issues/13734) [V-page]
- PIN-fallback concern: the #12853 requester argued a short Hello PIN is weaker than the master password, so access should expire. The request was rejected. — [#12853](https://github.com/keepassxreboot/keepassxc/issues/12853) [V-page]
- Title-only: [#8051](https://github.com/keepassxreboot/keepassxc/issues/8051) "Make User presence check with Windows Hello optional/configurable" (2022-05-12); [#11732](https://github.com/keepassxreboot/keepassxc/issues/11732) "Automatic unlocking of Database when using fingerprint Sensor" (2025-02-05); [#13155](https://github.com/keepassxreboot/keepassxc/issues/13155) "automatically unlock using fingerprint without need to click Unlock" (2026-03-16); [#7892](https://github.com/keepassxreboot/keepassxc/issues/7892) "Enable escape key to cancel Quick Unlock" (2022-04-14); [#12840](https://github.com/keepassxreboot/keepassxc/issues/12840) "Esc breaks Hello authenticated session" (2025-12-18); [#9834](https://github.com/keepassxreboot/keepassxc/issues/9834) "Initial database unlock triggers unnecessary Windows Hello prompt" (2023-09-08); [#13687](https://github.com/keepassxreboot/keepassxc/issues/13687) browser-extension PIN prompt oddity (2026-09-17); [#11655](https://github.com/keepassxreboot/keepassxc/issues/11655) "Unlock using Windows Domain account" (2025-01-13).

### Inferences
- The most frequent "complaint" is UX confusion about the setup prompt that appears *after* a full-password unlock. A new plugin should make enrollment explicit, with an opt-in per database and wording such as "Enable Windows Hello for this database?", rather than a surprise prompt.
- Persistence across restart is the most-requested capability. KeePassXC has had it in an open PR since 2024-12 and keeps postponing it, now to v2.9.0. Persisted, Hello-wrapped keys (for example, in the Windows Credential Manager or a DPAPI-protected file) are an opening for a KeePass 2 plugin, but would need a clear threat-model statement plus invalidation and expiry options. KeePassXC users ask for expiry and KeePassXC refuses it.
- Enterprise admins want a policy and registry way to force quick unlock off. A plugin could read an HKLM policy value.
- Focus/z-order is a persistent Windows 10/11 pain point that a plugin must budget for.

### Gaps
- **GitHub Discussions were not searched.** The GitHub search API was rate-limited (shared IP), so counts cover Issues/PRs only.
- Title-only items were not opened, so their content and maintainer response are unverified. Counts are approximate; some issues touch several themes, and macOS/Linux-only ones were excluded where obvious.
- Reaction counts were not systematically collected.

---

## Q4. What maintainers say about quick unlock security (docs, FAQ, statements)

### Takeaway
Official docs present quick unlock as storing credentials "securely" for biometric re-unlock and stress that the prompt happens after a full unlock. Maintainers frame it as a **convenience feature that does not protect the database**. The real protection is the master credential; Hello only gates re-entry during the current session. They are split on persistence and PIN fallback: droidmonkey favours it (PR #11520), while phoerious warns against quick unlock in critical environments and against biometric-only unlock.

### Cited Findings
- User Guide: "your credentials can be securely stored to enable subsequent unlocking of your database through biometric authentication. This is enabled by default on Windows using _Windows Hello_ …" It also notes the post-unlock prompt and that cancelling leaves quick unlock off. — [UserGuide](https://github.com/keepassxreboot/keepassxc/blob/develop/docs/topics/DatabaseOperations.adoc) [V-src]
- droidmonkey (2025-08-12): "The windows hello dialog is NOT protecting your database". — [#12376](https://github.com/keepassxreboot/keepassxc/issues/12376) [V-page]
- droidmonkey (2022-04-13): cancelling means quick unlock was never set up, and the user must re-enter credentials "as if it was the first time opening the database". — [#7886](https://github.com/keepassxreboot/keepassxc/issues/7886) [V-page]
- Commit (2022-02-22): the design "ensures the database password can only be decrypted following a successful authentication with Windows Hello"; timeouts were removed "for no security gain". — [commit 4f071035](https://github.com/keepassxreboot/keepassxc/commit/4f071035) [V-commit]
- phoerious (2022-05-02): biometrics are "easy to steal and impossible to change", and a keychain-encrypted payload would not transfer between devices. — [#7020](https://github.com/keepassxreboot/keepassxc/issues/7020) [V-page]
- droidmonkey (2023-01-18): Windows Hello "is not a passwordless workflow and does not move with you between machines"; he recommends YubiKeys or key files instead. — [#7020](https://github.com/keepassxreboot/keepassxc/issues/7020) [V-page]
- phoerious (2025-01-04, PR review): in critical environments "you definitely don't want to have quick unlock". — [PR #11520](https://github.com/keepassxreboot/keepassxc/pull/11520) [V-page]
- droidmonkey (2022-05-05) on usage limits: "This just appears counter to usability". — [#8013](https://github.com/keepassxreboot/keepassxc/issues/8013) [V-page]
- droidmonkey (2025-12-07) on the Linux design: the master key is kept in the persistent keyring encrypted with the user's PIN; fingerprint works until restart, then the PIN is needed again. — [#7020](https://github.com/keepassxreboot/keepassxc/issues/7020) [V-page]

### Inferences
- KeePassXC's implicit threat model: an attacker with a locked KeePassXC session on an unlocked or unattended PC must pass Hello to get the master key. Hello does not defend against an attacker who already has the master password, and it does not defend against same-user malware that reads process memory (the encrypted blob and, after unlock, the decrypted DB are both in memory). A plugin should state its threat model explicitly, and must avoid "most secure" claims.

### Gaps
- I did not find a dedicated FAQ entry on keepassxc.org about quick unlock security. The User Guide section was the only official doc found.
- No formal, written threat model for quick unlock exists in the repo docs, as far as I found.

---

## Q5. Latest KeePassXC version and 2025–2026 changes to quick unlock

### Takeaway
The latest **stable** release is **2.7.12 (2026-03-10)**, and the latest pre-release is **2.8.0-beta1 (2026-09-23)**. 2.8.0-beta1 adds quick unlock on Linux (Polkit) and a macOS device-password fallback. It carries the Windows refactor (public-UUID keying), but brings **no new Windows Hello behaviour**: no persistence, no PIN. Persistent "Remember Quick Unlock" plus PIN (PR #11520) was moved to the **v2.9.0** milestone on 2026-09-13.

### Cited Findings
- GitHub "latest release" is tag 2.7.12, published 2026-03-10T01:16Z. — [GitHub releases API](https://api.github.com/repos/keepassxreboot/keepassxc/releases/latest) [V]
- The CHANGELOG lists "2.8.0-beta1 (2026-09-23)" with "Linux: Add Quick Unlock using Polkit [#8983]", "macOS: Allow Quick Unlock using the device password when Touch ID is unavailable [#11410]", and "Windows: Add native Arm64 builds [#13678]". There are no Windows Hello entries. — [CHANGELOG](https://github.com/keepassxreboot/keepassxc/blob/develop/CHANGELOG.md) [V-src]
- 2.7.11 (2025-11-23) and 2.7.12 (2026-03-10) changelog sections contain no quick-unlock or Windows Hello entries. — [CHANGELOG](https://github.com/keepassxreboot/keepassxc/blob/develop/CHANGELOG.md) [V-src]
- PR #11520 and issue #7020 milestones moved v2.8.0 → v2.9.0 on 2026-09-13. — [PR #11520](https://github.com/keepassxreboot/keepassxc/pull/11520), [#7020](https://github.com/keepassxreboot/keepassxc/issues/7020) [V-page]
- 2025–2026 Windows Hello issue activity: #11864 (2025-03), #12376 (2025-08), #12853/#12870 (2025-12), #13171 (2026-03), #13595/#13609 (2026-08), #13707 (2026-09), #13734 (2026-10). Persistent-unlock community PRs #13564/#13565 (2026-08-04) were closed. — see Q3 links.
- The `develop` Windows Hello implementation file has had no commits since the 2023 Polkit refactor, per the GitHub commits API. — [commits API](https://api.github.com/repos/keepassxreboot/keepassxc/commits?path=src/quickunlock/WindowsHello.cpp)

### Inferences
- In 2.8.0 (when final), Windows users still lose quick unlock on every restart, reboot or database close. The earliest plausible KeePassXC release with persistent Windows Hello unlock is 2.9.0, with no date. KeePass 2 users also lack this today, which is the opportunity for "Miftawerk Unlock".

### Gaps
- 2.8.0 final release date unknown (beta only, as of 2026-10-18).
- I could not confirm the exact storage design of PR #11520 on Windows. The PR text says "Windows Credential Store", and AI review comments say "PasswordVault". The code was not read.
