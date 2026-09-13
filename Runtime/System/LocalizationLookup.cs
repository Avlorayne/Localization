using System;
using System.Collections.Generic;
using UnityEngine;

namespace Localization
{
    /// <summary>
    /// 纯索引与查找：按命名空间持有键索引并解析文本回退，
    /// 不依赖 MonoBehaviour 与场景状态，由 LocalizationSystem 持有并驱动，可在 EditMode 下独立测试。
    /// 命名空间和键都使用 OrdinalIgnoreCase 比较；同一命名空间内的重复键按加载顺序覆盖。
    /// </summary>
    internal class LocalizationLookup
    {
        private readonly Dictionary<string, Dictionary<string, LocalizationData>> _lookup =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly string _defaultLanguage;

        public LocalizationLookup(string defaultLanguage)
        {
            _defaultLanguage = defaultLanguage;
        }

        private void AddData(LanguageDataSO source)
        {
            if (source == null)
                return;

            string namespaceId = source.NamespaceId;
            if (!_lookup.TryGetValue(namespaceId, out var namespaceEntries))
            {
                namespaceEntries = new Dictionary<string, LocalizationData>(StringComparer.OrdinalIgnoreCase);
                _lookup[namespaceId] = namespaceEntries;
            }

            if (source.entries == null)
                return;

            foreach (var entry in source.entries)
            {
                if (string.IsNullOrEmpty(entry.key))
                {
                    Debug.LogWarning($"[Localization] Skipping entry with empty key in '{source.name}'");
                    continue;
                }

                if (namespaceEntries.ContainsKey(entry.key))
                {
                    Debug.LogError(
                        $"[Localization] Duplicate key '{entry.key}' in namespace '{namespaceId}'. " +
                        "Overwriting previous namespace entry.");
                }

                namespaceEntries[entry.key] = entry;
            }
        }

        public bool TryGetText(string nameSpace, string key, LanguageDefinition languageDefinition, out string result)
        {
            result = string.Empty;
            if (string.IsNullOrEmpty(nameSpace))
            {
                Debug.LogError("[Localization] namespace Empty!");
                return false;
            }

            if (!_lookup.TryGetValue(nameSpace, out var dict))
            {
                if (LanguageDataLoader.TryLoadDataResource(nameSpace, out var so))
                {
                    AddData(so);
                    dict = _lookup[nameSpace];
                }
                else
                {
                    Debug.LogError($"[Localization] Duplicate namespace '{nameSpace}'");
                    return false;
                }
            }

            if (dict.TryGetValue(key, out var data))
            {
                if (data.TryGet(languageDefinition.code, _defaultLanguage, out result)) return true;
                if (data.TryGet(languageDefinition.fallbackLanguage, _defaultLanguage, out result)) return true;
            }
            Debug.LogWarning($"[Localization] No such key '{key}' or language '{languageDefinition.displayName}'");
            return false;
        }

        public bool ContainsKey(string key, IReadOnlyList<string> namespaceIds)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return TryGetNamespacedEntry(key, namespaceIds, out _);
        }

        public IReadOnlyList<string> GetNamespaces()
        {
            return new List<string>(_lookup.Keys);
        }

        public IReadOnlyList<LocalizationData> GetEntries(string namespaceId)
        {
            return _lookup.TryGetValue(namespaceId.Trim(), out var entries)
                ? new List<LocalizationData>(entries.Values)
                : Array.Empty<LocalizationData>();
        }

        private bool TryGetNamespacedEntry(
            string key,
            IReadOnlyList<string> namespaceIds,
            out LocalizationData data)
        {
            data = default;
            if (namespaceIds != null)
            {
                foreach (var namespaceId in namespaceIds)
                {
                    if (string.IsNullOrWhiteSpace(namespaceId))
                        continue;

                    if (_lookup.TryGetValue(namespaceId.Trim(), out var namespaceEntries) &&
                        namespaceEntries.TryGetValue(key, out data))
                    {
                        return true;
                    }
                }
            }

            foreach (var namespaceEntries in _lookup.Values)
            {
                if (namespaceEntries.TryGetValue(key, out data))
                    return true;
            }

            return false;
        }
    }
}