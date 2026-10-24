# Phase 0 – Step 1: Existing Windows Hello plugins for KeePass 2

Date of research: 2026-10-18. Sources: public GitHub pages (READMEs, issue lists, single issues).
Only facts visible on those pages are listed here. Issue titles alone are marked "title only".

Local environment: KeePass 2.60.0.0, Windows 11 Pro 10.0.26300.

## 1. KeePassWinHello (sirAndros/KeePassWinHello)

- License: MIT. 369 stars, 46 open issues, 21 open pull requests.
- Latest release: v3.3.1 (hotfix). The release page shows no year. Tested by the author on KeePass 2.39.1 / 2.42.1.
- Technique: NCrypt calls (`NCryptEncrypt` / `NCryptDecrypt` / `NCryptFinalizeKey`) against a Windows Hello (NGC/"Passport") key.
  Issue #112 asks where `NgcCacheType` comes from, which suggests the plugin relies on undocumented details (not confirmed by the maintainer).
- Storage: default = encrypted key only in KeePass process memory. Optional = persistent key in Windows Credential Manager.
- Expiry: "Valid time period", default 24 h.
- Recurring complaints (open, mostly no maintainer reply):
  - `NCryptDecrypt 0x80098044` is the most common error (9 of the 25 newest open issue titles).
  - #120 (2025-03-29, still active 2026-08-29): **Face** fails, **PIN** works. A contributor narrowed it down: Face works for the in-memory
    session key, but fails for the **persistent** key after a KeePass restart. Began for one user after a Windows Update reset Hello.
  - #127 (2025-11-15): error after updating to **KeePass 2.60** (our version). No reply, no fix.
  - #113: error appears about every third unlock (Face). A contributor commit (2026-07-11) forces the Hello prompt into the foreground; not confirmed as fix.
  - Further codes: 0x80070006, 0x8007139F, 0x80090010, 0x80284001/2 (TPM-related range), 0xD000A002.
  - #124: request to re-enable Hello over RDP.

## 2. WinHelloUnlock (Angelelz/WinHelloUnlock, fork lazou/WinHelloUnlock)

The name "WinHelloQuickUnlock" was not found; this is the closest match.

- License: MIT. 22 open issues. Tested by the author only on KeePass 2.50. The author states they are not a programmer.
- Technique: WinRT/UWP APIs – `KeyCredential` (Windows Hello key credential), `CryptographicEngine.Encrypt`, storage in `PasswordVault`.
- Survives reboot: yes. Expiry: **none** (stated in README). Data can only be deleted manually.
- Recurring complaints (title only unless noted):
  - Breaks with KeePass updates: #54 (2.47), #72 (2.54).
  - Breaks after Windows updates / lock: #73, #76 ("works only once"), #78 (fingerprint fails after Windows lock), #67 (TPM error 0x8028008B on first login of the day).
  - Composite key / YubiKey errors: #61, #62.
  - #69 reports database corruption after "delete key" (title only, not verified).
  - Requests: multiple users / multiple PCs (#53, #70).

## Lessons for RamzX Unlock

1. Face + persistent key after restart is the weak point of KeePassWinHello → test Face, fingerprint and PIN separately, also after reboot.
2. WinHelloUnlock has no expiry → our design needs expiry (reboot and/or X days) from the start.
3. Both plugins broke on KeePass or Windows updates → small API surface, test on KeePass 2.60 and newer.
4. Hello prompt foreground handling is a known issue → handle the window owner/foreground explicitly.
5. The plugin must never write to the database file itself (see WinHelloUnlock #69).
6. Windows Hello always allows PIN as fallback → this is convenience, not a second factor.
7. Both projects appear to have little or no maintainer activity on recent issues.

## Sources

- https://github.com/sirAndros/KeePassWinHello
- https://github.com/sirAndros/KeePassWinHello/releases
- https://github.com/sirAndros/KeePassWinHello/issues (+ #113, #120, #127)
- https://github.com/Angelelz/WinHelloUnlock (+ /issues)
- https://github.com/lazou/WinHelloUnlock
