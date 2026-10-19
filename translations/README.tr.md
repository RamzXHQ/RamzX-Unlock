<div align="center">

<img src="../assets/banner.svg" alt="Miftawerk Unlock - KeePass Windows Hello plugin by SeeColors iT" width="100%">

# Miftawerk Unlock

### KeePass için Windows Hello eklentisi – KeePass 2'yi yüzünüz veya parmak izinizle açın

[English](../README.md) · [Deutsch](README.de.md) · [العربية](README.ar.md) · [Français](README.fr.md) · [Español](README.es.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) · [Polski](README.pl.md) · [Português](README.pt-BR.md) · **Türkçe**

Ücretsiz ve açık kaynak · Geliştiren: **[SeeColors iT](https://seecolors.de)**

</div>

---

**Miftawerk Unlock**, **KeePass 2** (KeePass Password Safe) için ücretsiz ve açık kaynaklı bir eklentidir.
Ana parolayı her seferinde yazmak yerine KeePass veritabanınızı **Windows Hello** ile –
**yüz tanıma, parmak izi veya Windows Hello PIN'i** ile – açmanızı sağlar.
Windows Hello anahtarı **TPM** içinde tutulur, hızlı kilit açma **otomatik olarak sona erer**
ve **Windows'u kilitlediğinizde KeePass da kilitlenir**.

> **Durum: alfa.** Testlerimizde Windows 11'de (yüz ve parmak izi) çalışıyor.
> İlk imzalı sürüme kadar lütfen bir test veritabanı kullanın.

## Özellikler

- 🔓 **Windows Hello ile kilit açma** – KeePass penceresinde yüz, parmak izi veya PIN
- 🔐 **Anahtar TPM'de** – Windows tarafından oluşturulur; eklenti yalnızca imza alır
- ⏱️ **Otomatik sona erme** – 7 gün sonra veya Windows yeniden başlatıldığında
- 🔒 **Windows ile birlikte kilitlenir** – Win+L, oturum kesilmesi veya uyku *(test bekleniyor)*
- 🗂️ **Her veritabanı için ayrı, yalnızca onayınızla** – varsayılan: *Hayır*
- 💾 **.kdbx dosyanızı asla değiştirmez**
- 🛟 **Hatada güvenli** – iptal veya hata → her zamanki ana parola penceresi
- 🚫 **Telemetri yok** – ağ erişimi yok, veri toplama yok

## Sık sorulan sorular

**KeePass'i parmak izi veya yüzümle açabilir miyim?**
Evet, bir eklentiyle. KeePass 2 Windows Hello'yu doğrudan desteklemez; Miftawerk Unlock bunu ekler.

**Windows Hello, KeePass için ikinci bir faktör müdür?**
Hayır – bunu açıkça söylüyoruz: Windows Hello her zaman PIN'i de kabul eder.
Bu bir **kolaylık özelliğidir**. Gerçek ikinci faktör olarak bir YubiKey modu planlanıyor.

**Eklenti biyometrik verilerimi görür mü?**
Hayır. Tanıma tamamen Windows Hello içinde gerçekleşir; Microsoft'a göre biyometrik veriler
cihazdan çıkmaz. Eklenti yalnızca bir imza alır.

## Gizlilik (KVKK / GDPR)

Eklentide biyometrik veri yok, telemetri yok, ağ bağlantısı yok.

## SeeColors iT hakkında

Miftawerk Unlock, **[SeeColors iT](https://seecolors.de)** tarafından geliştirilir ve bakımı yapılır.
Sorular, geri bildirim veya iş talepleri: **support@seecolors.de**

Tam teknik belgeler (İngilizce): [README](../README.md) · MIT Lisansı · © 2026 SeeColors iT ·
KeePass, Dominik Reichl'in bir projesidir; Miftawerk Unlock bağımsız bir eklentidir.
