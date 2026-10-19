// Miftawerk Unlock - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;

using KeePass.App.Configuration;

namespace MiftawerkUnlock
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

        internal static int ClampDays(long days)
        {
            if (days < MinValidityDays) return MinValidityDays;
            if (days > MaxValidityDays) return MaxValidityDays;
            return (int)days;
        }
    }
}
