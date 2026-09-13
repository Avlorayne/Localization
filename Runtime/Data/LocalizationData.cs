using System;
using System.Linq;

namespace Localization
{
    [Serializable]
    public struct LocalizationText
    {
        public string languageCode;
        public string text;
    }

    [Serializable]
    public struct LocalizationData : IEquatable<LocalizationData>
    {
        public string key;
        public LocalizationText[] texts;
        public string comment;

        public bool TryGet(string languageCode, string defaultLang, out string result)
        {
            result = string.Empty;
            if (texts == null || texts.Length == 0)
                return false;
            // 回退
            foreach (var localizationText in texts)
            {
                if (localizationText.languageCode == defaultLang)
                    result = localizationText.text;
            }

            // 查找
            foreach (var localizationText in texts)
            {
                if (localizationText.languageCode != languageCode) continue;
                result = localizationText.text;
                return true;
            }

            return false;
        }

        public bool Equals(LocalizationData other)
        {
            return key == other.key && Equals(texts, other.texts) && comment == other.comment;
        }

        public override bool Equals(object obj)
        {
            return obj is LocalizationData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(key, texts, comment);
        }
    }
}