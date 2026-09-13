#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using static Localization.Editor.LocalizationEditorText;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Localization.Editor
{
    public static class XlsxLocalizationExporter
    {
        private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        // 导出字段由 Schema 的语言列定义驱动，与导入侧共用同一份映射
        private static List<KeyValuePair<string, Func<LocalizationData, string>>> BuildExportFields()
        {
            var fields = new List<KeyValuePair<string, Func<LocalizationData, string>>>();
            foreach (LocalizationLanguageColumn column in LocalizationSourceSchema.LanguageColumns)
                fields.Add(new KeyValuePair<string, Func<LocalizationData, string>>(column.Header, column.GetValue));

            fields.Add(new KeyValuePair<string, Func<LocalizationData, string>>("Comment", data => data.comment));
            return fields;
        }

        public static bool ExportToFile(List<LocalizationData> entries, string xlsxPath)
        {
            try
            {
                LocalizationSourceSchema.InvalidateLanguageConfigCache();
                string dir = Path.GetDirectoryName(xlsxPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                bool exported = File.Exists(xlsxPath)
                    ? PatchExistingWorkbook(entries, xlsxPath)
                    : OpenXmlWorkbookBuilder.CreateWorkbook(entries, xlsxPath);

                if (exported)
                    Debug.Log(F("log.xlsx.exported", entries.Count, Path.GetFileName(xlsxPath)));

                return exported;
            }
            catch (Exception e)
            {
                Debug.LogError(F("log.xlsx.export.failed", xlsxPath, e.Message));
                return false;
            }
        }

        private static bool PatchExistingWorkbook(List<LocalizationData> entries, string xlsxPath)
        {
            string tempPath = $"{xlsxPath}.tmp";
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            try
            {
                using (var inputStream = new FileStream(xlsxPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var outputStream =
                       new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                using (var inputArchive = new ZipArchive(inputStream, ZipArchiveMode.Read))
                using (var outputArchive = new ZipArchive(outputStream, ZipArchiveMode.Create))
                {
                    string[] sharedStrings = ReadSharedStrings(inputArchive);
                    var entriesByKey = entries
                        .Where(entry => !string.IsNullOrEmpty(entry.key))
                        .GroupBy(entry => entry.key, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            var exportedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool foundLocalizationSheet = false;
            List<KeyValuePair<string, Func<LocalizationData, string>>> exportFields = BuildExportFields();

            foreach (ZipArchiveEntry inputEntry in inputArchive.Entries)
            {
                        ZipArchiveEntry outputEntry =
                            outputArchive.CreateEntry(inputEntry.FullName, CompressionLevel.Optimal);
                        CopyEntryMetadata(inputEntry, outputEntry);

                        if (IsWorksheetEntry(inputEntry))
                        {
                            using var reader = new StreamReader(inputEntry.Open(), Encoding.UTF8, true);
                            XDocument document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
                            bool sheetHandled = PatchWorksheet(document, entriesByKey, exportedKeys, sharedStrings,
                                !foundLocalizationSheet, exportFields);
                            if (sheetHandled)
                                foundLocalizationSheet = true;

                            using var writer = new StreamWriter(outputEntry.Open(), new UTF8Encoding(false));
                            document.Save(writer, SaveOptions.DisableFormatting);
                        }
                        else
                        {
                            using Stream source = inputEntry.Open();
                            using Stream destination = outputEntry.Open();
                            source.CopyTo(destination);
                        }
                    }

                    if (!foundLocalizationSheet)
                        throw new InvalidDataException(T("xlsx.no.key.sheet"));
                }

                File.Replace(tempPath, xlsxPath, null);
                return true;
            }
            catch
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);

                throw;
            }
        }

        private static bool PatchWorksheet(
            XDocument document,
            Dictionary<string, LocalizationData> entriesByKey,
            HashSet<string> exportedKeys,
            string[] sharedStrings,
            bool appendMissingEntries,
            List<KeyValuePair<string, Func<LocalizationData, string>>> exportFields)
        {
            XElement sheetData = document.Root?.Element(SpreadsheetNs + "sheetData");
            if (sheetData == null)
                return false;

            List<XElement> rows = sheetData.Elements(SpreadsheetNs + "row").ToList();
            XElement headerRow = null;
            Dictionary<string, int> headerIndexes = null;

            foreach (XElement row in rows)
            {
                Dictionary<string, int> candidateHeaders = ReadHeaders(row, sharedStrings);
                if (candidateHeaders.ContainsKey("Key"))
                {
                    headerRow = row;
                    headerIndexes = candidateHeaders;
                    break;
                }
            }

            if (headerRow == null || headerIndexes == null)
                return false;

            int keyColumn = headerIndexes["Key"];
            foreach (XElement row in rows)
            {
                if (row == headerRow)
                    continue;

                string key = GetCellText(GetCell(row, keyColumn), sharedStrings).Trim();
                if (string.IsNullOrEmpty(key) || !entriesByKey.TryGetValue(key, out LocalizationData data))
                    continue;

                exportedKeys.Add(key);
                foreach (var field in exportFields)
                {
                    if (!headerIndexes.TryGetValue(field.Key, out int columnIndex))
                        continue;

                    string targetValue = field.Value(data) ?? "";
                    if (field.Key.Equals("Comment", StringComparison.OrdinalIgnoreCase) &&
                        string.IsNullOrEmpty(targetValue))
                        continue;

                    XElement cell = GetOrCreateCell(row, columnIndex);
                    if (GetCellText(cell, sharedStrings) != targetValue)
                        SetCellText(cell, targetValue);
                }
            }

            if (appendMissingEntries)
            {
                foreach (LocalizationData data in entriesByKey.Values)
                {
                    if (exportedKeys.Contains(data.key))
                        continue;

                    XElement row = CreateLocalizationRow(data, headerIndexes, GetNextRowIndex(sheetData), exportFields);
                    sheetData.Add(row);
                    exportedKeys.Add(data.key);
                    UpdateDimension(document, sheetData);
                }
            }

            return true;
        }

        private static Dictionary<string, int> ReadHeaders(XElement row, string[] sharedStrings)
        {
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int fallbackColumn = 1;

            foreach (XElement cell in row.Elements(SpreadsheetNs + "c"))
            {
                int columnIndex = GetCellColumnIndex(cell, fallbackColumn);
                string header = GetCellText(cell, sharedStrings).Trim();
                if (LocalizationSourceHeaders.IsCommentHeader(header))
                    header = "Comment";
                else if (LocalizationSourceSchema.TryGetLanguageColumn(header, out LocalizationLanguageColumn column))
                    header = column.Header;

                if (!string.IsNullOrEmpty(header) && !headers.ContainsKey(header))
                    headers.Add(header, columnIndex);

                fallbackColumn++;
            }

            return headers;
        }

        private static XElement CreateLocalizationRow(
            LocalizationData data,
            Dictionary<string, int> headerIndexes,
            int rowIndex,
            List<KeyValuePair<string, Func<LocalizationData, string>>> exportFields)
        {
            var row = new XElement(SpreadsheetNs + "row", new XAttribute("r", rowIndex));
            var cells = new List<XElement>();

            if (headerIndexes.TryGetValue("Key", out int keyColumn))
                cells.Add(CreateInlineStringCell(keyColumn, rowIndex, data.key));

            foreach (var field in exportFields)
            {
                if (headerIndexes.TryGetValue(field.Key, out int columnIndex))
                    cells.Add(CreateInlineStringCell(columnIndex, rowIndex, field.Value(data) ?? ""));
            }

            foreach (XElement cell in cells.OrderBy(cell => GetCellColumnIndex(cell, 1)))
                row.Add(cell);

            return row;
        }

        private static XElement CreateInlineStringCell(int columnIndex, int rowIndex, string value)
        {
            var cell = new XElement(SpreadsheetNs + "c",
                new XAttribute("r", $"{OpenXmlWorkbookBuilder.GetColumnName(columnIndex)}{rowIndex}"));
            SetCellText(cell, value);
            return cell;
        }

        private static XElement GetOrCreateCell(XElement row, int columnIndex)
        {
            XElement existingCell = GetCell(row, columnIndex);
            if (existingCell != null)
                return existingCell;

            int rowIndex = GetRowIndex(row);
            XElement newCell = CreateInlineStringCell(columnIndex, rowIndex, "");
            XElement insertBefore = row.Elements(SpreadsheetNs + "c")
                .FirstOrDefault(cell => GetCellColumnIndex(cell, int.MaxValue) > columnIndex);

            if (insertBefore != null)
                insertBefore.AddBeforeSelf(newCell);
            else
                row.Add(newCell);

            return newCell;
        }

        private static XElement GetCell(XElement row, int columnIndex)
        {
            int fallbackColumn = 1;
            foreach (XElement cell in row.Elements(SpreadsheetNs + "c"))
            {
                if (GetCellColumnIndex(cell, fallbackColumn) == columnIndex)
                    return cell;

                fallbackColumn++;
            }

            return null;
        }

        private static string GetCellText(XElement cell, string[] sharedStrings)
        {
            if (cell == null)
                return "";

            string cellType = (string)cell.Attribute("t");
            if (cellType == "inlineStr")
                return string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(node => node.Value));

            XElement valueNode = cell.Element(SpreadsheetNs + "v");
            string value = valueNode?.Value ?? "";
            if (cellType == "s" && int.TryParse(value, out int sharedStringIndex) && sharedStringIndex >= 0 &&
                sharedStringIndex < sharedStrings.Length)
                return sharedStrings[sharedStringIndex];

            return value;
        }

        private static void SetCellText(XElement cell, string value)
        {
            XAttribute style = cell.Attribute("s");
            XAttribute cellReference = cell.Attribute("r");

            cell.RemoveNodes();
            cell.RemoveAttributes();

            if (cellReference != null)
                cell.Add(new XAttribute("r", cellReference.Value));
            if (style != null)
                cell.Add(new XAttribute("s", style.Value));

            cell.Add(new XAttribute("t", "inlineStr"));
            cell.Add(new XElement(
                SpreadsheetNs + "is",
                new XElement(SpreadsheetNs + "t", value ?? "")));
        }

        private static string[] ReadSharedStrings(ZipArchive archive)
        {
            ZipArchiveEntry sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
            if (sharedStringsEntry == null)
                return Array.Empty<string>();

            using var reader = new StreamReader(sharedStringsEntry.Open(), Encoding.UTF8, true);
            XDocument document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
            return document
                .Descendants(SpreadsheetNs + "si")
                .Select(item => string.Concat(item.Descendants(SpreadsheetNs + "t").Select(text => text.Value)))
                .ToArray();
        }

        private static bool IsWorksheetEntry(ZipArchiveEntry entry)
        {
            string path = entry.FullName.Replace("\\", "/");
            return path.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase) &&
                   path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
        }

        private static void CopyEntryMetadata(ZipArchiveEntry source, ZipArchiveEntry destination)
        {
            destination.LastWriteTime = source.LastWriteTime;
        }

        private static int GetCellColumnIndex(XElement cell, int fallbackColumn)
        {
            string cellReference = (string)cell.Attribute("r");
            if (string.IsNullOrEmpty(cellReference))
                return fallbackColumn;

            int columnIndex = 0;
            foreach (char c in cellReference)
            {
                if (!char.IsLetter(c))
                    break;

                columnIndex = columnIndex * 26 + char.ToUpperInvariant(c) - 'A' + 1;
            }

            return columnIndex > 0 ? columnIndex : fallbackColumn;
        }

        private static int GetRowIndex(XElement row)
        {
            if (int.TryParse((string)row.Attribute("r"), out int rowIndex))
                return rowIndex;

            XElement previousRow = row.ElementsBeforeSelf(SpreadsheetNs + "row").LastOrDefault();
            return previousRow == null ? 1 : GetRowIndex(previousRow) + 1;
        }

        private static int GetNextRowIndex(XElement sheetData)
        {
            int maxRowIndex = 0;
            foreach (XElement row in sheetData.Elements(SpreadsheetNs + "row"))
                maxRowIndex = Math.Max(maxRowIndex, GetRowIndex(row));

            return maxRowIndex + 1;
        }

        private static void UpdateDimension(XDocument document, XElement sheetData)
        {
            XElement dimension = document.Root?.Element(SpreadsheetNs + "dimension");
            if (dimension == null)
                return;

            int maxRowIndex = 1;
            int maxColumnIndex = 1;
            foreach (XElement row in sheetData.Elements(SpreadsheetNs + "row"))
            {
                maxRowIndex = Math.Max(maxRowIndex, GetRowIndex(row));
                foreach (XElement cell in row.Elements(SpreadsheetNs + "c"))
                    maxColumnIndex = Math.Max(maxColumnIndex, GetCellColumnIndex(cell, 1));
            }

            dimension.SetAttributeValue("ref",
                $"A1:{OpenXmlWorkbookBuilder.GetColumnName(maxColumnIndex)}{maxRowIndex}");
        }
    }
}
#endif
