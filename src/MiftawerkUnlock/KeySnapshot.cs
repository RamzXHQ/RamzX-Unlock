// Miftawerk Unlock - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Collections.Generic;
using System.IO;

using KeePassLib;
using KeePassLib.Keys;

namespace MiftawerkUnlock
{
    /// <summary>
    /// The derived key data of a database's master key components (never the plaintext
    /// password, never a key file path) plus the root group UUID as a database binding.
    /// KeePass builds the raw composite key by hashing the concatenated KeyData of all
    /// user keys in order, so the same bytes re-added as unhashed KcpCustomKeys open the
    /// database without changing its master key.
    /// </summary>
    internal sealed class KeySnapshot
    {
        internal const string QuickUnlockKeyName = "Miftawerk Unlock (Windows Hello)";

        private const byte TagPassword = 1;
        private const byte TagKeyFile = 2;
        private const byte TagUserAccount = 3;

        private readonly byte[] m_rootUuid;
        private readonly List<KeyValuePair<byte, byte[]>> m_parts;

        private KeySnapshot(byte[] rootUuid, List<KeyValuePair<byte, byte[]>> parts)
        {
            m_rootUuid = rootUuid;
            m_parts = parts;
        }

        internal byte[] RootGroupUuid { get { return m_rootUuid; } }

        /// <summary>
        /// Returns null if the master key contains components this version does not
        /// support (key providers such as YubiKey, or an existing quick-unlock key).
        /// </summary>
        internal static KeySnapshot TryCapture(PwDatabase db)
        {
            List<KeyValuePair<byte, byte[]>> parts = new List<KeyValuePair<byte, byte[]>>();
            foreach (IUserKey k in db.MasterKey.UserKeys)
            {
                byte tag;
                if (k is KcpPassword) tag = TagPassword;
                else if (k is KcpKeyFile) tag = TagKeyFile;
                else if (k is KcpUserAccount) tag = TagUserAccount;
                else
                {
                    foreach (KeyValuePair<byte, byte[]> p in parts) Array.Clear(p.Value, 0, p.Value.Length);
                    return null;
                }
                parts.Add(new KeyValuePair<byte, byte[]>(tag, k.KeyData.ReadData()));
            }
            if (parts.Count == 0) return null;

            return new KeySnapshot(db.RootGroup.Uuid.UuidBytes, parts);
        }

        internal CompositeKey ToCompositeKey()
        {
            CompositeKey ck = new CompositeKey();
            foreach (KeyValuePair<byte, byte[]> p in m_parts)
                ck.AddUserKey(new KcpCustomKey(QuickUnlockKeyName, p.Value, false));
            return ck;
        }

        internal byte[] Serialize()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms))
            {
                w.Write(m_rootUuid);
                w.Write((byte)m_parts.Count);
                foreach (KeyValuePair<byte, byte[]> p in m_parts)
                {
                    w.Write(p.Key);
                    w.Write(p.Value.Length);
                    w.Write(p.Value);
                }
                w.Flush();
                return ms.ToArray(); // caller clears; MemoryStream buffer is collected
            }
        }

        internal static KeySnapshot Deserialize(byte[] data)
        {
            using (BinaryReader r = new BinaryReader(new MemoryStream(data, false)))
            {
                byte[] uuid = r.ReadBytes(16);
                int count = r.ReadByte();
                List<KeyValuePair<byte, byte[]>> parts = new List<KeyValuePair<byte, byte[]>>(count);
                for (int i = 0; i < count; ++i)
                {
                    byte tag = r.ReadByte();
                    int len = r.ReadInt32();
                    if (len < 0 || len > 1024) throw new InvalidDataException("Bad key part.");
                    parts.Add(new KeyValuePair<byte, byte[]>(tag, r.ReadBytes(len)));
                }
                return new KeySnapshot(uuid, parts);
            }
        }

        internal void Clear()
        {
            foreach (KeyValuePair<byte, byte[]> p in m_parts) Array.Clear(p.Value, 0, p.Value.Length);
        }
    }
}
