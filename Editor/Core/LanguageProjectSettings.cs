#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Localization.Editor
{
    [FilePath(ProjectSettingsPath, FilePathAttribute.Location.ProjectFolder)]
    internal sealed class LanguageProjectSettings : ScriptableSingleton<LanguageProjectSettings>
    {
        public const string ProjectSettingsPath = "ProjectSettings/DotlineLocalizationSettings.asset";

        public string sourceFolderPath = "Assets/Editor/Text Files/Localization";
        public string soFolderPath = "Assets/Resources/Localization";
        public string languageDataAddressablesGroupName = string.Empty;
        public string defaultLanguage = "zh-Hans";

        public List<LanguageDefinition> languages = new()
        {
            new() { code = "zh-Hans", displayName = "简体中文", fallbackLanguage = "en" },
            new() { code = "zh-Hant", displayName = "繁體中文", fallbackLanguage = "zh-Hans" },
            new() { code = "en", displayName = "English", fallbackLanguage = "zh-Hans" },
            new() { code = "ja", displayName = "日本語", fallbackLanguage = "en" },
            new() { code = "ko", displayName = "한국어", fallbackLanguage = "en" },
        };

        public static LanguageProjectSettings GetOrCreate()
        {
            var settings = instance;
            settings.EnsureProjectSettingsFile();
            return settings;
        }

        public void SaveProjectSettings()
        {
            Save(true);
        }

        public void CopyTo(LanguageConfigSO config)
        {
            config.sourceFolderPath = sourceFolderPath;
            config.soFolderPath = soFolderPath;
            config.defaultLanguage = defaultLanguage;
            config.languages = CloneLanguages(languages);
        }

        /// <summary>分组名完全由用户在 Project Settings 中配置，未配置时返回空串（不注册）。</summary>
        public string GetLanguageDataAddressablesGroupName()
        {
            return languageDataAddressablesGroupName?.Trim() ?? string.Empty;
        }

        private void EnsureProjectSettingsFile()
        {
            string directory = Path.GetDirectoryName(ProjectSettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (!File.Exists(ProjectSettingsPath))
                SaveProjectSettings();
        }

        private static List<LanguageDefinition> CloneLanguages(IEnumerable<LanguageDefinition> source)
        {
            var result = new List<LanguageDefinition>();
            if (source == null)
                return result;

            foreach (LanguageDefinition language in source)
            {
                if (language == null)
                    continue;

                result.Add(new LanguageDefinition
                {
                    code = language.code,
                    displayName = language.displayName,
                    fallbackLanguage = language.fallbackLanguage
                });
            }

            return result;
        }
    }
}
#endif