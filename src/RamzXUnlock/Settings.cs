// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;

using KeePass.App.Configuration;

namespace RamzXUnlock
{
    /// <summary>
    /// Plugin settings, stored in KeePass's own configuration file (custom config section),
    /// so they follow KeePass's portable / per-user configuration rules.
    /// </summary>
    internal sealed class Settings
    {
        internal const int MinValidityDays = 1;
        internal const int MaxValidityDays = 30;
        internal const int DefaultValidityDays = 7;

        private const string KeyValidityDays = "MiftawerkUnlock.ValidityDays";
        private const string KeyOfferEnrollment = "MiftawerkUnlock.OfferEnrollment";
        private const string KeyLockWithWindows = "MiftawerkUnlock.LockWithWindows";
        private const string KeyLanguage = "MiftawerkUnlock.Language";

        private readonly AceCustomConfig m_cfg;

        internal Settings(AceCustomConfig cfg)
        {
            if (cfg == null) throw new ArgumentNullException("cfg");
            m_cfg = cfg;
        }

        /// <summary>How long a quick-unlock record stays valid (it always ends at a Windows restart).</summary>
        internal int ValidityDays
        {
            get { return ClampDays(m_cfg.GetLong(KeyValidityDays, DefaultValidityDays)); }
            set { m_cfg.SetLong(KeyValidityDays, ClampDays(value)); }
        }

        internal TimeSpan Validity { get { return TimeSpan.FromDays(ValidityDays); } }

        /// <summary>Ask "Enable Windows Hello?" after a manual unlock.</summary>
        internal bool OfferEnrollment
        {
            get { return m_cfg.GetBool(KeyOfferEnrollment, true); }
            set { m_cfg.SetBool(KeyOfferEnrollment, value); }
        }

        /// <summary>Lock KeePass on Windows lock / disconnect / sleep.</summary>
        internal bool LockWithWindows
        {
            get { return m_cfg.GetBool(KeyLockWithWindows, true); }
            set { m_cfg.SetBool(KeyLockWithWindows, value); }
        }

        /// <summary>UI language ("en", "de", "ar", ...); empty = automatic (KeePass, then Windows).</summary>
        internal string Language
        {
            get
            {
                string l = (m_cfg.GetString(KeyLanguage, string.Empty) ?? string.Empty).Trim().ToLowerInvariant();
                return Array.IndexOf(Strings.Languages, l) >= 0 ? l : string.Empty;
            }
            set { m_cfg.SetString(KeyLanguage, value ?? string.Empty); }
        }

        private const string KeyDeclinedAnother = "RamzXUnlock.DeclinedAnotherDevice";
        private const int MaxDeclined = 64;

        /// <summary>
        /// Databases (path hash, Base64) for which the user answered "Not now" to "also enable on this
        /// computer?" – remembered across KeePass starts, so the question does not come back every time.
        /// </summary>
        internal bool IsAnotherDeviceDeclined(string id)
        {
            return Array.IndexOf(Declined(), id) >= 0;
        }

        internal void DeclineAnotherDevice(string id)
        {
            System.Collections.Generic.List<string> l = new System.Collections.Generic.List<string>(Declined());
            if (l.Contains(id)) return;
            l.Add(id);
            if (l.Count > MaxDeclined) l.RemoveRange(0, l.Count - MaxDeclined);
            m_cfg.SetString(KeyDeclinedAnother, string.Join(";", l.ToArray()));
        }

        /// <summary>"Forget all" also forgets these answers.</summary>
        internal void ClearDeclined()
        {
            m_cfg.SetString(KeyDeclinedAnother, string.Empty);
        }

        private string[] Declined()
        {
            string s = m_cfg.GetString(KeyDeclinedAnother, string.Empty) ?? string.Empty;
            return s.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        internal static int ClampDays(long days)
        {
            if (days < MinValidityDays) return MinValidityDays;
            if (days > MaxValidityDays) return MaxValidityDays;
            return (int)days;
        }
    }
}
