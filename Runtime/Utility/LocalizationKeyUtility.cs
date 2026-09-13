namespace Localization
{
    public static class LocalizationKeyUtility
    {
        public static bool TryNormalizeLocalizationKey(string key, out string normalizedKey,
            bool allowDisplayKeys = false)
        {
            normalizedKey = "";
            if (string.IsNullOrWhiteSpace(key))
                return false;

            key = key.Trim();
            int openParen = key.IndexOf('(');
            if (openParen >= 0)
            {
                int closeParen = key.LastIndexOf(')');
                string bareKey = key.Substring(0, openParen).Trim();
                if (closeParen > openParen &&
                    string.IsNullOrWhiteSpace(key.Substring(closeParen + 1)) &&
                    IsValidBareLocalizationKey(bareKey))
                {
                    normalizedKey = bareKey;
                    return true;
                }
            }

            if (IsValidBareLocalizationKey(key) || (allowDisplayKeys && IsValidDisplayLocalizationKey(key)))
            {
                normalizedKey = key;
                return true;
            }

            return false;
        }

        public static bool IsValidLocalizationKey(string key, bool allowDisplayKeys = false)
        {
            return TryNormalizeLocalizationKey(key, out _, allowDisplayKeys);
        }

        public static bool IsValidDisplayLocalizationKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            key = key.Trim();
            // 长度下限 + 首字符大写/数字 + 排除 '='、空白、'/'、':'：
            // 避免把 TMP 富文本标签（如 <Color=red>、<Size=12>、<BR>）或普通单词误判为本地化键
            if (key.Length < 3)
                return false;

            if (!char.IsUpper(key[0]) && !char.IsDigit(key[0]))
                return false;

            foreach (char c in key)
            {
                if (c == '<' || c == '>' || c == '(' || c == ')' || c == ',' ||
                    c == '=' || c == '/' || c == ':' ||
                    c == '\r' || c == '\n' || char.IsWhiteSpace(c))
                    return false;
            }

            return true;
        }

        private static bool IsValidBareLocalizationKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            key = key.Trim();
            if (key.Length == 0 || key[0] < 'A' || key[0] > 'Z')
                return false;

            for (int i = 1; i < key.Length; i++)
            {
                char c = key[i];
                bool isUpperLetter = c is >= 'A' and <= 'Z';
                bool isDigit = c is >= '0' and <= '9';
                if (!isUpperLetter && !isDigit && c != '_')
                    return false;
            }

            return true;
        }
    }
}
