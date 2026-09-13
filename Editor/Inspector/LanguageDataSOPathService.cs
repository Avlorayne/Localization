#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace Localization.Editor
{
    /// <summary>源文件路径的解析与规整：项目内路径存 "Assets/..."，外部路径存绝对路径。</summary>
    internal static class LanguageDataSOPathService
    {
        public static string ResolveSourcePath(string storedPath)
        {
            if (Path.IsPathRooted(storedPath))
                return storedPath;

            return Path.GetFullPath(storedPath);
        }

        public static string StoreSourcePath(string absolutePath)
        {
            string normalizedPath = absolutePath.Replace("\\", "/");
            string normalizedDataPath = Application.dataPath.Replace("\\", "/");

            if (normalizedPath.StartsWith(normalizedDataPath, StringComparison.OrdinalIgnoreCase))
                return "Assets" + normalizedPath.Substring(normalizedDataPath.Length);

            return normalizedPath;
        }

        public static string GetInitialDirectory(string storedPath)
        {
            if (string.IsNullOrEmpty(storedPath))
                return Application.dataPath;

            string resolvedPath = ResolveSourcePath(storedPath);
            if (Directory.Exists(resolvedPath))
                return resolvedPath;

            string directory = Path.GetDirectoryName(resolvedPath);
            return !string.IsNullOrEmpty(directory) && Directory.Exists(directory) ? directory : Application.dataPath;
        }
    }
}
#endif