#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Localization.Editor
{
    /// <summary>
    /// 探测 Unity 编辑器界面语言并映射到本地化语言 code，带 2 秒缓存避免频繁反射。
    /// </summary>
    internal static class LocalizationEditorLanguage
    {
        private static int cachedLanguageIndex = -1;
        private static double lastLanguageCheck;
        private const double LanguageCacheSeconds = 2.0;

        // ==== 语言缓存：避免每次探测都反射 UnityEditor.LocalizationDatabase，是编辑器 UI 的热点 ====
        private static int GetCachedLanguageIndex()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (cachedLanguageIndex >= 0 && (now - lastLanguageCheck) < LanguageCacheSeconds)
                return cachedLanguageIndex;

            SystemLanguage language = GetUnityEditorLanguage();
            int index = language switch
            {
                SystemLanguage.ChineseSimplified => 1,
                SystemLanguage.ChineseTraditional => 2,
                SystemLanguage.Japanese => 3,
                SystemLanguage.Korean => 4,
                _ => 0,
            };
            cachedLanguageIndex = index;
            lastLanguageCheck = now;
            return index;
        }

        /// <summary>返回编辑器语言对应的多语言文案表索引：0=英，1=简中，2=繁中，3=日，4=韩。</summary>
        public static int GetTextLanguageIndex()
        {
            return GetCachedLanguageIndex();
        }

        public static string GetCurrentEditorLocalizationLanguageCode()
        {
            return GetCachedLanguageIndex() switch
            {
                1 => "zh-Hans",
                2 => "zh-Hant",
                3 => "ja",
                4 => "ko",
                _ => "en",
            };
        }

        // 缓存反射 Type 查找（最昂贵的反射操作，只做一次）
        private static System.Type s_localizationDbType;
        private static PropertyInfo s_langProperty;

        private static System.Type GetLocalizationDatabaseType()
        {
            return s_localizationDbType ??= typeof(UnityEditor.Editor).Assembly
                .GetType("UnityEditor.LocalizationDatabase");
        }

        private static SystemLanguage GetUnityEditorLanguage()
        {
            try
            {
                System.Type locDbType = GetLocalizationDatabaseType();
                PropertyInfo property = locDbType != null
                    ? s_langProperty ??= locDbType.GetProperty("currentEditorLanguage",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    : null;
                if (property != null)
                    return (SystemLanguage)property.GetValue(null);
            }
            catch
            {
            }

            string editorLanguage = EditorPrefs.GetString("EditorLanguage", "");
            if (TryParseEditorLanguage(editorLanguage, out SystemLanguage parsedLanguage))
                return parsedLanguage;

            return Application.systemLanguage;
        }

        private static bool TryParseEditorLanguage(string value, out SystemLanguage language)
        {
            language = SystemLanguage.English;
            if (string.IsNullOrEmpty(value))
                return false;

            string normalized = value.Replace("-", "").Replace("_", "").ToLowerInvariant();
            if (normalized.Contains("zhhans") || normalized.Contains("chinesesimplified") || normalized == "zhcn")
            {
                language = SystemLanguage.ChineseSimplified;
                return true;
            }

            if (normalized.Contains("zhhant") || normalized.Contains("chinesetraditional") || normalized == "zhtw")
            {
                language = SystemLanguage.ChineseTraditional;
                return true;
            }

            if (normalized.StartsWith("ja") || normalized.Contains("japanese"))
            {
                language = SystemLanguage.Japanese;
                return true;
            }

            if (normalized.StartsWith("ko") || normalized.Contains("korean"))
            {
                language = SystemLanguage.Korean;
                return true;
            }

            if (normalized.StartsWith("en") || normalized.Contains("english"))
            {
                language = SystemLanguage.English;
                return true;
            }

            return false;
        }
    }
}
#endif