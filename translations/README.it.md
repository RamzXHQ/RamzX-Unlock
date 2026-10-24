<div align="center">

<img src="../assets/banner.svg" alt="RamzX Unlock - KeePass Windows Hello plugin by RamzX" width="100%">

# RamzX Unlock

### Plugin Windows Hello per KeePass – sblocca KeePass 2 con il volto o l'impronta digitale

[English](../README.md) · [Deutsch](README.de.md) · [العربية](README.ar.md) · [Français](README.fr.md) · [Español](README.es.md) · **Italiano** · [Nederlands](README.nl.md) · [Polski](README.pl.md) · [Português](README.pt-BR.md) · [Türkçe](README.tr.md)

Gratuito e open source · Sviluppato da **[RamzX](https://ramzx.de)**

</div>

---

**RamzX Unlock** è un plugin gratuito e open source per **KeePass 2** (KeePass Password Safe).
Ti permette di **sbloccare il database di KeePass con Windows Hello** – **riconoscimento facciale,
impronta digitale o PIN di Windows Hello** – invece di digitare ogni volta la password principale.
La chiave di Windows Hello resta nel **TPM**, lo sblocco rapido **scade automaticamente**
e KeePass si **blocca quando blocchi Windows**.

> **Stato: alpha.** Funziona nei nostri test su Windows 11 (volto e impronta).
> Usa un database di prova fino alla prima versione firmata.

## Funzionalità

- 🔓 **Sblocco con Windows Hello** – volto, impronta o PIN nella finestra di KeePass
- 🔐 **Chiave nel TPM** – creata da Windows; il plugin riceve solo firme
- ⏱️ **Scadenza automatica** – dopo 7 giorni o al riavvio di Windows
- 🔒 **Si blocca con Windows** – Win+L, disconnessione o sospensione *(test in corso)*
- 🗂️ **Per database, solo con consenso** – impostazione predefinita: *No*
- 💾 **Non modifica mai il tuo file .kdbx**
- 🛟 **Sicuro in caso di errore** – annulla o errore → password principale come sempre
- 🚫 **Nessuna telemetria** – nessun accesso alla rete, nessuna raccolta di dati

## Domande frequenti

**Posso sbloccare KeePass con l'impronta o il volto?**
Sì, con un plugin. KeePass 2 non supporta Windows Hello in modo nativo; RamzX Unlock lo aggiunge.

**Windows Hello è un secondo fattore per KeePass?**
No, e lo diciamo apertamente: Windows Hello accetta sempre anche il PIN.
È una **funzione di comodità**. È prevista una modalità YubiKey come vero secondo fattore.

**Il plugin vede i miei dati biometrici?**
No. Il riconoscimento avviene interamente in Windows Hello; secondo Microsoft i dati biometrici
non lasciano il dispositivo. Il plugin riceve solo una firma.

## Privacy (GDPR)

Nessun dato biometrico nel plugin, nessuna telemetria, nessuna connessione di rete.

## Chi è RamzX

RamzX Unlock è sviluppato e mantenuto da **[RamzX](https://ramzx.de)**.
Domande, feedback o richieste commerciali: **support@ramzx.de**

Documentazione tecnica completa (inglese): [README](../README.md) · Licenza MIT · © 2026 RamzX ·
KeePass è un progetto di Dominik Reichl; RamzX Unlock è un plugin indipendente.
