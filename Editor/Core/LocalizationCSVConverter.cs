#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Localization.Editor.Source;
using UnityEditor;
using UnityEngine;
using static Localization.Editor.LocalizationEditorText;

namespace Localization.Editor
{
    internal static class LocalizationSourceConverter
    {
        private const string HashesJsonPath = "Assets/Settings/LocalizationSourceHashes.json";

        // ---- Menu Items ----

        [MenuItem("Tools/Localization/Open Language Config", priority = 1)]
        public static void OpenConfiguration()
        {
            LanguageConfigProjectSettingsProvider.Open();
        }

        [MenuItem("Tools/Localization/Convert Changed Source Files", priority = 20)]
        public static void ConvertChangedFiles()
        {
            ExecuteConversion(false);
        }

        [MenuItem("Tools/Localization/Convert All Source Files", priority = 21)]
        public static void ConvertAllFiles()
        {
            ExecuteConversion(true);
        }

        // ---- Core Logic ----

        internal static LanguageProjectSettings GetOrCreateSettings()
        {
            var settings = LanguageProjectSettings.GetOrCreate();
            if (!Directory.Exists(settings.sourceFolderPath)) Directory.CreateDirectory(settings.sourceFolderPath);
            return settings;
        }

        private static void ExecuteConversion(bool forceAll)
        {
            var settings = GetOrCreateSettings();
            LanguageConfigBaker.BakeRuntimeConfig(settings);
            if (string.IsNullOrEmpty(settings.sourceFolderPath) || string.IsNullOrEmpty(settings.soFolderPath))
            {
                Debug.LogError(T("log.missing.settings"));
                OpenConfiguration();
                return;
            }

            if (!Directory.Exists(settings.sourceFolderPath))
            {
                Debug.LogError(F("log.source.folder.missing", settings.sourceFolderPath));
                return;
            }

            if (!Directory.Exists(settings.soFolderPath))
            {
                Directory.CreateDirectory(settings.soFolderPath);
                AssetDatabase.Refresh();
            }

            var previousHashes = LoadHashes();
            var newHashes = new Dictionary<string, string>(previousHashes);
            bool anyConverted = false;

            var sourceFiles = new List<string>();
            foreach (string extension in LocalizationSourceParser.SupportedImportExtensions)
                sourceFiles.AddRange(Directory.GetFiles(settings.sourceFolderPath, $"*{extension}",
                    SearchOption.AllDirectories));
            var convertedFiles = new List<(string relativeSourcePath, string absoluteSourcePath, string soPath)>();

            foreach (string absolutePath in sourceFiles)
            {
                if (IsTemporaryOfficeLockFile(absolutePath))
                    continue;

                string relativeSourcePath = FileUtil.GetProjectRelativePath(absolutePath);
                if (string.IsNullOrEmpty(relativeSourcePath))
                {
                    // Fallback if the path is not under the project folder
                    relativeSourcePath = absolutePath.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                }

                string soName = BuildSoAssetName(absolutePath, settings.sourceFolderPath);
                string soPath = $"{settings.soFolderPath}/{soName}.asset";

                if (!TryGetFileSHA256(absolutePath, out string currentSourceHash))
                {
                    Debug.LogWarning(F("log.source.file.read.failed", relativeSourcePath));
                    continue;
                }

                string currentSoHash = "";
                if (File.Exists(soPath) && !TryGetFileSHA256(soPath, out currentSoHash))
                {
                    Debug.LogWarning(F("log.source.file.read.failed", soPath));
                    continue;
                }

                bool sourceChanged = !previousHashes.TryGetValue(relativeSourcePath, out string oldSourceHash) ||
                                     oldSourceHash != currentSourceHash;
                bool soChanged = !previousHashes.TryGetValue(soPath, out string oldSoHash) ||
                                 oldSoHash != currentSoHash;

                if (forceAll || sourceChanged || soChanged)
                {
                    if (ConvertSourceToSo(relativeSourcePath, absolutePath, soPath))
                    {
                        newHashes[relativeSourcePath] = currentSourceHash;
                        convertedFiles.Add((relativeSourcePath, absolutePath, soPath));
                        anyConverted = true;
                    }
                }
                else
                {
                    newHashes[soPath] = currentSoHash;
                }
            }

            // 源文件被删除时同步清理哈希记录；删除哈希需要落盘，故与常规转换相互独立地保存
            if (RemoveStaleHashEntries(previousHashes, newHashes, settings))
                SaveHashes(newHashes);

            if (anyConverted)
            {
                // 保存资源使其落盘，以便计算最新的 SO 文件哈希值
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                LanguageDataSODuplicateKeyValidator.InvalidateCache();

                foreach (var item in convertedFiles)
                {
                    if (File.Exists(item.soPath) && TryGetFileSHA256(item.soPath, out string newSoHash))
                        newHashes[item.soPath] = newSoHash;
                }

                SaveHashes(newHashes);
                LanguageDataSODuplicateKeyValidator.LogDuplicateKeys();
                Debug.Log(F("log.conversion.complete", convertedFiles.Count));
            }
            else
            {
                Debug.Log(T("log.no.changed.source"));
            }
        }

