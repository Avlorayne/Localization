#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Localization.Editor.LocalizationEditorText;

namespace Localization.Editor
{
    public static class LanguageDataSODuplicateKeyValidator
    {
        [MenuItem("Tools/Localization/Validate Duplicate Keys", priority = 22)]
        public static void ValidateDuplicateKeys()
        {
            LogDuplicateKeys();
        }

        public static bool LogDuplicateKeys(LanguageDataSO context = null)
        {
            var reports = CollectReports(context);
            if (reports.Count == 0)
                return false;

            string errorText = BuildErrorText(reports);
            if (context != null)
                Debug.LogError(errorText, context);
            else
                Debug.LogError(errorText);

            return true;
        }

        public static HashSet<string> CollectDuplicateKeys(LanguageDataSO currentData = null)
        {
            return CollectReports(currentData)
                .Select(report => report.Key)
                .ToHashSet(System.StringComparer.OrdinalIgnoreCase);
        }

        public static List<DuplicateKeyReport> CollectReportsInvolving(LanguageDataSO currentData)
        {
            if (currentData == null)
                return new List<DuplicateKeyReport>();

            string currentAssetPath = GetLanguageDataAssetPath(currentData);
            string currentNamespace = GetLanguageDataNamespace(currentData);
            return CollectReports(currentData)
                .Where(report =>
                    string.Equals(report.NamespaceId, currentNamespace, System.StringComparison.OrdinalIgnoreCase) &&
                    report.AssetPaths.Contains(currentAssetPath, System.StringComparer.OrdinalIgnoreCase))
                .ToList();
        }


        /// <summary>检查整个工程所有 LanguageDataSO 中同一命名空间内的重复键，返回按 Namespace + Key 排序的报告。</summary>
        public static List<DuplicateKeyReport> CollectReports(LanguageDataSO currentData = null)
        {
            var keyOccurrences =
                new Dictionary<string, List<DuplicateKeyOccurrence>>(System.StringComparer.OrdinalIgnoreCase);
            var allData = LoadAllLanguageDataAssets(currentData);

            foreach (LanguageDataSO data in allData)
            {
                if (data == null || data.entries == null)
                    continue;

                string assetPath = GetLanguageDataAssetPath(data);
                foreach (LocalizationData entry in data.entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.key))
                        continue;

                    string key = entry.key.Trim();
                    string namespaceId = GetLanguageDataNamespace(data);
                    string groupKey = $"{namespaceId}\n{key}";
                    if (!keyOccurrences.TryGetValue(groupKey, out var occurrences))
                    {
                        occurrences = new List<DuplicateKeyOccurrence>();
                        keyOccurrences[groupKey] = occurrences;
                    }

                    occurrences.Add(new DuplicateKeyOccurrence(assetPath, namespaceId));
                }
            }

            return keyOccurrences
                .Where(pair => pair.Value.Count > 1)
                .Select(pair =>
                {
                    string[] parts = pair.Key.Split('\n');
                    string namespaceId = parts.Length > 0 ? parts[0] : "";
                    string key = parts.Length > 1 ? parts[1] : pair.Key;
                    return new DuplicateKeyReport(namespaceId, key, pair.Value);
                })
                .OrderBy(report => report.NamespaceId)
                .ThenBy(report => report.Key)
                .ToList();
        }

        public static string BuildErrorText(LanguageDataSO currentData)
        {
            return BuildErrorText(CollectReports(currentData));
        }

        public static string BuildErrorText(List<DuplicateKeyReport> duplicateReports)
        {
            string duplicateDetails = string.Join("\n \n", duplicateReports.Select(FormatDuplicateKeyReport));
            return F("duplicate.key.error", duplicateReports.Count, $"\n{duplicateDetails}");
        }

        private static string FormatDuplicateKeyReport(DuplicateKeyReport report)
        {
            return
                $"{report.NamespaceId} | {report.Key} - {report.OccurrenceCount} entries in {report.AssetPaths.Count} SO file(s)\n  Collision files:\n  - {string.Join("\n  - ", report.AssetPaths)}";
        }

        private static string GetLanguageDataNamespace(LanguageDataSO data)
        {
            return data == null || string.IsNullOrWhiteSpace(data.NamespaceId)
                ? "<Empty Namespace>"
                : data.NamespaceId.Trim();
        }

        private static string GetLanguageDataAssetPath(LanguageDataSO data)
        {
            string path = AssetDatabase.GetAssetPath(data);
            if (!string.IsNullOrEmpty(path))
                return path;

            return string.IsNullOrEmpty(data.name)
                ? "Unsaved LanguageDataSO"
                : $"{data.name} (unsaved LanguageDataSO)";
        }

        private static List<LanguageDataSO> cachedLanguageDataAssets;
        private static double cachedLanguageDataLoadTime;
        private const double LanguageDataCacheSeconds = 10.0;

        private static List<LanguageDataSO> LoadAllLanguageDataAssets(LanguageDataSO currentData)
        {
            // 资产列表带 TTL 缓存：Key 输入防抖会高频触发本项目扫描，逐次 FindAssets 会卡编辑器
            double now = EditorApplication.timeSinceStartup;
            if (cachedLanguageDataAssets == null || (now - cachedLanguageDataLoadTime) >= LanguageDataCacheSeconds)
            {
                cachedLanguageDataAssets = new List<LanguageDataSO>();
                foreach (string guid in AssetDatabase.FindAssets("t:LanguageDataSO"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var data = AssetDatabase.LoadAssetAtPath<LanguageDataSO>(path);
                    if (data == null)
                        continue;

                    cachedLanguageDataAssets.Add(data);
                }

                cachedLanguageDataLoadTime = now;
            }

            var result = new List<LanguageDataSO>(cachedLanguageDataAssets);
            if (currentData != null && !result.Contains(currentData))
                result.Add(currentData);

            return result;
        }

        /// <summary>新建/删除 LanguageDataSO 资产后调用使缓存失效；条目数据始终实时读取，不受缓存影响。</summary>
        public static void InvalidateCache()
        {
            cachedLanguageDataAssets = null;
        }

        public sealed class DuplicateKeyReport
        {
            public DuplicateKeyReport(string namespaceId, string key, List<DuplicateKeyOccurrence> occurrences)
            {
                NamespaceId = namespaceId ?? "";
                Key = key;
                OccurrenceCount = occurrences.Count;
                AssetPaths = occurrences
                    .Select(occurrence => occurrence.AssetPath)
                    .Distinct(System.StringComparer.OrdinalIgnoreCase)
                    .OrderBy(path => path)
                    .ToList();
            }

            public string NamespaceId { get; }
            public string Key { get; }
            public int OccurrenceCount { get; }
            public List<string> AssetPaths { get; }
        }

        public sealed class DuplicateKeyOccurrence
        {
            public DuplicateKeyOccurrence(string assetPath, string namespaceId)
            {
                AssetPath = assetPath;
                NamespaceId = namespaceId ?? "";
            }

            public string AssetPath { get; }
            public string NamespaceId { get; }
        }
    }
}
#endif
