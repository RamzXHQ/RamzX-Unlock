// Miftawerk Unlock - KeePass 2 plugin
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;

namespace MiftawerkUnlock
{
    /// <summary>
    /// Detects a Windows restart, so records expire on reboot.
    /// Boot time = now - GetTickCount64 (documented; the tick count includes sleep).
    /// Open question (experiment 5): whether a Fast Startup shutdown resets the tick count.
    /// </summary>
    internal static class BootStamp
    {
        // Tolerance for clock adjustments (NTP) between estimate and check.
        private static readonly long Tolerance = TimeSpan.FromMinutes(2).Ticks;

        internal static long CurrentBootTimeUtcTicks()
        {
            long uptime = (long)NativeMethods.GetTickCount64() * TimeSpan.TicksPerMillisecond;
            return DateTime.UtcNow.Ticks - uptime;
        }

        internal static bool IsSameBoot(long storedBootTimeUtcTicks)
        {
            return Math.Abs(CurrentBootTimeUtcTicks() - storedBootTimeUtcTicks) <= Tolerance;
        }
    }
}
