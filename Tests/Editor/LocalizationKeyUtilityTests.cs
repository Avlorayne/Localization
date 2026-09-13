using NUnit.Framework;

namespace Localization.Tests
{
    public class LocalizationKeyUtilityTests
    {
        [Test]
        public void TryNormalizeLocalizationKey_acceptsUpperSnakeCase()
        {
            bool result = LocalizationKeyUtility.TryNormalizeLocalizationKey(
                " START_GAME ",
                out string normalized);

            Assert.That(result, Is.True);
            Assert.That(normalized, Is.EqualTo("START_GAME"));
        }

        [Test]
        public void TryNormalizeLocalizationKey_stripsTrailingParenthesizedNote()
        {
            bool result = LocalizationKeyUtility.TryNormalizeLocalizationKey(
                "START_GAME (main menu)",
                out string normalized);

            Assert.That(result, Is.True);
            Assert.That(normalized, Is.EqualTo("START_GAME"));
        }

        [Test]
        public void IsValidLocalizationKey_rejectsLowercaseByDefault()
        {
            Assert.That(LocalizationKeyUtility.IsValidLocalizationKey("start_game"), Is.False);
        }

        [Test]
        public void IsValidLocalizationKey_canAcceptDisplayKeys()
        {
            Assert.That(LocalizationKeyUtility.IsValidLocalizationKey("StartGame", true), Is.True);
        }
    }
}