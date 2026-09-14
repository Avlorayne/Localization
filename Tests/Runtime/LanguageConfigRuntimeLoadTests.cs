using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Localization.Tests
{
    public class LanguageConfigRuntimeLoadTests
    {
        [Test]
        public void ResourcesLoad_returnsBakedLanguageConfig()
        {
            var config = Resources.Load<LanguageConfigSO>("Localization/LanguageConfig");

            Assert.That(config, Is.Not.Null,
                "Expected a baked runtime config at Assets/Resources/Localization/LanguageConfig.asset.");
            Assert.That(config.languages, Is.Not.Null.And.Not.Empty);
            Assert.That(config.defaultLanguage, Is.Not.Null.And.Not.Empty);
            Assert.That(config.languages.Any(language => language.code == config.defaultLanguage), Is.True,
                "The baked runtime config should include its default language in the language list.");
        }

        [Test]
        public void LocalizationSystem_constructorDefersRuntimeConfigLoad()
        {
            var system = new LocalizationSystem();
            var initializedField = typeof(LocalizationSystem).GetField(
                "_initialized",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(initializedField, Is.Not.Null);
            Assert.That(initializedField.GetValue(system), Is.False);
        }

        [Test]
        public void LocalizationSystem_firstUseLoadsBakedLanguageConfig()
        {
            var system = new LocalizationSystem();

            Assert.That(system.IsReady, Is.True,
                "LocalizationSystem should automatically load the baked runtime config from Resources.");
            Assert.That(system.CurrentLanguageCode, Is.Not.Empty);
        }
    }
}