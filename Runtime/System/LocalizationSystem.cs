using System;
using UnityEngine;

namespace Localization
{
    [Serializable]
    public class LocalizationSystem
    {
        private LocalizationLookup _lookup;
        private LanguageConfigSO _languageConfig;
        public LanguageDefinition CurrentDefinition { get; private set; }
        
        public Action OnLanguageChanged;
        
        public LocalizationSystem(LanguageConfigSO languageConfig)
        {
            _languageConfig = languageConfig;
            _lookup = new LocalizationLookup(_languageConfig.defaultLanguage);
        }
        
        public void SetLanguage(string languageCode)
        {
            var newDefinition = _languageConfig.GetDefinition(languageCode);
            if (newDefinition == null)
            {
                Debug.LogWarning($"[LocalizationSystem] Cannot set language {languageCode} because it does not exist");
                return;
            }

            if (newDefinition != CurrentDefinition)
            {
                CurrentDefinition = newDefinition;
                OnLanguageChanged?.Invoke();
            }
        }
        
        public string GetLocalizedText(string template)
        {
           return LocalizationTemplateResolver.Resolve(template, CurrentDefinition, _lookup);
        }
    }
}
