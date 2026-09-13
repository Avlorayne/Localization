using System;
using Localization;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class BasicLocalizationExample : MonoBehaviour
{
    private static BasicLocalizationExample _instance;
    public static BasicLocalizationExample Instance
    {
        get
        {
            _instance ??= FindObjectOfType<BasicLocalizationExample>();
            if (_instance == null)
            {
                GameObject go = new GameObject("Localization Component");
                _instance = go.AddComponent<BasicLocalizationExample>();
                _instance.config ??= Resources.Load<LanguageConfigSO>("Localization/LanguageConfig");
                _instance._system ??= new LocalizationSystem(_instance.config);
            }

            return _instance;
        }
    }

    [FormerlySerializedAs("languageConfig")]
    public LanguageConfigSO config;
    [SerializeField] private string languageCode = "en";

    private LocalizationSystem _system;

    private void Awake()
    {
        _instance = this;
        config ??= Resources.Load<LanguageConfigSO>("Localization/LanguageConfig");

        if (config == null)
        {
            Debug.LogError("[BasicLocalizationExample] Assign a LanguageConfigSO or place one at Resources/Localization/LanguageConfig.", this);
            enabled = false;
            return;
        }

        _system = new LocalizationSystem(config);
        _system.SetLanguage(languageCode);
    }

    public void SetLanguage(string languageCode)
    {
        _system.SetLanguage(languageCode);
    }

    public string GetLanguageCode()
    {
        return _system.CurrentDefinition.code;
    }

    public string GetLocalizedText(string text)
    {
        return _system.GetLocalizedText(text);
    }

    public void AddListener(Action listener)
    {
        _system.OnLanguageChanged -= listener;
        _system.OnLanguageChanged += listener;
    }

    public void RemoveListener(Action listener)
    {
        _system.OnLanguageChanged -= listener;
    }
}
