# Phase 0 – Experiment results (spike `HelloSelfTest`)

Machine: Windows 11 Pro build 26300, KeePass 2.60 (isolated portable copy in `testenv/`), 64-bit process.
Plugin: `src/MiftawerkUnlock` v0.0.1, Microsoft.Windows.SDK.Contracts 10.0.26100.9169, net48.

## Run 2026-10-18 16:30 and 16:31 (fingerprint and face, KeePass in foreground)

| Check | Result |
|---|---|
| `KeyCredentialManager.IsSupportedAsync` | True |
| Prompt stays open inside KeePass.exe (exp. 1, cf. KeePassProtectedKeyStore #6) | **Pass** – create + 3 signs succeeded in both runs |
| Attestation | Success (key is TPM-backed) |
| Key | RSA 2048, signature 256 bytes |
| Deterministic signatures within one session (exp. 2, partial) | **Pass** – 3/3 identical in both runs |
| Padding | PKCS#1 v1.5 = True, PSS = False → the class-page claim "RSA PSS" does not hold here |
| Foreground (exp. 3, KeePass in foreground only) | Pass – `CredentialUIBroker/Credential Dialog Xaml Host` became foreground in every prompted call, without any workaround |

Observations:

- Run 2: `sign 1` finished in 435 ms directly after `RequestCreate`, with no visible prompt.
  Windows Hello reuses the fresh gesture for a short time. Design consequence: the unlock flow must not
  assume that every `RequestSignAsync` shows a prompt (no "user was asked" claims based on the call alone).
- `Environment.OSVersion` reports 6.2.9200 (no compatibility manifest in KeePass.exe). Use `RtlGetVersion`
  if the real build number is needed.

## Run ~17:20 (no foreground fix): prompt behind KeePass

The user reported the Hello dialog opening **behind** the KeePass window (screenshots). Same failure as
KeePassWinHello #86 / KeePassXC #9785. Baseline without workaround is therefore not reliable.

## Run 2026-10-18 17:28:13 and 17:28:43 (with foreground fix, after KeePass restart)

Fix: while a window of this process holds the foreground and a visible window of class
`Credential Dialog Xaml Host` owned by `CredentialUIBroker` exists, call `AllowSetForegroundWindow`
+ `SetForegroundWindow` on it (150 ms timer). Matching by class + process, not by localized title.

| Check | Result |
|---|---|
| Prompt in front | **Pass** – "pushed to front: 1x" on every sign, dialog visible on top (fingerprint and face) |
| Same signature as previous run after KeePass restart (exp. 2, restart part) | **Pass** – True in both runs |
| Deterministic within session | Pass – 3/3 |
| Padding | PKCS#1 v1.5 only |

## Still open

- Exp. 2 across sleep, Fast Startup shutdown and reboot (stored challenge is in place).
- Exp. 3 with KeePass minimized / in tray / Auto-Type hotkey / triggered from another app.
- Exp. 4 face on the first prompt after a cold KeePass start (0x80098044 pattern), 20 runs.
- Exp. 5 boot identifier vs Fast Startup. Exp. 6 public-API-only unlock incl. secure desktop.
