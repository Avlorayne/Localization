using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using static Localization.Editor.LocalizationEditorText;

#if UNITY_EDITOR
namespace Localization.Editor.Source
{
    internal static class CsvParser
    {
        // GB18030 是 GBK/GB2312 的超集，作为无 BOM 且非 UTF-8 的 CSV 的回退编码（简中 Excel 另存 ANSI 的典型情况）。
        private const string FallbackEncodingName = "GB18030";

        private static Encoding fallbackEncoding;

        public static List<LocalizationData> ImportFromText(string csvText)
        {
            LocalizationSourceSchema.InvalidateLanguageConfigCache();
            var result = new List<LocalizationData>();

            if (string.IsNullOrWhiteSpace(csvText))
                return result;

            List<List<string>> records = ParseCsvRecords(csvText);
            if (records.Count == 0)
                return result;

            string[] headerRow = records[0].ToArray();
            int keyIndex = -1;
            for (int i = 0; i < headerRow.Length; i++)
            {
                string header = headerRow[i].Trim();
                if (string.IsNullOrEmpty(header)) continue;

                if (header.Equals("Key", StringComparison.OrdinalIgnoreCase))
                    keyIndex = i;
            }

            bool hasHeader = keyIndex >= 0;
            if (!hasHeader)
            {
                headerRow = LocalizationSourceSchema.StandardHeaders;
                keyIndex = 0;
            }

            Debug.Log(F("log.csv.importing", headerRow.Length, keyIndex));

            int startRecord = hasHeader ? 1 : 0;
            for (int r = startRecord; r < records.Count; r++)
            {
                List<string> fields = records[r];

                var data = new LocalizationData();
                if (keyIndex < 0 || keyIndex >= fields.Count ||
                    !LocalizationKeyUtility.TryNormalizeLocalizationKey(fields[keyIndex], out string normalizedKey,
                        hasHeader))
                    continue;

                data.key = normalizedKey;

                for (int j = 0; j < headerRow.Length && j < fields.Count; j++)
                {
                    string val = fields[j].Trim();
                    if (string.IsNullOrEmpty(val)) continue;

                    LocalizationSourceSchema.ApplyField(ref data, headerRow[j].Trim(), val);
                }

                result.Add(data);
            }

            LocalizationSourceSchema.NormalizeEntries(result);
            Debug.Log(F("log.csv.import.complete", result.Count));
            return result;
        }

        public static List<LocalizationData> Import(string csvPath)
        {
            if (!File.Exists(csvPath))
            {
                Debug.LogError(F("log.csv.not.found", csvPath));
                return new List<LocalizationData>();
            }

            string text = ReadCsvText(csvPath);
            var result = ImportFromText(text);
            Debug.Log(F("log.csv.imported", result.Count, Path.GetFileName(csvPath)));
            return result;
        }

        /// <summary>
        /// 读取 CSV 文本：优先按 BOM 识别编码；无 BOM 时先按严格 UTF-8 尝试，
        /// 解码失败（简中 Excel 另存 ANSI/GBK 文件的典型情况）回退 GB18030。
        /// </summary>
        private static string ReadCsvText(string path)
        {
            byte[] bytes = LocalizationSourceFileAccess.ReadAllBytesShared(path);

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

            try
            {
                return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return GetFallbackEncoding().GetString(bytes);
            }
        }

        private static Encoding GetFallbackEncoding()
        {
            if (fallbackEncoding != null)
                return fallbackEncoding;

            try
            {
                fallbackEncoding = Encoding.GetEncoding(FallbackEncodingName);
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"[CsvParser] GB18030 encoding unavailable on this runtime, non-UTF-8 CSV will be read as UTF-8: {e.Message}");
                fallbackEncoding = Encoding.UTF8;
            }

            return fallbackEncoding;
        }

        /// <summary>
        /// 字符级 CSV 解析：引号字段内的逗号/换行/双写引号都作为内容保留。
        /// 修复旧实现"先按 \n 切行再逐行解析"导致的多行字段错位（导出的含换行字段自己读不回来）。
        /// </summary>
        private static List<List<string>> ParseCsvRecords(string csvText)
        {
            var records = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            void EndField()
            {
                row.Add(field.ToString().Trim());
                field.Clear();
            }

            void EndRecord()
            {
                EndField();
                if (row.Exists(f => f.Length > 0))
                    records.Add(row);
                row = new List<string>();
            }

            for (int i = 0; i < csvText.Length; i++)
            {
                char c = csvText[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csvText.Length && csvText[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }

                    continue;
                }

                if (c == '"' && field.Length == 0)
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    EndField();
                }
                else if (c == '\n')
                {
                    EndRecord();
                }
                else if (c == '\r')
                {
                    if (i + 1 < csvText.Length && csvText[i + 1] == '\n')
                        i++;

                    EndRecord();
                }
                else
                {
                    field.Append(c);
                }
            }

            if (field.Length > 0 || row.Count > 0 || inQuotes)
                EndRecord();

            return records;
        }

        public static string Export(List<LocalizationData> entries)
        {
            LocalizationSourceSchema.InvalidateLanguageConfigCache();
            var sb = new StringBuilder();
            string[] headers = LocalizationSourceSchema.StandardHeaders;
            sb.AppendLine(string.Join(",", headers));

            foreach (var e in entries)
            {
                var rowFields = new string[headers.Length];
                for (int i = 0; i < headers.Length; i++)
                    rowFields[i] = EscapeField(LocalizationSourceSchema.GetFieldValue(e, headers[i]));

                sb.AppendLine(string.Join(",", rowFields));
            }

            return sb.ToString();
        }

        public static void ExportToFile(List<LocalizationData> entries, string csvPath)
        {
            string dir = Path.GetDirectoryName(csvPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
            string content = Export(entries);
            byte[] data = Encoding.UTF8.GetBytes(content);

            using var fs = new FileStream(csvPath, FileMode.Create, FileAccess.Write);
            fs.Write(bom, 0, bom.Length);
            fs.Write(data, 0, data.Length);

            Debug.Log(F("log.csv.exported", entries.Count, Path.GetFileName(csvPath)));
        }

        private static string EscapeField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
                return $"\"{field.Replace("\"", "\"\"")}\"";
            return field;
        }
    }
}
#endif