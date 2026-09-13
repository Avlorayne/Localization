using System;
using UnityEngine;

namespace Localization
{
    [Serializable]
    public class LocalizationSystem
    {
        private LocalizationLookup _lookup;
        private LanguageConfigSO _languageConfig;
        private LanguageDefinition _currentDefinition;

        public Action OnLanguageChanged;

        public LocalizationSystem(LanguageConfigSO languageConfig)
        {
            _languageConfig = languageConfig;
            _lookup = new LocalizationLookup(_languageConfig.defaultLanguage);
        }

        public string CurrentLanguageCode
        {
            get => _currentDefinition.code;
            set
            {
                var newDefinition = _languageConfig.GetDefinition(value);
                if (newDefinition == null)
                {
                    Debug.LogWarning($"[LocalizationSystem] Cannot set language {value} because it does not exist");
                    return;
                }

                if (newDefinition != _currentDefinition)
                {
                    _currentDefinition = newDefinition;
                    OnLanguageChanged?.Invoke();
                }
            }
        }

        public string GetLocalizedText(string template)
        {
            return LocalizationTemplateResolver.Resolve(template, _currentDefinition, _lookup);
        }
    }
}