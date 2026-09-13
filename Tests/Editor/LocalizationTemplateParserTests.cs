using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Localization.Tests
{
    public class LocalizationTemplateParserTests
    {
        [Test]
        public void Parse_readsNamespacedPlaceholder()
        {
            var placeholders = LocalizationTemplateParser.Parse("<UI|START_GAME>");

            Assert.That(placeholders, Has.Count.EqualTo(1));
            Assert.That(placeholders[0].NamespaceId, Is.EqualTo("UI"));
            Assert.That(placeholders[0].Key, Is.EqualTo("START_GAME"));
            Assert.That(placeholders[0].ArgumentText, Is.Empty);
        }

        [Test]
        public void Parse_preservesNestedPlaceholderAndQuotedTextArguments()
        {
            var placeholders = LocalizationTemplateParser.Parse("<UI|WELCOME(<UI|PLAYER_NAME>, \"world\")>");

            Assert.That(placeholders, Has.Count.EqualTo(1));
            Assert.That(placeholders[0].Key, Is.EqualTo("WELCOME"));
            Assert.That(placeholders[0].ArgumentText, Is.EqualTo(new[] { "<UI|PLAYER_NAME>", "world" }));
        }

        [Test]
        public void Parse_removesQuotesFromLiteralArguments()
        {
            var placeholders = LocalizationTemplateParser.Parse(
                "<NAMESPACE1|KEY1(<NAMESPACE2|KEY2>, \"Custom Text\")>");

            Assert.That(placeholders, Has.Count.EqualTo(1));
            Assert.That(placeholders[0].ArgumentText,
                Is.EqualTo(new[] { "<NAMESPACE2|KEY2>", "Custom Text" }));
        }

        [Test]
        public void Parse_acceptsInterpolatedStringVariableInsideQuotes()
        {
            string name = "William";
            string template = $"<UI|WELCOME(<UI|PLAYER_NAME>, \"{name}\")>";

            var placeholders = LocalizationTemplateParser.Parse(template);

            Assert.That(placeholders, Has.Count.EqualTo(1));
            Assert.That(placeholders[0].ArgumentText,
                Is.EqualTo(new[] { "<UI|PLAYER_NAME>", "William" }));
        }

        [Test]
        public void Parse_rejectsBareTextArguments()
        {
            var placeholders = LocalizationTemplateParser.Parse("<NAMESPACE1|KEY1(<NAMESPACE2|KEY2>, Custom Text)>");

            Assert.That(placeholders, Is.Empty);
        }

        [Test]
        public void Parse_preservesUnquotedNumericArguments()
        {
            var placeholders = LocalizationTemplateParser.Parse("<UI|ITEM_COUNT(3)>");

            Assert.That(placeholders, Has.Count.EqualTo(1));
            Assert.That(placeholders[0].ArgumentText, Is.EqualTo(new[] { "3" }));
        }

        [Test]
        public void Parse_ignoresTextMeshProRichTextTags()
        {
            var placeholders = LocalizationTemplateParser.Parse("<color=red>Hello</color>");

            Assert.That(placeholders, Is.Empty);
        }

        [Test]
        public void ContainsPlaceholder_findsNestedKeys()
        {
            bool contains = LocalizationTemplateParser.ContainsPlaceholder(
                "<UI|WELCOME(<UI|PLAYER_NAME>)>",
                "PLAYER_NAME");

            Assert.That(contains, Is.True);
        }

        [Test]
        public void Resolve_replacesNestedPlaceholderIncludingAngleBrackets()
        {
            var source = ScriptableObject.CreateInstance<LanguageDataSO>();
            source.name = "UI";
            source.entries = new List<LocalizationData>
            {
                new()
                {
                    key = "WELCOME",
                    texts = new[] { new LocalizationText { languageCode = "en", text = "Welcome {0}" } }
                },
                new()
                {
                    key = "PLAYER_NAME",
                    texts = new[] { new LocalizationText { languageCode = "en", text = "Captain" } }
                }
            };

            try
            {
                Type lookupType = typeof(LanguageDataSO).Assembly.GetType("Localization.LocalizationLookup");
                object lookup = Activator.CreateInstance(
                    lookupType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new object[] { "en" },
                    null);
                lookupType.GetMethod("AddData", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(lookup, new object[] { source });

                Type resolverType =
                    typeof(LanguageDataSO).Assembly.GetType("Localization.LocalizationTemplateResolver");
                string result = (string)resolverType
                    .GetMethod("Resolve", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Invoke(null, new object[]
                    {
                        "<UI|WELCOME(<UI|PLAYER_NAME>)>",
                        new LanguageDefinition { code = "en" },
                        lookup
                    });

                Assert.That(result, Is.EqualTo("Welcome Captain"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }
    }
}