<div align="center">

# Miftawerk Unlock

### Plugin Windows Hello pour KeePass – déverrouillez KeePass 2 avec votre visage ou votre empreinte digitale

[English](../README.md) · [Deutsch](README.de.md) · [العربية](README.ar.md) · **Français** · [Español](README.es.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) · [Polski](README.pl.md) · [Português](README.pt-BR.md) · [Türkçe](README.tr.md)

Gratuit et open source · Développé par **[SeeColors iT](https://seecolors.de)**

</div>

---

**Miftawerk Unlock** est un plugin gratuit et open source pour **KeePass 2** (KeePass Password Safe).
Il permet de **déverrouiller votre base KeePass avec Windows Hello** – **reconnaissance faciale,
empreinte digitale ou code PIN Windows Hello** – au lieu de saisir le mot de passe maître à chaque fois.
La clé Windows Hello est conservée dans le **TPM**, le déverrouillage rapide **expire automatiquement**
et KeePass est **verrouillé lorsque vous verrouillez Windows**.

> **Statut : alpha.** Fonctionne dans nos tests sous Windows 11 (visage et empreinte).
> Utilisez une base de test jusqu'à la première version signée.

## Fonctionnalités

- 🔓 **Déverrouillage Windows Hello** – visage, empreinte ou PIN dans la fenêtre de KeePass
- 🔐 **Clé dans le TPM** – créée par Windows, le plugin ne reçoit que des signatures
- ⏱️ **Expiration automatique** – après 7 jours ou au prochain redémarrage de Windows
- 🔒 **Verrouillage avec Windows** – Win+L, déconnexion ou mise en veille *(test en cours)*
- 🗂️ **Par base, sur consentement** – réglage par défaut : *Non*
- 💾 **Ne modifie jamais votre fichier .kdbx**
- 🛟 **Sûr en cas d'erreur** – annulation ou erreur → saisie habituelle du mot de passe maître
- 🚫 **Aucune télémétrie** – aucun accès réseau, aucune collecte de données

## Questions fréquentes

**Puis-je déverrouiller KeePass avec mon empreinte ou mon visage ?**
Oui, avec un plugin. KeePass 2 ne prend pas en charge Windows Hello nativement ; Miftawerk Unlock l'ajoute.

**Windows Hello est-il un second facteur pour KeePass ?**
Non, et nous le disons clairement : Windows Hello accepte toujours aussi le code PIN.
C'est une **fonction de confort**. Un mode YubiKey comme véritable second facteur est prévu.

**Le plugin voit-il mes données biométriques ?**
Non. La reconnaissance se fait entièrement dans Windows Hello ; selon Microsoft, les données
biométriques ne quittent pas l'appareil. Le plugin ne reçoit qu'une signature.

## Confidentialité (RGPD)

Aucune donnée biométrique dans le plugin, aucune télémétrie, aucune connexion réseau.

## À propos de SeeColors iT

Miftawerk Unlock est développé et maintenu par **[SeeColors iT](https://seecolors.de)**.
Questions, retours ou demandes professionnelles : **support@seecolors.de**

Documentation technique complète (anglais) : [README](../README.md) · Licence MIT · © 2026 SeeColors iT ·
KeePass est un projet de Dominik Reichl ; Miftawerk Unlock est un plugin indépendant.
