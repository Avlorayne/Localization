using System;
using Localization;
using UnityEngine;

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
                _instance._system ??= new LocalizationSystem();
            }

            return _instance;
        }
    }

    [SerializeField] private string languageCode = "en";

    [NonSerialized] private LocalizationSystem _system;
    public bool IsReady => EnsureSystem().IsReady;

    private void Awake()
    {
        _instance = this;
        EnsureSystem();

        if (!_system.IsReady)
        {
            enabled = false;
            return;
        }

        _system.CurrentLanguageCode = languageCode;
    }

    public void SetLanguage(string languageCode)
    {
        if (EnsureSystem().IsReady)
            _system.CurrentLanguageCode = languageCode;
    }

    public string GetLanguageCode()
    {
        return EnsureSystem().CurrentLanguageCode;
    }

    public string GetLocalizedText(string text)
    {
        return EnsureSystem().IsReady ? _system.GetLocalizedText(text) : text;
    }

    public void AddListener(Action listener)
    {
        EnsureSystem().OnLanguageChanged -= listener;
        _system.OnLanguageChanged += listener;
    }

    public void RemoveListener(Action listener)
    {
        EnsureSystem().OnLanguageChanged -= listener;
    }

    private LocalizationSystem EnsureSystem()
    {
        return _system ??= new LocalizationSystem();
    }
}
