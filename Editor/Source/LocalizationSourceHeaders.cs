#if UNITY_EDITOR
namespace Localization.Editor
{
    /// <summary>本地化源文件（CSV/XLSX）表头的语义判断。</summary>
    public static class LocalizationSourceHeaders
    {
        public static bool IsCommentHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return false;

            string normalized = header
                .Trim()
                .Replace(" ", "")
                .Replace("　", "")
                .Replace("（", "(")
                .Replace("）", ")")
                .ToLowerInvariant();

            return normalized == "comment" ||
                   normalized == "comments" ||
                   normalized == "note" ||
                   normalized == "notes" ||
                   normalized == "注释" ||
                   normalized == "备注" ||
                   normalized.Contains("comment");
        }
    }
}
#endif