#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Localization.Editor.LocalizationEditorText;

namespace Localization.Editor
{
    internal static class LocalizationSourceExporter
    {
        public static readonly string[] SupportedExtensions = { ".xlsx", ".csv" };

        public static bool Export(List<LocalizationData> entries, string outputPath, bool logUnsupportedFormat = true)
        {
            string extension = Path.GetExtension(outputPath).ToLowerInvariant();

            if (extension == ".csv")
            {
                CsvParser.ExportToFile(entries, outputPath);
                return true;
            }

            if (extension == ".xlsx")
                return XlsxLocalizationExporter.ExportToFile(entries, outputPath);

            if (logUnsupportedFormat)
                Debug.LogError(F("log.unsupported.export", outputPath));

            return false;
        }

        public static bool IsSupported(string outputPath)
        {
            string extension = Path.GetExtension(outputPath).ToLowerInvariant();
            foreach (string supportedExtension in SupportedExtensions)
            {
                if (extension == supportedExtension)
                    return true;
            }

            return false;
        }
    }
}
#endif