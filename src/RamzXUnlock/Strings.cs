// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace RamzXUnlock
{
    /// <summary>
    /// User-visible texts in ten languages. The language follows the plugin setting, otherwise
    /// the KeePass translation, otherwise the Windows display language; fallback English.
    /// </summary>
    internal static class Strings
    {
        internal static readonly string[] Languages = { "en", "de", "ar", "fr", "es", "it", "nl", "pl", "pt", "tr" };

        /// <summary>Native language names for the settings dialog, same order as <see cref="Languages"/>.</summary>
        internal static readonly string[] LanguageNames =
            { "English", "Deutsch", "العربية", "Français", "Español", "Italiano", "Nederlands", "Polski", "Português", "Türkçe" };

        private static Dictionary<string, string> s_current = En();
        private static string s_lang = "en";

        internal static string Language { get { return s_lang; } }
        internal static bool RightToLeft { get { return s_lang == "ar"; } }

        /// <summary>
        /// <paramref name="preferred"/> (plugin setting, "" = automatic) wins; then the KeePass UI
        /// language if KeePass is translated, otherwise the Windows display language.
        /// </summary>
        internal static void Select(string keePassIso6391, string preferred = null)
        {
            string lang = (preferred ?? string.Empty).Trim().ToLowerInvariant();
            if (lang.Length == 0) lang = (keePassIso6391 ?? string.Empty).Trim().ToLowerInvariant();
            if (lang.Length == 0 || lang == "en")
                lang = string.IsNullOrEmpty(preferred) ? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName : "en";
            SetLanguage(lang);
        }

        internal static void SetLanguage(string lang)
        {
            string l = (lang ?? string.Empty).ToLowerInvariant();
            if (Array.IndexOf(Languages, l) < 0) l = "en";
            s_current = Table(l);
            s_lang = l;
        }

        internal static string Get(string key)
        {
            string s;
            if (s_current.TryGetValue(key, out s)) return s;
            if (En().TryGetValue(key, out s)) return s;
            return key;
        }

        internal static string Format(string key, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, Get(key), args);
        }

        internal static Dictionary<string, string> Table(string lang)
        {
            switch (lang)
            {
                case "de": return De();
                case "ar": return Ar();
                case "fr": return Fr();
                case "es": return Es();
                case "it": return It();
                case "nl": return Nl();
                case "pl": return Pl();
                case "pt": return Pt();
                case "tr": return Tr();
                default: return En();
            }
        }

        // Keys
        internal const string MenuSettings = "MenuSettings";
        internal const string MenuForget = "MenuForget";
        internal const string MenuSelfTest = "MenuSelfTest";
        internal const string MenuDeleteSelfTest = "MenuDeleteSelfTest";
        internal const string ForgetDone = "ForgetDone";
        internal const string DeleteFailed = "DeleteFailed";
        internal const string EnrollQuestion = "EnrollQuestion";
        internal const string EnrollAnotherDevice = "EnrollAnotherDevice";
        internal const string HelloNotSetUp = "HelloNotSetUp";
        internal const string HelloKeyUnavailable = "HelloKeyUnavailable";
        internal const string HelloStatus = "HelloStatus";
        internal const string NotRepeatable = "NotRepeatable";
        internal const string SelfCheckFailed = "SelfCheckFailed";
        internal const string EnrollDone = "EnrollDone";
        internal const string EnrollFailed = "EnrollFailed";
        internal const string UnlockFailed = "UnlockFailed";
        internal const string WrongDatabase = "WrongDatabase";
        internal const string OldPluginFile = "OldPluginFile";
        internal const string SettingsTitle = "SettingsTitle";
        internal const string SettingsSubtitle = "SettingsSubtitle";
        internal const string SectionStatus = "SectionStatus";
        internal const string SectionSettings = "SectionSettings";
        internal const string SectionData = "SectionData";
        internal const string ValidityLabel = "ValidityLabel";
        internal const string ValidityNote = "ValidityNote";
        internal const string OfferEnrollment = "OfferEnrollment";
        internal const string LockWithWindows = "LockWithWindows";
        internal const string LanguageLabel = "LanguageLabel";
        internal const string LanguageAuto = "LanguageAuto";
        internal const string LanguageNote = "LanguageNote";
        internal const string StatusHello = "StatusHello";
        internal const string StatusHelloNo = "StatusHelloNo";
        internal const string StatusPasskey = "StatusPasskey";
        internal const string StatusPasskeyNo = "StatusPasskeyNo";
        internal const string StatusRemote = "StatusRemote";
        internal const string StatusUsage = "StatusUsage";
        internal const string ForgetButton = "ForgetButton";
        internal const string ForgetConfirm = "ForgetConfirm";
        internal const string EnrollTitle = "EnrollTitle";
        internal const string EnrollEnable = "EnrollEnable";
        internal const string EnrollNotNow = "EnrollNotNow";
        internal const string PinNote = "PinNote";
        internal const string Ok = "Ok";
        internal const string Cancel = "Cancel";

        private static Dictionary<string, string> En()
        {
            return new Dictionary<string, string>
            {
                { MenuSettings, "Settings..." },
                { MenuForget, "Forget all Windows Hello unlock data" },
                { MenuSelfTest, "Windows Hello self-test" },
                { MenuDeleteSelfTest, "Delete self-test key" },
                { ForgetDone, "{0} stored unlock record(s) deleted." },
                { DeleteFailed, "Delete failed: {0}" },
                { EnrollQuestion, "Enable Windows Hello quick unlock for this database?\r\n\r\n{0}\r\n\r\n" +
                    "You can then open it with face, fingerprint or your Windows Hello PIN of the PC you are " +
                    "using - also over Remote Desktop - for {1} day(s) or until this computer restarts. " +
                    "After that, the master password is required again.\r\n\r\n" +
                    "Important: Windows Hello always accepts the PIN. Anyone who knows your Windows Hello PIN " +
                    "and can use your Windows session can open this database during that time. " +
                    "This is a convenience feature, not a second factor." },
                { EnrollAnotherDevice, "Quick unlock with Windows Hello is already enabled for this database on another device.\r\n\r\n{0}\r\n\r\n" +
                    "Enable it for the PC you are using now as well (face, fingerprint or PIN, also over Remote Desktop), " +
                    "for {1} day(s) or until this computer restarts?\r\n\r\n" +
                    "Important: Windows Hello always accepts the PIN. This is a convenience feature, not a second factor." },
                { HelloNotSetUp, "Windows Hello is not set up on this computer. Nothing was changed." },
                { HelloKeyUnavailable, "Windows Hello key not available ({0}). Nothing was changed." },
                { HelloStatus, "Windows Hello: {0}. Nothing was changed." },
                { NotRepeatable, "Windows Hello signatures are not repeatable on this computer. " +
                    "Quick unlock cannot be used safely. Nothing was changed." },
                { SelfCheckFailed, "Self-check of the stored data failed. Nothing was kept." },
                { EnrollDone, "Windows Hello quick unlock is enabled for this database until {0} or the next restart of this computer." },
                { EnrollFailed, "Windows Hello setup failed: {0}" },
                { UnlockFailed, "Windows Hello unlock failed: {0}\r\nPlease use your master password." },
                { WrongDatabase, "This file is not the database Windows Hello was enabled for. Its Windows Hello data was removed." },
                { OldPluginFile, "The old plugin file is still there: {0}\r\nKeePass loads it as well, so Windows Hello may ask twice. Please close KeePass and delete this file. Nothing is lost." },
                { SettingsTitle, "RamzX Unlock - Settings" },
                { SettingsSubtitle, "Windows Hello quick unlock for KeePass" },
                { SectionStatus, "Status" },
                { SectionSettings, "Settings" },
                { SectionData, "Stored data" },
                { ValidityLabel, "Quick unlock valid for (days):" },
                { ValidityNote, "Quick unlock always ends when this computer restarts. A changed value applies to databases enabled from now on." },
                { OfferEnrollment, "Offer Windows Hello after opening a database with the master password" },
                { LockWithWindows, "Lock KeePass when Windows is locked, disconnected or goes to sleep" },
                { LanguageLabel, "Language:" },
                { LanguageAuto, "Automatic (KeePass / Windows)" },
                { LanguageNote, "A changed language applies fully after restarting KeePass." },
                { StatusHello, "Windows Hello is set up on this computer." },
                { StatusHelloNo, "Windows Hello is not set up on this computer (Windows Settings > Accounts > Sign-in options)." },
                { StatusPasskey, "Passkeys are available: quick unlock also works over Remote Desktop." },
                { StatusPasskeyNo, "Passkeys are not available on this Windows version: quick unlock works only at this computer." },
                { StatusRemote, "Remote Desktop session: Windows Hello of the PC you are sitting at is used." },
                { StatusUsage, "{0} database(s) with quick unlock, {1} linked device(s)." },
                { ForgetButton, "Forget all quick-unlock data" },
                { ForgetConfirm, "Delete all stored quick-unlock data? The master password is then required again for every database." },
                { EnrollTitle, "Quick unlock with Windows Hello" },
                { EnrollEnable, "Enable" },
                { EnrollNotNow, "Not now" },
                { PinNote, "Windows Hello always accepts the Windows Hello PIN. This is a convenience feature, not a second factor." },
                { Ok, "OK" },
                { Cancel, "Cancel" },
            };
        }

        private static Dictionary<string, string> De()
        {
            return new Dictionary<string, string>
            {
                { MenuSettings, "Einstellungen..." },
                { MenuForget, "Alle Windows-Hello-Entsperrdaten löschen" },
                { MenuSelfTest, "Windows-Hello-Selbsttest" },
                { MenuDeleteSelfTest, "Selbsttest-Schlüssel löschen" },
                { ForgetDone, "{0} gespeicherte Entsperrdaten gelöscht." },
                { DeleteFailed, "Löschen fehlgeschlagen: {0}" },
                { EnrollQuestion, "Windows-Hello-Schnellentsperrung für diese Datenbank aktivieren?\r\n\r\n{0}\r\n\r\n" +
                    "Sie können sie dann {1} Tag(e) lang oder bis zum Neustart dieses Computers mit Gesicht, " +
                    "Fingerabdruck oder Windows-Hello-PIN des PCs öffnen, an dem Sie arbeiten - auch über Remotedesktop. " +
                    "Danach ist wieder das Master-Passwort nötig.\r\n\r\n" +
                    "Wichtig: Windows Hello akzeptiert immer auch die PIN. Wer Ihre Windows-Hello-PIN kennt und " +
                    "Ihre Windows-Sitzung nutzen kann, kann diese Datenbank in dieser Zeit öffnen. " +
                    "Das ist eine Komfortfunktion, kein zweiter Faktor." },
                { EnrollAnotherDevice, "Die Windows-Hello-Schnellentsperrung ist für diese Datenbank bereits auf einem anderen Gerät aktiv.\r\n\r\n{0}\r\n\r\n" +
                    "Auch für den PC aktivieren, an dem Sie jetzt arbeiten (Gesicht, Fingerabdruck oder PIN, auch über Remotedesktop), " +
                    "{1} Tag(e) lang oder bis zum Neustart dieses Computers?\r\n\r\n" +
                    "Wichtig: Windows Hello akzeptiert immer auch die PIN. Das ist eine Komfortfunktion, kein zweiter Faktor." },
                { HelloNotSetUp, "Windows Hello ist auf diesem Computer nicht eingerichtet. Es wurde nichts geändert." },
                { HelloKeyUnavailable, "Windows-Hello-Schlüssel nicht verfügbar ({0}). Es wurde nichts geändert." },
                { HelloStatus, "Windows Hello: {0}. Es wurde nichts geändert." },
                { NotRepeatable, "Windows-Hello-Signaturen sind auf diesem Computer nicht wiederholbar. " +
                    "Die Schnellentsperrung kann nicht sicher verwendet werden. Es wurde nichts geändert." },
                { SelfCheckFailed, "Die Selbstprüfung der gespeicherten Daten ist fehlgeschlagen. Es wurde nichts gespeichert." },
                { EnrollDone, "Die Windows-Hello-Schnellentsperrung ist für diese Datenbank bis {0} oder bis zum nächsten Neustart dieses Computers aktiv." },
                { EnrollFailed, "Einrichtung von Windows Hello fehlgeschlagen: {0}" },
                { UnlockFailed, "Entsperren mit Windows Hello fehlgeschlagen: {0}\r\nBitte verwenden Sie Ihr Master-Passwort." },
                { WrongDatabase, "Diese Datei ist nicht die Datenbank, für die Windows Hello aktiviert wurde. Die Windows-Hello-Daten wurden entfernt." },
                { OldPluginFile, "Die alte Plugin-Datei ist noch vorhanden: {0}\r\nKeePass lädt sie zusätzlich, Windows Hello fragt dann evtl. doppelt. Bitte KeePass schließen und diese Datei löschen. Es geht nichts verloren." },
                { SettingsTitle, "RamzX Unlock - Einstellungen" },
                { SettingsSubtitle, "Schnellentsperrung für KeePass mit Windows Hello" },
                { SectionStatus, "Status" },
                { SectionSettings, "Einstellungen" },
                { SectionData, "Gespeicherte Daten" },
                { ValidityLabel, "Schnellentsperrung gültig für (Tage):" },
                { ValidityNote, "Die Schnellentsperrung endet immer beim Neustart dieses Computers. Ein geänderter Wert gilt für ab jetzt aktivierte Datenbanken." },
                { OfferEnrollment, "Windows Hello anbieten, nachdem eine Datenbank mit dem Master-Passwort geöffnet wurde" },
                { LockWithWindows, "KeePass sperren, wenn Windows gesperrt, getrennt oder in den Standby versetzt wird" },
                { LanguageLabel, "Sprache:" },
                { LanguageAuto, "Automatisch (KeePass / Windows)" },
                { LanguageNote, "Eine geänderte Sprache gilt vollständig nach einem Neustart von KeePass." },
                { StatusHello, "Windows Hello ist auf diesem Computer eingerichtet." },
                { StatusHelloNo, "Windows Hello ist auf diesem Computer nicht eingerichtet (Windows-Einstellungen > Konten > Anmeldeoptionen)." },
                { StatusPasskey, "Passkeys sind verfügbar: Die Schnellentsperrung funktioniert auch über Remotedesktop." },
                { StatusPasskeyNo, "Passkeys sind in dieser Windows-Version nicht verfügbar: Die Schnellentsperrung funktioniert nur an diesem Computer." },
                { StatusRemote, "Remotedesktop-Sitzung: Es wird Windows Hello des PCs verwendet, an dem Sie sitzen." },
                { StatusUsage, "{0} Datenbank(en) mit Schnellentsperrung, {1} verknüpfte(s) Gerät(e)." },
                { ForgetButton, "Alle Schnellentsperrdaten löschen" },
                { ForgetConfirm, "Alle gespeicherten Schnellentsperrdaten löschen? Danach ist für jede Datenbank wieder das Master-Passwort nötig." },
                { EnrollTitle, "Schnellentsperrung mit Windows Hello" },
                { EnrollEnable, "Aktivieren" },
                { EnrollNotNow, "Nicht jetzt" },
                { PinNote, "Windows Hello akzeptiert immer auch die Windows-Hello-PIN. Das ist eine Komfortfunktion, kein zweiter Faktor." },
                { Ok, "OK" },
                { Cancel, "Abbrechen" },
            };
        }

        private static Dictionary<string, string> Ar()
        {
            return new Dictionary<string, string>
            {
                { MenuSettings, "الإعدادات..." },
                { MenuForget, "حذف كل بيانات الفتح عبر Windows Hello" },
                { MenuSelfTest, "اختبار Windows Hello" },
                { MenuDeleteSelfTest, "حذف مفتاح الاختبار" },
                { ForgetDone, "تم حذف {0} من سجلات الفتح المحفوظة." },
                { DeleteFailed, "فشل الحذف: {0}" },
                { EnrollQuestion, "تفعيل الفتح السريع عبر Windows Hello لهذه القاعدة؟\r\n\r\n{0}\r\n\r\n" +
                    "يمكنك بعدها فتحها بالوجه أو البصمة أو رمز PIN في الجهاز الذي تستخدمه - حتى عبر سطح المكتب البعيد - " +
                    "لمدة {1} يوم/أيام أو حتى إعادة تشغيل هذا الحاسوب. بعد ذلك تُطلب كلمة السر الرئيسية من جديد.\r\n\r\n" +
                    "مهم: يقبل Windows Hello دائماً رمز PIN أيضاً. من يعرف رمز PIN ويستطيع استخدام جلسة Windows " +
                    "الخاصة بك يمكنه فتح هذه القاعدة خلال هذه المدة. هذه ميزة للراحة، وليست عامل تحقق ثانياً." },
                { EnrollAnotherDevice, "الفتح السريع عبر Windows Hello مفعّل لهذه القاعدة على جهاز آخر.\r\n\r\n{0}\r\n\r\n" +
                    "تفعيله أيضاً للجهاز الذي تستخدمه الآن (الوجه أو البصمة أو PIN، حتى عبر سطح المكتب البعيد) " +
                    "لمدة {1} يوم/أيام أو حتى إعادة تشغيل هذا الحاسوب؟\r\n\r\n" +
                    "مهم: يقبل Windows Hello دائماً رمز PIN أيضاً. هذه ميزة للراحة، وليست عامل تحقق ثانياً." },
                { HelloNotSetUp, "Windows Hello غير مُعدّ على هذا الجهاز. لم يتغيّر شيء." },
                { HelloKeyUnavailable, "مفتاح Windows Hello غير متاح ({0}). لم يتغيّر شيء." },
                { HelloStatus, "Windows Hello: {0}. لم يتغيّر شيء." },
                { NotRepeatable, "توقيعات Windows Hello غير قابلة للتكرار على هذا الجهاز، ولا يمكن استخدام الفتح السريع بأمان. لم يتغيّر شيء." },
                { SelfCheckFailed, "فشل الفحص الذاتي للبيانات المحفوظة. لم يُحفظ شيء." },
                { EnrollDone, "تم تفعيل الفتح السريع عبر Windows Hello لهذه القاعدة حتى {0} أو حتى إعادة تشغيل هذا الحاسوب." },
                { EnrollFailed, "فشل إعداد Windows Hello: {0}" },
                { UnlockFailed, "فشل الفتح عبر Windows Hello: {0}\r\nاستخدم كلمة السر الرئيسية من فضلك." },
                { WrongDatabase, "هذا الملف ليس القاعدة التي فُعّل لها Windows Hello. تم حذف بيانات Windows Hello الخاصة به." },
                { OldPluginFile, "ملف الإضافة القديم ما زال موجوداً: {0}\r\nيحمّله KeePass أيضاً، فقد يسأل Windows Hello مرتين. أغلق KeePass واحذف هذا الملف. لن يضيع شيء." },
                { SettingsTitle, "RamzX Unlock - الإعدادات" },
                { SettingsSubtitle, "الفتح السريع لـ KeePass عبر Windows Hello" },
                { SectionStatus, "الحالة" },
                { SectionSettings, "الإعدادات" },
                { SectionData, "البيانات المحفوظة" },
                { ValidityLabel, "مدة صلاحية الفتح السريع (بالأيام):" },
                { ValidityNote, "ينتهي الفتح السريع دائماً عند إعادة تشغيل هذا الحاسوب. القيمة الجديدة تسري على القواعد التي تُفعَّل من الآن." },
                { OfferEnrollment, "عرض Windows Hello بعد فتح قاعدة بكلمة السر الرئيسية" },
                { LockWithWindows, "قفل KeePass عند قفل Windows أو قطع الجلسة أو وضع السكون" },
                { LanguageLabel, "اللغة:" },
                { LanguageAuto, "تلقائي (KeePass / Windows)" },
                { LanguageNote, "تسري اللغة الجديدة بالكامل بعد إعادة تشغيل KeePass." },
                { StatusHello, "Windows Hello مُعدّ على هذا الحاسوب." },
                { StatusHelloNo, "Windows Hello غير مُعدّ على هذا الحاسوب (إعدادات Windows > الحسابات > خيارات تسجيل الدخول)." },
                { StatusPasskey, "مفاتيح المرور متاحة: الفتح السريع يعمل أيضاً عبر سطح المكتب البعيد." },
                { StatusPasskeyNo, "مفاتيح المرور غير متاحة في نسخة Windows هذه: الفتح السريع يعمل على هذا الحاسوب فقط." },
                { StatusRemote, "جلسة سطح مكتب بعيد: يُستخدم Windows Hello في الجهاز الذي تجلس أمامه." },
                { StatusUsage, "{0} قاعدة/قواعد بفتح سريع، {1} جهاز/أجهزة مربوطة." },
                { ForgetButton, "حذف كل بيانات الفتح السريع" },
                { ForgetConfirm, "حذف كل بيانات الفتح السريع المحفوظة؟ بعدها تُطلب كلمة السر الرئيسية من جديد لكل قاعدة." },
                { EnrollTitle, "الفتح السريع عبر Windows Hello" },
                { EnrollEnable, "تفعيل" },
                { EnrollNotNow, "ليس الآن" },
                { PinNote, "يقبل Windows Hello دائماً رمز PIN أيضاً. هذه ميزة للراحة، وليست عامل تحقق ثانياً." },
                { Ok, "موافق" },
                { Cancel, "إلغاء" },
            };
        }

        private static Dictionary<string, string> Fr()
        {
            return new Dictionary<string, string>
            {
                { MenuSettings, "Paramètres..." },
                { MenuForget, "Oublier toutes les données de déverrouillage Windows Hello" },
                { MenuSelfTest, "Autotest Windows Hello" },
                { MenuDeleteSelfTest, "Supprimer la clé d'autotest" },
                { ForgetDone, "{0} enregistrement(s) de déverrouillage supprimé(s)." },
                { DeleteFailed, "Échec de la suppression : {0}" },
                { EnrollQuestion, "Activer le déverrouillage rapide Windows Hello pour cette base de données ?\r\n\r\n{0}\r\n\r\n" +
                    "Vous pourrez alors l'ouvrir avec le visage, l'empreinte digitale ou le code PIN Windows Hello du PC que vous " +
                    "utilisez - y compris via le Bureau à distance - pendant {1} jour(s) ou jusqu'au redémarrage de cet ordinateur. " +
                    "Ensuite, le mot de passe principal sera de nouveau demandé.\r\n\r\n" +
                    "Important : Windows Hello accepte toujours le code PIN. Toute personne qui connaît votre code PIN Windows Hello " +
                    "et peut utiliser votre session Windows peut ouvrir cette base pendant cette période. " +
                    "Il s'agit d'une fonction de confort, pas d'un second facteur." },
                { EnrollAnotherDevice, "Le déverrouillage rapide Windows Hello est déjà activé pour cette base sur un autre appareil.\r\n\r\n{0}\r\n\r\n" +
                    "L'activer aussi pour le PC que vous utilisez maintenant (visage, empreinte ou PIN, y compris via le Bureau à distance), " +
                    "pendant {1} jour(s) ou jusqu'au redémarrage de cet ordinateur ?\r\n\r\n" +
                    "Important : Windows Hello accepte toujours le code PIN. Il s'agit d'une fonction de confort, pas d'un second facteur." },
                { HelloNotSetUp, "Windows Hello n'est pas configuré sur cet ordinateur. Rien n'a été modifié." },
                { HelloKeyUnavailable, "Clé Windows Hello indisponible ({0}). Rien n'a été modifié." },
                { HelloStatus, "Windows Hello : {0}. Rien n'a été modifié." },
                { NotRepeatable, "Les signatures Windows Hello ne sont pas reproductibles sur cet ordinateur. " +
                    "Le déverrouillage rapide ne peut pas être utilisé en toute sécurité. Rien n'a été modifié." },
                { SelfCheckFailed, "La vérification des données enregistrées a échoué. Rien n'a été conservé." },
                { EnrollDone, "Le déverrouillage rapide Windows Hello est activé pour cette base jusqu'au {0} ou jusqu'au prochain redémarrage de cet ordinateur." },
                { EnrollFailed, "La configuration de Windows Hello a échoué : {0}" },
                { UnlockFailed, "Le déverrouillage Windows Hello a échoué : {0}\r\nVeuillez utiliser votre mot de passe principal." },
                { WrongDatabase, "Ce fichier n'est pas la base pour laquelle Windows Hello a été activé. Ses données Windows Hello ont été supprimées." },
                { OldPluginFile, "L'ancien fichier du plugin est toujours présent : {0}\r\nKeePass le charge aussi, Windows Hello peut donc demander deux fois. Fermez KeePass et supprimez ce fichier. Rien n'est perdu." },
                { SettingsTitle, "RamzX Unlock - Paramètres" },
                { SettingsSubtitle, "Déverrouillage rapide de KeePass avec Windows Hello" },
                { SectionStatus, "État" },
                { SectionSettings, "Paramètres" },
                { SectionData, "Données enregistrées" },
                { ValidityLabel, "Déverrouillage rapide valable (jours) :" },
                { ValidityNote, "Le déverrouillage rapide prend toujours fin au redémarrage de cet ordinateur. Une valeur modifiée s'applique aux bases activées à partir de maintenant." },
                { OfferEnrollment, "Proposer Windows Hello après l'ouverture d'une base avec le mot de passe principal" },
                { LockWithWindows, "Verrouiller KeePass lorsque Windows est verrouillé, déconnecté ou mis en veille" },
                { LanguageLabel, "Langue :" },
                { LanguageAuto, "Automatique (KeePass / Windows)" },
                { LanguageNote, "Une langue modifiée s'applique entièrement après le redémarrage de KeePass." },
                { StatusHello, "Windows Hello est configuré sur cet ordinateur." },
                { StatusHelloNo, "Windows Hello n'est pas configuré sur cet ordinateur (Paramètres Windows > Comptes > Options de connexion)." },
                { StatusPasskey, "Les clés d'accès sont disponibles : le déverrouillage rapide fonctionne aussi via le Bureau à distance." },
                { StatusPasskeyNo, "Les clés d'accès ne sont pas disponibles sur cette version de Windows : le déverrouillage rapide ne fonctionne que sur cet ordinateur." },
                { StatusRemote, "Session Bureau à distance : Windows Hello du PC devant lequel vous êtes assis est utilisé." },
                { StatusUsage, "{0} base(s) avec déverrouillage rapide, {1} appareil(s) lié(s)." },
                { ForgetButton, "Oublier toutes les données de déverrouillage rapide" },
                { ForgetConfirm, "Supprimer toutes les données de déverrouillage rapide ? Le mot de passe principal sera ensuite de nouveau demandé pour chaque base." },
                { EnrollTitle, "Déverrouillage rapide avec Windows Hello" },
                { EnrollEnable, "Activer" },
                { EnrollNotNow, "Pas maintenant" },
                { PinNote, "Windows Hello accepte toujours le code PIN Windows Hello. Il s'agit d'une fonction de confort, pas d'un second facteur." },
                { Ok, "OK" },
                { Cancel, "Annuler" },
            };
        }

        private static Dictionary<string, string> Es()
        {
            return new Dictionary<string, string>
            {
                { MenuSettings, "Configuración..." },
                { MenuForget, "Olvidar todos los datos de desbloqueo de Windows Hello" },
                { MenuSelfTest, "Autoprueba de Windows Hello" },
                { MenuDeleteSelfTest, "Eliminar la clave de autoprueba" },
                { ForgetDone, "{0} registro(s) de desbloqueo eliminado(s)." },
                { DeleteFailed, "Error al eliminar: {0}" },
                { EnrollQuestion, "¿Activar el desbloqueo rápido con Windows Hello para esta base de datos?\r\n\r\n{0}\r\n\r\n" +
                    "Podrá abrirla con la cara, la huella dactilar o el PIN de Windows Hello del PC que esté usando - también " +
                    "por Escritorio remoto - durante {1} día(s) o hasta que este equipo se reinicie. " +
                    "Después se volverá a pedir la contraseña maestra.\r\n\r\n" +
                    "Importante: Windows Hello siempre acepta el PIN. Quien conozca su PIN de Windows Hello y pueda usar " +
                    "su sesión de Windows podrá abrir esta base durante ese tiempo. " +
                    "Es una función de comodidad, no un segundo factor." },
                { EnrollAnotherDevice, "El desbloqueo rápido con Windows Hello ya está activado para esta base en otro dispositivo.\r\n\r\n{0}\r\n\r\n" +
                    "¿Activarlo también para el PC que usa ahora (cara, huella o PIN, también por Escritorio remoto), " +
                    "durante {1} día(s) o hasta que este equipo se reinicie?\r\n\r\n" +
                    "Importante: Windows Hello siempre acepta el PIN. Es una función de comodidad, no un segundo factor." },
                { HelloNotSetUp, "Windows Hello no está configurado en este equipo. No se ha cambiado nada." },
                { HelloKeyUnavailable, "Clave de Windows Hello no disponible ({0}). No se ha cambiado nada." },
                { HelloStatus, "Windows Hello: {0}. No se ha cambiado nada." },
                { NotRepeatable, "Las firmas de Windows Hello no son repetibles en este equipo. " +
                    "El desbloqueo rápido no se puede usar de forma segura. No se ha cambiado nada." },
                { SelfCheckFailed, "La comprobación de los datos guardados ha fallado. No se ha guardado nada." },
                { EnrollDone, "El desbloqueo rápido con Windows Hello está activado para esta base hasta el {0} o hasta el próximo reinicio de este equipo." },
                { EnrollFailed, "Error al configurar Windows Hello: {0}" },
                { UnlockFailed, "Error al desbloquear con Windows Hello: {0}\r\nUse su contraseña maestra." },
                { WrongDatabase, "Este archivo no es la base para la que se activó Windows Hello. Se han eliminado sus datos de Windows Hello." },
                { OldPluginFile, "El archivo antiguo del plugin sigue ahí: {0}\r\nKeePass también lo carga, así que Windows Hello puede preguntar dos veces. Cierre KeePass y elimine este archivo. No se pierde nada." },
                { SettingsTitle, "RamzX Unlock - Configuración" },
                { SettingsSubtitle, "Desbloqueo rápido de KeePass con Windows Hello" },
                { SectionStatus, "Estado" },
                { SectionSettings, "Configuración" },
                { SectionData, "Datos guardados" },
                { ValidityLabel, "Desbloqueo rápido válido durante (días):" },
                { ValidityNote, "El desbloqueo rápido siempre termina cuando este equipo se reinicia. Un valor cambiado se aplica a las bases activadas a partir de ahora." },
                { OfferEnrollment, "Ofrecer Windows Hello tras abrir una base con la contraseña maestra" },
                { LockWithWindows, "Bloquear KeePass cuando Windows se bloquea, se desconecta o entra en suspensión" },
                { LanguageLabel, "Idioma:" },
                { LanguageAuto, "Automático (KeePass / Windows)" },
                { LanguageNote, "Un idioma cambiado se aplica por completo tras reiniciar KeePass." },
                { StatusHello, "Windows Hello está configurado en este equipo." },
                { StatusHelloNo, "Windows Hello no está configurado en este equipo (Configuración de Windows > Cuentas > Opciones de inicio de sesión)." },
                { StatusPasskey, "Las claves de acceso están disponibles: el desbloqueo rápido también funciona por Escritorio remoto." },
                { StatusPasskeyNo, "Las claves de acceso no están disponibles en esta versión de Windows: el desbloqueo rápido solo funciona en este equipo." },
                { StatusRemote, "Sesión de Escritorio remoto: se usa Windows Hello del PC en el que está sentado." },
                { StatusUsage, "{0} base(s) con desbloqueo rápido, {1} dispositivo(s) vinculado(s)." },
                { ForgetButton, "Olvidar todos los datos de desbloqueo rápido" },
                { ForgetConfirm, "¿Eliminar todos los datos de desbloqueo rápido? Después se volverá a pedir la contraseña maestra para cada base." },
                { EnrollTitle, "Desbloqueo rápido con Windows Hello" },
                { EnrollEnable, "Activar" },
                { EnrollNotNow, "Ahora no" },
                { PinNote, "Windows Hello siempre acepta el PIN de Windows Hello. Es una función de comodidad, no un segundo factor." },
                { Ok, "Aceptar" },
                { Cancel, "Cancelar" },
            };
        }

        private static Dictionary<string, string> It()
        {
            return new Dictionary<string, string>
            {
                { MenuSettings, "Impostazioni..." },
                { MenuForget, "Dimentica tutti i dati di sblocco Windows Hello" },
                { MenuSelfTest, "Autotest di Windows Hello" },
                { MenuDeleteSelfTest, "Elimina la chiave di autotest" },
                { ForgetDone, "{0} record di sblocco eliminati." },
                { DeleteFailed, "Eliminazione non riuscita: {0}" },
                { EnrollQuestion, "Attivare lo sblocco rapido con Windows Hello per questo database?\r\n\r\n{0}\r\n\r\n" +
                    "Potrai aprirlo con il volto, l'impronta digitale o il PIN di Windows Hello del PC che stai usando - anche " +
                    "tramite Desktop remoto - per {1} giorno/i o fino al riavvio di questo computer. " +
                    "Dopo sarà di nuovo richiesta la password principale.\r\n\r\n" +
                    "Importante: Windows Hello accetta sempre il PIN. Chi conosce il tuo PIN di Windows Hello e può usare " +
                    "la tua sessione di Windows può aprire questo database in quel periodo. " +
                    "È una funzione di comodità, non un secondo fattore." },
                { EnrollAnotherDevice, "Lo sblocco rapido con Windows Hello è già attivo per questo database su un altro dispositivo.\r\n\r\n{0}\r\n\r\n" +
                    "Attivarlo anche per il PC che stai usando ora (volto, impronta o PIN, anche tramite Desktop remoto), " +
                    "per {1} giorno/i o fino al riavvio di questo computer?\r\n\r\n" +
                    "Importante: Windows Hello accetta sempre il PIN. È una funzione di comodità, non un secondo fattore." },
                { HelloNotSetUp, "Windows Hello non è configurato su questo computer. Non è stato modificato nulla." },
                { HelloKeyUnavailable, "Chiave Windows Hello non disponibile ({0}). Non è stato modificato nulla." },
                { HelloStatus, "Windows Hello: {0}. Non è stato modificato nulla." },
                { NotRepeatable, "Le firme di Windows Hello non sono ripetibili su questo computer. " +
                    "Lo sblocco rapido non può essere usato in modo sicuro. Non è stato modificato nulla." },
                { SelfCheckFailed, "Il controllo dei dati salvati non è riuscito. Non è stato salvato nulla." },
                { EnrollDone, "Lo sblocco rapido con Windows Hello è attivo per questo database fino al {0} o al prossimo riavvio di questo computer." },
                { EnrollFailed, "Configurazione di Windows Hello non riuscita: {0}" },
                { UnlockFailed, "Sblocco con Windows Hello non riuscito: {0}\r\nUsa la password principale." },
                { WrongDatabase, "Questo file non è il database per cui è stato attivato Windows Hello. I suoi dati Windows Hello sono stati rimossi." },
                { OldPluginFile, "Il vecchio file del plugin è ancora presente: {0}\r\nKeePass carica anche questo, quindi Windows Hello potrebbe chiedere due volte. Chiudi KeePass ed elimina questo file. Non si perde nulla." },
                { SettingsTitle, "RamzX Unlock - Impostazioni" },
                { SettingsSubtitle, "Sblocco rapido di KeePass con Windows Hello" },
                { SectionStatus, "Stato" },
                { SectionSettings, "Impostazioni" },
                { SectionData, "Dati salvati" },
                { ValidityLabel, "Sblocco rapido valido per (giorni):" },
                { ValidityNote, "Lo sblocco rapido termina sempre al riavvio di questo computer. Un valore modificato vale per i database attivati da ora in poi." },
                { OfferEnrollment, "Proponi Windows Hello dopo aver aperto un database con la password principale" },
                { LockWithWindows, "Blocca KeePass quando Windows viene bloccato, disconnesso o va in sospensione" },
                { LanguageLabel, "Lingua:" },
                { LanguageAuto, "Automatica (KeePass / Windows)" },
                { LanguageNote, "Una lingua modificata vale completamente dopo il riavvio di KeePass." },
                { StatusHello, "Windows Hello è configurato su questo computer." },
                { StatusHelloNo, "Windows Hello non è configurato su questo computer (Impostazioni di Windows > Account > Opzioni di accesso)." },
                { StatusPasskey, "Le passkey sono disponibili: lo sblocco rapido funziona anche tramite Desktop remoto." },
                { StatusPasskeyNo, "Le passkey non sono disponibili in questa versione di Windows: lo sblocco rapido funziona solo su questo computer." },
                { StatusRemote, "Sessione Desktop remoto: viene usato Windows Hello del PC davanti a cui sei seduto." },
                { StatusUsage, "{0} database con sblocco rapido, {1} dispositivo/i collegato/i." },
                { ForgetButton, "Dimentica tutti i dati di sblocco rapido" },
                { ForgetConfirm, "Eliminare tutti i dati di sblocco rapido? Poi sarà di nuovo richiesta la password principale per ogni database." },
                { EnrollTitle, "Sblocco rapido con Windows Hello" },
                { EnrollEnable, "Attiva" },
                { EnrollNotNow, "Non ora" },
                { PinNote, "Windows Hello accetta sempre il PIN di Windows Hello. È una funzione di comodità, non un secondo fattore." },
                { Ok, "OK" },
                { Cancel, "Annulla" },
            };
        }

        private static Dictionary<string, string> Nl()
        {
            return new Dictionary<string, string>
            {
                { MenuSettings, "Instellingen..." },
                { MenuForget, "Alle Windows Hello-ontgrendelgegevens vergeten" },
                { MenuSelfTest, "Windows Hello-zelftest" },
                { MenuDeleteSelfTest, "Zelftestsleutel verwijderen" },
                { ForgetDone, "{0} opgeslagen ontgrendelrecord(s) verwijderd." },
                { DeleteFailed, "Verwijderen mislukt: {0}" },
                { EnrollQuestion, "Snelle ontgrendeling met Windows Hello inschakelen voor deze database?\r\n\r\n{0}\r\n\r\n" +
                    "U kunt deze dan openen met gezicht, vingerafdruk of de Windows Hello-pincode (PIN) van de pc die u " +
                    "gebruikt - ook via Extern bureaublad - gedurende {1} dag(en) of tot deze computer opnieuw opstart. " +
                    "Daarna is het hoofdwachtwoord weer nodig.\r\n\r\n" +
                    "Belangrijk: Windows Hello accepteert altijd ook de PIN. Wie uw Windows Hello-PIN kent en uw " +
                    "Windows-sessie kan gebruiken, kan deze database in die tijd openen. " +
                    "Dit is een gemaksfunctie, geen tweede factor." },
                { EnrollAnotherDevice, "Snelle ontgrendeling met Windows Hello is voor deze database al ingeschakeld op een ander apparaat.\r\n\r\n{0}\r\n\r\n" +
                    "Ook inschakelen voor de pc die u nu gebruikt (gezicht, vingerafdruk of PIN, ook via Extern bureaublad), " +
                    "gedurende {1} dag(en) of tot deze computer opnieuw opstart?\r\n\r\n" +
                    "Belangrijk: Windows Hello accepteert altijd ook de PIN. Dit is een gemaksfunctie, geen tweede factor." },
                { HelloNotSetUp, "Windows Hello is niet ingesteld op deze computer. Er is niets gewijzigd." },
                { HelloKeyUnavailable, "Windows Hello-sleutel niet beschikbaar ({0}). Er is niets gewijzigd." },
                { HelloStatus, "Windows Hello: {0}. Er is niets gewijzigd." },
                { NotRepeatable, "Windows Hello-handtekeningen zijn op deze computer niet herhaalbaar. " +
                    "Snelle ontgrendeling kan niet veilig worden gebruikt. Er is niets gewijzigd." },
                { SelfCheckFailed, "De controle van de opgeslagen gegevens is mislukt. Er is niets bewaard." },
                { EnrollDone, "Snelle ontgrendeling met Windows Hello is ingeschakeld voor deze database tot {0} of tot de volgende herstart van deze computer." },
                { EnrollFailed, "Instellen van Windows Hello mislukt: {0}" },
                { UnlockFailed, "Ontgrendelen met Windows Hello mislukt: {0}\r\nGebruik uw hoofdwachtwoord." },
                { WrongDatabase, "Dit bestand is niet de database waarvoor Windows Hello is ingeschakeld. De Windows Hello-gegevens zijn verwijderd." },
                { OldPluginFile, "Het oude pluginbestand staat er nog: {0}\r\nKeePass laadt het ook, waardoor Windows Hello mogelijk twee keer vraagt. Sluit KeePass en verwijder dit bestand. Er gaat niets verloren." },
                { SettingsTitle, "RamzX Unlock - Instellingen" },
                { SettingsSubtitle, "Snelle ontgrendeling van KeePass met Windows Hello" },
                { SectionStatus, "Status" },
                { SectionSettings, "Instellingen" },
                { SectionData, "Opgeslagen gegevens" },
                { ValidityLabel, "Snelle ontgrendeling geldig gedurende (dagen):" },
                { ValidityNote, "Snelle ontgrendeling eindigt altijd bij een herstart van deze computer. Een gewijzigde waarde geldt voor databases die vanaf nu worden ingeschakeld." },
                { OfferEnrollment, "Windows Hello aanbieden na het openen van een database met het hoofdwachtwoord" },
                { LockWithWindows, "KeePass vergrendelen wanneer Windows wordt vergrendeld, ontkoppeld of in slaapstand gaat" },
                { LanguageLabel, "Taal:" },
                { LanguageAuto, "Automatisch (KeePass / Windows)" },
                { LanguageNote, "Een gewijzigde taal geldt volledig na het opnieuw starten van KeePass." },
                { StatusHello, "Windows Hello is ingesteld op deze computer." },
                { StatusHelloNo, "Windows Hello is niet ingesteld op deze computer (Windows-instellingen > Accounts > Aanmeldingsopties)." },
                { StatusPasskey, "Passkeys zijn beschikbaar: snelle ontgrendeling werkt ook via Extern bureaublad." },
                { StatusPasskeyNo, "Passkeys zijn niet beschikbaar in deze Windows-versie: snelle ontgrendeling werkt alleen op deze computer." },
                { StatusRemote, "Extern-bureaubladsessie: Windows Hello van de pc waar u achter zit wordt gebruikt." },
                { StatusUsage, "{0} database(s) met snelle ontgrendeling, {1} gekoppeld(e) apparaat/apparaten." },
                { ForgetButton, "Alle snelle-ontgrendelingsgegevens vergeten" },
                { ForgetConfirm, "Alle opgeslagen snelle-ontgrendelingsgegevens verwijderen? Daarna is voor elke database weer het hoofdwachtwoord nodig." },
                { EnrollTitle, "Snelle ontgrendeling met Windows Hello" },
                { EnrollEnable, "Inschakelen" },
                { EnrollNotNow, "Niet nu" },
                { PinNote, "Windows Hello accepteert altijd ook de Windows Hello-PIN. Dit is een gemaksfunctie, geen tweede factor." },
                { Ok, "OK" },
                { Cancel, "Annuleren" },
            };
        }

        private static Dictionary<string, string> Pl()
        {
            return new Dictionary<string, string>
            {
                { MenuSettings, "Ustawienia..." },
                { MenuForget, "Zapomnij wszystkie dane odblokowania Windows Hello" },
                { MenuSelfTest, "Autotest Windows Hello" },
                { MenuDeleteSelfTest, "Usuń klucz autotestu" },
                { ForgetDone, "Usunięto zapisane dane odblokowania: {0}." },
                { DeleteFailed, "Usuwanie nie powiodło się: {0}" },
                { EnrollQuestion, "Włączyć szybkie odblokowanie Windows Hello dla tej bazy danych?\r\n\r\n{0}\r\n\r\n" +
                    "Będzie można ją otworzyć twarzą, odciskiem palca lub kodem PIN Windows Hello komputera, którego " +
                    "używasz - także przez Pulpit zdalny - przez {1} dni lub do ponownego uruchomienia tego komputera. " +
                    "Potem ponownie wymagane będzie hasło główne.\r\n\r\n" +
                    "Ważne: Windows Hello zawsze akceptuje kod PIN. Każdy, kto zna Twój PIN Windows Hello i może " +
                    "korzystać z Twojej sesji Windows, może w tym czasie otworzyć tę bazę. " +
                    "To funkcja wygody, a nie drugi składnik uwierzytelniania." },
                { EnrollAnotherDevice, "Szybkie odblokowanie Windows Hello jest już włączone dla tej bazy na innym urządzeniu.\r\n\r\n{0}\r\n\r\n" +
                    "Włączyć je także dla komputera, którego używasz teraz (twarz, odcisk palca lub PIN, także przez Pulpit zdalny), " +
                    "przez {1} dni lub do ponownego uruchomienia tego komputera?\r\n\r\n" +
                    "Ważne: Windows Hello zawsze akceptuje kod PIN. To funkcja wygody, a nie drugi składnik uwierzytelniania." },
                { HelloNotSetUp, "Windows Hello nie jest skonfigurowane na tym komputerze. Nic nie zostało zmienione." },
                { HelloKeyUnavailable, "Klucz Windows Hello niedostępny ({0}). Nic nie zostało zmienione." },
                { HelloStatus, "Windows Hello: {0}. Nic nie zostało zmienione." },
                { NotRepeatable, "Podpisy Windows Hello nie są powtarzalne na tym komputerze. " +
                    "Szybkiego odblokowania nie można bezpiecznie używać. Nic nie zostało zmienione." },
                { SelfCheckFailed, "Sprawdzenie zapisanych danych nie powiodło się. Nic nie zostało zachowane." },
                { EnrollDone, "Szybkie odblokowanie Windows Hello jest włączone dla tej bazy do {0} lub do następnego uruchomienia tego komputera." },
                { EnrollFailed, "Konfiguracja Windows Hello nie powiodła się: {0}" },
                { UnlockFailed, "Odblokowanie Windows Hello nie powiodło się: {0}\r\nUżyj hasła głównego." },
                { WrongDatabase, "Ten plik nie jest bazą, dla której włączono Windows Hello. Jego dane Windows Hello zostały usunięte." },
                { OldPluginFile, "Stary plik wtyczki nadal istnieje: {0}\r\nKeePass też go wczytuje, więc Windows Hello może pytać dwa razy. Zamknij KeePass i usuń ten plik. Nic nie zostanie utracone." },
                { SettingsTitle, "RamzX Unlock - Ustawienia" },
                { SettingsSubtitle, "Szybkie odblokowanie KeePass za pomocą Windows Hello" },
                { SectionStatus, "Stan" },
                { SectionSettings, "Ustawienia" },
                { SectionData, "Zapisane dane" },
                { ValidityLabel, "Szybkie odblokowanie ważne przez (dni):" },
                { ValidityNote, "Szybkie odblokowanie zawsze kończy się przy ponownym uruchomieniu tego komputera. Zmieniona wartość dotyczy baz włączonych od teraz." },
                { OfferEnrollment, "Proponuj Windows Hello po otwarciu bazy hasłem głównym" },
                { LockWithWindows, "Blokuj KeePass, gdy Windows jest blokowany, rozłączany lub usypiany" },
                { LanguageLabel, "Język:" },
                { LanguageAuto, "Automatycznie (KeePass / Windows)" },
                { LanguageNote, "Zmieniony język obowiązuje w pełni po ponownym uruchomieniu KeePass." },
                { StatusHello, "Windows Hello jest skonfigurowane na tym komputerze." },
                { StatusHelloNo, "Windows Hello nie jest skonfigurowane na tym komputerze (Ustawienia Windows > Konta > Opcje logowania)." },
                { StatusPasskey, "Klucze dostępu są dostępne: szybkie odblokowanie działa także przez Pulpit zdalny." },
                { StatusPasskeyNo, "Klucze dostępu nie są dostępne w tej wersji Windows: szybkie odblokowanie działa tylko na tym komputerze." },
                { StatusRemote, "Sesja Pulpitu zdalnego: używane jest Windows Hello komputera, przy którym siedzisz." },
                { StatusUsage, "Bazy z szybkim odblokowaniem: {0}, połączone urządzenia: {1}." },
                { ForgetButton, "Zapomnij wszystkie dane szybkiego odblokowania" },
                { ForgetConfirm, "Usunąć wszystkie zapisane dane szybkiego odblokowania? Potem dla każdej bazy ponownie wymagane będzie hasło główne." },
                { EnrollTitle, "Szybkie odblokowanie z Windows Hello" },
                { EnrollEnable, "Włącz" },
                { EnrollNotNow, "Nie teraz" },
                { PinNote, "Windows Hello zawsze akceptuje PIN Windows Hello. To funkcja wygody, a nie drugi składnik uwierzytelniania." },
                { Ok, "OK" },
                { Cancel, "Anuluj" },
            };
        }

        private static Dictionary<string, string> Pt()
        {
            return new Dictionary<string, string>
            {
                { MenuSettings, "Configurações..." },
                { MenuForget, "Esquecer todos os dados de desbloqueio do Windows Hello" },
                { MenuSelfTest, "Autoteste do Windows Hello" },
                { MenuDeleteSelfTest, "Excluir a chave de autoteste" },
                { ForgetDone, "{0} registro(s) de desbloqueio excluído(s)." },
                { DeleteFailed, "Falha ao excluir: {0}" },
                { EnrollQuestion, "Ativar o desbloqueio rápido com Windows Hello para este banco de dados?\r\n\r\n{0}\r\n\r\n" +
                    "Você poderá abri-lo com rosto, impressão digital ou o PIN do Windows Hello do PC que está usando - também " +
                    "pela Área de Trabalho Remota - por {1} dia(s) ou até este computador reiniciar. " +
                    "Depois, a senha mestra será pedida novamente.\r\n\r\n" +
                    "Importante: o Windows Hello sempre aceita o PIN. Quem souber o seu PIN do Windows Hello e puder usar " +
                    "a sua sessão do Windows poderá abrir este banco durante esse tempo. " +
                    "É um recurso de conveniência, não um segundo fator." },
                { EnrollAnotherDevice, "O desbloqueio rápido com Windows Hello já está ativado para este banco em outro dispositivo.\r\n\r\n{0}\r\n\r\n" +
                    "Ativá-lo também para o PC que você está usando agora (rosto, impressão digital ou PIN, também pela Área de Trabalho Remota), " +
                    "por {1} dia(s) ou até este computador reiniciar?\r\n\r\n" +
                    "Importante: o Windows Hello sempre aceita o PIN. É um recurso de conveniência, não um segundo fator." },
                { HelloNotSetUp, "O Windows Hello não está configurado neste computador. Nada foi alterado." },
                { HelloKeyUnavailable, "Chave do Windows Hello indisponível ({0}). Nada foi alterado." },
                { HelloStatus, "Windows Hello: {0}. Nada foi alterado." },
                { NotRepeatable, "As assinaturas do Windows Hello não são repetíveis neste computador. " +
                    "O desbloqueio rápido não pode ser usado com segurança. Nada foi alterado." },
                { SelfCheckFailed, "A verificação dos dados salvos falhou. Nada foi mantido." },
                { EnrollDone, "O desbloqueio rápido com Windows Hello está ativado para este banco até {0} ou até a próxima reinicialização deste computador." },
                { EnrollFailed, "Falha ao configurar o Windows Hello: {0}" },
                { UnlockFailed, "Falha ao desbloquear com Windows Hello: {0}\r\nUse a sua senha mestra." },
                { WrongDatabase, "Este arquivo não é o banco para o qual o Windows Hello foi ativado. Os dados do Windows Hello foram removidos." },
                { OldPluginFile, "O arquivo antigo do plugin ainda existe: {0}\r\nO KeePass também o carrega, então o Windows Hello pode pedir duas vezes. Feche o KeePass e exclua este arquivo. Nada é perdido." },
                { SettingsTitle, "RamzX Unlock - Configurações" },
                { SettingsSubtitle, "Desbloqueio rápido do KeePass com Windows Hello" },
                { SectionStatus, "Status" },
                { SectionSettings, "Configurações" },
                { SectionData, "Dados salvos" },
                { ValidityLabel, "Desbloqueio rápido válido por (dias):" },
                { ValidityNote, "O desbloqueio rápido sempre termina quando este computador reinicia. Um valor alterado vale para bancos ativados a partir de agora." },
                { OfferEnrollment, "Oferecer o Windows Hello após abrir um banco com a senha mestra" },
                { LockWithWindows, "Bloquear o KeePass quando o Windows for bloqueado, desconectado ou entrar em suspensão" },
                { LanguageLabel, "Idioma:" },
                { LanguageAuto, "Automático (KeePass / Windows)" },
                { LanguageNote, "Um idioma alterado vale totalmente após reiniciar o KeePass." },
                { StatusHello, "O Windows Hello está configurado neste computador." },
                { StatusHelloNo, "O Windows Hello não está configurado neste computador (Configurações do Windows > Contas > Opções de entrada)." },
                { StatusPasskey, "As chaves de acesso estão disponíveis: o desbloqueio rápido também funciona pela Área de Trabalho Remota." },
                { StatusPasskeyNo, "As chaves de acesso não estão disponíveis nesta versão do Windows: o desbloqueio rápido funciona só neste computador." },
                { StatusRemote, "Sessão de Área de Trabalho Remota: é usado o Windows Hello do PC em que você está sentado." },
                { StatusUsage, "{0} banco(s) com desbloqueio rápido, {1} dispositivo(s) vinculado(s)." },
                { ForgetButton, "Esquecer todos os dados de desbloqueio rápido" },
                { ForgetConfirm, "Excluir todos os dados de desbloqueio rápido? Depois, a senha mestra será pedida novamente para cada banco." },
                { EnrollTitle, "Desbloqueio rápido com Windows Hello" },
                { EnrollEnable, "Ativar" },
                { EnrollNotNow, "Agora não" },
                { PinNote, "O Windows Hello sempre aceita o PIN do Windows Hello. É um recurso de conveniência, não um segundo fator." },
                { Ok, "OK" },
                { Cancel, "Cancelar" },
            };
        }

        private static Dictionary<string, string> Tr()
        {
            return new Dictionary<string, string>
            {
                { MenuSettings, "Ayarlar..." },
                { MenuForget, "Tüm Windows Hello kilit açma verilerini unut" },
                { MenuSelfTest, "Windows Hello kendi kendine testi" },
                { MenuDeleteSelfTest, "Test anahtarını sil" },
                { ForgetDone, "{0} kayıtlı kilit açma verisi silindi." },
                { DeleteFailed, "Silme başarısız: {0}" },
                { EnrollQuestion, "Bu veritabanı için Windows Hello ile hızlı kilit açma etkinleştirilsin mi?\r\n\r\n{0}\r\n\r\n" +
                    "Ardından veritabanını, kullandığınız bilgisayarın yüzü, parmak izi veya Windows Hello PIN'i ile - Uzak " +
                    "Masaüstü üzerinden de - {1} gün boyunca veya bu bilgisayar yeniden başlayana kadar açabilirsiniz. " +
                    "Sonra ana parola yeniden gerekir.\r\n\r\n" +
                    "Önemli: Windows Hello her zaman PIN'i de kabul eder. Windows Hello PIN'inizi bilen ve Windows " +
                    "oturumunuzu kullanabilen biri bu süre içinde veritabanını açabilir. " +
                    "Bu bir kolaylık özelliğidir, ikinci faktör değildir." },
                { EnrollAnotherDevice, "Windows Hello ile hızlı kilit açma bu veritabanı için başka bir cihazda zaten etkin.\r\n\r\n{0}\r\n\r\n" +
                    "Şu anda kullandığınız bilgisayar için de etkinleştirilsin mi (yüz, parmak izi veya PIN, Uzak Masaüstü üzerinden de), " +
                    "{1} gün boyunca veya bu bilgisayar yeniden başlayana kadar?\r\n\r\n" +
                    "Önemli: Windows Hello her zaman PIN'i de kabul eder. Bu bir kolaylık özelliğidir, ikinci faktör değildir." },
                { HelloNotSetUp, "Windows Hello bu bilgisayarda ayarlanmamış. Hiçbir şey değiştirilmedi." },
                { HelloKeyUnavailable, "Windows Hello anahtarı kullanılamıyor ({0}). Hiçbir şey değiştirilmedi." },
                { HelloStatus, "Windows Hello: {0}. Hiçbir şey değiştirilmedi." },
                { NotRepeatable, "Windows Hello imzaları bu bilgisayarda tekrarlanabilir değil. " +
                    "Hızlı kilit açma güvenli şekilde kullanılamaz. Hiçbir şey değiştirilmedi." },
                { SelfCheckFailed, "Kayıtlı verilerin kontrolü başarısız oldu. Hiçbir şey saklanmadı." },
                { EnrollDone, "Windows Hello ile hızlı kilit açma bu veritabanı için {0} tarihine veya bu bilgisayarın bir sonraki yeniden başlatılmasına kadar etkin." },
                { EnrollFailed, "Windows Hello kurulumu başarısız: {0}" },
                { UnlockFailed, "Windows Hello ile kilit açma başarısız: {0}\r\nLütfen ana parolanızı kullanın." },
                { WrongDatabase, "Bu dosya, Windows Hello'nun etkinleştirildiği veritabanı değil. Windows Hello verileri kaldırıldı." },
                { OldPluginFile, "Eski eklenti dosyası hâlâ duruyor: {0}\r\nKeePass bunu da yüklüyor, bu yüzden Windows Hello iki kez sorabilir. KeePass'i kapatıp bu dosyayı silin. Hiçbir şey kaybolmaz." },
                { SettingsTitle, "RamzX Unlock - Ayarlar" },
                { SettingsSubtitle, "Windows Hello ile KeePass hızlı kilit açma" },
                { SectionStatus, "Durum" },
                { SectionSettings, "Ayarlar" },
                { SectionData, "Kayıtlı veriler" },
                { ValidityLabel, "Hızlı kilit açma geçerlilik süresi (gün):" },
                { ValidityNote, "Hızlı kilit açma bu bilgisayar yeniden başladığında her zaman sona erer. Değiştirilen değer bundan sonra etkinleştirilen veritabanları için geçerlidir." },
                { OfferEnrollment, "Ana parola ile bir veritabanı açıldıktan sonra Windows Hello öner" },
                { LockWithWindows, "Windows kilitlendiğinde, bağlantı kesildiğinde veya uyku moduna geçtiğinde KeePass'i kilitle" },
                { LanguageLabel, "Dil:" },
                { LanguageAuto, "Otomatik (KeePass / Windows)" },
                { LanguageNote, "Değiştirilen dil, KeePass yeniden başlatıldıktan sonra tamamen geçerli olur." },
                { StatusHello, "Windows Hello bu bilgisayarda ayarlanmış." },
                { StatusHelloNo, "Windows Hello bu bilgisayarda ayarlanmamış (Windows Ayarları > Hesaplar > Oturum açma seçenekleri)." },
                { StatusPasskey, "Geçiş anahtarları kullanılabilir: hızlı kilit açma Uzak Masaüstü üzerinden de çalışır." },
                { StatusPasskeyNo, "Bu Windows sürümünde geçiş anahtarları kullanılamıyor: hızlı kilit açma yalnızca bu bilgisayarda çalışır." },
                { StatusRemote, "Uzak Masaüstü oturumu: önünde oturduğunuz bilgisayarın Windows Hello'su kullanılır." },
                { StatusUsage, "Hızlı kilit açmalı {0} veritabanı, {1} bağlı cihaz." },
                { ForgetButton, "Tüm hızlı kilit açma verilerini unut" },
                { ForgetConfirm, "Tüm kayıtlı hızlı kilit açma verileri silinsin mi? Ardından her veritabanı için ana parola yeniden gerekir." },
                { EnrollTitle, "Windows Hello ile hızlı kilit açma" },
                { EnrollEnable, "Etkinleştir" },
                { EnrollNotNow, "Şimdi değil" },
                { PinNote, "Windows Hello her zaman Windows Hello PIN'ini de kabul eder. Bu bir kolaylık özelliğidir, ikinci faktör değildir." },
                { Ok, "Tamam" },
                { Cancel, "İptal" },
            };
        }
    }
}
