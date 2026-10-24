// RamzX Unlock - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Threading.Tasks;
using System.Windows.Forms;

using Windows.Security.Credentials.UI;

namespace MiftawerkUnlock
{
    /// <summary>
    /// Small public interface for other plugins (for example RamzX OTP), found by reflection,
    /// so neither plugin depends on the other. Callers check <see cref="ApiVersion"/> first.
    /// Biometric data never leaves Windows; callers only get "verified" or "not verified".
    /// </summary>
    public static class MiftawerkUnlockApi
    {
        /// <summary>Increased only on incompatible changes.</summary>
        public const int ApiVersion = 1;

        /// <summary>True if Windows Hello can be used now (set up, not a Remote Desktop session).</summary>
        public static async Task<bool> IsWindowsHelloAvailableAsync()
        {
            if (SystemInformation.TerminalServerSession) return false;
            try
            {
                UserConsentVerifierAvailability a = await UserConsentVerifier.CheckAvailabilityAsync();
                return a == UserConsentVerifierAvailability.Available;
            }
            catch (Exception) { return false; }
        }

        /// <summary>
        /// Asks the user for Windows Hello (face, fingerprint or PIN) with the given message.
        /// Returns true only if Windows confirmed the user; false on cancel or any error.
        /// </summary>
        public static async Task<bool> VerifyUserAsync(IWin32Window owner, string message)
        {
            if (!await IsWindowsHelloAvailableAsync()) return false;
            try
            {
                UserConsentVerificationResult r = await HelloForeground.RunAsync(owner as Form,
                    () => UserConsentVerifier.RequestVerificationAsync(message ?? "RamzX Unlock").AsTask());
                return r == UserConsentVerificationResult.Verified;
            }
            catch (Exception) { return false; }
        }
    }
}
