# YubiKey HMAC-SHA1 Challenge-Response: KeePassXC format and KeePass 2 compatibility (research notes, 2026-10-18)

Legend: **[VERIFIED-SRC]** = read directly in source code or official docs during this session (Oct 2026, `develop` branch of KeePassXC; KeePass 2 source via the dlech/KeePass2.x GitHub mirror, copyright header 2003-2025). **[INFERRED]** = my reasoning from verified facts, which needs a test against real files before relying on it. KeePassXC is GPL-2/3 and KeePass 2 is GPL-2+. Everything below describes behavior in my own words. No code is copied.

## Q1. KeePassXC format: exact challenge, slot/mode, padding, and how the response enters the key

### Takeaway
KeePassXC sends the YubiKey an HMAC-SHA1 challenge (slot 1 or 2), padded to 64 bytes with PKCS#7-style padding, and keeps the 20-byte response. **In KDBX 4 the challenge is the KDF seed** (the Argon2 salt or AES-KDF seed from the KdfParameters header field). SHA-256 of the response is appended as the **last** component of the composite-key SHA-256, before the KDF runs. **In KDBX 3.1 the challenge is the 32-byte master seed.** There the response hash is inserted into the *final* key hash, between the master seed and the transformed key. That is a non-standard change to the KDBX3 cipher-key derivation.

