# Miftawerk Unlock

**Unlock KeePass 2 with Windows Hello – face, fingerprint or Windows Hello PIN.**
A free, open-source KeePass 2 plugin by [SeeColors iT](https://seecolors.de), MIT license.

> **Status: early development (alpha).** Not ready for production databases yet.
> Please test only with a test database.

## What it does

1. You open a database once with your master password as usual.
2. Miftawerk Unlock asks: *"Enable Windows Hello quick unlock for this database?"* (opt-in, default *No*).
3. Next time KeePass asks for the master key, Windows Hello appears on top of the prompt.
   Face or fingerprint → the database opens. Cancel → you type the master password as always.
4. Quick unlock ends automatically after **7 days** or when **Windows restarts**.
   Then the master password is required again.
5. KeePass is **locked when Windows is locked** (Win+L), the session disconnects or the PC goes
   to sleep (implemented, test pending).

## How it is built

- **Windows Hello key in the TPM.** The plugin creates its own key with the Windows
  `KeyCredentialManager` API. Windows keeps the private key (in the TPM where one is present,
  confirmed by attestation on our test machine). The plugin only receives signatures.
- **Nothing readable on disk.** The database key material is stored encrypted
  (HKDF-SHA256 → AES-256-CBC + HMAC-SHA256) with a key derived from a fresh Windows Hello
  signature, then wrapped again with Windows DPAPI, in `%LOCALAPPDATA%\SeeColors\MiftawerkUnlock`.
  The plain master password and key file paths are never stored.
- **Never writes to your database file.** Quick unlock rebuilds your existing master key;
  your `.kdbx` stays exactly as KeePass wrote it.
- **Fail-safe.** Any error, cancel, expiry or Windows Hello reset simply falls back to the
  normal master password prompt.
- **Windows Hello dialog in front.** The dialog is brought to the front of KeePass
  (matched by window class, independent of the Windows language).
- **Written from scratch.** No code copied from other projects. Design decisions are based on
  documented user reports of existing solutions, see [`reports/`](reports/) and
  [`docs/phase0/`](docs/phase0/).

## Honest security notes

- **Windows Hello always accepts the Windows Hello PIN.** Anyone who knows your Windows Hello PIN
  and can use your Windows session can open an enabled database while quick unlock is valid.
  This is a **convenience feature, not a second factor.** For a real second factor, a YubiKey
  mode is planned.
- Malware running as your Windows user can read KeePass memory after any unlock. No quick-unlock
  design protects against that.
- An unexpected Windows Hello prompt should be treated like an unexpected UAC prompt: cancel it.

## Privacy (GDPR)

- **No biometric data reaches the plugin.** Face and fingerprint recognition happen entirely inside
  Windows Hello; according to Microsoft, biometric data does not leave the device.
  The plugin only receives a signature or an error status.
- **No telemetry, no network access.** The plugin does not send anything anywhere.

## Requirements

- Windows 10/11 with Windows Hello set up (face, fingerprint or PIN)
- KeePass 2.60 or newer (tested with 2.60), .NET Framework 4.8

### Smart App Control

Windows 11 *Smart App Control* blocks unsigned plugin DLLs. Signed releases are planned
(see *Code signing policy*). Until then, the plugin cannot load on PCs with Smart App Control
enabled.

## Roadmap

- [x] Windows Hello quick unlock with expiry (days / Windows restart)
- [ ] Lock KeePass when Windows locks (implemented, test pending)
- [ ] Signed releases
- [ ] Settings (validity period), German UI
- [ ] YubiKey challenge-response as a real second factor (KeePassXC-compatible format),
      mandatory backup key, lock when the key is removed
- [ ] Integration with the browser extension *Miftawerk Connect*

## Build

Requires the .NET SDK (no Visual Studio needed).

```
dotnet build src/MiftawerkUnlock/MiftawerkUnlock.csproj -c Release
```

The build references `KeePass.exe` from `testenv/KeePass/` (an isolated portable KeePass copy,
not part of the repository).

## Code signing policy

Planned: free code signing provided by [SignPath.io](https://about.signpath.io),
certificate by [SignPath Foundation](https://signpath.org). Only binaries built automatically
from this repository will be signed. Details will be published here before the first signed release.

## Deutsch (Kurzfassung)

Miftawerk Unlock entsperrt KeePass 2 mit Windows Hello (Gesicht, Fingerabdruck, PIN).
Opt-in pro Datenbank, Ablauf nach 7 Tagen oder Windows-Neustart, Sperre beim Sperren von Windows.
Biometrische Daten verlassen Windows nicht; keine Telemetrie. Windows Hello akzeptiert immer auch
die PIN – Komfortfunktion, kein zweiter Faktor. Status: Alpha, nur mit Testdatenbank verwenden.

## License

MIT – see [LICENSE](LICENSE). © 2026 SeeColors iT.
KeePass is a project by Dominik Reichl; Miftawerk Unlock is an independent plugin.
