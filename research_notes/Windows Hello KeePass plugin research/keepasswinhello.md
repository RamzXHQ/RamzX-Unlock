# KeePassWinHello (sirAndros) – complaints, failure modes, feature requests, design, forks

Research date: 2026-10-18. Method: the full issue and PR list (all states) plus all 315 comments were downloaded through the GitHub REST API (`/repos/sirAndros/KeePassWinHello/issues?state=all`, `/issues/comments`, `/releases`, `/commits`, `/forks`, `/compare`). Every issue body and comment was read. The current `master` source was read for the design analysis only. No code is reproduced here; designs are paraphrased. "Verified" means the statement was read in an issue or PR body, a comment, the source or a doc. "Title-only" means the issue has only a title or an empty template.

Repo facts: MIT license; 369 stars, 30 forks, not archived; `pushed_at` 2026-06-21 — [repo API](https://api.github.com/repos/sirAndros/KeePassWinHello). The tracker holds 162 numbers: 110 issues (46 open) and 52 PRs (21 open). Number #84 is missing (deleted or transferred) — [issues](https://github.com/sirAndros/KeePassWinHello/issues?q=is%3Aissue).

---

## 1. Catalogue of issues by theme (counts, representative issues, dates)

### Takeaway
The largest cluster by far is NCrypt/TPM/WinBio error-code dialogs: 45 of 110 issues (41%), 28 still open. The single biggest open problem is `NCryptDecrypt 0x80098044` (WINBIO_E_INVALID_TICKET): face recognition fails but PIN works. It has 14 issues filed between 2024-10 and 2026-05 and none is fixed upstream. Second is the prompt's focus, foreground and position (7 issues, 5 open, running since 2019). Persistent-key lifecycle problems after Windows Update, PIN reset or TPM reset are third.

### Cited Findings

**Theme counts.** Each of the 110 issues has exactly one primary theme. Open counts are as of 2026-10-18.

| # | Theme | Issues | Open | Issue numbers |
|---|---|---|---|---|
| T1 | `NCryptDecrypt 0x80098044`: face fails, PIN works | 14 | 12 | 113,116,118,119,120,127,128,130,132,135,136,137,138,139 |
| T2 | Other NCrypt/TPM/TBS/NT error codes | 29 | 14 | 33,42,51,60,63,64,68,69,71,72,77,87,89,93,98,101,114,115,121,122,123,125,126,129,131,134,160,161,162 |
| T3 | Credential Manager `CredWrite` failures | 2 | 2 | 67,82 |
| T4 | Prompt focus, foreground, position, window state | 7 | 5 | 11,25,48,54,56,86,107 |
| T5 | KeePass "secure desktop" interaction | 5 | 0 | 12,18,19,49,92 |
| T6 | RDP (feature request; error reports are in T2) | 1 | 1 | 124 |
| T7 | Elevated process / UAC registry detection | 4 | 0 | 61,85,99,100 |
| T8 | Windows Hello not available, environment, policy | 5 | 0 | 1,2,17,62,96 |
| T9 | Modality choice (face/fingerprint/PIN) | 4 | 2 | 13,50,53,57 |
| T10 | Persistent storage, startup unlock, revoke | 7 | 0 | 7,10,20,23,30,55,133 |
| T11 | Security / trust model | 4 | 2 | 38,59,83,106 |
| T12 | Expiry / valid period | 2 | 1 | 3,40 |
| T13 | Multiple databases | 3 | 2 | 14,91,111 |
| T14 | Key files / composite keys | 1 | 0 | 9 |
| T15 | KeePass version compatibility, plugin loading | 6 | 1 | 47,58,90,105,109,110 |
| T16 | Distribution, install, packaging (Chocolatey, winget, PLGX) | 7 | 1 | 4,5,16,22,41,65,108 |
| T17 | Misc (API questions, Auto-Type, unrelated) | 9 | 3 | 6,8,15,52,66,95,97,112,117 |

Source for all rows: [issue tracker](https://github.com/sirAndros/KeePassWinHello/issues?q=is%3Aissue). Some themes overlap. RDP accounts for 3 issues in total (#51 and #64 sit in T2, #124 in T6). Elevation also figures in #11, #82 and #130.

**T1 – 0x80098044, face fails and PIN works (14 issues, 12 open)**
- First report was 2024-10-30 on Win11, KeePass 2.57.1 and plugin 3.3.1, Surface Laptop Studio 2 with face: "error occurred almost every 3 time" (verified) — [#113](https://github.com/sirAndros/KeePassWinHello/issues/113).
- Face recognition fails while PIN works. Re-enrolling face and resetting Hello did not help (verified; Win11 24H2, KeePass 2.58, plugin 3.3.1, 2025-03-29) — [#120](https://github.com/sirAndros/KeePassWinHello/issues/120).
- MrSco (2026-07-11) narrowed it down after a Windows Update reset his Hello enrollment. Face fails only on the first prompt after a fresh KeePass start (the persistent-key path). Lock and unlock inside the same session works with face, and PIN always works. Deleting and recreating the plugin's NGC key did not help. A full Credential Manager vault had separately caused CredWrite error 8 (verified) — [#120 comment](https://github.com/sirAndros/KeePassWinHello/issues/120).
- Franky319 (2026-08-29) sees the same behavior on both KeePass 2.61.1 and 2.60, so downgrading KeePass does not help (verified) — [#120](https://github.com/sirAndros/KeePassWinHello/issues/120).
- Conflicting report: DanielaE (2026-03-06) says the error appeared after upgrading KeePass 2.60 to 2.61 and went away after reverting to 2.60. MrLivre says face only, PIN works. Another user says running KeePass as administrator works around it (verified) — [#130](https://github.com/sirAndros/KeePassWinHello/issues/130).
- Reported with KeePass 2.60 after a camera-based authentication (2025-11-15) — [#127](https://github.com/sirAndros/KeePassWinHello/issues/127). Reported on a new Windows profile with a USB Hello camera and KeePass 2.61 (2026-05-07) — [#138](https://github.com/sirAndros/KeePassWinHello/issues/138).
- A non-technical user has hit it intermittently for about a year after KeePass "went dormant" (2026-03-16) — [#132](https://github.com/sirAndros/KeePassWinHello/issues/132).
- Title-only or empty-template reports: [#116](https://github.com/sirAndros/KeePassWinHello/issues/116) (2025-02-11), [#118](https://github.com/sirAndros/KeePassWinHello/issues/118) (2025-02-28), [#119](https://github.com/sirAndros/KeePassWinHello/issues/119) (2025-02-28, "on automatic start"), [#128](https://github.com/sirAndros/KeePassWinHello/issues/128) (2025-11-27), [#135](https://github.com/sirAndros/KeePassWinHello/issues/135)/[#136](https://github.com/sirAndros/KeePassWinHello/issues/136) (stack trace only, 2026-04), [#137](https://github.com/sirAndros/KeePassWinHello/issues/137) (facial recognition, 2026-04-26), [#139](https://github.com/sirAndros/KeePassWinHello/issues/139) (2026-05-18).
- The stack trace is identical in every report. The error is raised in the decrypt-on-unlock path, which runs from the KeePass WindowAdded hook through KeyManager.Unlock and ExtractCompositeKey down to WinHelloProvider.PromptToDecrypt (verified) — [#130](https://github.com/sirAndros/KeePassWinHello/issues/130).

**T2 – other error codes (29 issues).** Meanings are in section 3.
- `0x80090010` NTE_PERM (5 issues):
  - Over RDP on lock and unlock (2019-12-17, three users) — [#51](https://github.com/sirAndros/KeePassWinHello/issues/51); duplicate [#64](https://github.com/sirAndros/KeePassWinHello/issues/64) (2021-01-21).
  - `NCryptFinalizeKey` fails when creating the persistent key after a Windows Update. Removing and re-registering the fingerprint fixed it (2025-04-08) — [#121](https://github.com/sirAndros/KeePassWinHello/issues/121). The same fix was reported in [#125](https://github.com/sirAndros/KeePassWinHello/issues/125) (2025-07-24).
  - `NCryptDecrypt` fails only with the internal one of two fingerprint readers (2025-06-11). The reporter switched from KeePassXC because it lacks persistent keys — [#123](https://github.com/sirAndros/KeePassWinHello/issues/123).
- `0x8007139F` ERROR_INVALID_STATE (3 issues, all open):
  - `NCryptDeleteKey`, empty template (2022-01-14) — [#87](https://github.com/sirAndros/KeePassWinHello/issues/87).
  - `NCryptEncrypt` "on starting and closing KeePass" (2025-12-19) — [#129](https://github.com/sirAndros/KeePassWinHello/issues/129).
  - Title only (2026-02-04) — [#131](https://github.com/sirAndros/KeePassWinHello/issues/131).
- `0x80070006` invalid handle on decrypt (3 issues): title only [#122](https://github.com/sirAndros/KeePassWinHello/issues/122) (2025-06-03) and [#126](https://github.com/sirAndros/KeePassWinHello/issues/126) (2025-08-10); "50:50 chance" [#162](https://github.com/sirAndros/KeePassWinHello/issues/162) (2026-08-04, confirmed 2026-08-30).
- TPM/TBS codes (6 issues):
  - `0x80280159` after switching from fingerprint to face (2019-09-30) — [#42](https://github.com/sirAndros/KeePassWinHello/issues/42).
  - `0x8028008B` TPM_20_E_HANDLE after restart; Credential Manager entries had vanished (2021-05-17) — [#68](https://github.com/sirAndros/KeePassWinHello/issues/68).
  - `0x80280095` TPM_20_E_SIZE (2021-08-19) — [#77](https://github.com/sirAndros/KeePassWinHello/issues/77).
  - `0x80284002` TBS_E_BAD_PARAMETER on Win11 25H2 with KeePass 2.61, secure desktop on, "TPM not loadable" (2026-03-30) — [#134](https://github.com/sirAndros/KeePassWinHello/issues/134).
  - `0x80284001` TBS_E_INTERNAL_ERROR on encrypt at close and on decrypt via Ctrl+Alt+K, KeePass 2.61.1 (2026-07-26, 2026-08-02) — [#161](https://github.com/sirAndros/KeePassWinHello/issues/161).
  - `0xD000A002` after a CPU repaste reset the TPM/Hello. Survives reboots (2026-07-26) — [#160](https://github.com/sirAndros/KeePassWinHello/issues/160).
- Others (11 issues):
  - `0x80090029` when creating the persistent key on Win10 1803 (2019-08-27) — [#33](https://github.com/sirAndros/KeePassWinHello/issues/33).
  - `0x8009000D` NTE_NO_KEY: key lost after PIN reset or a GPO change (2020-09-02) — [#60](https://github.com/sirAndros/KeePassWinHello/issues/60).
  - `0x80090026` NTE_INVALID_HANDLE after sleep (2021-01-08) — [#63](https://github.com/sirAndros/KeePassWinHello/issues/63); again in 2024-12, resolved by a later Windows Update — [#114](https://github.com/sirAndros/KeePassWinHello/issues/114).
  - `0x80090016` NTE_BAD_KEYSET while minimized (2021-06-18) — [#69](https://github.com/sirAndros/KeePassWinHello/issues/69).
  - `0x80090005` when setting `NCRYPT_WINDOW_HANDLE_PROPERTY` on 32-bit Windows (2021-08-08) — [#71](https://github.com/sirAndros/KeePassWinHello/issues/71).
  - `0x800704C7` on encrypt after a DB file move (2021-08-08) — [#72](https://github.com/sirAndros/KeePassWinHello/issues/72).
  - `0x80070102` WAIT_TIMEOUT from `NCryptFinalizeKey` (2022-07-10) — [#89](https://github.com/sirAndros/KeePassWinHello/issues/89).
  - `0x80098046` (2023-01-14) — [#93](https://github.com/sirAndros/KeePassWinHello/issues/93).
  - `0xD0000225` "after Windows updates" (2023-09-16) — [#101](https://github.com/sirAndros/KeePassWinHello/issues/101).
  - `0x8009002D` NTE_INTERNAL_ERROR (2025-02-09) — [#115](https://github.com/sirAndros/KeePassWinHello/issues/115).
  - Long timeout then abort. The maintainer said the error is thrown when the decrypted key length (`pbKey`) is not 32 (2023-05-22) — [#98](https://github.com/sirAndros/KeePassWinHello/issues/98).

**T3 – Credential Manager write (2 issues, open)**
- `CredWrite` error 8 (ERROR_NOT_ENOUGH_MEMORY) at lock (2021-05-03) — [#67](https://github.com/sirAndros/KeePassWinHello/issues/67). MrSco later traced error 8 to a full Credential Manager vault (2026-07-11) — [#120](https://github.com/sirAndros/KeePassWinHello/issues/120).
- `CredWrite` 0x520 (ERROR_NO_SUCH_LOGON_SESSION) pops up repeatedly, under an hour apart, with KeePass elevated (2021-10-19) — [#82](https://github.com/sirAndros/KeePassWinHello/issues/82).

**T4 – Focus, foreground, position (7 issues, 5 open; the longest-running UX complaint)**
- Prompt opens in the background (2019-01-17). Fixed in 2.2: the bug reproduced when KeePass ran elevated, because the prompt process is not elevated — [#11](https://github.com/sirAndros/KeePassWinHello/issues/11), [PR #24](https://github.com/sirAndros/KeePassWinHello/pull/24).
- Prompt hidden behind KeePass with "OK"/"Cancel" not clickable; the user must kill KeePass (2019-06-20, open) — [#25](https://github.com/sirAndros/KeePassWinHello/issues/25).
- Multi-monitor problems (2019-10-10, open): the prompt opens on the wrong monitor, the Auto-Type window drops behind, and the DB does not re-lock after Auto-Type — [#48](https://github.com/sirAndros/KeePassWinHello/issues/48).
- Prompt at the top-left of the screen when KeePass is minimized or in the tray (2020-04-15, open; 8 comments; still reproduces on 3.3 in 2024-11 with "minimize to tray" or "start minimized and locked") — [#54](https://github.com/sirAndros/KeePassWinHello/issues/54).
- `AllowSetForegroundWindow` ACCESS_DENIED when KeePassHttp triggers the unlock (2020-06-05; fixed 3.2) — [#56](https://github.com/sirAndros/KeePassWinHello/issues/56).
- Prompt has no focus when the global Auto-Type hotkey is used (2022-01-12, open, 10 comments), so the fingerprint is not read until the user clicks the prompt. KeeAgent SSH hangs while the prompt sits behind the terminal. The "show window" hotkey works but Auto-Type does not, because KeePass's hotkey handler only makes the window visible for ShowWindow. Two users said they moved to KeePassXC (verified) — [#86](https://github.com/sirAndros/KeePassWinHello/issues/86).
- On non-English Windows the foreground helper never finds the dialog, because it searches for the English title "Windows Security" (German: "Windows-Sicherheit") (2026-09-25) — [#86 comment](https://github.com/sirAndros/KeePassWinHello/issues/86), [PR #163](https://github.com/sirAndros/KeePassWinHello/pull/163).
- 3.3.1 regression (2024-04-08, open): KeePass is minimized to the tray after authentication, ignoring KeePass's own "minimize after opening" setting. Users downgraded to 3.2. Clicking the taskbar icon while locked no longer shows the prompt (verified) — [#107](https://github.com/sirAndros/KeePassWinHello/issues/107), [#110](https://github.com/sirAndros/KeePassWinHello/issues/110).

**T5 – Secure desktop (5 issues, all closed)**
- With "Enter master key on secure desktop", the Hello prompt is unreachable because it is on the normal desktop (2019-01-18; addressed in 2.1) — [#12](https://github.com/sirAndros/KeePassWinHello/issues/12).
- Duplicate WebDAV credential prompts and a missing Auto-Type dialog (fixed 3.1) — [#18](https://github.com/sirAndros/KeePassWinHello/issues/18).
- Duplicate Hello dialogs and a hung KeePass when another plugin triggers the unlock (closed "not confirmed" 2020-09-02) — [#19](https://github.com/sirAndros/KeePassWinHello/issues/19).
- The prompt leaves the secure desktop by design, because an external-process Hello prompt cannot be shown there (2019-10-31) — [#49](https://github.com/sirAndros/KeePassWinHello/issues/49).
- After the Win11 22H2 upgrade, "The master key is invalid!" pops up before every Hello unlock. A user traced it to secure-desktop mode. Fixed in 3.3 (2024-04-07) — [#92](https://github.com/sirAndros/KeePassWinHello/issues/92), [v3.3 notes](https://github.com/sirAndros/KeePassWinHello/releases/tag/v3.3).

**T7 – Elevated / UAC (4 issues, closed)**
- The plugin read the `EnableLUA` registry value to detect elevation and crashed (NullReferenceException) or reported "Windows Hello is disabled" when the value was absent:
  - Win10 Enterprise 1909, workaround was adding `EnableLUA=1` (2020-10-12) — [#61](https://github.com/sirAndros/KeePassWinHello/issues/61).
  - Win11, where the value was deleted again on reboot (2021-12-15) — [#85](https://github.com/sirAndros/KeePassWinHello/issues/85); duplicate [#99](https://github.com/sirAndros/KeePassWinHello/issues/99).
  - Also crashed on startup, and showed errors on Windows 7 (2023-09-15) — [#100](https://github.com/sirAndros/KeePassWinHello/issues/100).
- Fixed by commit "don't fall if UAC status is undefined" (2023-02-07) and "skip checks for WinHello on a priori not supported OS" (2024-04-01) — [commits](https://github.com/sirAndros/KeePassWinHello/commits/master).

**T8 – Hello availability and environment (5 issues)**
- Crash when Hello is absent (2018-08-31; fixed 1.0.1) — [#1](https://github.com/sirAndros/KeePassWinHello/issues/1).
- Windows Hello for Business policies break the convenience PIN. The fix is to enable "Convenience PIN sign-in" and disable WHfB on non-domain Pro/Enterprise PCs — [#2](https://github.com/sirAndros/KeePassWinHello/issues/2), [#17](https://github.com/sirAndros/KeePassWinHello/issues/17), [wiki "Windows Hello issues"](https://github.com/sirAndros/KeePassWinHello/wiki/Windows-Hello-issues).
- Fingerprint enrolled but no PIN means "Hello unavailable" (2023-02-24) — [#96](https://github.com/sirAndros/KeePassWinHello/issues/96).
- Unlocking with face while Zoom is using the camera froze or crashed Zoom; fixed by a Zoom update (2020-12-08) — [#62](https://github.com/sirAndros/KeePassWinHello/issues/62).

**T9 – Modality (4 issues, 2 open).** Users want to remember or force face, fingerprint or PIN, and to forbid PIN.
- Prompt always defaults to the folded-away fingerprint reader on a Surface (2019-01-20, open) — [#13](https://github.com/sirAndros/KeePassWinHello/issues/13).
- "Remove ability to sign in with windows PIN" (2019-12-08, open). The maintainer says the API offers no way to restrict methods — [#50](https://github.com/sirAndros/KeePassWinHello/issues/50).
- Duplicates [#53](https://github.com/sirAndros/KeePassWinHello/issues/53) and [#57](https://github.com/sirAndros/KeePassWinHello/issues/57).

**T10 – Persistent storage, startup unlock, revoke (7 issues)**
- "Hello at first open of KeePass" (2018-09-25, 24 comments, mostly +1). Delivered as the Credential Manager option in v3.0 (2019-08-26) — [#7](https://github.com/sirAndros/KeePassWinHello/issues/7).
- Hello only works if KeePass locks instead of exiting (2019-04-23) — [#20](https://github.com/sirAndros/KeePassWinHello/issues/20).
- Manual revoke (2018-11-15) — [#10](https://github.com/sirAndros/KeePassWinHello/issues/10).
- Option to not revoke the key on cancel (fixed 3.1) — [#30](https://github.com/sirAndros/KeePassWinHello/issues/30).
- Changing the Windows login password made the stored Credential Manager entries disappear (2020-05-19) — [#55](https://github.com/sirAndros/KeePassWinHello/issues/55).
- "When a key is deleted … Windows Hello appears" instead of the password prompt (title only, Win11, KeePass 2.61, 2026-03-25) — [#133](https://github.com/sirAndros/KeePassWinHello/issues/133).

**T12 – Expiry (2 issues)**
- The prompt "rarely" appeared because of a short valid period; setting it to a week solved it (2018-09-18) — [#3](https://github.com/sirAndros/KeePassWinHello/issues/3).
- Request for a 3-day option, removal of the 10-minute option, and a max-scan-attempts setting (2019-09-20, open) — [#40](https://github.com/sirAndros/KeePassWinHello/issues/40). "3 days" was added in [PR #45](https://github.com/sirAndros/KeePassWinHello/pull/45).

**T13 – Multiple databases (3 issues)**
- With KeeAutoOpen, Auto-Type unlocks a random secondary DB; solved with KeePass triggers (2019-01-29) — [#14](https://github.com/sirAndros/KeePassWinHello/issues/14).
- Enable Hello per database (2022-11-16, open; the maintainer says the settings UI would need rework) — [#91](https://github.com/sirAndros/KeePassWinHello/issues/91).
- Unlock all open DBs with one face scan (2024-07-18, open) — [#111](https://github.com/sirAndros/KeePassWinHello/issues/111).

**T14 – Key files / composite keys (1 issue)**
- The key-file path was overwritten in KeePass's last-used state; fixed by the v2.0 rewrite (2018-11 to 2018-12) — [#9](https://github.com/sirAndros/KeePassWinHello/issues/9).
- No issue mentions YubiKey, KeeChallenge or OtpKeyProv (keyword search over all 110 issues and 315 comments) — [tracker](https://github.com/sirAndros/KeePassWinHello/issues?q=yubikey).

**T15 – KeePass versions (6 issues)**
- 3.3 failed PLGX compilation on KeePass 2.56 ("state is invalid"); hotfix 3.3.1 the same day (2024-04-07) — [#105](https://github.com/sirAndros/KeePassWinHello/issues/105).
- Plugin vanished on 2.57/2.57.1; cured by "Clear plugin cache" (2024-06 to 2024-10, still open) — [#110](https://github.com/sirAndros/KeePassWinHello/issues/110).
- User errors: KeePass still running in the tray during install [#47](https://github.com/sirAndros/KeePassWinHello/issues/47); stale `KeePass.exe.config` [#58](https://github.com/sirAndros/KeePassWinHello/issues/58); "Incompatible with 2.49" unreproduced [#90](https://github.com/sirAndros/KeePassWinHello/issues/90); a `KeePass.config.enforced.xml` setting overrides the UI [#109](https://github.com/sirAndros/KeePassWinHello/issues/109).
- KeePass 2.60/2.61 appear only inside T1 and T2 error reports (#127, #130, #134, #135, #138, #160, #161, #162). No issue identifies a KeePass API break.

**T16 – Distribution (7 issues)**
- Chocolatey requested and delivered 2019-07 — [#22](https://github.com/sirAndros/KeePassWinHello/issues/22).
- `choco upgrade` registry-path failure (2019) — [#41](https://github.com/sirAndros/KeePassWinHello/issues/41).
- Chocolatey package not approved after 3.3; fixed 2024-04-19 — [#108](https://github.com/sirAndros/KeePassWinHello/issues/108).
- winget request (2021, open; the maintainer said winget needs a real installer) — [#65](https://github.com/sirAndros/KeePassWinHello/issues/65).
- Version shown as 2.0 for a 2.1 build — [#16](https://github.com/sirAndros/KeePassWinHello/issues/16).
- Cygwin/git clone confusion — [#4](https://github.com/sirAndros/KeePassWinHello/issues/4), [#5](https://github.com/sirAndros/KeePassWinHello/issues/5).

**T17 – Misc (9 issues)**
- The extra "OK" in Windows's own dialog cannot be removed — [#6](https://github.com/sirAndros/KeePassWinHello/issues/6).
- API questions about `NgcGetDefaultDecryptionKeyName` — [#8](https://github.com/sirAndros/KeePassWinHello/issues/8) — and the undocumented `NgcCacheType` — [#112](https://github.com/sirAndros/KeePassWinHello/issues/112).
- Cross-thread exception when debugging on Win11/VS2022 (fixed 3.3) — [#97](https://github.com/sirAndros/KeePassWinHello/issues/97).
- Request to trigger Auto-Type from a fingerprint scan — [#95](https://github.com/sirAndros/KeePassWinHello/issues/95).
- Skip KeePass's "Open From URL" dialog — [#117](https://github.com/sirAndros/KeePassWinHello/issues/117).

### Inferences
- At least 8 recent error reports are title-only or empty templates (#116, #118, #119, #122, #126, #128, #131, #139). This is probably because every unknown status raises a generic "Something went wrong. Please report the issue…" dialog with a Help button that opens a new GitHub issue ([PR #74](https://github.com/sirAndros/KeePassWinHello/pull/74), 2021). A new plugin should map known statuses to plain-language recovery messages instead of "report a bug".
- Several encrypt-side errors happen while KeePass is locking or closing: #129, #160 and #161 (`KeyManager.Lock` → `NCryptEncrypt`), and #67 and #82 (`CredWrite`). The design that re-wraps and re-stores the key on every lock adds a failure point at lock and exit time. Details are in section 4.
- "Works after re-enrolling fingerprint or face" (#121, #125) and "after Windows Update" (#101, #114, #120, #121) suggest the NGC container or biometric enrollment can change under the plugin. A new plugin needs a clear "Windows Hello changed, please re-register" path.

### Gaps
- Screenshots in many issues (#101, #115, #122, #126, #134) were not OCR'd. Error details that exist only in images were not captured.
- No reporter supplied event logs or enough data to tell whether #129/#131 (0x8007139F) happen with persistent or local storage.

---

## 2. What was fixed and how, what remains open, and maintainer activity

### Takeaway
Classic maintenance by the two authors, sirAndros and shuffle-c, ran from 2018 to 2024-06. The last release is v3.3.1 (2024-04-07), and every 0x80098044 face-unlock report (2024-10 onward) is unfixed in any release. In June 2026 an account named "SirAndrosBot" opened 16 AI-generated ("[codex]") PRs. Only an AGENTS.md was merged, and no release followed. Outside contributors (MrSco, blacksun80) and forks (MJRT, cuprum-72) now carry the 0x80098044, focus and expiry fixes.

### Cited Findings

**Releases** — [releases](https://github.com/sirAndros/KeePassWinHello/releases)

| Version | Date |
|---|---|
| v1.0 | 2018-08-26 |
| v1.0.1 | 2018-09-01 |
| v2.0 | 2018-12-14 (rewrite) |
| v2.1 | 2019-02-18 |
| 2.2 | 2019-06-13 |
| v3.0 | 2019-08-26 (persistent Credential Manager storage) |
| v3.1 | 2019-09-01 |
| v3.1.1 | 2019-09-10 |
| v3.2 | 2021-08-19 |
| v3.3 | 2024-04-07 |
| v3.3.1 | 2024-04-07 |

**Activity**
- The last human comment by sirAndros is 2024-06-03 ([#109](https://github.com/sirAndros/KeePassWinHello/issues/109)); shuffle-c's last is 2023-02-07 ([#93](https://github.com/sirAndros/KeePassWinHello/issues/93)). Both computed from the full comment dump.
- Last human code commit is 2024-06-01 (Chocolatey script fix). Last commit on master is 2026-06-11, "Add agent repository guidance (#142)" by SirAndrosBot — [commits](https://github.com/sirAndros/KeePassWinHello/commits/master), [PR #142](https://github.com/sirAndros/KeePassWinHello/pull/142).
- SirAndrosBot opened PRs #141 and #143–#158 between 2026-06-08 and 2026-06-21. Each states that no manual Windows Hello testing was done because the agent environment lacked prerequisites, or that manual testing is still needed. All are open — [PR #141](https://github.com/sirAndros/KeePassWinHello/pull/141), [PR #158](https://github.com/sirAndros/KeePassWinHello/pull/158).
- SirAndrosBot also answered issue #117 on 2026-06-23 — [#117](https://github.com/sirAndros/KeePassWinHello/issues/117).
- The 2021 refactoring PR #80 and the README PR #104 (2024-04-07) are still open. #104 is linked as the answer that closed issue #83, so the clarification was never merged — [PR #80](https://github.com/sirAndros/KeePassWinHello/pull/80), [PR #104](https://github.com/sirAndros/KeePassWinHello/pull/104), [#83](https://github.com/sirAndros/KeePassWinHello/issues/83).

**Fixed, with the design-level remedy**

*v1.0.1 / v2.x (2018–2019)*
- Graceful "Hello unavailable" — [#1](https://github.com/sirAndros/KeePassWinHello/issues/1).
- v2.0 rewrote the plugin from a key provider into a prompt hook, which fixed the key-file path problem — [#9](https://github.com/sirAndros/KeePassWinHello/issues/9).
- Secure-desktop escape in 2.1 — [#12](https://github.com/sirAndros/KeePassWinHello/issues/12).
- Elevated-process foreground in 2.2 — [#11](https://github.com/sirAndros/KeePassWinHello/issues/11), [PR #24](https://github.com/sirAndros/KeePassWinHello/pull/24).
- Revoke-all button — [PR #26](https://github.com/sirAndros/KeePassWinHello/pull/26).

*v3.0–3.1.1 (2019)*
- Persistent NGC key plus Credential Manager storage — [PR #28](https://github.com/sirAndros/KeePassWinHello/pull/28).
- Revoke-on-cancel made optional — [PR #35](https://github.com/sirAndros/KeePassWinHello/pull/35).
- Old property name used as fallback on Win10 ≤1803 (#33) — [PR #34](https://github.com/sirAndros/KeePassWinHello/pull/34).
- New secure-desktop algorithm that does not reopen the prompt (#18) — [PR #37](https://github.com/sirAndros/KeePassWinHello/pull/37).
- v3.1.1 security fix: the plugin now checks that the persistent key is non-importable and has mandatory authentication before using it — [#38](https://github.com/sirAndros/KeePassWinHello/issues/38), [v3.1.1](https://github.com/sirAndros/KeePassWinHello/releases/tag/v3.1.1).

*v3.2 (2021-08-19)*
- Retry up to 3 times with a 2-second delay on known transient errors. Decrypt: TPM_20_E_HANDLE, TPM_20_E_SIZE, 0x80280159. Encrypt: NTE_BAD_KEYSET, NTE_INVALID_HANDLE, ERROR_CANCELLED.
- Skip Hello in RDP sessions with a one-time warning (#51) — [PR #75](https://github.com/sirAndros/KeePassWinHello/pull/75).
- Fall back to local mode when the persistent key is missing (#60) — [PR #76](https://github.com/sirAndros/KeePassWinHello/pull/76).
- Ignore NTE_BAD_DATA when setting the window handle (#71).
- Sources: [v3.2 notes](https://github.com/sirAndros/KeePassWinHello/releases/tag/v3.2), source `WinHelloProvider.cs` ([src](https://github.com/sirAndros/KeePassWinHello/blob/master/src/AuthProviders/WinHelloProvider.cs)).

*v3.3 (2024-04-07)*
- Retry on 0x80098046 (#93) — [PR #94](https://github.com/sirAndros/KeePassWinHello/pull/94).
- UAC null tolerance (#85).
- Use the nearest visible KeePass window as the prompt parent through a "stack of UI contexts" (#48, #54); described as only lowering the probability of #25 and #86.
- Warning-suppression thread moved to the main desktop (#92).
- Sources: [PR #102](https://github.com/sirAndros/KeePassWinHello/pull/102), [PR #103](https://github.com/sirAndros/KeePassWinHello/pull/103), [v3.3 notes](https://github.com/sirAndros/KeePassWinHello/releases/tag/v3.3).
- That change introduced the #107 "minimized to tray" regression — [#107](https://github.com/sirAndros/KeePassWinHello/issues/107), [PR #140/141 description](https://github.com/sirAndros/KeePassWinHello/pull/141).

**Open, with no released fix as of 2026-10-18**
- T1 0x80098044. Unmerged PRs [#158](https://github.com/sirAndros/KeePassWinHello/pull/158) (retry) and [#159](https://github.com/sirAndros/KeePassWinHello/pull/159) (foreground enforcement).
- Focus and position: #25, #48, #54, #86, #107. Unmerged PRs [#141](https://github.com/sirAndros/KeePassWinHello/pull/141), [#147](https://github.com/sirAndros/KeePassWinHello/pull/147), [#149](https://github.com/sirAndros/KeePassWinHello/pull/149), [#163](https://github.com/sirAndros/KeePassWinHello/pull/163).
- CredWrite 0x8 and 0x520 — unmerged [#151](https://github.com/sirAndros/KeePassWinHello/pull/151) and [#152](https://github.com/sirAndros/KeePassWinHello/pull/152).
- 0x8007139F delete — unmerged [#153](https://github.com/sirAndros/KeePassWinHello/pull/153).
- WAIT_TIMEOUT treated as cancel — unmerged [#144](https://github.com/sirAndros/KeePassWinHello/pull/144).
- Bad 32-byte key — unmerged [#155](https://github.com/sirAndros/KeePassWinHello/pull/155).
- Per-database selection — unmerged [#154](https://github.com/sirAndros/KeePassWinHello/pull/154).
- winget — unmerged [#150](https://github.com/sirAndros/KeePassWinHello/pull/150).
- Docs PRs on modality limits ([#145](https://github.com/sirAndros/KeePassWinHello/pull/145), [#148](https://github.com/sirAndros/KeePassWinHello/pull/148)), the undocumented properties ([#157](https://github.com/sirAndros/KeePassWinHello/pull/157)), plugin cache ([#156](https://github.com/sirAndros/KeePassWinHello/pull/156)) and the recovery notice ([#143](https://github.com/sirAndros/KeePassWinHello/pull/143)).
- Remaining open error codes with no PR: 0x80070006, 0x80284001/2, 0xD000A002, 0xD0000225, 0x8009002D, and 0x80090010 with multiple fingerprint readers.

### Inferences
- The project is effectively in maintenance limbo. Users who need the face fix are pointed to forks or unmerged PRs (e.g. the MJRT fork README), which is a distribution and trust problem. A new plugin is entering a gap in which the incumbent has had no release for 2.5 years.
- Two fix strategies for 0x80098044 compete and neither has been validated on real hardware by the maintainer. #158 retries and preserves the key. #159 enforces the foreground and argues retry alone cannot work.

### Gaps
- It is not known whether the maintainer intends to merge the bot PRs or cut a 3.4 release. Nothing was stated publicly.
- Chocolatey package status after 2024-06 was not checked on chocolatey.org.

---

## 3. What the error codes mean

### Takeaway
Most codes are documented CNG, TPM Base Services or Win32 statuses. The headline code 0x80098044 is `WINBIO_E_INVALID_TICKET`, which exists in the Windows SDK metadata but is absent from Microsoft's public WinBio error table. 0x80280159 has no public name. Several codes are NTSTATUS values wrapped as HRESULTs (the 0xD000xxxx form).

### Cited Findings

**Code reference.** Message texts were resolved with `certutil -error` on Windows 11 build 26300 (German locale; translated here). Names were cross-checked against Microsoft Learn or the SDK metadata.

| Code | Name and meaning | Where seen | Source |
|---|---|---|---|
| 0x80098044 | `WINBIO_E_INVALID_TICKET` (SDK metadata). Not listed in Microsoft's Winbio_err.h table, which stops at 0x8009803D. certutil has no message text. PR #158 describes it as "biometric ticket is incorrect or expired". | T1 | [windows-rs 0.59.0 BiometricFramework metadata](https://raw.githubusercontent.com/microsoft/windows-rs/0.59.0/crates/libs/sys/src/Windows/Win32/Devices/BiometricFramework/mod.rs); [MS Client Error Codes](https://learn.microsoft.com/en-us/windows/win32/secbiomet/client-error-codes); [PR #158](https://github.com/sirAndros/KeePassWinHello/pull/158) |
| 0x80098046 | `WINBIO_E_DATA_PROTECTION_FAILURE` (SDK metadata). The plugin source comments it as "biometric service could not decrypt the data". Not in the public MS table. | #93 | [windows-rs metadata](https://raw.githubusercontent.com/microsoft/windows-rs/0.59.0/crates/libs/sys/src/Windows/Win32/Devices/BiometricFramework/mod.rs); [src](https://github.com/sirAndros/KeePassWinHello/blob/master/src/AuthProviders/WinHelloProvider.cs) |
| 0x80090031 | `NTE_AUTHENTICATION_IGNORED`. The #139 comment wrongly gave this name to 0x80098044. | — | [windows-rs Foundation](https://raw.githubusercontent.com/microsoft/windows-rs/0.59.0/crates/libs/sys/src/Windows/Win32/Foundation/mod.rs); [#139](https://github.com/sirAndros/KeePassWinHello/issues/139) |
| 0x8007139F | `HRESULT_FROM_WIN32(ERROR_INVALID_STATE 5023)`: "group or resource is not in the correct state to perform the requested operation" | #87, #129, #131 | certutil; [windows-rs](https://raw.githubusercontent.com/microsoft/windows-rs/0.59.0/crates/libs/sys/src/Windows/Win32/Foundation/mod.rs) |
| 0x80090010 | `NTE_PERM`: "Access denied" | #51, #64, #121, #123, #125 | [MS COM Error Codes (Security and Setup)](https://learn.microsoft.com/en-us/windows/win32/com/com-error-codes-4) |
| 0x80284001 | `TBS_E_INTERNAL_ERROR`: "An internal software error occurred" (TPM Base Services) | #161 | [MS TBS Return Codes](https://learn.microsoft.com/en-us/windows/win32/tbs/tbs-return-codes) |
| 0x80284002 | `TBS_E_BAD_PARAMETER`: "One or more parameter values are not valid" | #134 | [MS TBS Return Codes](https://learn.microsoft.com/en-us/windows/win32/tbs/tbs-return-codes) |
| 0x8028008B | `TPM_20_E_HANDLE`: "TPM 2.0: the handle is not correct for the use" | #68 | [windows-rs](https://raw.githubusercontent.com/microsoft/windows-rs/0.59.0/crates/libs/sys/src/Windows/Win32/Foundation/mod.rs); [#68](https://github.com/sirAndros/KeePassWinHello/issues/68) |
| 0x80280095 | `TPM_20_E_SIZE`: "TPM 2.0: the structure is the wrong size" | #77 | windows-rs; certutil |
| 0x80280159 | No public name. The TPM_20_E range in the SDK runs to 0x80280155. Plugin calls it `TPM_20_E_159`. | #42 | [windows-rs](https://raw.githubusercontent.com/microsoft/windows-rs/0.59.0/crates/libs/sys/src/Windows/Win32/Foundation/mod.rs) |
| 0x80090029 | `NTE_NOT_SUPPORTED` | #33 | [MS COM codes](https://learn.microsoft.com/en-us/windows/win32/com/com-error-codes-4) |
| 0x8009002D | `NTE_INTERNAL_ERROR`: "An internal consistency check failed" | #115 | [MS COM codes](https://learn.microsoft.com/en-us/windows/win32/com/com-error-codes-4) |
| 0x8009000D | `NTE_NO_KEY`: "Key does not exist" | #60 | [MS COM codes](https://learn.microsoft.com/en-us/windows/win32/com/com-error-codes-4) |
| 0x80090016 | `NTE_BAD_KEYSET`: "Keyset does not exist" | #69 | [MS COM codes](https://learn.microsoft.com/en-us/windows/win32/com/com-error-codes-4) |
| 0x80090026 | `NTE_INVALID_HANDLE` | #63, #114 | [MS COM codes](https://learn.microsoft.com/en-us/windows/win32/com/com-error-codes-4) |
| 0x80090005 | `NTE_BAD_DATA` | #71 | [MS COM codes](https://learn.microsoft.com/en-us/windows/win32/com/com-error-codes-4) |
| 0x80090036 | `NTE_USER_CANCELLED`. The plugin treats it as a cancel. | design | certutil; src |
| 0x80070006 | `HRESULT_FROM_WIN32(ERROR_INVALID_HANDLE 6)` | #122, #126, #162 | certutil |
| 0x800704C7 | `HRESULT_FROM_WIN32(ERROR_CANCELLED 1223)` | #72 | certutil |
| 0x80070102 | `HRESULT_FROM_WIN32(WAIT_TIMEOUT 258)`, from `NCryptFinalizeKey` | #89 | certutil; [PR #144](https://github.com/sirAndros/KeePassWinHello/pull/144) |
| 0xD0000225 | NTSTATUS `STATUS_NOT_FOUND` (0xC0000225) carried in an HRESULT through the FACILITY_NT bit | #101 | certutil |
| 0xD000A002 | NTSTATUS `STATUS_AUTH_TAG_MISMATCH` (0xC000A002): "computed authentication tag did not match" | #160 | certutil; [windows-rs](https://raw.githubusercontent.com/microsoft/windows-rs/0.59.0/crates/libs/sys/src/Windows/Win32/Foundation/mod.rs) |
| CredWrite 0x8 | `ERROR_NOT_ENOUGH_MEMORY` | #67 | certutil |
| CredWrite 0x520 | `ERROR_NO_SUCH_LOGON_SESSION` (1312) | #82 | certutil; [PR #152](https://github.com/sirAndros/KeePassWinHello/pull/152) |
| AllowSetForegroundWindow 0x5 | `ERROR_ACCESS_DENIED` | #56 | [#56](https://github.com/sirAndros/KeePassWinHello/issues/56) |

**Root-cause claims for 0x80098044 (competing)**
- MrSco ([PR #159](https://github.com/sirAndros/KeePassWinHello/pull/159), 2026-07-11) claims a foreground race:
  - The Hello dialog (class "Credential Dialog Xaml Host", hosted in another process) loses the foreground while KeePass's own windows appear at startup.
  - Windows then rejects a biometric ticket completed against a non-foreground dialog.
  - Evidence: 100% reproduction on Win11 25H2 on the first prompt of a fresh launch. A standalone console harness replaying the same NCrypt sequence never fails.
  - Face scans instantly, inside the race window. PIN entry takes seconds, by which time window activation has settled.
  - It cites Bitwarden as confirming the same failure mode ("Windows Hello fails if the prompt is not in the foreground"). Not independently verified here.
- SirAndrosBot ([PR #158](https://github.com/sirAndros/KeePassWinHello/pull/158)) treats it as a short-lived ticket problem. It retries 3 times with a 2-second delay and keeps the stored key, arguing that revoking would destroy valid credentials because PIN still decrypts.
- A third proposal (comment on #139) is to reclassify the error as an "invalid key" and revoke all keys — [#139](https://github.com/sirAndros/KeePassWinHello/issues/139). It is contradicted by the evidence that PIN still decrypts the same key (#120, #130).

### Inferences
- Because PIN decrypts the same key that face cannot, 0x80098044 is about the authorization gesture or ticket, not key corruption. A new plugin should not revoke stored material on this code.
- 0xD000A002 after a CPU repaste and an fTPM reset, together with "TPM not loadable" in #134, indicate that the TPM-bound NGC keys became unusable. The right response is "Windows Hello keys were reset; re-register", not a bug report.

### Gaps
- Microsoft publishes no description of WINBIO_E_INVALID_TICKET or WINBIO_E_DATA_PROTECTION_FAILURE on Learn. Meanings come from SDK metadata names and community PRs.
- No public source names 0x80280159.

---

## 4. Technical design (paraphrased; MIT-licensed source, no code copied)

### Takeaway
KeePassWinHello is not a KeePass key provider. It hooks KeePass's master-key prompt window and its file-closing event. On lock or close it captures the database's in-memory CompositeKey. It encrypts each component with AES under a fresh random 256-bit secret, and wraps that secret with an RSA key held by the "Microsoft Passport Key Storage Provider" (NGC/Windows Hello). On the next prompt it decrypts with an NCrypt call that triggers the Hello UI, injects the rebuilt CompositeKey into the KeePass prompt by reflection, and closes it. Storage is either process memory or Windows Credential Manager. The key name and some properties rely on undocumented Passport/NGC APIs.

### Cited Findings

**Hooks** — [KeePassWinHelloExt.cs](https://github.com/sirAndros/KeePassWinHello/blob/master/src/KeePassWinHelloExt.cs)
- The plugin subscribes to `MainWindow.FileClosingPre` to capture the key on lock or close, and to `GlobalWindowManager.WindowAdded` to detect `KeyPromptForm` (unlock) and `OptionsForm` (adds a "WindowsHello" options tab).
- It reads the DB path and injects the key through reflection on KeePass's private `KeyPromptForm` fields (the IO-info field and the key field). It then hides the form, sets it to "OK" and closes it.
- It checks whether the closing event is a "Locking" flag through reflection on `FileClosingEventArgs.Flags`.
- Source: [KeyManager.cs](https://github.com/sirAndros/KeePassWinHello/blob/master/src/KeyManagement/KeyManager.cs).

**Envelope** — [ProtectedKey.cs](https://github.com/sirAndros/KeePassWinHello/blob/master/src/KeyManagement/ProtectedKey.cs), [KeyCipher.cs](https://github.com/sirAndros/KeePassWinHello/blob/master/src/KeyManagement/KeyCipher.cs)
- A random 32-byte secret is generated per stored record and wrapped by the Hello key (RSA, PKCS#1 padding).
- Each composite-key component is AES-encrypted with KeePass's AES engine under that secret, with a fixed all-zero 16-byte IV:
  - password: encrypted as text;
  - key file: only the file path is stored, not the file content;
  - custom key-provider bytes: the provider's output is cached;
  - Windows user account: a marker only.
- The record is serialized with .NET BinaryFormatter (a custom binder restricts types). Plaintext buffers are zeroed after use.
- A record that decrypts to a secret of the wrong length reaches KeePass AES as `pbKey != 32`, which is the cause of #98 — [#98](https://github.com/sirAndros/KeePassWinHello/issues/98), [PR #155](https://github.com/sirAndros/KeePassWinHello/pull/155).

**NCrypt usage** — [WinHelloProvider.cs](https://github.com/sirAndros/KeePassWinHello/blob/master/src/AuthProviders/WinHelloProvider.cs)
- Provider is opened by the name "Microsoft Passport Key Storage Provider".
- "Local" (memory) mode uses the user's *default* NGC decryption key, found through the undocumented `cryptngc.dll!NgcGetDefaultDecryptionKeyName(SID)`. If that call returns nothing, Hello is treated as unavailable. Windows versions below 10 are rejected — [#8](https://github.com/sirAndros/KeePassWinHello/issues/8).
- "Persistent" mode creates its own NGC key:
  - name `<SID>//KeePassWinHello//KeePassWinHello`, confirmed by MrSco deleting it with `certutil -user -csp "Microsoft Passport Key Storage Provider" -delkey` ([#120](https://github.com/sirAndros/KeePassWinHello/issues/120));
  - RSA 2048;
  - usage decrypt and sign;
  - an `NgcCacheType` property set to 1 ("auth mandatory"), falling back to the property name `NgcCacheTypeProperty` on older builds such as 1803 (#33);
  - the window handle and a "use context" message are set before `NCryptFinalizeKey`, which shows the Hello prompt.
- Encrypt opens the key silently (public-key operation, no prompt).
- Decrypt opens the key normally, sets these properties, then calls `NCryptDecrypt`, which shows Hello:
  - `HWND Handle` (parent window);
  - `Use Context` (the text "Authentication to access KeePass database", plus "Failed to authenticate, please try again." on retries);
  - `PinCacheIsGestureRequired`=1.
- Before each use of the persistent key, an integrity check confirms the key is not importable and that the cache type is "auth mandatory" (the #38 fix).
- PR #157 notes that `NgcCacheType`/`NgcCacheTypeProperty` are not in Microsoft's public property docs and should not be assumed stable — [PR #157](https://github.com/sirAndros/KeePassWinHello/pull/157). An external developer asked Microsoft Q&A about this — [#112](https://github.com/sirAndros/KeePassWinHello/issues/112).
- Per a maintainer-side comment, no NCrypt property selects face, fingerprint or PIN. The WinBio APIs cannot authorize an NGC decrypt — [PR #145 comment](https://github.com/sirAndros/KeePassWinHello/pull/145).

**Storage and expiry** — [KeyWindowsStorage.cs](https://github.com/sirAndros/KeePassWinHello/blob/master/src/KeyManagement/Storage/KeyWindowsStorage.cs), [KeyMemoryStorage.cs](https://github.com/sirAndros/KeePassWinHello/blob/master/src/KeyManagement/Storage/KeyMemoryStorage.cs), [Settings.cs](https://github.com/sirAndros/KeePassWinHello/blob/master/src/Settings/Settings.cs)
- Memory mode uses a dictionary keyed by DB path.
- Credential Manager mode stores a generic credential:
  - target `KeePassWinHello_<db path>`, username "KeePassWinHello";
  - persistence `CRED_PERSIST_LOCAL_MACHINE`;
  - blob capped at 2,560 bytes.
- Expiry is measured from the record's write time: `LastWritten` for Credential Manager, creation time in memory.
- Valid period defaults to 24 h. "Unlimited" is supported, and a 3-day option was added in 2019 ([PR #45](https://github.com/sirAndros/KeePassWinHello/pull/45)).
- Settings live in KeePass `CustomConfig` keys `WindowsHello.QuickUnlock.*`: ValidPeriod, Enabled (default on), WindowsStorage.Enabled (default off), RevokeOnCancel (default on).
- On every lock, and on close in persistent mode, the plugin re-creates and re-stores the record. In local mode a plain close removes it.
- Fork commit 868f6eba9e (2026-09-03) states that "the saved key expiration period was unintentionally reset every time the database was locked." That fork renews only after a manual authentication — [cuprum-72 fork](https://github.com/cuprum-72/KeePassWinHello/commits/master).

**Secure desktop** — [KeyManager.cs](https://github.com/sirAndros/KeePassWinHello/blob/master/src/KeyManagement/KeyManager.cs), [KeePassWarningSuppresser.cs](https://github.com/sirAndros/KeePassWinHello/blob/master/src/KeyManagement/KeePassWarningSuppresser.cs)
- If the prompt is on the secure desktop and a cached key exists, the plugin temporarily edits KeePass's runtime config: it turns off `MasterKeyOnSecureDesktop` and raises `MasterKeyTries` by one.
- It closes the secure prompt with an empty key, so KeePass re-prompts on the normal desktop where Hello can run.
- A background thread polls every 10 ms for the KeePass "invalid key" message box (dialog class "#32770", title "KeePass") and closes it. The original settings are restored later.
- The maintainers state the Hello prompt is an external process and cannot appear on the secure desktop — [#49](https://github.com/sirAndros/KeePassWinHello/issues/49).

**Foreground**
- When elevated, a decorator grants `AllowSetForegroundWindow` to any process, searches for up to 2 s for a window of class "Credential Dialog Xaml Host" titled "Windows Security", forces it to the foreground, and afterwards re-foregrounds the KeePass window.
- The English-title search fails on localized Windows — [WinHelloProviderForegroundDecorator.cs](https://github.com/sirAndros/KeePassWinHello/blob/master/src/AuthProviders/WinHelloProviderForegroundDecorator.cs), [PR #163](https://github.com/sirAndros/KeePassWinHello/pull/163).
- PR #159 says this decorator only activates when KeePass is elevated, and only once per unlock — [PR #159](https://github.com/sirAndros/KeePassWinHello/pull/159).

**RDP and errors**
- RDP is detected through `SystemInformation.TerminalServerSession`. In a remote session Hello is skipped and keys are not saved on close.
- Error mapping: user-cancel → cancel; NTE_NO_KEY → "key not found", fall back to local mode and turn off persistent storage; integrity failure → revoke and fall back to local. Everything else raises a generic exception dialog — [KeyManager.cs](https://github.com/sirAndros/KeePassWinHello/blob/master/src/KeyManagement/KeyManager.cs).
- Any non-cancel exception during unlock removes the cached key for that DB (catch-all in ExtractCompositeKey) — [KeyManager.cs](https://github.com/sirAndros/KeePassWinHello/blob/master/src/KeyManagement/KeyManager.cs).

**Security notice and packaging**
- The README tells users to treat an unrequested Hello prompt like an unrequested UAC prompt, because any process can request a Hello decrypt — [README](https://github.com/sirAndros/KeePassWinHello/blob/master/ReadMe.md).
- The maintainer opened an issue proposing a user-defined phrase in the prompt so users can verify where it came from — [#106](https://github.com/sirAndros/KeePassWinHello/issues/106).
- Ships as PLGX, compiled by KeePass at load. The project targets .NET Framework 4.0 / C# 5 per the agent guidance PR — [PR #142](https://github.com/sirAndros/KeePassWinHello/pull/142).
- shuffle-c chose the native API over UWP's KeyCredentialManager because a PLGX plugin compiled by KeePass cannot use the UWP stack — [#7 comment](https://github.com/sirAndros/KeePassWinHello/issues/7).

### Inferences
- **Fragile coupling.** Reflection on private KeePass fields and on the `FileClosingEventArgs` flags, temporary edits to KeePass's security config, closing KeePass message boxes by class and title, and finding the Hello dialog by English title are all brittle. The #105 PLGX break, the #92 warning leak and the #163 localization bug are examples. A new plugin should prefer public KeePass APIs (a KeyProvider, or an official "pre-filled key" path if one exists) and window-class plus process matching.
- **Sliding expiry.** Re-encrypting at every lock both resets expiry (a user who locks often never has to re-enter the master password) and multiplies encrypt-time failures (#129, #160, #161, #67, #82). A new plugin could store the record once at manual unlock, keep an absolute expiry, and avoid touching storage on lock.
- **Weak spots in the envelope:**
  - Storing only the key-file path means unlock silently depends on the file still existing at that path.
  - A zero IV with one AES key for several fields in a record is avoidable.
  - BinaryFormatter is deprecated in modern .NET.
  - Keying records by DB path breaks on move or rename (#72, PR #154 notes).
- **Local mode uses the user's default NGC key, not an app-specific key**, so any process running as the user could request a decrypt of that blob (consistent with #106). A plugin-specific NGC key with a distinctive "Use Context" message gives the user at least a cue.

### Gaps
- Not verified: whether `NgcGetDefaultDecryptionKeyName` or the NgcCacheType properties behave differently on Windows 11 24H2/25H2 with Enhanced Sign-in Security. MrSco's machine had ESS off — [PR #159](https://github.com/sirAndros/KeePassWinHello/pull/159).
- No Microsoft documentation was found for the "Microsoft Passport Key Storage Provider" property set the plugin relies on beyond generic NCrypt docs. That gap was itself confirmed by PR #157.

---

## 5. Notable forks and what they changed

### Takeaway
Of 30 forks, three carry real changes. MrSco is the source of PR #159 (foreground enforcer for 0x80098044). MJRT bundles #159 and #163 into prebuilt PLGX releases. cuprum-72 (self-described non-developer, "code vibing") rewrote the focus handling and fixed sliding expiry. Older forks (Angelelz, SeppPenner) are historical.

### Cited Findings

Fork statistics come from `/forks` and `/compare` API calls on 2026-10-18 — [network](https://github.com/sirAndros/KeePassWinHello/forks).

- **MrSco/KeePassWinHello** (created 2026-03-11, pushed 2026-07-11, 1 commit ahead). Adds a "credential dialog foreground enforcer" around every decrypt prompt:
  - grants foreground rights and re-pushes the Hello dialog to the front every 100 ms for about 2 s, stopping once it is foreground;
  - on 0x80098044, brings the parent to the front and re-prompts immediately;
  - after retries are exhausted, raises a friendly "use PIN" exception;
  - includes a console harness, "HelloTicketProbe", and a .NET 4.0-compatible fix in KeyManager.
  - Upstream as [PR #159](https://github.com/sirAndros/KeePassWinHello/pull/159) (open). The author tested it on hardware that reproduces the bug every time.
  - [blacksun80, PR #163](https://github.com/sirAndros/KeePassWinHello/pull/163) notes that #159 also hard-codes the English dialog title.
- **MJRT/KeePassWinHello** (created and pushed 2026-10-03, 6 commits ahead): #159 + #163 + Sonar cleanups + a GitHub Actions workflow that builds a patched PLGX. Its README says it "adds fixes for Windows Hello face unlock failures seen as NCryptDecrypt: 0x80098044 … on a fresh KeePass launch" and offers prebuilt `.plgx` releases — [MJRT README](https://github.com/MJRT/KeePassWinHello).
- **cuprum-72/KeePassWinHello** (created 2026-06-05, pushed 2026-09-03, 3 commits ahead; ships a "3.3.2"). Per its ReleaseNotes:
  - preserves the KeePass window state after Hello (#107);
  - waits until KeePass's KeyPromptForm is shown before starting Hello, so the owner window is visible and active (#86);
  - removes global foreground permission, synthetic Alt key input, forced restore/minimize, and localized title searches;
  - makes UI-context handling thread-safe;
  - stops expiry from resetting on each lock.
  - The commit message says "Using code vibing as I'm not a developer." It also commits a `KeePassWinHello.dll` binary — [cuprum-72 ReleaseNotes](https://github.com/cuprum-72/KeePassWinHello/blob/master/ReleaseNotes.md), [commits](https://github.com/cuprum-72/KeePassWinHello/commits/master).
- **blacksun80/KeePassWinHello** (2026-09-25): no difference on master. The work is in PR #163, which matches the dialog by window class only and takes the dialog that appears after the prompt starts. Even with the right window, a tray-minimized KeePass on German Win11 still did not get focus. What reliably worked was first bringing KeePass's own main window forward using `AttachThreadInput` with the foreground thread (the approach KeePassRPC uses) — [PR #163](https://github.com/sirAndros/KeePassWinHello/pull/163), [#86 comment](https://github.com/sirAndros/KeePassWinHello/issues/86).
- Historical:
  - **Angelelz** built "WinHelloUnlock" (UWP KeyCredentialManager based, "heavily based on KeePassWinHello") and reported the #38 persistent-key spoofing flaw — [#7](https://github.com/sirAndros/KeePassWinHello/issues/7), [#38](https://github.com/sirAndros/KeePassWinHello/issues/38).
  - The #38 write-up says an attacker running as the same user could replace the persistent key with one not requiring Hello. Credentials re-saved at the next lock would then be decryptable without a prompt. The integrity check existed but checked the wrong value — [Angelelz/SecurityTests#1](https://github.com/Angelelz/SecurityTests/issues/1).
  - **SeppPenner** turned the code into a "WindowsHello" library — [#7](https://github.com/sirAndros/KeePassWinHello/issues/7).
  - **davidegiacometti** proposed centering the prompt on the desktop window. Closed; it did not work on Win11 — [PR #88](https://github.com/sirAndros/KeePassWinHello/pull/88).
- The remaining forks (Tom-V, YR1044, Tsuki-polaris, and others) are identical to upstream or not reachable through compare — [forks](https://github.com/sirAndros/KeePassWinHello/forks).

### Inferences
- Users now pick between an unmaintained upstream, AI-generated upstream PRs and community forks with self-built binaries. A trustworthy, signed and tested release channel is itself a differentiator.
- The forks and #163 agree on what fixes focus: make KeePass's own window foreground and active before calling NCrypt, and identify the Hello dialog by class and process rather than title.

### Gaps
- None of the fork fixes were tested here. Their effectiveness rests on the authors' reports.
- Forks hosted outside GitHub, or not registered as forks, were not searched.

---

## 6. Feature requests users keep asking for

### Takeaway
The most requested features are:
1. Unlock at KeePass startup (persistent) — 24-comment thread, delivered in 2019.
2. Choosing or remembering face vs fingerprint vs PIN, and disabling PIN.
3. Per-database enablement and unlock-all-at-once.
4. Reliable focus and position of the prompt.
5. Fine-grained expiry options.
6. Distribution through package managers.

### Cited Findings
- **Startup unlock without the master password:** [#7](https://github.com/sirAndros/KeePassWinHello/issues/7) (2018-09-25 to 2019-08-26, many +1), [#23](https://github.com/sirAndros/KeePassWinHello/issues/23), [#55](https://github.com/sirAndros/KeePassWinHello/issues/55), [#21](https://github.com/sirAndros/KeePassWinHello/pull/21). Delivered in v3.0.
- **Choose or remember the method:** [#13](https://github.com/sirAndros/KeePassWinHello/issues/13) (2019, still open), [#53](https://github.com/sirAndros/KeePassWinHello/issues/53) (2020). Force PIN when face fails: [#120](https://github.com/sirAndros/KeePassWinHello/issues/120) (2025).
- **Disallow PIN:** [#50](https://github.com/sirAndros/KeePassWinHello/issues/50) (2019, 7 comments), [#57](https://github.com/sirAndros/KeePassWinHello/issues/57) (2020).
- **Max scan attempts, then fall back to password; 3-day period:** [#40](https://github.com/sirAndros/KeePassWinHello/issues/40) (2019).
- **Per-database enablement:** [#91](https://github.com/sirAndros/KeePassWinHello/issues/91) (2022), [#14](https://github.com/sirAndros/KeePassWinHello/issues/14) (2019). **Unlock all DBs at once:** [#111](https://github.com/sirAndros/KeePassWinHello/issues/111) (2024).
- **Prompt in front, centered, on the current monitor; Auto-Type window in front; re-lock after Auto-Type:** [#48](https://github.com/sirAndros/KeePassWinHello/issues/48), [#54](https://github.com/sirAndros/KeePassWinHello/issues/54) (a suggested "always on top" option), [#86](https://github.com/sirAndros/KeePassWinHello/issues/86).
- **Use Hello over RDP on Windows Server 2022+:** [#124](https://github.com/sirAndros/KeePassWinHello/issues/124) (2025).
- **Manual revoke; don't revoke on cancel:** [#10](https://github.com/sirAndros/KeePassWinHello/issues/10), [#30](https://github.com/sirAndros/KeePassWinHello/issues/30) (both delivered).
- **Prompt-origin verification, a user-chosen phrase shown in the Hello prompt (maintainer's own idea):** [#106](https://github.com/sirAndros/KeePassWinHello/issues/106) (2024). **Master-password recovery reminder:** [#59](https://github.com/sirAndros/KeePassWinHello/issues/59) (2020; PR [#143](https://github.com/sirAndros/KeePassWinHello/pull/143)).
- **Distribution:** Chocolatey [#22](https://github.com/sirAndros/KeePassWinHello/issues/22) (done), winget [#65](https://github.com/sirAndros/KeePassWinHello/issues/65) (open since 2021).
- **Trigger Auto-Type straight from a fingerprint scan:** [#95](https://github.com/sirAndros/KeePassWinHello/issues/95). Answered as not possible, because Hello is on-demand and cannot poll the sensor.
- **Documentation of how keys are stored in memory and Credential Manager:** [#83](https://github.com/sirAndros/KeePassWinHello/issues/83). **Better UX than the superfluous OK:** [#6](https://github.com/sirAndros/KeePassWinHello/issues/6).
- **Comparisons users make:**
  - KeePassXC's built-in Hello was cited as working better for focus — [#86](https://github.com/sirAndros/KeePassWinHello/issues/86).
  - KeePassXC was cited as lacking persistent keys, which drove a user to this plugin — [#123](https://github.com/sirAndros/KeePassWinHello/issues/123).
  - Reviewers repeatedly ask for clearer error messages instead of "report the issue" dialogs (implicit in PRs [#151](https://github.com/sirAndros/KeePassWinHello/pull/151) and [#152](https://github.com/sirAndros/KeePassWinHello/pull/152)).

### Inferences
- Method selection and PIN-disallow cannot be honestly provided through the NGC/NCrypt path, per [PR #145](https://github.com/sirAndros/KeePassWinHello/pull/145) and [PR #148](https://github.com/sirAndros/KeePassWinHello/pull/148). A new plugin should say this upfront rather than offer a checkbox it cannot enforce.
- Per-database opt-in, an absolute (non-sliding) expiry, a re-lock policy after Auto-Type, and a visible "Windows Hello changed, please re-register" recovery flow are all achievable inside a plugin and are clearly wanted.

### Gaps
- No usage or download statistics per feature were available. "Most requested" rests on issue and comment counts, not user surveys.
- Requests made on other forums (KeePass SourceForge discussion, Reddit) were not covered. The only one seen is the SourceForge thread linked from [#92](https://github.com/sirAndros/KeePassWinHello/issues/92).
