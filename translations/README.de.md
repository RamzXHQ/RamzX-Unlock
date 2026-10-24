<div align="center">

<img src="../assets/banner.svg" alt="RamzX Unlock - KeePass Windows Hello plugin by RamzX" width="100%">

# RamzX Unlock

### KeePass Windows-Hello-Plugin – KeePass 2 mit Gesicht oder Fingerabdruck entsperren

[English](../README.md) · **Deutsch** · [العربية](README.ar.md) · [Français](README.fr.md) · [Español](README.es.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) · [Polski](README.pl.md) · [Português](README.pt-BR.md) · [Türkçe](README.tr.md)

Kostenlos und Open Source · Entwickelt von **[RamzX](https://ramzx.de)**

</div>

---

**RamzX Unlock** ist ein kostenloses Open-Source-Plugin für **KeePass 2** (KeePass Password Safe).
Damit entsperren Sie Ihre KeePass-Datenbank mit **Windows Hello** – **Gesichtserkennung,
Fingerabdruck oder Windows-Hello-PIN** – statt jedes Mal das Master-Passwort einzutippen.
Der Windows-Hello-Schlüssel liegt im **TPM**, die Schnellentsperrung **läuft automatisch ab**,
und KeePass wird **gesperrt, wenn Sie Windows sperren**.

> **Status: Alpha.** In unseren Tests unter Windows 11 mit Gesicht und Fingerabdruck funktionsfähig.
> Bitte bis zur ersten signierten Version nur mit einer Testdatenbank verwenden.

## Funktionen

- 🔓 **Entsperren mit Windows Hello** – Gesicht, Fingerabdruck oder PIN direkt am KeePass-Anmeldedialog
- 🔐 **Schlüssel im TPM** – von Windows erzeugt, das Plugin erhält nur Signaturen
- ⏱️ **Automatischer Ablauf** – nach 7 Tagen oder beim nächsten Windows-Neustart
- 🔒 **Sperrt mit Windows** – bei Win+L, Sitzungstrennung oder Standby *(Test ausstehend)*
- 🗂️ **Pro Datenbank, nur mit Zustimmung** – Voreinstellung ist *Nein*
- 💾 **Ändert Ihre .kdbx-Datei nie**
- 🛟 **Ausfallsicher** – Abbrechen oder Fehler → normale Master-Passwort-Abfrage
- 🚫 **Keine Telemetrie** – kein Netzwerkzugriff, keine Datensammlung

## So funktioniert es

1. Datenbank wie gewohnt einmal mit dem Master-Passwort öffnen.
2. RamzX Unlock fragt: *„Windows-Hello-Schnellentsperrung für diese Datenbank aktivieren?"*
3. Beim nächsten Öffnen erscheint Windows Hello – Blick in die Kamera oder Finger auflegen, fertig.
4. Nach 7 Tagen oder einem Windows-Neustart ist wieder das Master-Passwort nötig.

## Häufige Fragen

**Kann ich KeePass mit Fingerabdruck oder Gesicht entsperren?**
Ja, mit einem Plugin. KeePass 2 unterstützt Windows Hello nicht von Haus aus. RamzX Unlock
ergänzt Windows Hello (Gesicht, Fingerabdruck, PIN) am KeePass-Anmeldedialog.

**Ist Windows Hello ein zweiter Faktor für KeePass?**
Nein – und das sagen wir offen. Windows Hello akzeptiert immer auch die Windows-Hello-PIN.
Wer Ihre PIN kennt und Ihre Windows-Sitzung nutzen kann, kann eine aktivierte Datenbank öffnen,
solange die Schnellentsperrung gültig ist. Es ist eine **Komfortfunktion**. Ein YubiKey-Modus als
echter zweiter Faktor ist geplant.

**Sieht das Plugin meine biometrischen Daten?**
Nein. Die Erkennung findet vollständig in Windows Hello statt; laut Microsoft verlassen biometrische
Daten das Gerät nicht. Das Plugin erhält nur eine Signatur.

**Funktioniert es mit KeePassXC?**
Nein. KeePassXC ist eine eigene Anwendung. RamzX Unlock ist ein Plugin für KeePass 2.

## Datenschutz (DSGVO)

Keine biometrischen Daten im Plugin, keine Telemetrie, keine Netzwerkverbindungen.
Alle Daten bleiben verschlüsselt auf Ihrem PC unter Ihrem Windows-Benutzer.

## Über RamzX

RamzX Unlock wird von **[RamzX](https://ramzx.de)** entwickelt und gepflegt.
Fragen, Feedback oder geschäftliche Anfragen: **support@ramzx.de**

Ausführliche technische Dokumentation (Englisch): [README](../README.md)

Lizenz: MIT · © 2026 RamzX · KeePass ist ein Projekt von Dominik Reichl; RamzX Unlock ist
ein unabhängiges Plugin. Windows Hello ist eine Marke von Microsoft.
