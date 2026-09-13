#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Localization.Editor
{
    /// <summary>
    /// 从零构建最小化 XLSX 工作簿的 OOXML 构造器（与"补丁既有工作簿"逻辑分离）。
    /// 仅承载底层 OOXML/Zip 结构，不含本地化业务映射。
    /// </summary>
    internal static class OpenXmlWorkbookBuilder
    {
        public static bool CreateWorkbook(List<LocalizationData> entries, string xlsxPath)
        {
            using var stream = new FileStream(xlsxPath, FileMode.Create, FileAccess.Write);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

            WriteEntry(archive, "[Content_Types].xml", BuildContentTypesXml());
            WriteEntry(archive, "_rels/.rels", BuildRootRelationshipsXml());
            WriteEntry(archive, "xl/workbook.xml", BuildWorkbookXml());
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationshipsXml());
            WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(entries));
            return true;
        }

        public static string GetColumnName(int columnIndex)
        {
            var name = new StringBuilder();
            while (columnIndex > 0)
            {
                columnIndex--;
                name.Insert(0, (char)('A' + columnIndex % 26));
                columnIndex /= 26;
            }

            return name.ToString();
        }

        private static void WriteEntry(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(content);
        }

        private static string BuildContentTypesXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                   "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                   "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                   "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                   "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                   "</Types>";
        }

        private static string BuildRootRelationshipsXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                   "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                   "</Relationships>";
        }

        private static string BuildWorkbookXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                   "<sheets><sheet name=\"Localization\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                   "</workbook>";
        }

        private static string BuildWorkbookRelationshipsXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                   "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                   "</Relationships>";
        }

        private static string BuildWorksheetXml(List<LocalizationData> entries)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            string[] headers = LocalizationSourceSchema.StandardHeaders;
            sb.Append($"<dimension ref=\"A1:{GetColumnName(headers.Length)}{Math.Max(entries.Count + 1, 1)}\"/>");
            sb.Append("<sheetData>");
            AppendRow(sb, 1, headers);

            for (int i = 0; i < entries.Count; i++)
            {
                LocalizationData e = entries[i];
                var rowValues = new string[headers.Length];
                for (int column = 0; column < headers.Length; column++)
                    rowValues[column] = LocalizationSourceSchema.GetFieldValue(e, headers[column]);

                AppendRow(sb, i + 2, rowValues);
            }

            sb.Append("</sheetData>");
            sb.Append("</worksheet>");
            return sb.ToString();
        }

        private static void AppendRow(StringBuilder sb, int rowIndex, params string[] values)
        {
            sb.Append($"<row r=\"{rowIndex}\">");
            for (int i = 0; i < values.Length; i++)
            {
                string cellRef = $"{GetColumnName(i + 1)}{rowIndex}";
                sb.Append($"<c r=\"{cellRef}\" t=\"inlineStr\"><is><t>");
                sb.Append(EscapeXml(values[i]));
                sb.Append("</t></is></c>");
            }

            sb.Append("</row>");
        }

        private static string EscapeXml(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
#endif