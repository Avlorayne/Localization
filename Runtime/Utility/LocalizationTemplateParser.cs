using System;
using System.Collections.Generic;
using System.Globalization;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace Localization
{
    /// <summary>
    /// 使用 Superpower 语法扫描并解析本地化模板。该类型不访问 Unity 对象，可直接用于 EditMode 单元测试。
    /// </summary>
    public static class LocalizationTemplateParser
    {
        private sealed class ParsedPlaceholder
        {
            public ParsedPlaceholder(string namespaceId, string key, string[] argumentText)
            {
                NamespaceId = namespaceId;
                Key = key;
                ArgumentText = argumentText;
            }

            public string NamespaceId { get; }

            public string Key { get; }

            public string[] ArgumentText { get; }
        }

        private static readonly TextParser<Unit> QuotedStringGroup = CreateQuotedStringGroup();
        private static readonly TextParser<Unit> BalancedAngleGroup = CreateBalancedAngleGroup();
        private static readonly TextParser<Unit> BalancedParenthesisGroup = CreateBalancedParenthesisGroup();

        private static readonly TextParser<string> NamespaceParser =
            Span.MatchedBy(Character.ExceptIn('<', '>', '(', ')', '|').AtLeastOnce())
                .Select(span => span.ToStringValue().Trim())
                .Where(value => value.Length > 0, "localization namespace");

        private static readonly TextParser<string> NamespacePrefixParser =
            (from namespaceId in NamespaceParser
                from separator in Character.EqualTo('|')
                select namespaceId);

        private static readonly TextParser<string> KeyParser =
            Span.MatchedBy(Character.ExceptIn('<', '>', '(', ')', '|').AtLeastOnce())
                .Select(span => span.ToStringValue().Trim())
                .Where(IsLocalizationKey, "localization key");

        private static readonly TextParser<string[]> ArgumentGroupParser =
            Span.MatchedBy(BalancedParenthesisGroup)
                .Select(span => ParseArguments(span.Slice(1, span.Length - 2).ToStringValue()))
                .Where(arguments => arguments != null, "localization arguments");

        private static readonly TextParser<ParsedPlaceholder> PlaceholderParser =
            from openAngle in Character.EqualTo('<')
            from namespaceId in NamespacePrefixParser
            from key in KeyParser
            from argumentText in ArgumentGroupParser.OptionalOrDefault(Array.Empty<string>())
            from trailingWhitespace in Character.WhiteSpace.IgnoreMany()
            from closeAngle in Character.EqualTo('>')
            select new ParsedPlaceholder(namespaceId, key, argumentText);

        public static IReadOnlyList<LocalizationPlaceholder> Parse(string template)
        {
            if (string.IsNullOrEmpty(template))
                return Array.Empty<LocalizationPlaceholder>();

            var placeholders = new List<LocalizationPlaceholder>();
            var source = new TextSpan(template);
            int scanIndex = 0;
            while (scanIndex < template.Length)
            {
                if (template[scanIndex] != '<')
                {
                    scanIndex++;
                    continue;
                }

                TextSpan remaining = source.Skip(scanIndex);
                if (!TryParsePlaceholder(remaining, out ParsedPlaceholder parsed, out int length))
                {
                    scanIndex += TryParseBalancedAngleGroup(remaining, out int invalidLength)
                        ? invalidLength
                        : 1;
                    continue;
                }

                int endIndex = scanIndex + length - 1;
                placeholders.Add(new LocalizationPlaceholder(
                    scanIndex,
                    endIndex,
                    template.Substring(scanIndex + 1, length - 2),
                    parsed.NamespaceId,
                    parsed.Key,
                    parsed.ArgumentText));
                scanIndex = endIndex + 1;
            }

            return placeholders;
        }

        /// <summary>
        /// 检查模板任意嵌套层级中是否包含指定键，不执行本地化查找。
        /// </summary>
        public static bool ContainsPlaceholder(string template, string key)
        {
            if (string.IsNullOrEmpty(template) || string.IsNullOrEmpty(key))
                return false;

            var source = new TextSpan(template);
            for (int i = 0; i < template.Length; i++)
            {
                if (template[i] != '<')
                    continue;

                TextSpan remaining = source.Skip(i);
                if (!TryParsePlaceholder(remaining, out ParsedPlaceholder parsed, out _))
                {
                    if (TryParseBalancedAngleGroup(remaining, out int invalidLength))
                        i += invalidLength - 1;
                    continue;
                }

                if (string.Equals(parsed.Key, key, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string[] ParseArguments(string argumentText)
        {
            if (string.IsNullOrWhiteSpace(argumentText))
                return Array.Empty<string>();

            var arguments = new List<string>();
            int startIndex = 0;
            int angleDepth = 0;
            int parenthesisDepth = 0;
            bool inQuotedString = false;

            for (int i = 0; i < argumentText.Length; i++)
            {
                char current = argumentText[i];
                if (inQuotedString)
                {
                    if (current == '"')
                        inQuotedString = false;
                    continue;
                }

                switch (current)
                {
                    case '"':
                        inQuotedString = true;
                        break;
                    case '<':
                        angleDepth++;
                        break;
                    case '>':
                        if (angleDepth == 0)
                            return null;
                        angleDepth--;
                        break;
                    case '(':
                        parenthesisDepth++;
                        break;
                    case ')':
                        if (parenthesisDepth == 0)
                            return null;
                        parenthesisDepth--;
                        break;
                    case ',':
                        if (angleDepth == 0 && parenthesisDepth == 0)
                        {
                            if (!TryParseArgument(argumentText.Substring(startIndex, i - startIndex),
                                    out string argument))
                                return null;

                            arguments.Add(argument);
                            startIndex = i + 1;
                        }

                        break;
                }
            }

            if (inQuotedString || angleDepth != 0 || parenthesisDepth != 0 ||
                !TryParseArgument(argumentText.Substring(startIndex), out string finalArgument))
            {
                return null;
            }

            arguments.Add(finalArgument);
            return arguments.ToArray();
        }

        private static bool TryParseArgument(string rawArgument, out string argument)
        {
            argument = null;
            string value = rawArgument.Trim();
            if (value.Length == 0)
                return false;

            if (value[0] == '"')
            {
                if (value.Length < 2 || value[value.Length - 1] != '"')
                    return false;

                for (int i = 1; i < value.Length - 1; i++)
                {
                    if (value[i] == '"')
                        return false;
                }

                argument = value.Substring(1, value.Length - 2);
                return true;
            }

            if (value.IndexOf('"') >= 0)
                return false;

            if (TryParsePlaceholder(new TextSpan(value), out _, out int length) && length == value.Length)
            {
                argument = value;
                return true;
            }

            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                return false;

            argument = value;
            return true;
        }

        private static bool TryParsePlaceholder(
            TextSpan input,
            out ParsedPlaceholder placeholder,
            out int length)
        {
            Result<ParsedPlaceholder> result = PlaceholderParser(input);
            if (!result.HasValue)
            {
                placeholder = null;
                length = 0;
                return false;
            }

            placeholder = result.Value;
            length = input.Length - result.Remainder.Length;
            return true;
        }

        private static bool TryParseBalancedAngleGroup(TextSpan input, out int length)
        {
            Result<Unit> result = BalancedAngleGroup(input);
            if (!result.HasValue)
            {
                length = 0;
                return false;
            }

            length = input.Length - result.Remainder.Length;
            return true;
        }

        private static TextParser<Unit> CreateBalancedAngleGroup()
        {
            TextParser<Unit> angleGroup = null;
            TextParser<Unit> content = Superpower.Parse.OneOf(
                QuotedStringGroup,
                Superpower.Parse.Ref(() => angleGroup),
                Character.ExceptIn('<', '>', '"').Value(Unit.Value));

            angleGroup =
                from openAngle in Character.EqualTo('<')
                from items in content.Many()
                from closeAngle in Character.EqualTo('>')
                select Unit.Value;
            return angleGroup;
        }

        private static TextParser<Unit> CreateBalancedParenthesisGroup()
        {
            TextParser<Unit> parenthesisGroup = null;
            TextParser<Unit> content = Superpower.Parse.OneOf(
                QuotedStringGroup,
                BalancedAngleGroup,
                Superpower.Parse.Ref(() => parenthesisGroup),
                Character.ExceptIn('<', '>', '(', ')', '"').Value(Unit.Value));

            parenthesisGroup =
                from openParenthesis in Character.EqualTo('(')
                from items in content.Many()
                from closeParenthesis in Character.EqualTo(')')
                select Unit.Value;
            return parenthesisGroup;
        }

        private static TextParser<Unit> CreateQuotedStringGroup()
        {
            return
                from openQuote in Character.EqualTo('"')
                from content in Character.ExceptIn('"').Value(Unit.Value).Many()
                from closeQuote in Character.EqualTo('"')
                select Unit.Value;
        }

        private static bool IsLocalizationKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();
            if (value.StartsWith("MISSING:", StringComparison.Ordinal) ||
                value.StartsWith("EMPTY:", StringComparison.Ordinal))
            {
                return false;
            }

            return LocalizationKeyUtility.IsValidLocalizationKey(value, allowDisplayKeys: true);
        }
    }
}