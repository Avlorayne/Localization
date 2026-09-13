#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using ExcelDataReader;
using UnityEngine;
using static Localization.Editor.LocalizationEditorText;

namespace Localization.Editor
{
    internal static class XlsxLocalizationParser
    {
        public static List<LocalizationData> Import(string xlsxPath)
        {
            LocalizationSourceSchema.InvalidateLanguageConfigCache();
            var result = new List<LocalizationData>();

            if (!File.Exists(xlsxPath))
            {
                Debug.LogError(F("log.xlsx.not.found", xlsxPath));
                return result;
            }

            try
            {
                using var stream = LocalizationSourceFileAccess.OpenSharedRead(xlsxPath);
                using var reader = ExcelReaderFactory.CreateReader(stream);

                int sheetIndex = 0;
                do
                {
                    // 无表头约定只对第一个 sheet 生效；其余 sheet 必须有 Key 表头，
                    // 防止无关工作表（如统计表 A1 恰好是大写蛇形词）被误判为本地化数据
                    ImportCurrentSheet(reader, result, allowHeaderless: sheetIndex == 0);
                    sheetIndex++;
                } while (reader.NextResult());

                LocalizationSourceSchema.NormalizeEntries(result);
                Debug.Log(F("log.xlsx.imported", result.Count, Path.GetFileName(xlsxPath)));
            }
            catch (System.Exception e)
            {
                Debug.LogError(F("log.xlsx.import.failed", xlsxPath, e.Message));
            }

            return result;
        }

        private static void ImportCurrentSheet(IExcelDataReader reader, List<LocalizationData> result,
            bool allowHeaderless)
        {
            string[] headers = null;
            int keyIndex = -1;
            int importedInSheet = 0;
            bool allowDisplayKeys = false;

            while (reader.Read())
            {
                string[] row = ReadRow(reader);

                if (headers == null)
                {
                    keyIndex = FindHeaderIndex(row, "Key");
                    if (keyIndex < 0)
                    {
                        if (!allowHeaderless || !LooksLikeHeaderlessLocalizationRow(row))
                            continue;

                        headers = LocalizationSourceSchema.StandardHeaders;
                        keyIndex = 0;
                        allowDisplayKeys = false;
                    }
                    else
                    {
                        headers = row;
                        allowDisplayKeys = true;
                        Debug.Log(F("log.xlsx.sheet.importing", reader.Name, headers.Length, keyIndex));
                        continue;
                    }

                    Debug.Log(F("log.xlsx.sheet.importing", reader.Name, headers.Length, keyIndex));
                }

                if (keyIndex >= row.Length)
                    continue;

                if (!LocalizationKeyUtility.TryNormalizeLocalizationKey(row[keyIndex], out string normalizedKey,
                        allowDisplayKeys))
                    continue;

                var data = new LocalizationData { key = normalizedKey };

                for (int i = 0; i < headers.Length && i < row.Length; i++)
                {
                    string header = headers[i].Trim();
                    string value = row[i].Trim();
                    if (string.IsNullOrEmpty(value))
                        continue;

                    LocalizationSourceSchema.ApplyField(ref data, header, value);
                }

                result.Add(data);
                importedInSheet++;
            }

            if (headers != null)
                Debug.Log(F("log.xlsx.sheet.complete", reader.Name, importedInSheet));
            else
                Debug.Log(F("log.xlsx.sheet.skipped", reader.Name));
        }

        private static string[] ReadRow(IExcelDataReader reader)
        {
            var row = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
                row[i] = reader.GetValue(i)?.ToString() ?? string.Empty;

            return row;
        }

        private static int FindHeaderIndex(string[] headers, string expectedHeader)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i].Trim().Equals(expectedHeader, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static bool LooksLikeHeaderlessLocalizationRow(string[] row)
        {
            return row != null && row.Length > 0 && LocalizationKeyUtility.IsValidLocalizationKey(row[0]);
        }
    }
}
#endif