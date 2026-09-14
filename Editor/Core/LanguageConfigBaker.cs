#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using static Localization.Editor.LocalizationEditorText;

namespace Localization.Editor
{
    internal static class LanguageConfigBaker
    {
        public const string RuntimeConfigPath = "Assets/Resources/Localization/LanguageConfig.asset";

        [InitializeOnLoadMethod]
        private static void EnsureRuntimeConfigExistsAfterReload()
        {
            EditorApplication.delayCall -= EnsureRuntimeConfigExists;
            EditorApplication.delayCall += EnsureRuntimeConfigExists;
            EditorApplication.playModeStateChanged -= BakeRuntimeConfigBeforePlayMode;
            EditorApplication.playModeStateChanged += BakeRuntimeConfigBeforePlayMode;
        }

        [MenuItem("Tools/Localization/Bake Runtime Language Config", priority = 2)]
        public static LanguageConfigSO BakeRuntimeConfig()
        {
            LanguageProjectSettings settings = LanguageProjectSettings.GetOrCreate();
            return BakeRuntimeConfig(settings);
        }

        public static LanguageConfigSO BakeRuntimeConfig(LanguageProjectSettings settings)
        {
            string directory = Path.GetDirectoryName(RuntimeConfigPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var config = AssetDatabase.LoadAssetAtPath<LanguageConfigSO>(RuntimeConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<LanguageConfigSO>();
                AssetDatabase.CreateAsset(config, RuntimeConfigPath);
                Debug.Log(F("log.created.settings", RuntimeConfigPath));
            }

            settings.CopyTo(config);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(RuntimeConfigPath);
            return config;
        }

        public static LanguageConfigSO BakeAndValidateRuntimeConfig()
        {
            var config = BakeRuntimeConfig();
            if (config == null)
                throw new BuildFailedException(
                    $"[Localization] Failed to bake runtime language config at {RuntimeConfigPath}.");

            if (config.languages == null || config.languages.Count == 0)
                throw new BuildFailedException(
                    $"[Localization] Runtime language config at {RuntimeConfigPath} has no languages.");

            if (string.IsNullOrEmpty(config.defaultLanguage))
                throw new BuildFailedException(
                    $"[Localization] Runtime language config at {RuntimeConfigPath} has no default language.");

            if (config.GetDefinition(config.defaultLanguage) == null)
                throw new BuildFailedException(
                    $"[Localization] Runtime language config at {RuntimeConfigPath} does not include its default language '{config.defaultLanguage}'.");

            return config;
        }

        private static void EnsureRuntimeConfigExists()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall -= EnsureRuntimeConfigExists;
                EditorApplication.delayCall += EnsureRuntimeConfigExists;
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<LanguageConfigSO>(RuntimeConfigPath) != null)
                return;

            BakeRuntimeConfig();
        }

        private static void BakeRuntimeConfigBeforePlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            BakeAndValidateRuntimeConfig();
        }
    }

    internal sealed class LanguageConfigBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            LanguageConfigBaker.BakeAndValidateRuntimeConfig();
        }
    }
}
#endif