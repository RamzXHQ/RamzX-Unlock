// RamzX Unlock - tests
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Linq;
using System.Text.RegularExpressions;

using KeePass.App.Configuration;

using Xunit;

namespace RamzXUnlock.Tests
{
    public class SettingsTests
    {
        [Fact]
        public void Defaults()
        {
            Settings s = new Settings(new AceCustomConfig());
            Assert.Equal(7, s.ValidityDays);
            Assert.Equal(TimeSpan.FromDays(7), s.Validity);
            Assert.True(s.OfferEnrollment);
            Assert.True(s.LockWithWindows);
        }

        [Fact]
        public void Values_AreStoredInKeePassConfig()
        {
            AceCustomConfig cfg = new AceCustomConfig();
            Settings s = new Settings(cfg);
            s.ValidityDays = 3;
            s.OfferEnrollment = false;
            s.LockWithWindows = false;

            Settings reloaded = new Settings(cfg);
            Assert.Equal(3, reloaded.ValidityDays);
            Assert.False(reloaded.OfferEnrollment);
            Assert.False(reloaded.LockWithWindows);
        }

        [Theory]
        [InlineData(-5, 1)]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(30, 30)]
        [InlineData(31, 30)]
        [InlineData(long.MaxValue, 30)]
        public void ValidityDays_IsClamped(long stored, int expected)
        {
            AceCustomConfig cfg = new AceCustomConfig();
            cfg.SetLong("MiftawerkUnlock.ValidityDays", stored);
            Assert.Equal(expected, new Settings(cfg).ValidityDays);
        }

        [Fact]
        public void NullConfig_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new Settings(null));
        }

        [Fact]
        public void Language_DefaultsToAutomatic_AndOnlyKnownValues()
        {
            AceCustomConfig cfg = new AceCustomConfig();
            Settings s = new Settings(cfg);
            Assert.Equal(string.Empty, s.Language);
            s.Language = "fr";
            Assert.Equal("fr", new Settings(cfg).Language);
            cfg.SetString("MiftawerkUnlock.Language", "klingon");
            Assert.Equal(string.Empty, new Settings(cfg).Language);
        }

        [Fact]
        public void AnotherDeviceDecline_IsRemembered_Bounded_AndCleared()
        {
            AceCustomConfig cfg = new AceCustomConfig();
            Settings s = new Settings(cfg);
            Assert.False(s.IsAnotherDeviceDeclined("db1"));
            s.DeclineAnotherDevice("db1");
            s.DeclineAnotherDevice("db1");
            Assert.True(new Settings(cfg).IsAnotherDeviceDeclined("db1")); // survives a KeePass restart
            Assert.False(s.IsAnotherDeviceDeclined("db2"));
            for (int i = 0; i < 100; ++i) s.DeclineAnotherDevice("x" + i);
            Assert.False(s.IsAnotherDeviceDeclined("db1"));                  // oldest dropped (max 64)
            Assert.True(s.IsAnotherDeviceDeclined("x99"));
            s.ClearDeclined();
            Assert.False(s.IsAnotherDeviceDeclined("x99"));
        }
    }

    public class BrandTests
    {
        [Fact]
        public void Logo_IsDrawn_InBrandColours()
        {
            using (System.Drawing.Bitmap b = Brand.Logo(64))
            {
                Assert.Equal(64, b.Width);
                System.Drawing.Color centre = b.GetPixel(32, 32);
                System.Drawing.Color corner = b.GetPixel(0, 0);
                Assert.Equal(0, corner.A);                     // rounded corner is transparent
                Assert.True(centre.B > centre.R);              // the blue "X" or navy background
                Assert.Equal(Brand.Navy.ToArgb(), b.GetPixel(32, 6).ToArgb());
            }
            using (System.Drawing.Bitmap s = Brand.HelloSymbol(48)) Assert.Equal(48, s.Height);
        }
    }

    public class StringsTests : IDisposable
    {
        public void Dispose() { Strings.SetLanguage("en"); }

        [Fact]
        public void AllLanguages_HaveAllKeys_WithSamePlaceholders()
        {
            var en = Strings.Table("en");
            foreach (string lang in Strings.Languages)
            {
                var t = Strings.Table(lang);
                Assert.Equal(en.Keys.OrderBy(k => k), t.Keys.OrderBy(k => k));
                foreach (string key in en.Keys)
                {
                    Assert.False(string.IsNullOrWhiteSpace(t[key]), lang + "/" + key + " is empty");
                    Assert.Equal(Placeholders(en[key]), Placeholders(t[key]));
                }
            }
        }

        private static string[] Placeholders(string s)
        {
            return Regex.Matches(s, @"\{\d+\}").Cast<Match>().Select(m => m.Value).Distinct()
                .OrderBy(x => x).ToArray();
        }

        [Fact]
        public void SecurityNotice_IsPresentInEveryLanguage()
        {
            foreach (string lang in Strings.Languages)
            {
                Assert.Contains("PIN", Strings.Table(lang)[Strings.EnrollQuestion]);
                Assert.Contains("PIN", Strings.Table(lang)[Strings.EnrollAnotherDevice]);
            }
        }

        [Fact]
        public void TenLanguages_WithNativeNames()
        {
            Assert.Equal(10, Strings.Languages.Length);
            Assert.Equal(Strings.Languages.Length, Strings.LanguageNames.Length);
            Assert.Equal(Strings.Languages.Length, new System.Collections.Generic.HashSet<string>(Strings.Languages).Count);
        }

        [Fact]
        public void Select_PreferredLanguageWins()
        {
            Strings.Select("de", "tr");
            Assert.Equal("tr", Strings.Language);
            Strings.Select("de", "");
            Assert.Equal("de", Strings.Language);
            Strings.Select("de", "en");
            Assert.Equal("en", Strings.Language);
            Strings.Select("de", "xx");
            Assert.Equal("en", Strings.Language);
        }

        [Theory]
        [InlineData("de", "de", false)]
        [InlineData("ar", "ar", true)]
        [InlineData("fr", "fr", false)]
        [InlineData("TR", "tr", false)]
        [InlineData("ja", "en", false)]
        [InlineData("", "en", false)]
        [InlineData(null, "en", false)]
        public void SetLanguage_SelectsTableAndDirection(string requested, string expected, bool rtl)
        {
            Strings.SetLanguage(requested);
            Assert.Equal(expected, Strings.Language);
            Assert.Equal(rtl, Strings.RightToLeft);
            Assert.Equal(Strings.Table(expected)[Strings.Ok], Strings.Get(Strings.Ok));
        }

        [Fact]
        public void Select_UsesKeePassLanguageWhenTranslated()
        {
            Strings.Select("DE");
            Assert.Equal("de", Strings.Language);
            Strings.Select("ar");
            Assert.Equal("ar", Strings.Language);
        }

        [Fact]
        public void Format_InsertsArguments_AndUnknownKeyReturnsKey()
        {
            Strings.SetLanguage("en");
            Assert.Equal("3 stored unlock record(s) deleted.", Strings.Format(Strings.ForgetDone, 3));
            Assert.Equal("NoSuchKey", Strings.Get("NoSuchKey"));
        }
    }
}
