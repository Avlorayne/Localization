using System;
using System.Collections.Generic;
using UnityEngine;

namespace Localization
{
    [CreateAssetMenu(menuName = "Settings/Language Config")]
    public class LanguageConfigSO : ScriptableObject
    {
        public string sourceFolderPath = "Assets/Editor/Text Files/Localization";
        public string soFolderPath = "Assets/Resources/Localization";

        public string defaultLanguage = "zh-Hans";

        public List<LanguageDefinition> languages = new()
        {
            new() { code = "zh-Hans", displayName = "简体中文", fallbackLanguage = "en" },
            new() { code = "zh-Hant", displayName = "繁體中文", fallbackLanguage = "zh-Hans" },
            new() { code = "en", displayName = "English", fallbackLanguage = "zh-Hans" },
            new() { code = "ja", displayName = "日本語", fallbackLanguage = "en" },
            new() { code = "ko", displayName = "한국어", fallbackLanguage = "en" },
        };

        private Dictionary<string, LanguageDefinition> _languageDict = new();

        public LanguageDefinition GetDefinition(string languageCode)
        {
            if (_languageDict.Count == 0)
            {
                foreach (var language in languages)
                    _languageDict[language.code] = language;
            }

            if (_languageDict.TryGetValue(languageCode, out var definition))
                return definition;

            Debug.LogWarning($"Language Code {languageCode} not found");
            _languageDict.TryGetValue(defaultLanguage, out definition);
            return definition;
        }
    }

    [Serializable]
    public class LanguageDefinition
    {
        public string code;
        public string displayName;
        public string fallbackLanguage;
    }
}