using System;
using System.Text;

namespace Localization
{
    /// <summary>
    /// 描述模板中一个已经通过语法校验的本地化占位符。
    /// </summary>
    public readonly struct LocalizationPlaceholder : IEquatable<LocalizationPlaceholder>
    {
        /// <summary>占位符左尖括号在原模板中的索引。</summary>
        public int StartIndex { get; }

        /// <summary>占位符右尖括号在原模板中的索引（包含该字符）。</summary>
        public int EndIndex { get; }

        /// <summary>尖括号内部未经裁剪的原始文本。</summary>
        public string RawContent { get; }

        /// <summary>占位符显式声明的命名空间。</summary>
        public string NamespaceId { get; }

        /// <summary>经过首尾空白裁剪的本地化键。</summary>
        public string Key { get; }

        /// <summary>解析后的参数文本数组；无参数时为空数组。</summary>
        public string[] ArgumentText { get; }

        public int Length => EndIndex - StartIndex + 1;
        
        public LocalizationPlaceholder(
            int startIndex,
            int endIndex,
            string rawContent,
            string namespaceId,
            string key,
            string[] argumentText)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            RawContent = rawContent ?? string.Empty;
            NamespaceId = namespaceId ?? string.Empty;
            Key = key ?? string.Empty;
            ArgumentText = argumentText ?? Array.Empty<string>();
        }

        public string Print()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"NamespaceID: {NamespaceId}\n");
            sb.Append($"Key: {Key}\n");
            sb.Append($"ArgumentText: [{string.Join(", ", ArgumentText)}]\n");
            return sb.ToString();
        }

        public bool Equals(LocalizationPlaceholder other)
        {
            return StartIndex == other.StartIndex && EndIndex == other.EndIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is LocalizationPlaceholder other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartIndex, EndIndex, RawContent, NamespaceId, Key, ArgumentText);
        }
    }
}
