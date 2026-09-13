#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Localization.Editor
{
    /// <summary>
    /// 将通过本地化数据校验的 LanguageDataSO 注册到专用的 Addressables 分组。
    /// </summary>
    internal static class LanguageDataSOAddressableRegistrar
    {
        internal const string LocalizationGroupName = "Localization";

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

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning(
                    $"[Localization] Addressables settings are unavailable; skipped registration for '{assetPath}'.",
                    data);
                return false;
            }

            var group = settings.FindGroup(LocalizationGroupName);
            if (group == null)
            {
                group = settings.CreateGroup(
                    LocalizationGroupName,
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
                    $"[Localization] Could not create Addressables group '{LocalizationGroupName}'.",
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
                $"[Localization] Registered '{assetPath}' as Addressable '{address}' in group '{LocalizationGroupName}'.",
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
