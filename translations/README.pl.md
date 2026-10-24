<div align="center">

<img src="../assets/banner.svg" alt="RamzX Unlock - KeePass Windows Hello plugin by RamzX" width="100%">

# RamzX Unlock

### Wtyczka Windows Hello do KeePass – odblokuj KeePass 2 twarzą lub odciskiem palca

[English](../README.md) · [Deutsch](README.de.md) · [العربية](README.ar.md) · [Français](README.fr.md) · [Español](README.es.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) · **Polski** · [Português](README.pt-BR.md) · [Türkçe](README.tr.md)

Bezpłatna i otwartoźródłowa · Tworzona przez **[RamzX](https://ramzx.de)**

</div>

---

**RamzX Unlock** to bezpłatna wtyczka open source dla **KeePass 2** (KeePass Password Safe).
Pozwala **odblokować bazę KeePass za pomocą Windows Hello** – **rozpoznawania twarzy,
odcisku palca lub kodu PIN Windows Hello** – zamiast za każdym razem wpisywać hasło główne.
Klucz Windows Hello jest przechowywany w **TPM**, szybkie odblokowanie **wygasa automatycznie**,
a KeePass jest **blokowany, gdy blokujesz Windows**.

> **Status: alfa.** W naszych testach działa w Windows 11 (twarz i odcisk palca).
> Do czasu pierwszej podpisanej wersji używaj testowej bazy danych.

## Funkcje

- 🔓 **Odblokowanie przez Windows Hello** – twarz, odcisk palca lub PIN w oknie KeePass
- 🔐 **Klucz w TPM** – tworzony przez Windows; wtyczka otrzymuje tylko podpisy
- ⏱️ **Automatyczne wygasanie** – po 7 dniach lub przy ponownym uruchomieniu Windows
- 🔒 **Blokada razem z Windows** – Win+L, rozłączenie sesji lub uśpienie *(test w toku)*
- 🗂️ **Dla każdej bazy osobno, tylko za zgodą** – domyślnie: *Nie*
- 💾 **Nigdy nie zmienia pliku .kdbx**
- 🛟 **Bezpieczna przy błędach** – anulowanie lub błąd → zwykłe hasło główne
- 🚫 **Brak telemetrii** – brak dostępu do sieci, brak zbierania danych

## Najczęstsze pytania

**Czy mogę odblokować KeePass odciskiem palca lub twarzą?**
Tak, za pomocą wtyczki. KeePass 2 nie obsługuje Windows Hello natywnie; RamzX Unlock to dodaje.

**Czy Windows Hello to drugi składnik uwierzytelniania dla KeePass?**
Nie – mówimy to otwarcie: Windows Hello zawsze akceptuje także kod PIN.
To **funkcja wygody**. Planowany jest tryb YubiKey jako prawdziwy drugi składnik.

**Czy wtyczka widzi moje dane biometryczne?**
Nie. Rozpoznawanie odbywa się w całości w Windows Hello; według Microsoft dane biometryczne
nie opuszczają urządzenia. Wtyczka otrzymuje tylko podpis.

## Prywatność (RODO)

Brak danych biometrycznych we wtyczce, brak telemetrii, brak połączeń sieciowych.

## O RamzX

RamzX Unlock jest rozwijany i utrzymywany przez **[RamzX](https://ramzx.de)**.
Pytania, opinie lub zapytania biznesowe: **support@ramzx.de**

Pełna dokumentacja techniczna (angielski): [README](../README.md) · Licencja MIT · © 2026 RamzX ·
KeePass jest projektem Dominika Reichla; RamzX Unlock to niezależna wtyczka.
