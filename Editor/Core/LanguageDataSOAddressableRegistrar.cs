#if UNITY_EDITOR
using Localization.Editor.Source;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Localization.Editor
{
    /// <summary>
    /// 将通过本地化数据校验的 LanguageDataSO 注册到专用的 Addressables 分组。
    /// </summary>
    internal static class LanguageDataSOAddressableRegistrar
    {
        [InitializeOnLoadMethod]
        private static void ScheduleRegistrationForExistingAssets()
        {
            EditorApplication.delayCall -= RegisterAllValidAssets;
            EditorApplication.delayCall += RegisterAllValidAssets;
        }

        private static void RegisterAllValidAssets()
        {
            EditorApplication.delayCall -= RegisterAllValidAssets;

            foreach (string guid in AssetDatabase.FindAssets("t:LanguageDataSO"))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<LanguageDataSO>(assetPath);
                if (data != null)
                    TryRegisterIfValid(data);
            }
        }

        /// <summary>
        /// 当且仅当数据通过 Inspector 的完整合法性规则时，确保其位于 Localization Addressables 分组中。
        /// </summary>
        public static bool TryRegisterIfValid(LanguageDataSO data)
        {
            if (!IsValidForRegistration(data))
                return false;

            string assetPath = AssetDatabase.GetAssetPath(data);
            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(assetGuid))
                return false;

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: true);
            if (settings == null)
            {
                Debug.LogWarning(
                    $"[Localization] Addressables settings are unavailable; skipped registration for '{assetPath}'.",
                    data);
                return false;
            }

            string groupName = LanguageProjectSettings.GetOrCreate().GetLanguageDataAddressablesGroupName();
            if (string.IsNullOrEmpty(groupName))
            {
                Debug.LogWarning(
                    $"[Localization] No Addressables group is configured in Project Settings > Localization; " +
                    $"skipped registration for '{assetPath}'.",
                    data);
                return false;
            }

            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(
                    groupName,
                    setAsDefaultGroup: false,
                    readOnly: false,
                    postEvent: true,
                    schemasToCopy: null,
                    typeof(BundledAssetGroupSchema),
                    typeof(ContentUpdateGroupSchema));
            }

            if (group == null)
            {
                Debug.LogError(
                    $"[Localization] Could not create Addressables group '{groupName}'.",
                    data);
                return false;
            }

            string address = data.NamespaceId;
            var existingEntry = settings.FindAssetEntry(assetGuid);
            if (existingEntry != null && existingEntry.parentGroup == group && existingEntry.address == address)
                return true;

            var entry = settings.CreateOrMoveEntry(assetGuid, group, readOnly: false, postEvent: true);
            if (entry == null)
            {
                Debug.LogError($"[Localization] Could not register '{assetPath}' as an Addressable.", data);
                return false;
            }

            entry.address = address;
            EditorUtility.SetDirty(group);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[Localization] Registered '{assetPath}' as Addressable '{address}' in group '{groupName}'.",
                data);
            return true;
        }

        private static bool IsValidForRegistration(LanguageDataSO data)
        {
            if (data == null || data.entries == null)
                return false;

            foreach (LocalizationData entry in data.entries)
            {
                if (!IsValidKeyLiteral(entry.key))
                    return false;
            }

            if (LanguageDataSODuplicateKeyValidator.CollectReportsInvolving(data).Count > 0)
                return false;

            return LocalizationSourceSchema.CollectEmbeddedLocalizationKeyViolations(data.entries).Count == 0;
        }

        private static bool IsValidKeyLiteral(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            for (int i = 0; i < key.Length; i++)
            {
                char character = key[i];
                bool isUpperLetter = character is >= 'A' and <= 'Z';
                bool isDigit = character is >= '0' and <= '9';
                if (!isUpperLetter && !isDigit && character != '_')
                    return false;
            }

            return true;
        }
    }
}
#endif