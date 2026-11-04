// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;

using KeePass.Util;

namespace RamzXUnlock
{
    /// <summary>
    /// Update address of all RamzX plugins for KeePass's own update check (Help -> Check for Updates, RamzX Update):
    /// one version file for every RamzX plugin. The file is signed (RSA-4096, SHA-512): KeePass ignores it unless
    /// the signature matches this public key, so a hacked web server cannot announce a fake version. The private key
    /// never leaves the release machine (tools/Sign-VersionFile.ps1 in RamzX Tools). Same key as RamzX Connect.
    /// </summary>
    internal static class UpdateInfo
    {
        internal const string Url = "https://ramzx.de/plugins/version.txt";
        internal const string PublicKey = "<RSAKeyValue><Modulus>krB9Pbuy02akJX4IMRFs4K7cVx0NK4InCiW6nE6Ptbb8VOJ0Yef810jt8gPm2W4tYMC4J9pUyCBje5z9gw9RqKbuau9FaNhXo8NxfJQdhoiPElJwb7dBRJAP6cIEbUoY0Vvo7SKSLkpSPmIdP+xZvcadzxzYIKapHGO2ai4wm2/0tRa088QRY2W41LBHVkU97mgRU0txoFF9KXjB9IZzObVTN9HSp8jsN4YDc7brm+Zz1X9+8/nwwedmcYAPgNfOt5AlC7Tll7BPpwsEzqchwb1NxvlH7gpCNuN+wzwAe/79i9fl4nXUg2MV2ZFovj/W03pjdbwi2Jw0+Vtbs1nAlxZoAzIGO12lVh8BOrfGwKMEGrZGRR0uJoVjdK6dD0cvOqeoHFi0IINfMzw+ezTLcCZWWCz/LaVrhZL6nLsBw0ZEDO2ujk9zSJ58T7QLpfVYiUxjHvJfWGPguyYSvOobJPv8NtUY33k576+xG6GFtN08RLgUXMRhKJ030a+9q2lT7fUOy879CCJ8XmoQ00OWuqI9xHHWxjoR+1dwQLRfz4QOYFcGa8UC0UoIkqVHfJUKolaH1UxpLp5vvH7idoqkYxaR5fZzI/9/nNsd+XZYq+74KN9qLHAOBuDQzFZ1XZFRHdpLbiDCHYuogr+7cTVKr2Hdekx9FdeQCBleb5FoBLM=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        /// <summary>Call in Initialize, before KeePass checks for updates.</summary>
        internal static void Register()
        {
            try { UpdateCheckEx.SetFileSigKey(Url, PublicKey); } catch (Exception) { }
        }
    }
}
