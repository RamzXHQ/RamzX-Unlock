<div align="center">

<img src="../assets/banner.svg" alt="Miftawerk Unlock - KeePass Windows Hello plugin by SeeColors iT" width="100%">

# Miftawerk Unlock

### Plugin de Windows Hello para KeePass – desbloquea KeePass 2 con tu rostro o tu huella dactilar

[English](../README.md) · [Deutsch](README.de.md) · [العربية](README.ar.md) · [Français](README.fr.md) · **Español** · [Italiano](README.it.md) · [Nederlands](README.nl.md) · [Polski](README.pl.md) · [Português](README.pt-BR.md) · [Türkçe](README.tr.md)

Gratuito y de código abierto · Desarrollado por **[SeeColors iT](https://seecolors.de)**

</div>

---

**Miftawerk Unlock** es un plugin gratuito y de código abierto para **KeePass 2** (KeePass Password Safe).
Permite **desbloquear tu base de datos de KeePass con Windows Hello** – **reconocimiento facial,
huella dactilar o PIN de Windows Hello** – en lugar de escribir la contraseña maestra cada vez.
La clave de Windows Hello se guarda en el **TPM**, el desbloqueo rápido **caduca automáticamente**
y KeePass se **bloquea cuando bloqueas Windows**.

> **Estado: alfa.** Funciona en nuestras pruebas con Windows 11 (rostro y huella).
> Usa una base de datos de prueba hasta la primera versión firmada.

## Características

- 🔓 **Desbloqueo con Windows Hello** – rostro, huella o PIN en la ventana de KeePass
- 🔐 **Clave en el TPM** – creada por Windows; el plugin solo recibe firmas
- ⏱️ **Caducidad automática** – tras 7 días o al reiniciar Windows
- 🔒 **Se bloquea con Windows** – Win+L, desconexión o suspensión *(prueba pendiente)*
- 🗂️ **Por base de datos, solo con tu consentimiento** – valor predeterminado: *No*
- 💾 **Nunca modifica tu archivo .kdbx**
- 🛟 **Seguro ante errores** – cancelar o cualquier error → contraseña maestra como siempre
- 🚫 **Sin telemetría** – sin acceso a la red, sin recopilación de datos

## Preguntas frecuentes

**¿Puedo desbloquear KeePass con mi huella o mi rostro?**
Sí, con un plugin. KeePass 2 no admite Windows Hello de forma nativa; Miftawerk Unlock lo añade.

**¿Es Windows Hello un segundo factor para KeePass?**
No, y lo decimos abiertamente: Windows Hello siempre acepta también el PIN.
Es una **función de comodidad**. Está previsto un modo YubiKey como segundo factor real.

**¿Ve el plugin mis datos biométricos?**
No. El reconocimiento ocurre por completo dentro de Windows Hello; según Microsoft, los datos
biométricos no salen del dispositivo. El plugin solo recibe una firma.

## Privacidad (RGPD)

Sin datos biométricos en el plugin, sin telemetría, sin conexiones de red.

## Sobre SeeColors iT

Miftawerk Unlock es desarrollado y mantenido por **[SeeColors iT](https://seecolors.de)**.
Preguntas, comentarios o consultas comerciales: **support@seecolors.de**

Documentación técnica completa (inglés): [README](../README.md) · Licencia MIT · © 2026 SeeColors iT ·
KeePass es un proyecto de Dominik Reichl; Miftawerk Unlock es un plugin independiente.