### Cited Findings
**Key component and serialization (src/keys/ChallengeResponseKey.cpp)**
- [VERIFIED-SRC] The `ChallengeResponseKey` class has UUID `e092495c-e77d-498b-84a1-05ae0d955508`. It passes the challenge bytes to the YubiKey driver unchanged, with no hashing. Its raw key is exactly the bytes the driver returns. When it is serialized (for KeePassXC's own "remember key" settings), it stores only the UUID and the slot, never any key material. A failed challenge clears the key and records the driver's error message. — [ChallengeResponseKey.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/keys/ChallengeResponseKey.cpp)

**Composite key (src/keys/CompositeKey.cpp)**
- [VERIFIED-SRC] The composite raw key is SHA-256 over every "static" key component (password, key file, and so on) in insertion order. If a seed is supplied, one more 32-byte value is appended at the end. That value is itself SHA-256 over the raw responses of all challenge-response keys, in insertion order. With one YubiKey this is SHA-256(20-byte response). Challenge-response keys always come after the static keys. With no challenge-response keys, nothing is appended. — [CompositeKey.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/keys/CompositeKey.cpp)
- [VERIFIED-SRC] In `CompositeKey::transform`:
  - If the KDF UUID is the legacy KDBX3 AES one, the raw key is computed **without** a seed, so no challenge-response component is included. A source comment says it is added later, for KDBX3.
  - For every other KDF, the KDF's own seed (`kdf.seed()`, asserted non-empty) is the challenge. The resulting composite hash, which includes SHA-256(response), is then fed to the KDF transform. — [CompositeKey.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/keys/CompositeKey.cpp)

**KDF UUIDs (src/format/KeePass2.cpp, src/crypto/kdf/AesKdf.cpp)**
- [VERIFIED-SRC] KDF UUID constants:
  - AES-KDBX3: `c9d9f39a-628a-4460-bf74-0d08c18a4fea`
  - AES-KDBX4 (KeePassXC-internal): `7c02bb82-79a7-4ac0-927d-114a00648238`
  - Argon2d: `ef636ddf-8c29-444b-91f7-a9a403e30a0c`
  - Argon2id: `9e298b19-56db-4773-b23d-fc3ec6f0a1e6`
- [VERIFIED-SRC] When KeePassXC reads a KDBX4 header whose AES UUID is c9d9f39a…, it upgrades it to the internal KDBX4 AES variant. **When writing, AES-KDF always writes the c9d9f39a… UUID** ("for compatibility with other applications"). So a KDBX4 + AES-KDF file looks standard to KeePass 2, but KeePassXC still treats it as KDBX4 for challenge-response: the KDF seed is the challenge and the response goes into the composite key. — [KeePass2.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/format/KeePass2.cpp); [AesKdf.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/crypto/kdf/AesKdf.cpp)

**KDBX4 read and write (Kdbx4Reader.cpp, Kdbx4Writer.cpp)**
- [VERIFIED-SRC] KDBX4 read: `setKey(key, false, false)` transforms the composite key using the stored KDF seed. That is where the challenge happens. The final cipher key is then the standard SHA-256(masterSeed ‖ transformedKey), and the HMAC key is the standard SHA-512(masterSeed ‖ transformedKey ‖ 0x01). The KDBX4 reader and writer contain no other challenge-response code. — [Kdbx4Reader.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/format/Kdbx4Reader.cpp); [KeePass2.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/format/KeePass2.cpp)
- [VERIFIED-SRC] KDBX4 write calls `setKey(db->key(), false, true)`. Its third parameter is `updateTransformSalt`, so **the KDF seed is randomized on every save** and the YubiKey is challenged again with the new seed. The writer also generates a fresh 32-byte master seed, a fresh IV, and a fresh 64-byte inner-stream key. — [Kdbx4Writer.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/format/Kdbx4Writer.cpp); [Database.h](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Database.h); [Database.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Database.cpp)

**KDBX 3.1 (Kdbx3Reader.cpp, Kdbx3Writer.cpp, Database.cpp)**
- [VERIFIED-SRC] KDBX 3.1 read:
  1. `setKey` computes the transformed key. There is no challenge here, because of the legacy-AES path.
  2. `challengeMasterSeed(masterSeed)` sends the **32-byte header master seed** as the challenge and stores SHA-256(response) as `challengeResponseKey`. With no challenge-response key, it is an empty value.
  3. The final key is SHA-256(masterSeed ‖ challengeResponseKey ‖ transformedKey).
- [VERIFIED-SRC] KDBX 3.1 write generates a new 32-byte master seed, challenges it, and uses the same three-part final hash. — [Kdbx3Reader.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/format/Kdbx3Reader.cpp); [Kdbx3Writer.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/format/Kdbx3Writer.cpp); [Database.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/core/Database.cpp)

**YubiKey driver (src/keys/drivers/YubiKeyInterfaceUSB.cpp)**
- [VERIFIED-SRC] The driver always uses HMAC-SHA1 challenge-response (ykcore commands `SLOT_CHAL_HMAC1` / `SLOT_CHAL_HMAC2`, for slot 1 or 2).
- [VERIFIED-SRC] **Padding:** if the challenge is shorter than 64 bytes, the driver appends (64 − len) bytes, each equal to (64 − len). That is PKCS#7-style. For a 32-byte seed this means 32 bytes of `0x20`, giving a 64-byte buffer. Longer inputs are not truncated.
- [VERIFIED-SRC] The response buffer is 64 bytes and is then truncated to 20 bytes, the HMAC-SHA1 digest.
- [VERIFIED-SRC] Other behavior:
  - Touch is supported. The real challenge may block waiting for touch, and a timeout gives a "timed out waiting for user interaction" error.
  - Slot detection checks the `CONFIG1_VALID` / `CONFIG2_VALID` status bits. On newer keys it also sends a non-blocking 1-byte test challenge.
  - It scans up to 4 devices.
  - The vendor/product whitelist includes Yubico and OnlyKey. — [YubiKeyInterfaceUSB.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/keys/drivers/YubiKeyInterfaceUSB.cpp)
- [VERIFIED-SRC] A KeePassXC commit message states the reason for padding: always send 64 bytes "for compatibility with all configurations," following PKCS7. — [commit mirror](https://git.hackliberty.org/Git-Mirrors/keepassxc/commit/44ac7d152b4024c2355cb83c09ce3bf41491f0bf)
- [VERIFIED-SRC] KeePassXC's documented backup recipe programs **slot 2** with `ykpersonalize -2 -a -ochal-resp -ochal-hmac -ohmac-lt64 -oserial-api-visible -oallow-update`. `hmac-lt64` is the variable-length input mode. It writes a 20-byte secret generated from /dev/random. — [docs/topics/Reference.adoc "Creating a YubiKey backup"](https://github.com/keepassxreboot/keepassxc/blob/develop/docs/topics/Reference.adoc)
- [VERIFIED-SRC, secondary] In variable-length mode the YubiKey works out the message length by stripping the last byte and all adjacent identical trailing bytes. This is a quirk because the protocol has no length field. — [Yubico Forum](https://forum.yubico.com/viewtopic6eda.html?p=4903)
- Third-party descriptions of the challenge:
  - KeePassium's article says KeePassXC-style challenge-response uses "the database's master seed" as the challenge, and that it changes every save. — [KeePassium KB](https://support.keepassium.com/kb/yubikey-keechallenge/)
  - A GNOME Secrets issue says the "master key" is the challenge. — [GNOME Secrets #72](https://gitlab.gnome.org/World/secrets/-/issues/72)
  - **Conflict:** the KeePassXC source shows the master seed is the challenge only for KDBX 3.1. For KDBX4 it is the KDF seed. These descriptions are simplified.
- Ecosystem support for this format:
  - KeePassXC-style challenge-response is supported by KeePassDX, Keepass2Android (since 1.06f, KDBX4 required), KeePassium, Strongbox and others. — [keepass2android 1.06f notes](https://www.apkmirror.com/?p=498349); [KeePassium compatibility KB](https://support.keepassium.com/kb/yubikey-compatibility/); [KeeWeb wiki](https://github.com/keeweb/keeweb/wiki/YubiKey)
  - The Perl module File::KDBX::Key::YubiKey (v0.906, 2022) implements "KeePassXC-style" challenge-response via `ykchalresp`. — [metacpan](https://metacpan.org/pod/File::KDBX::Key::YubiKey)

### Inferences
- [INFERRED] **Spec for a KDBX4 file, which is what a KeePass 2 plugin should target:**
  1. Parse the outer KDBX4 header.
  2. Take the KdfParameters VariantDictionary entry `S` (the seed/salt, normally 32 bytes).
  3. Pad it PKCS#7-style to 64 bytes (seed ‖ 32×`0x20`).
  4. Send the HMAC-SHA1 challenge to slot N and take the 20-byte response R.
  5. The key component is SHA-256(R), which must be the last data concatenated into the composite-key SHA-256, after SHA-256(UTF-8 password) and the key-file key.
  6. Everything after that (KDF, final key, HMAC key) is standard KDBX4. KeePass 2 does it natively.
- [INFERRED] Fixed-64 versus variable-length slots both work with the 64-byte padded buffer. In fixed mode the HMAC covers all 64 bytes. In lt64 mode the key strips trailing `0x20` bytes, so it computes HMAC over the 32-byte seed. If the seed's own last byte(s) are `0x20`, those are stripped too. Either way the answer matches what KeePassXC gets, **provided the plugin sends exactly the same 64-byte buffer**. A plugin must never send the unpadded 32 bytes.
- [INFERRED] KeePass 2's `KcpPassword` is SHA-256 of the UTF-8 password and KeePassXC's password key is computed the same way. Cross-opening of password-only files between the two apps depends on this, but I did not re-read PasswordKey.cpp in this session.
- [INFERRED] KeePassXC adds the password before the key file, which matches KeePass 2's order.
- [INFERRED] **KDBX 3.1 KeePassXC + YubiKey files cannot be expressed through KeePass 2's composite-key / IUserKey model.** The response enters the final-key hash, not the composite key. A KeePass 2 plugin can realistically support only KDBX4. KeePassXC defaults to KDBX4, and Keepass2Android also requires KDBX4 for this mode.

### Gaps
- Not re-verified in this session: the exact KDF seed length KeePassXC generates for Argon2 and AES. It is assumed to be 32 bytes and is not needed for compatibility, because the plugin just reads `S`.
- I did not find a Yubico primary document (only a forum post) that precisely specifies lt64 trailing-byte stripping.
- The order in which KeePassXC's DatabaseOpenWidget adds the password and key file was not re-read.

## Q2. Does the challenge change on each save? Backup YubiKeys and risks

### Takeaway
Yes. KeePassXC re-randomizes the challenge on every save: the KDF seed for KDBX4, the master seed for KDBX3. The YubiKey must therefore be present (and touched, if touch is configured) to save. Any backup key must hold the **identical 20-byte HMAC secret**, which means writing the same secret to two keys at programming time. Keys with their own factory-generated secrets cannot serve as backups.

### Cited Findings
- [VERIFIED-SRC] The KDBX4 writer re-randomizes the KDF seed on each save (`updateTransformSalt = true`). The KDBX3 writer generates a new 32-byte master seed on each save and challenges it. — [Kdbx4Writer.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/format/Kdbx4Writer.cpp); [Kdbx3Writer.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/format/Kdbx3Writer.cpp)
- KeePassium describes the response as changing "every time you save the database," with the secret never leaving the YubiKey. — [KeePassium KB](https://support.keepassium.com/kb/yubikey-keechallenge/)
- [VERIFIED-SRC] KeePassXC's backup guide:
  1. Generate a 20-byte HMAC key.
  2. Write it to slot 2 of both keys with `ykpersonalize` (flags as in Q1).
  3. Store the hex secret safely, for example on paper, so a key can be recreated later.
  The guide gives no explicit warnings and no touch flag. — [Reference.adoc](https://github.com/keepassxreboot/keepassxc/blob/develop/docs/topics/Reference.adoc)
- [VERIFIED-SRC] `ykman otp chalresp` alternative:
  - It programs slot 1 or 2.
  - `KEY` is given in hex.
  - `-g/--generate` creates a random key but **cannot be combined with KEY**.
  - `-t/--touch` requires touch.
  - `ykman otp calculate` sends a hex challenge.
  - The docs do not say whether chalresp configures lt64 by default. — [ykman OTP commands](https://docs.yubico.com/software/yubikey/tools/ykman/OTP_Commands.html)
- [VERIFIED-SRC] With the Yubico .NET SDK, a slot is programmed through `ConfigureChallengeResponse` with `UseHmacSha1()`. The key comes from `UseKey()` (20 bytes) or `GenerateKey()`. — [Yubico SDK: program a challenge-response credential](https://docs.yubico.com/yesdk/users-manual/application-otp/how-to-program-a-challenge-response-credential.html)
- KeePassXC issues related to the presence requirement:
  - #10227 "Database overwrite when Yubikey not allways accessible" (closed Jan 2024).
  - #10251 "Changes to database on second device is not synced to first device if security key is pulled out" (closed Jan 2024). — [KeePassXC issue search](https://github.com/keepassxreboot/keepassxc/issues?q=yubikey+lock+removed)

### Inferences
- [INFERRED] For a backup key, `ykman otp chalresp --generate` on each key separately produces **different** secrets. The workflow is to generate the secret once and write the same hex `KEY` to both keys. That requires the secret to be exposed on the host once, at setup.
- [INFERRED] Risks:
  - The secret, once printed or stored, is a full substitute for the YubiKey.
  - A slot reprogrammed by accident (for example `ykman otp delete`, or overwriting slot 2 for another app) destroys access if no copy of the secret exists.
  - If touch is configured, every save needs a touch.
  - Saving while the key is absent fails or is blocked. Sync tools can then propagate partial states (see #10227 and #10251).
  - A KeePass 2 plugin that lets KeePass randomize the seed without re-challenging would write an unopenable file (see Q3).

### Gaps
- I found no Yubico statement on whether `ykman otp chalresp` sets variable-length (lt64) mode by default. This matters only for the padding quirk, and the padded 64-byte buffer works either way, as noted in Q1.

## Q3. Existing KeePass 2 plugins and whether any is KeePassXC-compatible, with complaints grouped

### Takeaway
No KeePass 2 plugin implementing KeePassXC-compatible challenge-response turned up in this session's searches, and keepass.info lists none. KeeChallenge (brush701, plus the KeeChallenge-Modern fork) uses an incompatible scheme: a separate XML file holding an encrypted static secret. Its issue tracker is dominated by version breakage, maintenance, and cross-app incompatibility. Users explicitly ask for a "KeeChallengeXC". OtpKeyProv (HOTP) complaints center on counter desync.

### Cited Findings
**KeeChallenge and KeeChallenge-Modern**
- keepass.info lists KeeChallenge (Ben Rush, Troy Dhayer) with links to KeeChallenge-Modern and the original. MultiCipher (Titas Raha) also claims YubiKey challenge-response support. No listed plugin mentions KeePassXC. Other hardware key providers listed:
  - KeePassPasskeyKeyProvider (FIDO2 hmac-secret/PRF, Windows Hello)
  - KeePassRFID
  - KeePass2Trezor
  - Smart Certificate Key Provider — [keepass.info plugins](https://keepass.info/plugins.html)
- [VERIFIED-SRC] KeeChallenge-Modern (tdhayer):
  - GPL-3.0, Windows only, v2.0.8 current. A 2026-05-18 notice withdrew v2.0.6 and v2.0.7, which "fail to load silently".
  - It uses native Yubico DLLs (`libykpers-1-1.dll`, `libyubikey-0.dll`) in 32bit and 64bit folders.
  - It stores a `.xml` next to the `.kdbx` with the challenge, IV and encrypted secret. The HMAC response is the AES-256 key that decrypts the secret.
  - It has a Recovery Mode that uses the secret shown at setup.
  - It does not mention KeePassXC. It had 13 stars at the time of reading. — [KeeChallenge-Modern](https://github.com/tdhayer/KeeChallenge-Modern)
- KeePassium's critique of KeeChallenge:
  - The secret never changes and is only re-encrypted, so it acts like a static key file.
  - Old XML files can be replayed.
  - The secret leaves the YubiKey, which allows covert cloning.
  - A Yubico developer raised a concern in 2016 that was still open in 2024.
  - No meaningful updates since 2016. — [KeePassium KB](https://support.keepassium.com/kb/yubikey-keechallenge/)
- KeePassium (iOS) is "compatible with KeePassXC and almost all other apps" except KeePass 2.x + KeeChallenge. — [KeePassium compatibility KB](https://support.keepassium.com/kb/yubikey-compatibility/); [SourceForge KeePass thread](https://sourceforge.net/p/keepass/discussion/329221/thread/74d14f77a5/)
- **brush701/keechallenge issue tracker:** 55 issues total, 39 open. Page 1 (25 newest, #39–#64) reviewed — [issues](https://github.com/brush701/keechallenge/issues?q=is%3Aissue). Complaints by theme, counts from page 1 only:
  - **Breaks with KeePass/Windows updates (6):**
    - #63 KeePass 2.59 + KeeChallenge 1.5 (closed Apr 2026)
    - #56 v1.5 not working with 2.51.1 (May 2022, 4 comments)
    - #50 not working with 2.47 (Jan 2021)
    - #45 2.45 "incompatible version" bug (May 2020)
    - #55 Win11 does not start (closed Mar 2022)
    - #49 setup not working on Win10 (Oct 2020)
  - **Incompatible with KeePassXC, Keepass2Android and KeePassDX (2):**
    - #62 "only works with itself" (Sep 2024). A Nov 2025 reply asks for a KeePassXC-style mode or a "KeeChallengeXC" plugin. — [#62](https://github.com/brush701/keechallenge/issues/62)
    - #57 cannot open a file created in KeePassXC (Jul 2022)
  - **Maintenance and abandonment (3):**
    - #64 "For anyone looking for a modern, maintained version (2026)" (Apr 2026)
    - #52 "Is KeeChallenge still supported?" (Mar 2021)
    - #59 broken link to Yubico prebuilt binaries (Apr 2023)
  - **Key-combination and lifecycle limits (4):**
    - #51 cannot combine password + key file + KeeChallenge, since KeePass offers either a key file or a provider (Feb 2021)
    - #58 cannot remove challenge-response (Dec 2022)
    - #44 expected way to remove KeeChallenge (Jan 2020)
    - #40 cannot use slots 1 and 2 at once in two KeePass instances (Apr 2019)
  - **Deployment and environment (3):**
    - #61 requires both 32bit and 64bit folders (Dec 2023)
    - #60 RDP session with a locally connected YubiKey (Apr 2023)
    - #42 Ubuntu "error connecting to yubikey" (Nov 2019)
  - **Hardware and algorithm requests (5):**
    - #53 other HMAC-SHA1 tokens (Nov 2021)
    - #48 YubiKey 5C NFC (Sep 2020)
    - #47 FIDO2 (Aug 2020)
    - #41 OnlyKey (Oct 2019)
    - #39 HMAC-SHA256 (Apr 2019)
  - **Recovery (1):** #43 recreate the secret (Jan 2020)
- A Keepass2Android issue: KeePassXC + YubiKey databases did not trigger the challenge app (older issue, status unclear). — [keepass2android #4](https://github.com/PhilippC/keepass2android/issues/4)

**OtpKeyProv**
- [VERIFIED-SRC] Dominik Reichl's OtpKeyProv: OATH HOTP (RFC 4226), v2.7 for KeePass 2.54+. — [keepass.info plugins](https://keepass.info/plugins.html)
- Complaints:
  - HOTP counter desync between token and KeePass makes the database unopenable. — [Nitrokey forum](https://support.nitrokey.com/t/keepass-otp-generierung-klappt-nicht-mehr/788)
  - Recommended look-ahead of at least (n×2)+1. — [Yubico forum](https://forum.yubico.com/viewtopic7bd4.html?p=4206)
  - Cloned tokens replay already-used OTPs, and resyncing means reading the counter from the `.otp.xml` file and generating that many codes ("painful"). — [Yubico forum](https://forum.yubico.com/viewtopic7a14.html?p=4199)
  - Setup mismatches (counter vs. moving factor, short vs. long press slot). — [Yubico forum](https://forum.yubico.com/viewtopic67cb.html?p=4201)
- KeePassium rejected both KeeChallenge and OtpKeyProv because they rely on auxiliary files, which complicate sync. — [KeePassium compatibility KB](https://support.keepassium.com/kb/yubikey-compatibility/)

**KeePass 2 key-provider plumbing**
- [VERIFIED-SRC] KeePass 2 `KcpCustomKey(name, data, bPerformHash)`:
  - If `bPerformHash` is set, it stores SHA-256(data).
  - Otherwise it stores the data as-is. There is no length check in that class. — [KcpCustomKey.cs mirror](https://raw.githubusercontent.com/dlech/KeePass2.x/master/KeePassLib/Keys/KcpCustomKey.cs)
- [VERIFIED-SRC] **KeePass 2 `KdbxFile.Save` generates a new 32-byte master seed and calls `kdf.Randomize(KdfParameters)` on every save.** This happens for KDBX3 and KDBX4, and it regenerates the KDF seed. It happens inside Save, before `ComputeKeys`. — [KdbxFile.Write.cs mirror](https://raw.githubusercontent.com/dlech/KeePass2.x/master/KeePassLib/Serialization/KdbxFile.Write.cs)

### Inferences
- [INFERRED] **Password + YubiKey through a standard KeyProvider can match KeePassXC byte-for-byte.** The provider returns the raw 20-byte response with hashing left on (not DirectKey). KeePass wraps it as SHA-256(R), and the composite becomes SHA-256(SHA-256(pw) ‖ SHA-256(R)), which is the KeePassXC formula. The provider needs the KDF seed, so it must open and parse the KDBX4 outer header itself from the database's IOConnectionInfo.
- [INFERRED] **Password + key file + YubiKey:** KeePass's dialog allows only a key file *or* a provider (the same limit as KeeChallenge #51). One option is a DirectKey (unhashed) provider returning 64 bytes = keyFileKey(32) ‖ SHA-256(R)(32), which reproduces the same concatenation. Whether KeePass's UI or the KeyProvider path accepts a 64-byte DirectKey must be tested. The alternative is the plugin's own unlock dialog.
- [INFERRED, CRITICAL] **Save trap:** KeePass 2 re-randomizes the KDF seed on every save, but the composite key in memory still holds SHA-256(response to the old seed). After a save, the file header carries a new seed. KeePassXC, and the plugin itself, would then challenge the new seed, get a different response, and **fail to open the file**. That is a data-loss risk. The plugin must, every time the database is saved, take the new seed, challenge the YubiKey (touch if needed), and replace the challenge-response user key in the database's master key **before** the cipher keys are derived. Possible hooks (unverified):
  - Wrap or replace the registered KdfEngine for the UUID so that its `Randomize` re-challenges.
  - Pre-set the seed and suppress randomization.
  - A FileSaving-event approach is probably too early, because Randomize runs inside Save.

  This must be prototyped and tested with round-trips (KeePass save → KeePassXC open, and the reverse).
- [INFERRED] A KDBX 3.1 KeePassXC+YubiKey file cannot be supported without patching KeePassLib's final-key derivation. The plugin should detect KDBX < 4 and refuse, explaining why.

### Gaps
- No Reddit threads came up in the searches. Complaint evidence is GitHub, forums and vendor KBs only.
- Only page 1 of 3 of the KeeChallenge issues was read, so the counts are a lower bound.
- I did not read KeeChallenge-Modern's own issues.
- I did not read KeePass 2 `KeyProvider`/`KeyProviderQueryContext` source: the mirror URL returned 404. The semantics of DirectKey and Exclusive and the exact query-context members still need checking.
- MultiCipher's YubiKey format was not examined.
- I found no KeePass 2 plugin named "KPXCCompat", "YubiKeyChallengeResponse" or "KeePassChallengeResponse", and none that is KeePassXC-compatible. This is "not found", not proof of absence.

## Q4. Talking to a YubiKey from .NET Framework 4.8 without vendor installs (Yubico.YubiKey SDK and ykman)

### Takeaway
The Yubico.YubiKey .NET SDK (Apache-2.0) ships `net472`, `netstandard2.0` and `netstandard2.1` builds (v1.18.0, Sep 2026), so it can be consumed from .NET Framework 4.8. It supports OTP-application HMAC-SHA1 challenge-response and has a device arrival/removal listener. It needs no separately installed Yubico software, but it brings managed dependencies plus a native shim library. Windows requires admin for **FIDO** HID access. This session found no Yubico statement that the OTP (keyboard HID) interface needs elevation.

### Cited Findings
- [VERIFIED-SRC] NuGet Yubico.YubiKey 1.18.0 (updated 2026-09-20):
  - Targets .NET Framework 4.7.2, .NET Standard 2.0 and 2.1.
  - Depends on Yubico.Core, CommunityToolkit.Diagnostics, Microsoft.Bcl.AsyncInterfaces, Microsoft.Extensions.Logging.Abstractions, System.Formats.Asn1 and System.Formats.Cbor.
  - Yubico.NativeShims is an internal unmanaged library.
  - 508K total downloads.
  - Version 1.12.0 is deprecated. A v2 alpha ("yubikit" branch) is not production-ready. — [NuGet](https://www.nuget.org/packages/Yubico.YubiKey)
- [VERIFIED-SRC] The SDK repo is Apache-2.0, has 153 stars, and runs CI on Windows, Ubuntu and macOS. — [GitHub Yubico.NET.SDK](https://github.com/Yubico/Yubico.NET.SDK)
- [VERIFIED-SRC] Challenge-response API:
  1. Get a device with `YubiKeyDevice.FindAll()`.
  2. Create an `OtpSession` from it.
  3. Call `CalculateChallengeResponse(Slot.ShortPress | Slot.LongPress)`.
  4. Chain `.UseChallenge(bytes)` and `.UseYubiOtp(false)` for HMAC-SHA1. `UseYubiOtp` is mandatory and has no default.
  5. Optionally add `.UseTouchNotifier(action)`, which runs on a separate thread.
  6. Finish with `.GetDataBytes()`.

  **An HMAC-SHA1 challenge must be 64 bytes unless the slot was configured with `UseSmallChallenge()`.** — [How to calculate a challenge-response](https://docs.yubico.com/yesdk/users-manual/application-otp/how-to-calculate-a-challenge-response-code.html); [UseYubiOtp](https://docs.yubico.com/yesdk/yubikey-api/Yubico.YubiKey.Otp.Operations.CalculateChallengeResponse.UseYubiOtp.html)
- [VERIFIED-SRC] The OTP interface presents as a virtual HID keyboard. The FIDO interface uses HID usage page 0xF1D0. — [Yubico SDK physical interfaces](https://docs.yubico.com/yesdk/users-manual/yubikey-reference/transports/overview.html)
- [VERIFIED-SRC] Yubico YSA-2024-01: "On Windows systems, the ability to communicate with FIDO authenticators requires Administrator privileges" (an OS limitation). Running ykman GUI elevated caused a privilege-escalation risk (CVSS 7.7). — [YSA-2024-01](https://yubico.com/support/security-advisories/ysa-2024-01)
- [VERIFIED-SRC] KeePassXC itself talks to the OTP application over USB HID through bundled ykcore, with no Yubico install. — [YubiKeyInterfaceUSB.cpp](https://github.com/keepassxreboot/keepassxc/blob/develop/src/keys/drivers/YubiKeyInterfaceUSB.cpp)
- [VERIFIED-SRC] The ykman CLI alternative is `ykman otp calculate <slot> <hex-challenge>`. — [ykman OTP commands](https://docs.yubico.com/software/yubikey/tools/ykman/OTP_Commands.html)
- [VERIFIED-SRC] File::KDBX warns that shelling out to `ykchalresp` is unreliable on Windows because of process-fork deadlocks. — [metacpan](https://metacpan.org/pod/File::KDBX::Key::YubiKey)

### Inferences
- [INFERRED] With the SDK, the plugin should pass the KeePassXC-style **64-byte PKCS#7-padded** buffer. That meets the SDK's 64-byte rule and gives the KeePassXC-identical response for both fixed and lt64 slots.
- [INFERRED] `Slot.ShortPress` is slot 1 and `Slot.LongPress` is slot 2, by Yubico naming convention. The page read did not state this explicitly, so confirm it in the API reference.
- [INFERRED] Since the OTP challenge-response path uses the OTP/keyboard HID interface, not FIDO, it should not need admin. Test on a standard-user Windows 11 account.
- [INFERRED] KeePass 2 loads PLGX or DLL plugins. The SDK and its dependencies (including the native shim, x86 and x64) favor DLL distribution with the dependencies side by side. Check assembly-binding conflicts (System.* versions) inside KeePass.exe's AppDomain.
- [INFERRED] Shelling out to `ykman` adds an install dependency and process-spawn latency. It is better as a diagnostic fallback than as the primary path.
- [INFERRED] The SDK is Apache-2.0 and the plugin is MIT, so the plugin needs a NOTICE/attribution but has no license conflict.

### Gaps
- I did not confirm whether Yubico.NativeShims is needed on Windows for OTP HID, or which native files the net472 package drops.
- I found no Yubico doc explicitly saying OTP HID works unelevated on Windows.
- Behavior over RDP (see KeeChallenge #60) is unknown.

## Q5. Detecting YubiKey removal to lock KeePass

### Takeaway
It is feasible on Windows in two ways. One is the SDK's `YubiKeyDeviceListener` (Arrived/Removed events, one event per physical key). The other is Win32 `WM_DEVICECHANGE` with `DBT_DEVICEREMOVECOMPLETE`, after registering for device-interface notifications (for example the HID interface class) on the KeePass main window.

### Cited Findings
- [VERIFIED-SRC] `YubiKeyDeviceListener`, added in SDK 1.2.0:
  - It is a singleton reached through `.Instance`, with `Arrived` and `Removed` events carrying `YubiKeyDeviceEventArgs.Device`. The device is populated on removal too.
  - It raises one event per physical key, even though a USB key exposes up to 3 child devices.
  - It only talks to keys it hasn't seen before, to avoid disrupting keys in use. — [Yubico SDK device notifications](https://docs.yubico.com/yesdk/users-manual/sdk-programming-guide/device-notifications.html)
- [VERIFIED-SRC] Win32 `WM_DEVICECHANGE` delivers `DBT_DEVICEARRIVAL` (0x8000), `DBT_DEVICEREMOVECOMPLETE` (0x8004) and `DBT_DEVNODES_CHANGED` (0x0007). A forcible removal may arrive without a query-remove. — [Microsoft Learn WM_DEVICECHANGE](https://learn.microsoft.com/en-us/windows/win32/devio/wm-devicechange)

### Inferences
- [INFERRED] Match removal on the YubiKey serial used at unlock. The SDK exposes device serial, and KeePassXC also keys slots by serial. Then call KeePass's lock-workspace action.
- [INFERRED] Problem cases a plugin must handle:
  - Unsaved changes at removal time. A save needs the key, because the seed must be re-challenged, so a lock that saves first is impossible once the key is gone.
  - Brief USB re-enumeration. KeePassXC users report "Certain actions remove Yubikey" and udev remove events on launch, so debounce removal events.
  - Multiple keys (primary and backup).
- [INFERRED] `RegisterDeviceNotification` with `DBT_DEVTYP_DEVICEINTERFACE` is needed to receive per-interface removal for HID devices. This is standard Win32 practice, but the page fetched did not cover registration.

### Gaps
- I did not verify which Windows mechanism the SDK listener uses internally, or its threading.
- The SDK listener's latency was not measured.

## Q6. Does KeePassXC lock the database when the YubiKey is removed?

### Takeaway
No, not as of October 2026. The request has been open since **2019** (#3616) and was put on milestone **v2.9.0 on 2026-09-13**. Maintainers are lukewarm. They prefer USB event subscriptions over polling, worry about unsaved changes that need the key to save, and agreed it would be opt-in only.

### Cited Findings
- [VERIFIED-SRC] #3616 "Add option to lock database when Yubikey is removed":
  - Opened 2019-10-10. Still open, with 12 comments and the `feature: Hardware Keys` label. Milestone v2.9.0 set 2026-09-13. No linked PR.
  - phoerious rejected polling as slow and battery-draining and said a proper fix needs platform-specific USB change subscriptions.
  - droidmonkey said it "would really suck" if the key is needed to save unsaved changes, then agreed it would be opt-in. — [#3616](https://github.com/keepassxreboot/keepassxc/issues/3616)
- [VERIFIED-SRC] #10331 "Automatically lock the database if Yubikey is removed" (2024-02-25) asked for a timeout setting. It was closed the same day as a duplicate, "not planned", and points to #3616. — [#10331](https://github.com/keepassxreboot/keepassxc/issues/10331)
- [VERIFIED-SRC] Related KeePassXC hardware-key complaints:
  - #10077 "Certain actions remove Yubikey" (19 comments, closed Apr 2024)
  - #10888 KeePassXC triggers a udev REMOVE event on the YubiKey (closed Jun 2024)
  - #13515 Flatpak: hardware key not detected after key removal (14 comments, closed Sep 2026)
  - #7207 YubiKey ejected on open (2021) — [issue search](https://github.com/keepassxreboot/keepassxc/issues?q=yubikey+lock+removed)
  - #3329: macOS hid permission and Secure Keyboard Entry broke slot detection. It was closed as "downstream" in Dec 2020. — [#3329](https://github.com/keepassxreboot/keepassxc/issues/3329)
- Other platform reports:
  - openSUSE: KeePassXC 2.7.10 "hardware key not detected". — [openSUSE forum](https://forums.opensuse.org/t/keepassxc-doesnt-detect-yubikey/186560)
  - Qubes: hangs at "please touch your YubiKey", fixed by assigning more CPU cores. — [Qubes forum](https://forum.qubes-os.org/t/keepassxc-freezing-with-yubikey-after-please-touch-your-yubikey-message/3643/3)

### Inferences
- [INFERRED] Lock-on-removal is a long-standing unmet request (7 years open). An opt-in implementation in a KeePass 2 plugin would answer a documented gap. It must handle the unsaved-changes case explicitly, for example by refusing to lock silently or by warning first.

### Gaps
- The full comment thread of #3616 (reaction counts) was not fully visible.
- I did not check whether a v2.9.0 PR exists after 2026-09-13.