        private static bool ConvertSourceToSo(string relativeSourcePath, string absoluteSourcePath, string soPath)
        {
            var importedEntries = LocalizationSourceParser.Import(absoluteSourcePath);
            if (importedEntries.Count == 0)
            {
                Debug.LogWarning(F("log.empty.source.skip", relativeSourcePath));
                return false;
            }

            bool sourceContentIsValid =
                LocalizationSourceSchema.ValidateNoEmbeddedLocalizationKeys(importedEntries, relativeSourcePath);

            // 1. Find or create SO
            var so = AssetDatabase.LoadAssetAtPath<LanguageDataSO>(soPath);
            bool isNew = false;
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<LanguageDataSO>();
                AssetDatabase.CreateAsset(so, soPath);
                isNew = true;
                Debug.Log(F("log.created.so", soPath));
            }

            so.sourceFilePath = relativeSourcePath;

            // 2. Import data
            so.entries = importedEntries;

            EditorUtility.SetDirty(so);

            if (sourceContentIsValid)
                LanguageDataSOAddressableRegistrar.TryRegisterIfValid(so);

            if (!isNew)
            {
                Debug.Log(F("log.updated.so", soPath));
            }

            return true;
        }

        /// <summary>
        /// 由源文件相对 sourceFolderPath 的子路径生成 SO 资产名（目录分隔符归一为 '_'），
        /// 避免不同子目录下同名源文件（如 Source/UI/a.xlsx 与 Source/Battle/a.xlsx）互相覆盖同一个 SO。
        /// </summary>
        public static string BuildSoAssetName(string absoluteSourcePath, string sourceFolderPath)
        {
            string root = Path.GetFullPath(sourceFolderPath)
                              .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                          + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(absoluteSourcePath);

            string relative = full.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? full.Substring(root.Length)
                : Path.GetFileName(full);

            string name = Path.ChangeExtension(relative.Replace('\\', '/'), null).Replace('/', '_');
            return string.IsNullOrEmpty(name) ? Path.GetFileNameWithoutExtension(absoluteSourcePath) : name;
        }

        /// <summary>
        /// 源文件被删除后：移除其哈希记录（含已不存在的 SO 记录），需要落盘时返回 true；
        /// 对应 SO 资产保留并提示人工确认，不做自动删除以免误删数据。
        /// </summary>
        private static bool RemoveStaleHashEntries(
            Dictionary<string, string> previousHashes,
            Dictionary<string, string> newHashes,
            LanguageProjectSettings settings)
        {
            string soFolderPrefix = settings.soFolderPath.Replace('\\', '/').TrimEnd('/') + "/";
            bool removedAny = false;

            foreach (string hashKey in previousHashes.Keys)
            {
                if (newHashes.ContainsKey(hashKey) || File.Exists(ToProjectAbsolutePath(hashKey)))
                    continue;

                newHashes.Remove(hashKey);
                removedAny = true;

                bool isSoEntry = hashKey.Replace('\\', '/')
                    .StartsWith(soFolderPrefix, StringComparison.OrdinalIgnoreCase);
                if (isSoEntry)
                    continue;

                string soName = BuildSoAssetName(ToProjectAbsolutePath(hashKey), settings.sourceFolderPath);
                Debug.LogWarning(F("log.stale.source.cleaned",
                    hashKey, $"{settings.soFolderPath}/{soName}.asset"));
            }

            return removedAny;
        }

        private static string ToProjectAbsolutePath(string projectRelativePath)
        {
            return Path.IsPathRooted(projectRelativePath)
                ? projectRelativePath
                : Path.GetFullPath(projectRelativePath);
        }

        // ---- Hashing Utility ----

        [Serializable]
        private class HashData
        {
            public string FilePath;
            public string Hash;
        }

        [Serializable]
        private class HashDataList
        {
            public List<HashData> Hashes = new List<HashData>();
        }

        private static Dictionary<string, string> LoadHashes()
        {
            var dict = new Dictionary<string, string>();
            if (File.Exists(HashesJsonPath))
            {
                try
                {
                    string json = File.ReadAllText(HashesJsonPath);
                    var list = JsonUtility.FromJson<HashDataList>(json);
                    if (list != null && list.Hashes != null)
                    {
                        foreach (var item in list.Hashes)
                        {
                            dict[item.FilePath] = item.Hash;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(F("log.hash.load.failed", e.Message));
                }
            }

            return dict;
        }

        private static void SaveHashes(Dictionary<string, string> dict)
        {
            var list = new HashDataList();
            foreach (var kvp in dict)
            {
                list.Hashes.Add(new HashData { FilePath = kvp.Key, Hash = kvp.Value });
            }

            string dir = Path.GetDirectoryName(HashesJsonPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string json = JsonUtility.ToJson(list, true);
            File.WriteAllText(HashesJsonPath, json);
        }

        private static string GetFileSHA256(string filePath)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = LocalizationSourceFileAccess.OpenSharedRead(filePath))
                {
                    byte[] hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private static bool IsTemporaryOfficeLockFile(string filePath)
        {
            return Path.GetFileName(filePath).StartsWith("~$", StringComparison.Ordinal);
        }

        private static bool TryGetFileSHA256(string filePath, out string hash)
        {
            try
            {
                hash = GetFileSHA256(filePath);
                return true;
            }
            catch (IOException)
            {
                hash = "";
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                hash = "";
                return false;
            }
        }
    }
}
#endif