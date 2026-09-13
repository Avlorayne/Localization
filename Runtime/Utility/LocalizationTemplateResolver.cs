using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Localization
{
    /// <summary>
    /// 使用调用方提供的查找函数解析模板
    /// </summary>
    internal static class LocalizationTemplateResolver
    {
        public static string Resolve(string template, LanguageDefinition languageDefinition, LocalizationLookup lookup)
        {
            var placeholders = LocalizationTemplateParser.Parse(template);
            if(placeholders.Count == 0) return template;
            
            var subResultDict = new Dictionary<LocalizationPlaceholder, string>();
            foreach (var placeholder in placeholders)
            {
                if (!lookup.TryGetText(placeholder.NamespaceId,
                        placeholder.Key,
                        languageDefinition,
                        out var subtemplate)) 
                    continue;
                var args = placeholder.ArgumentText;
                var argResults = new string[args.Length];
                for (int i = 0; i < argResults.Length; i++)
                    argResults[i] = Resolve(args[i], languageDefinition, lookup);
                
                var subResult = ApplyArguments(subtemplate, argResults);
                subResultDict[placeholder] = subResult;
            }
            
            var resultBuilder = new StringBuilder(template);
            foreach (var placeholder in placeholders)
            {
                if(subResultDict.TryGetValue(placeholder, out var subResult))
                    resultBuilder.Replace($"<{placeholder.RawContent}>", subResult);
            }
            
            return resultBuilder.ToString();
        }

        private static readonly Regex Regex = new (@"\{\s*(\d+)\s*\}", RegexOptions.Compiled, TimeSpan.FromSeconds(0.5));
        
        private static string ApplyArguments(string template, params string[] args)
        {
            return Regex.Replace(template, match =>
            {
                int index = int.Parse(match.Groups[1].Value);
                // 找不到对应参数时保留原占位符
                return (uint)index < (uint)args.Length ? args[index] : match.Value;
            });
        }
    }
}
