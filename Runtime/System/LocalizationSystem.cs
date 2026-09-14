using System;
using UnityEngine;

namespace Localization
{
    [Serializable]
    public class LocalizationSystem
    {
        public const string RuntimeConfigResourcePath = "Localization/LanguageConfig";

        private LocalizationLookup _lookup;
        private LanguageConfigSO _languageConfig;
        private LanguageDefinition _currentDefinition;
        private bool _initialized;

        public Action OnLanguageChanged;

        public bool IsReady
        {
            get
            {
                EnsureInitialized();
                return _languageConfig != null && _lookup != null && _currentDefinition != null;
            }
        }

        public string CurrentLanguageCode
        {
            get
            {
                EnsureInitialized();
                return _currentDefinition?.code ?? string.Empty;
            }
            set
            {
                EnsureInitialized();
                if (_languageConfig == null)
                {
                    Debug.LogError(
                        $"[LocalizationSystem] Cannot set language because runtime language config '{RuntimeConfigResourcePath}' was not loaded.");
                    return;
                }

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
            EnsureInitialized();
            if (!IsReady)
            {
                Debug.LogError(
                    $"[LocalizationSystem] Cannot resolve text because runtime language config '{RuntimeConfigResourcePath}' is missing or invalid.");
                return template;
            }

            return LocalizationTemplateResolver.Resolve(template, _currentDefinition, _lookup);
        }

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            _initialized = true;
            _languageConfig = LoadLanguageConfig();
            string defaultLanguage = _languageConfig == null ? string.Empty : _languageConfig.defaultLanguage;
            _lookup = new LocalizationLookup(defaultLanguage);
            _currentDefinition = _languageConfig?.GetDefinition(defaultLanguage);

            if (_languageConfig != null && _currentDefinition == null)
            {
                Debug.LogError(
                    $"[LocalizationSystem] Runtime language config '{RuntimeConfigResourcePath}' does not include a valid default language.");
            }
        }

        private static LanguageConfigSO LoadLanguageConfig()
        {
            var config = Resources.Load<LanguageConfigSO>(RuntimeConfigResourcePath);
            if (config == null)
            {
                Debug.LogError(
                    $"[LocalizationSystem] Missing runtime language config. Expected a baked asset at Resources path '{RuntimeConfigResourcePath}'.");
            }

            return config;
        }
    }
}