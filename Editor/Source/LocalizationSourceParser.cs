#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Localization.Editor.LocalizationEditorText;

namespace Localization.Editor.Source
{
    internal static class LocalizationSourceParser
    {
        public static readonly string[] SupportedImportExtensions = { ".xlsx", ".csv" };

        public static List<LocalizationData> Import(string sourcePath, bool logUnsupportedFormat = true)
        {
            string extension = Path.GetExtension(sourcePath).ToLowerInvariant();

            if (extension == ".xlsx")
                return XlsxLocalizationParser.Import(sourcePath);

            if (extension == ".csv")
                return CsvParser.Import(sourcePath);

            if (logUnsupportedFormat)
                Debug.LogError(F("log.unsupported.source", sourcePath));

            return new List<LocalizationData>();
        }

        public static bool IsSupported(string sourcePath)
        {
            string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            foreach (string supportedExtension in SupportedImportExtensions)
            {
                if (extension == supportedExtension)
                    return true;
            }

            return false;
        }
    }
}
#endif