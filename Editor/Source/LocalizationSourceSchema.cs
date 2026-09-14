#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Localization.Editor.Source
{
    /// <summary>一种本地化语言的源文件列定义：规范表头 + 读写条目字段的委托。</summary>
    internal readonly struct LocalizationLanguageColumn
    {
        public LocalizationLanguageColumn(
            string header,
            string languageCode,
            string displayName = null)
        {
            Header = header;
            LanguageCode = languageCode;
            DisplayName = LocalizationSourceSchema.GetDisplayName(languageCode, displayName);
        }

        public string Header { get; }

        public string LanguageCode { get; }

        public string DisplayName { get; }

        public bool Matches(string header)
        {
            string trimmed = header?.Trim();
            return string.Equals(Header, trimmed, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(LanguageCode, trimmed, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(DisplayName, trimmed, StringComparison.OrdinalIgnoreCase) ||
                   LocalizationSourceSchema.LanguageIdsEqual(LanguageCode, trimmed);
        }

        public string GetValue(LocalizationData data)
        {
            return LocalizationSourceSchema.GetText(data, LanguageCode);
        }

        public void SetValue(ref LocalizationData data, string value)
        {
            LocalizationSourceSchema.SetText(ref data, LanguageCode, value);
        }
    }

    /// <summary>
    /// 本地化源文件（CSV/XLSX）的规范表头与表头→字段映射。
    /// CSV 与 XLSX 两个导入器共用；语言列与顺序优先由工程 Project Settings 中的语言配置驱动。
    /// </summary>
    internal static class LocalizationSourceSchema
    {
        private static readonly LocalizationLanguageColumn[] FallbackLanguageColumns =
        {
            new("zh-Hans", "zh-Hans", "简体中文"),
            new("zh-Hant", "zh-Hant", "繁體中文"),
            new("en", "en", "English"),
            new("ja", "ja", "日本語"),
            new("ko", "ko", "한국어"),
        };

        private static LocalizationLanguageColumn[] cachedLanguageColumns;
        private static bool warnedMissingLanguageConfig;
        private static bool warnedDuplicateLanguageCode;

        /// <summary>语言列（导出/导入/编辑器 UI 的统一数据源，顺序与 Project Settings 语言配置一致）。</summary>
        public static LocalizationLanguageColumn[] LanguageColumns =>
            cachedLanguageColumns ??= LoadLanguageColumns();

        /// <summary>规范表头列（含 Key 与 Comment），也是无表头导入时的列序依据与导出表头行的内容。</summary>
        public static string[] StandardHeaders => BuildStandardHeaders(LanguageColumns);

        private static string[] BuildStandardHeaders(IReadOnlyList<LocalizationLanguageColumn> languageColumns)
        {
            var headers = new string[languageColumns.Count + 2];
            headers[0] = "Key";
            for (int i = 0; i < languageColumns.Count; i++)
                headers[i + 1] = languageColumns[i].Header;
            headers[^1] = "Comment";
            return headers;
        }

        private static readonly HashSet<string> warnedUnknownHeaders =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>按规范表头名（大小写不敏感）把值写入条目对应字段；Comment 表头走 IsCommentHeader 语义判断；未知表头告警一次并忽略。</summary>
        public static void ApplyField(ref LocalizationData data, string header, string value)
        {
            if (header.Equals("Key", StringComparison.OrdinalIgnoreCase))
            {
                // CsvParser normalizes and assigns the key before applying the remaining fields.
                // Keep that normalized value instead of writing the raw CSV cell back.
                return;
            }

            if (TryGetLanguageColumn(header, out LocalizationLanguageColumn column))
            {
                column.SetValue(ref data, value);
                return;
            }

            if (LocalizationSourceHeaders.IsCommentHeader(header))
            {
                data.comment = value;
                return;
            }

            WarnUnknownHeader(header);
        }

        /// <summary>按规范表头名（大小写不敏感）读取条目对应字段值；未知表头告警一次并返回空字符串。</summary>
        public static string GetFieldValue(LocalizationData data, string header)
        {
            if (header.Equals("Key", StringComparison.OrdinalIgnoreCase))
                return data.key;

            if (TryGetLanguageColumn(header, out LocalizationLanguageColumn column))
                return column.GetValue(data);

            if (LocalizationSourceHeaders.IsCommentHeader(header))
                return data.comment;

            WarnUnknownHeader(header);
            return "";
        }

        public static bool TryGetLanguageColumn(string header, out LocalizationLanguageColumn result)
        {
            foreach (LocalizationLanguageColumn column in LanguageColumns)
            {
                if (column.Matches(header))
                {
                    result = column;
                    return true;
                }
            }

            result = default;
            return false;
        }

        public static LocalizationText[] CreateEmptyTextArray()
        {
            return LanguageColumns
                .Select(column => new LocalizationText { languageCode = column.LanguageCode, text = "" })
                .ToArray();
        }

        public static string GetText(LocalizationData data, string languageCode)
        {
            if (data.texts == null || string.IsNullOrWhiteSpace(languageCode))
                return "";

            foreach (LocalizationText text in data.texts)
            {
                if (LanguageIdsEqual(text.languageCode, languageCode))
                    return text.text ?? "";
            }

            return "";
        }

        public static void SetText(ref LocalizationData data, string languageCode, string value)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return;

            if (data.texts == null)
                data.texts = Array.Empty<LocalizationText>();

            for (int i = 0; i < data.texts.Length; i++)
            {
                if (!LanguageIdsEqual(data.texts[i].languageCode, languageCode))
                    continue;

                data.texts[i] = new LocalizationText
                {
                    languageCode = string.IsNullOrWhiteSpace(data.texts[i].languageCode)
                        ? languageCode
                        : data.texts[i].languageCode,
                    text = value ?? ""
                };
                return;
            }

            Array.Resize(ref data.texts, data.texts.Length + 1);
            data.texts[^1] = new LocalizationText { languageCode = languageCode, text = value ?? "" };
        }

        public static void NormalizeEntries(List<LocalizationData> entries)
        {
            if (entries == null)
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                LocalizationData data = entries[i];
                data.texts = CreateNormalizedTextArray(data);
                entries[i] = data;
            }
        }

        public static bool ValidateNoEmbeddedLocalizationKeys(
            List<LocalizationData> entries,
            string sourceLabel,
            UnityEngine.Object context = null)
        {
            var violations = CollectEmbeddedLocalizationKeyViolations(entries);
            if (violations.Count == 0)
                return true;

            const int maxShown = 20;
            var message = new StringBuilder();
            message.AppendLine(
                $"[Localization] Language content must not contain localization keys. Source: {sourceLabel}");
            for (int i = 0; i < violations.Count && i < maxShown; i++)
            {
                var violation = violations[i];
                message.AppendLine(
                    $"- Entry '{violation.EntryKey}', language '{violation.LanguageCode}': <{violation.Placeholder.RawContent}>");
            }

            if (violations.Count > maxShown)
                message.AppendLine($"... and {violations.Count - maxShown} more.");

            Debug.LogError(message.ToString(), context);
            return false;
        }

        public static List<EmbeddedLocalizationKeyViolation> CollectEmbeddedLocalizationKeyViolations(
            List<LocalizationData> entries)
        {
            var result = new List<EmbeddedLocalizationKeyViolation>();
            if (entries == null)
                return result;

            LocalizationLanguageColumn[] columns = LanguageColumns;
            foreach (LocalizationData entry in entries)
            {
                foreach (LocalizationLanguageColumn column in columns)
                {
                    string text = column.GetValue(entry);
                    if (string.IsNullOrEmpty(text))
                        continue;

                    foreach (LocalizationPlaceholder placeholder in LocalizationTemplateParser.Parse(text))
                    {
                        result.Add(new EmbeddedLocalizationKeyViolation(
                            entry.key,
                            column.LanguageCode,
                            placeholder));
                    }
                }
            }

            return result;
        }

        public static void InvalidateLanguageConfigCache()
        {
            cachedLanguageColumns = null;
        }

        private static LocalizationText[] CreateNormalizedTextArray(LocalizationData data)
        {
            LocalizationLanguageColumn[] columns = LanguageColumns;
            var result = new LocalizationText[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                LocalizationLanguageColumn column = columns[i];
                result[i] = new LocalizationText
                {
                    languageCode = column.LanguageCode,
                    text = GetText(data, column.LanguageCode)
                };
            }

            return result;
        }

        private static LocalizationLanguageColumn[] LoadLanguageColumns()
        {
            LanguageProjectSettings config = LoadLanguageConfig();
            if (config == null || config.languages == null || config.languages.Count == 0)
            {
                if (!warnedMissingLanguageConfig)
                {
                    warnedMissingLanguageConfig = true;
                    Debug.LogWarning(
                        "[Localization] No LanguageConfigSO with language definitions was found. Falling back to built-in language columns.");
                }

                return FallbackLanguageColumns;
            }

            var columns = new List<LocalizationLanguageColumn>();
            var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (LanguageDefinition definition in config.languages)
            {
                string code = definition?.code?.Trim();
                if (string.IsNullOrEmpty(code))
                    continue;

                string normalizedCode = NormalizeLanguageId(code);
                if (!usedCodes.Add(normalizedCode))
                {
                    if (!warnedDuplicateLanguageCode)
                    {
                        warnedDuplicateLanguageCode = true;
                        Debug.LogWarning(
                            $"[Localization] Duplicate language code '{code}' in LanguageConfigSO was ignored.");
                    }

                    continue;
                }

                columns.Add(new LocalizationLanguageColumn(code, code, definition.displayName));
            }

            return columns.Count > 0 ? columns.ToArray() : FallbackLanguageColumns;
        }

        private static LanguageProjectSettings LoadLanguageConfig()
        {
            return LanguageProjectSettings.GetOrCreate();
        }

        internal static bool LanguageIdsEqual(string left, string right)
        {
            return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(NormalizeLanguageId(left), NormalizeLanguageId(right),
                       StringComparison.OrdinalIgnoreCase);
        }

        internal static string GetDisplayName(string languageCode, string displayName)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName.Trim();

            return NormalizeLanguageId(languageCode) switch
            {
                "zhhans" => "简体中文",
                "zhcn" => "简体中文",
                "zhhant" => "繁體中文",
                "zhtw" => "繁體中文",
                "en" => "English",
                "enus" => "English",
                "ja" => "日本語",
                "jajp" => "日本語",
                "ko" => "한국어",
                "kokr" => "한국어",
                _ => string.IsNullOrWhiteSpace(languageCode) ? "Unnamed Language" : languageCode.Trim()
            };
        }

        private static string NormalizeLanguageId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim()
                .Replace("-", "")
                .Replace("_", "")
                .Replace(" ", "")
                .ToLowerInvariant();
        }

        /// <summary>未知表头 = 拼写错误或外来列，整列数据会被跳过，属高危信号；同一表头只告警一次避免逐行刷屏。</summary>
        private static void WarnUnknownHeader(string header)
        {
            string trimmed = header?.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return;

            if (warnedUnknownHeaders.Add(trimmed))
                Debug.LogWarning(
                    $"[Localization] Unknown source column '{trimmed}', its data will be skipped. " +
                    $"Expected columns: {string.Join(", ", StandardHeaders)}");
        }
    }

    internal readonly struct EmbeddedLocalizationKeyViolation
    {
        public EmbeddedLocalizationKeyViolation(
            string entryKey,
            string languageCode,
            LocalizationPlaceholder placeholder)
        {
            EntryKey = entryKey ?? "";
            LanguageCode = languageCode ?? "";
            Placeholder = placeholder;
        }

        public string EntryKey { get; }

        public string LanguageCode { get; }

        public LocalizationPlaceholder Placeholder { get; }
    }
}
#endif