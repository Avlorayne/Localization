using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Localization.Tests
{
    public class LocalizationDataTests
    {
        [Test]
        public void TryGet_returnsExactLanguageText()
        {
            var data = new LocalizationData
            {
                key = "START_GAME",
                texts = new[]
                {
                    new LocalizationText { languageCode = "zh-Hans", text = "开始游戏" },
                    new LocalizationText { languageCode = "en", text = "Start Game" }
                }
            };

            bool result = data.TryGet("en", "zh-Hans", out string text);

            Assert.That(result, Is.True);
            Assert.That(text, Is.EqualTo("Start Game"));
        }

        [Test]
        public void TryGet_returnsFalseWhenTextsAreEmpty()
        {
            var data = new LocalizationData { key = "EMPTY" };

            bool result = data.TryGet("en", "zh-Hans", out string text);

            Assert.That(result, Is.False);
            Assert.That(text, Is.Empty);
        }
    }

    public class LanguageConfigSOTests
    {
        [Test]
        public void GetDefinition_firstLookup_returnsRequestedLanguage()
        {
            var config = ScriptableObject.CreateInstance<LanguageConfigSO>();
            config.defaultLanguage = "zh-Hans";
            config.languages = new List<LanguageDefinition>
            {
                new() { code = "zh-Hans", displayName = "Simplified Chinese" },
                new() { code = "en", displayName = "English" }
            };

            try
            {
                LanguageDefinition definition = config.GetDefinition("en");

                Assert.That(definition, Is.Not.Null);
                Assert.That(definition.code, Is.EqualTo("en"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
