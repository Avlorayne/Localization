using System;
using Localization;
using UnityEngine;

public sealed class BasicLocalizationExample : MonoBehaviour
{
    public static BasicLocalizationExample Instance { get; private set; }

    [SerializeField] private LanguageConfigSO languageConfig;
    [SerializeField] private string languageCode = "en";

    private LocalizationSystem _localization;

    private void Awake()
    {
        Instance ??= this;
        
        if (languageConfig == null)
        {
            Debug.LogError("[BasicLocalizationExample] Assign a LanguageConfigSO first.", this);
            enabled = false;
            return;
        }

        _localization = new LocalizationSystem(languageConfig);
        
        _localization.SetLanguage(languageCode);
    }

    public void AddListener(Action callback)
    {
        _localization.OnLanguageChanged -= callback;
        _localization.OnLanguageChanged += callback;
    }

    public void RemoveListener(Action callback)
    {
        _localization.OnLanguageChanged -= callback;
    }

    public void SetLanguage(string code)
    {
        languageCode = code;
        _localization?.SetLanguage(code);
    }

    public string GetLocalizedText(string key)
    {
        return _localization?.GetLocalizedText(key);
    }
}

