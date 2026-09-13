using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Localization
{
    [CreateAssetMenu(fileName = "LanguageData", menuName = "Localization/Language Data")]
    public class LanguageDataSO : ScriptableObject
    {
        private static readonly Dictionary<string, LanguageDataSO> NamespaceCache = new();
        
        [Tooltip("Source localization file path. Supports importer formats registered in editor tools.")]
        public string sourceFilePath;

        [SerializeField,Tooltip(
            "Optional namespace used to scope key lookup and editor key selection. Falls back to source file name or asset name when empty.")]
        private string namespaceId;
        
        public string NamespaceId
        {
            get
            {
                if (!string.IsNullOrEmpty(namespaceId))
                    return namespaceId.Trim();
                
                if (NamespaceCache.TryGetValue(name, out var resource))
                {
                    if (resource != this)
                        Debug.LogWarning($"{name} 命名空间不唯一");
                }
                else
                    NamespaceCache.Add(name, this);
                
                return name.Trim();
            }
        }

        [FormerlySerializedAs("csvFile")] [HideInInspector]
        public TextAsset legacyCsvFile;

        public List<LocalizationData> entries = new();
        
    }
}