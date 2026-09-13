using Localization;
using Localization.Editor;
using NUnit.Framework;

namespace Localization.Tests
{
    public class LocalizationSourceHeadersTests
    {
        [TestCase("Comment")]
        [TestCase("comments")]
        [TestCase("Note")]
        [TestCase("备注")]
        [TestCase("注释")]
        [TestCase("Translator Comment")]
        public void IsCommentHeader_acceptsSupportedAliases(string header)
        {
            Assert.That(LocalizationSourceHeaders.IsCommentHeader(header), Is.True);
        }

        [Test]
        public void IsCommentHeader_rejectsLanguageColumn()
        {
            Assert.That(LocalizationSourceHeaders.IsCommentHeader("en"), Is.False);
        }

        [Test]
        public void ApplyField_ignoresKeyHeader_afterParserAssignedNormalizedKey()
        {
            var data = new LocalizationData { key = "START_GAME" };

            LocalizationSourceSchema.ApplyField(ref data, "Key", " START_GAME(argument) ");

            Assert.That(data.key, Is.EqualTo("START_GAME"));
        }
    }
}
