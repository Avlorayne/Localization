#if UNITY_EDITOR
using Localization.Editor.Source;
using UnityEditor;
using UnityEngine;

namespace Localization.Editor
{
    internal static class LanguageConfigProjectSettingsProvider
    {
        private const string SettingsPath = "Project/Localization";

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "Localization",
                keywords = new[] { "Localization", "Language", "LanguageConfig" },
                guiHandler = _ =>
                {
                    LanguageProjectSettings settings = LanguageProjectSettings.GetOrCreate();
                    var serializedSettings = new SerializedObject(settings);

                    serializedSettings.Update();
                    EditorGUILayout.HelpBox(
                        $"Source settings are stored at {LanguageProjectSettings.ProjectSettingsPath}. Runtime reads the baked asset at {LanguageConfigBaker.RuntimeConfigPath}.",
                        MessageType.Info);
                    LanguageConfigSettingsDrawer.Draw(serializedSettings);

                    if (serializedSettings.ApplyModifiedProperties())
                    {
                        settings.SaveProjectSettings();
                        LocalizationSourceSchema.InvalidateLanguageConfigCache();
                        LanguageConfigBaker.BakeRuntimeConfig(settings);
                    }

                    EditorGUILayout.Space();
                    if (GUILayout.Button("Bake Runtime Config"))
                        LanguageConfigBaker.BakeRuntimeConfig(settings);
                }
            };
        }

        public static void Open()
        {
            LanguageProjectSettings.GetOrCreate().SaveProjectSettings();
            SettingsService.OpenProjectSettings(SettingsPath);
        }
    }
}
#endif