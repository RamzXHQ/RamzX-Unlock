// Miftawerk Unlock - tests
// Copyright (c) 2026 SeeColors iT. MIT License.

using System;
using System.Linq;
using System.Text.RegularExpressions;

using KeePass.App.Configuration;

using Xunit;

namespace MiftawerkUnlock.Tests
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
            Assert.Contains("PIN", Strings.Table("en")[Strings.EnrollQuestion]);
            Assert.Contains("PIN", Strings.Table("de")[Strings.EnrollQuestion]);
            Assert.Contains("PIN", Strings.Table("ar")[Strings.EnrollQuestion]);
        }

        [Theory]
        [InlineData("de", "de", false)]
        [InlineData("ar", "ar", true)]
        [InlineData("fr", "en", false)]
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
