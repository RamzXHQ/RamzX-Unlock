// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Collections.Generic;

using KeePass;
using KeePass.App.Configuration;
using KeePassLib.Serialization;

namespace RamzXUnlock
{
    /// <summary>
    /// KeePass remembers the key sources of every opened database ("remember key sources").
    /// After a quick unlock it would remember our KcpCustomKey as a key provider that does
    /// not exist: the next master key prompt then has the password box unticked and an
    /// unknown provider ticked. These helpers keep the association the user had before.
    /// </summary>
    internal static class KeySources
    {
        /// <summary>Copy of the current association of the database (null if none).</summary>
        internal static AceKeyAssoc Capture(IOConnectionInfo ioc)
        {
            AceKeyAssoc a = Program.Config.Defaults.GetKeySources(ioc);
            if (a == null || IsOurs(a.KeyProvider)) return null;
            AceKeyAssoc c = new AceKeyAssoc();
            c.DatabasePath = a.DatabasePath;
            c.Password = a.Password;
            c.KeyFilePath = a.KeyFilePath;
            c.KeyProvider = a.KeyProvider;
            c.UserAccount = a.UserAccount;
            return c;
        }

        /// <summary>Drops what KeePass remembered for the quick unlock and puts back <paramref name="before"/>.</summary>
        internal static void Restore(AceKeyAssoc before)
        {
            List<AceKeyAssoc> list = Program.Config.Defaults.KeySources;
            RemoveOurs(list);
            if (before == null) return;
            foreach (AceKeyAssoc a in list)
                if (string.Equals(a.DatabasePath, before.DatabasePath, StringComparison.OrdinalIgnoreCase)) return;
            list.Add(before);
        }

        /// <summary>Removes associations with our key name (also the 0.x name); returns how many.</summary>
        internal static int RemoveOurs(List<AceKeyAssoc> list)
        {
            if (list == null) return 0;
            return list.RemoveAll(a => a != null && IsOurs(a.KeyProvider));
        }

        internal static int CleanUp()
        {
            return RemoveOurs(Program.Config.Defaults.KeySources);
        }

        private static bool IsOurs(string provider)
        {
            return provider == KeySnapshot.QuickUnlockKeyName || provider == KeySnapshot.LegacyQuickUnlockKeyName;
        }
    }
}
