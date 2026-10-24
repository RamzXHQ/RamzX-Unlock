// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace MiftawerkUnlock
{
    /// <summary>User-visible texts in English, German and Arabic.</summary>
    internal static class Strings
    {
        internal static readonly string[] Languages = { "en", "de", "ar" };

        private static Dictionary<string, string> s_current = En();
        private static string s_lang = "en";

        internal static string Language { get { return s_lang; } }
        internal static bool RightToLeft { get { return s_lang == "ar"; } }

        /// <summary>
        /// Uses the KeePass UI language if KeePass is translated, otherwise the Windows display
        /// language. Unsupported languages fall back to English.
        /// </summary>
        internal static void Select(string keePassIso6391)
        {
            string lang = (keePassIso6391 ?? string.Empty).Trim().ToLowerInvariant();
            if (lang.Length == 0 || lang == "en")
                lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            SetLanguage(lang);
        }

        internal static void SetLanguage(string lang)
        {
            switch ((lang ?? string.Empty).ToLowerInvariant())
            {
                case "de": s_current = De(); s_lang = "de"; break;
                case "ar": s_current = Ar(); s_lang = "ar"; break;
                default: s_current = En(); s_lang = "en"; break;
            }
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
        internal const string HelloNotSetUp = "HelloNotSetUp";
        internal const string HelloKeyUnavailable = "HelloKeyUnavailable";
        internal const string HelloStatus = "HelloStatus";
        internal const string NotRepeatable = "NotRepeatable";
        internal const string SelfCheckFailed = "SelfCheckFailed";
        internal const string EnrollDone = "EnrollDone";
        internal const string EnrollFailed = "EnrollFailed";
        internal const string UnlockFailed = "UnlockFailed";
        internal const string WrongDatabase = "WrongDatabase";
        internal const string SettingsTitle = "SettingsTitle";
        internal const string ValidityLabel = "ValidityLabel";
        internal const string ValidityNote = "ValidityNote";
        internal const string OfferEnrollment = "OfferEnrollment";
        internal const string LockWithWindows = "LockWithWindows";
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
                    "You can then open it with face, fingerprint or your Windows Hello PIN for {1} day(s) " +
                    "or until Windows restarts. After that, the master password is required again.\r\n\r\n" +
                    "Important: Windows Hello always accepts the PIN. Anyone who knows your Windows Hello PIN " +
                    "and can use your Windows session can open this database during that time. " +
                    "This is a convenience feature, not a second factor." },
                { HelloNotSetUp, "Windows Hello is not set up on this computer. Nothing was changed." },
                { HelloKeyUnavailable, "Windows Hello key not available ({0}). Nothing was changed." },
                { HelloStatus, "Windows Hello: {0}. Nothing was changed." },
                { NotRepeatable, "Windows Hello signatures are not repeatable on this computer. " +
                    "Quick unlock cannot be used safely. Nothing was changed." },
                { SelfCheckFailed, "Self-check of the stored data failed. Nothing was kept." },
                { EnrollDone, "Windows Hello quick unlock is enabled for this database until {0} or the next Windows restart." },
                { EnrollFailed, "Windows Hello setup failed: {0}" },
                { UnlockFailed, "Windows Hello unlock failed: {0}\r\nPlease use your master password." },
                { WrongDatabase, "This file is not the database Windows Hello was enabled for. Its Windows Hello data was removed." },
                { SettingsTitle, "RamzX Unlock - Settings" },
                { ValidityLabel, "Quick unlock valid for (days):" },
                { ValidityNote, "Quick unlock always ends at the next Windows restart. A changed value applies to databases enabled from now on." },
                { OfferEnrollment, "Offer Windows Hello after opening a database with the master password" },
                { LockWithWindows, "Lock KeePass when Windows is locked, disconnected or goes to sleep" },
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
                    "Sie können sie dann {1} Tag(e) lang oder bis zum nächsten Windows-Neustart mit Gesicht, " +
                    "Fingerabdruck oder Windows-Hello-PIN öffnen. Danach ist wieder das Master-Passwort nötig.\r\n\r\n" +
                    "Wichtig: Windows Hello akzeptiert immer auch die PIN. Wer Ihre Windows-Hello-PIN kennt und " +
                    "Ihre Windows-Sitzung nutzen kann, kann diese Datenbank in dieser Zeit öffnen. " +
                    "Das ist eine Komfortfunktion, kein zweiter Faktor." },
                { HelloNotSetUp, "Windows Hello ist auf diesem Computer nicht eingerichtet. Es wurde nichts geändert." },
                { HelloKeyUnavailable, "Windows-Hello-Schlüssel nicht verfügbar ({0}). Es wurde nichts geändert." },
                { HelloStatus, "Windows Hello: {0}. Es wurde nichts geändert." },
                { NotRepeatable, "Windows-Hello-Signaturen sind auf diesem Computer nicht wiederholbar. " +
                    "Die Schnellentsperrung kann nicht sicher verwendet werden. Es wurde nichts geändert." },
                { SelfCheckFailed, "Die Selbstprüfung der gespeicherten Daten ist fehlgeschlagen. Es wurde nichts gespeichert." },
                { EnrollDone, "Die Windows-Hello-Schnellentsperrung ist für diese Datenbank bis {0} oder bis zum nächsten Windows-Neustart aktiv." },
                { EnrollFailed, "Einrichtung von Windows Hello fehlgeschlagen: {0}" },
                { UnlockFailed, "Entsperren mit Windows Hello fehlgeschlagen: {0}\r\nBitte verwenden Sie Ihr Master-Passwort." },
                { WrongDatabase, "Diese Datei ist nicht die Datenbank, für die Windows Hello aktiviert wurde. Die Windows-Hello-Daten wurden entfernt." },
                { SettingsTitle, "RamzX Unlock - Einstellungen" },
                { ValidityLabel, "Schnellentsperrung gültig für (Tage):" },
                { ValidityNote, "Die Schnellentsperrung endet immer beim nächsten Windows-Neustart. Ein geänderter Wert gilt für ab jetzt aktivierte Datenbanken." },
                { OfferEnrollment, "Windows Hello anbieten, nachdem eine Datenbank mit dem Master-Passwort geöffnet wurde" },
                { LockWithWindows, "KeePass sperren, wenn Windows gesperrt, getrennt oder in den Standby versetzt wird" },
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
                    "يمكنك بعدها فتحها بالوجه أو البصمة أو رمز PIN لمدة {1} يوم/أيام أو حتى إعادة تشغيل Windows. " +
                    "بعد ذلك تُطلب كلمة السر الرئيسية من جديد.\r\n\r\n" +
                    "مهم: يقبل Windows Hello دائماً رمز PIN أيضاً. من يعرف رمز PIN ويستطيع استخدام جلسة Windows " +
                    "الخاصة بك يمكنه فتح هذه القاعدة خلال هذه المدة. هذه ميزة للراحة، وليست عامل تحقق ثانياً." },
                { HelloNotSetUp, "Windows Hello غير مُعدّ على هذا الجهاز. لم يتغيّر شيء." },
                { HelloKeyUnavailable, "مفتاح Windows Hello غير متاح ({0}). لم يتغيّر شيء." },
                { HelloStatus, "Windows Hello: {0}. لم يتغيّر شيء." },
                { NotRepeatable, "توقيعات Windows Hello غير قابلة للتكرار على هذا الجهاز، ولا يمكن استخدام الفتح السريع بأمان. لم يتغيّر شيء." },
                { SelfCheckFailed, "فشل الفحص الذاتي للبيانات المحفوظة. لم يُحفظ شيء." },
                { EnrollDone, "تم تفعيل الفتح السريع عبر Windows Hello لهذه القاعدة حتى {0} أو حتى إعادة تشغيل Windows." },
                { EnrollFailed, "فشل إعداد Windows Hello: {0}" },
                { UnlockFailed, "فشل الفتح عبر Windows Hello: {0}\r\nاستخدم كلمة السر الرئيسية من فضلك." },
                { WrongDatabase, "هذا الملف ليس القاعدة التي فُعّل لها Windows Hello. تم حذف بيانات Windows Hello الخاصة به." },
                { SettingsTitle, "RamzX Unlock - الإعدادات" },
                { ValidityLabel, "مدة صلاحية الفتح السريع (بالأيام):" },
                { ValidityNote, "ينتهي الفتح السريع دائماً عند إعادة تشغيل Windows. القيمة الجديدة تسري على القواعد التي تُفعَّل من الآن." },
                { OfferEnrollment, "عرض Windows Hello بعد فتح قاعدة بكلمة السر الرئيسية" },
                { LockWithWindows, "قفل KeePass عند قفل Windows أو قطع الجلسة أو وضع السكون" },
                { PinNote, "يقبل Windows Hello دائماً رمز PIN أيضاً. هذه ميزة للراحة، وليست عامل تحقق ثانياً." },
                { Ok, "موافق" },
                { Cancel, "إلغاء" },
            };
        }
    }
}
