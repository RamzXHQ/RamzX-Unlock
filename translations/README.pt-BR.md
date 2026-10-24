<div align="center">

<img src="../assets/banner.svg" alt="RamzX Unlock - KeePass Windows Hello plugin by RamzX" width="100%">

# RamzX Unlock

### Plugin Windows Hello para KeePass – desbloqueie o KeePass 2 com o rosto ou a impressão digital

[English](../README.md) · [Deutsch](README.de.md) · [العربية](README.ar.md) · [Français](README.fr.md) · [Español](README.es.md) · [Italiano](README.it.md) · [Nederlands](README.nl.md) · [Polski](README.pl.md) · **Português** · [Türkçe](README.tr.md)

Gratuito e de código aberto · Desenvolvido pela **[RamzX](https://ramzx.de)**

</div>

---

**RamzX Unlock** é um plugin gratuito e de código aberto para o **KeePass 2** (KeePass Password Safe).
Ele permite **desbloquear seu banco de dados do KeePass com o Windows Hello** – **reconhecimento facial,
impressão digital ou PIN do Windows Hello** – em vez de digitar a senha mestra toda vez.
A chave do Windows Hello fica no **TPM**, o desbloqueio rápido **expira automaticamente**
e o KeePass é **bloqueado quando você bloqueia o Windows**.

> **Status: alfa.** Funciona em nossos testes no Windows 11 (rosto e impressão digital).
> Use um banco de dados de teste até a primeira versão assinada.

## Recursos

- 🔓 **Desbloqueio com Windows Hello** – rosto, impressão digital ou PIN na janela do KeePass
- 🔐 **Chave no TPM** – criada pelo Windows; o plugin recebe apenas assinaturas
- ⏱️ **Expiração automática** – após 7 dias ou na próxima reinicialização do Windows
- 🔒 **Bloqueia junto com o Windows** – Win+L, desconexão ou suspensão *(teste pendente)*
- 🗂️ **Por banco de dados, só com consentimento** – padrão: *Não*
- 💾 **Nunca altera seu arquivo .kdbx**
- 🛟 **Seguro em caso de erro** – cancelar ou qualquer erro → senha mestra como sempre
- 🚫 **Sem telemetria** – sem acesso à rede, sem coleta de dados

## Perguntas frequentes

**Posso desbloquear o KeePass com a impressão digital ou o rosto?**
Sim, com um plugin. O KeePass 2 não oferece suporte nativo ao Windows Hello; o RamzX Unlock adiciona isso.

**O Windows Hello é um segundo fator para o KeePass?**
Não – e dizemos isso abertamente: o Windows Hello sempre aceita também o PIN.
É um **recurso de conveniência**. Um modo YubiKey como segundo fator real está planejado.

**O plugin vê meus dados biométricos?**
Não. O reconhecimento acontece inteiramente no Windows Hello; segundo a Microsoft, os dados
biométricos não saem do dispositivo. O plugin recebe apenas uma assinatura.

## Privacidade (LGPD / GDPR)

Nenhum dado biométrico no plugin, sem telemetria, sem conexões de rede.

## Sobre a RamzX

O RamzX Unlock é desenvolvido e mantido pela **[RamzX](https://ramzx.de)**.
Dúvidas, sugestões ou contatos comerciais: **support@ramzx.de**

Documentação técnica completa (inglês): [README](../README.md) · Licença MIT · © 2026 RamzX ·
O KeePass é um projeto de Dominik Reichl; o RamzX Unlock é um plugin independente.
