#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Localization;
using TMPro;
using UnityEditor;
using UnityEngine;

internal sealed class LocalizedTextMeshProTool
{
    internal static readonly List<LocalizedKeyOption> AllOptions = new();
    
    internal static readonly Dictionary<string, List<LocalizedKeyOption>> OptionsByNamespace =
        new(StringComparer.OrdinalIgnoreCase);
    
    internal static string[] cachedNamespaces = Array.Empty<string>();
    private static bool keysLoaded;
    private static double lastKeyLoadTime;
    
    private const double KeyCacheDuration = 5.0;
    
    private static GUIStyle statusLabelStyle;
    
    public void Draw(UnityEngine.Object[] targets, Action repaint)
    {
        LoadKeys();
    
        EditorGUILayout.Space(6f);
    
        if (statusLabelStyle == null)
        {
            statusLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 0, 0)
            };
        }
    
        const float rowHeight = 24f;
        const float btnWidth = 140f;
        const float btnHeight = 22f;
    
        Rect rowRect = EditorGUILayout.GetControlRect(false, rowHeight);
    
        Rect btnRect = new Rect(
            rowRect.x + (rowRect.width - btnWidth) * 0.5f,
            rowRect.y + (rowHeight - btnHeight) * 0.5f,
            btnWidth,
            btnHeight);
    
        Rect labelRect = new Rect(
            rowRect.x,
            rowRect.y,
            Mathf.Max(0f, btnRect.x - rowRect.x - 8f),
            rowHeight);
    
        string status = AllOptions.Count == 0
            ? "未找到本地化资源"
            : GetCurrentTargetsStatus(targets);
    
        GUI.Label(labelRect, status, statusLabelStyle);
    
        using (new EditorGUI.DisabledScope(AllOptions.Count == 0))
        {
            if (GUI.Button(btnRect, "选择 Key", EditorStyles.miniButton))
            {
                var dropdown = new LocalizedKeyPopup(
                    option =>
                    {
                        ApplyOption(targets, option);
                        repaint?.Invoke();
                    },
                    repaint);
                PopupWindow.Show(btnRect, dropdown);
            }
        }
    
        EditorGUILayout.Space(2f);
    }
    
    private static string GetCurrentTargetsStatus(UnityEngine.Object[] targets)
    {
        if (targets == null || targets.Length == 0) return "未选择任何对象";
    
        var tmpTexts = new List<TMP_Text>();
        foreach (var t in targets)
        {
            if (t is TMP_Text tmp) tmpTexts.Add(tmp);
        }
    
        if (tmpTexts.Count == 0) return "无 TMP 组件";
        if (tmpTexts.Count > 1) return $"已选择 {tmpTexts.Count} 个对象";
    
        string text = tmpTexts[0].text ?? "";
        if (string.IsNullOrEmpty(text)) return "文本为空";
    
        var placeholders = LocalizationTemplateParser.Parse(text);
        if (placeholders.Count > 0)
        {
            return $"已绑定: <{placeholders[0].NamespaceId}|{placeholders[0].Key}>";
        }
    
        string preview = text.Length > 15 ? text.Substring(0, 15) + "..." : text;
        return $"未绑定 ({preview})";
    }
    
    internal static void LoadKeys(bool force = false)
    {
        double now = EditorApplication.timeSinceStartup;
        if (!force && keysLoaded && now - lastKeyLoadTime < KeyCacheDuration)
            return;
    
        AllOptions.Clear();
        OptionsByNamespace.Clear();
    
        var namespaceSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateGuard = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        LanguageDataSO[] assets = Resources.LoadAll<LanguageDataSO>("Localization");
        string previewLanguage = GetPreviewLanguageCode();
    
        foreach (LanguageDataSO source in assets)
        {
            if (source == null || source.entries == null)
                continue;
    
            string namespaceId = source.NamespaceId?.Trim();
            if (string.IsNullOrEmpty(namespaceId))
                continue;
    
            namespaceSet.Add(namespaceId);
            if (!OptionsByNamespace.TryGetValue(namespaceId, out List<LocalizedKeyOption> namespaceOptions))
            {
                namespaceOptions = new List<LocalizedKeyOption>();
                OptionsByNamespace[namespaceId] = namespaceOptions;
            }
    
            foreach (LocalizationData entry in source.entries)
            {
                string key = entry.key?.Trim();
                if (string.IsNullOrEmpty(key))
                    continue;
    
                if (!duplicateGuard.Add($"{namespaceId}|{key}"))
                    continue;
    
                var option = new LocalizedKeyOption
                {
                    NamespaceId = namespaceId,
                    Key = key,
                    Preview = GetPreviewText(entry, previewLanguage),
                    Comment = entry.comment ?? ""
                };
    
                AllOptions.Add(option);
                namespaceOptions.Add(option);
            }
        }
    
        SortOptions(AllOptions);
        foreach (List<LocalizedKeyOption> namespaceOptions in OptionsByNamespace.Values)
            SortOptions(namespaceOptions);
    
        var namespaces = new List<string>(namespaceSet);
        namespaces.Sort(StringComparer.OrdinalIgnoreCase);
        cachedNamespaces = namespaces.ToArray();
        keysLoaded = true;
        lastKeyLoadTime = now;
    }
    
    private static void SortOptions(List<LocalizedKeyOption> options)
    {
        options.Sort((left, right) =>
        {
            int namespaceCompare = string.Compare(left.NamespaceId, right.NamespaceId,
                StringComparison.OrdinalIgnoreCase);
            return namespaceCompare != 0
                ? namespaceCompare
                : string.Compare(left.Key, right.Key, StringComparison.OrdinalIgnoreCase);
        });
    }
    
    private static string GetPreviewLanguageCode()
    {
        return Application.systemLanguage switch
        {
            SystemLanguage.ChineseSimplified => "zh-Hans",
            SystemLanguage.ChineseTraditional => "zh-Hant",
            SystemLanguage.Japanese => "ja",
            SystemLanguage.Korean => "ko",
            _ => "en"
        };
    }
    
    private static string GetPreviewText(LocalizationData data, string languageCode)
    {
        if (data.texts == null)
            return "";
    
        string english = "";
        string first = "";
        foreach (LocalizationText text in data.texts)
        {
            if (string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(text.text))
                first = text.text;
    
            if (string.Equals(text.languageCode, languageCode, StringComparison.OrdinalIgnoreCase))
                return text.text ?? "";
    
            if (string.Equals(text.languageCode, "en", StringComparison.OrdinalIgnoreCase))
                english = text.text ?? "";
        }
    
        return string.IsNullOrEmpty(english) ? first : english;
    }
    
    private void ApplyOption(UnityEngine.Object[] targets, LocalizedKeyOption option)
    {
        if (option == null)
            return;
    
        string placeholder = option.Placeholder;
        var changedObjects = new List<UnityEngine.Object>();
    
        foreach (UnityEngine.Object selectedTarget in targets)
        {
            if (selectedTarget is TMP_Text tmp)
                changedObjects.Add(tmp);
        }
    
        if (changedObjects.Count == 0)
            return;
    
        Undo.RecordObjects(changedObjects.ToArray(), "修改本地化键文本");
        foreach (UnityEngine.Object changedObject in changedObjects)
        {
            var tmp = (TMP_Text)changedObject;
            tmp.text = ApplyPlaceholder(tmp.text, placeholder);
            EditorUtility.SetDirty(tmp);
    
            if (PrefabUtility.IsPartOfPrefabInstance(tmp))
                PrefabUtility.RecordPrefabInstancePropertyModifications(tmp);
        }
    }
    
    private static string ApplyPlaceholder(string currentText, string placeholder)
    {
        string text = currentText ?? "";
        if (string.IsNullOrEmpty(placeholder))
        {
            return TryFindFirstPlaceholder(text, out _, out _, out int removeStart, out int removeLength)
                ? text.Remove(removeStart, removeLength)
                : text;
        }
    
        if (string.IsNullOrEmpty(text) || text == "New Text")
            return placeholder;
    
        return TryFindFirstPlaceholder(text, out _, out _, out int start, out int length)
            ? text.Remove(start, length).Insert(start, placeholder)
            : $"{text}{placeholder}";
    }
    
    private static bool TryFindFirstPlaceholder(
        string text,
        out string namespaceId,
        out string key,
        out int start,
        out int length)
    {
        namespaceId = "";
        key = "";
        start = -1;
        length = 0;
    
        IReadOnlyList<LocalizationPlaceholder> placeholders = LocalizationTemplateParser.Parse(text);
        if (placeholders.Count == 0)
            return false;
    
        LocalizationPlaceholder placeholder = placeholders[0];
        namespaceId = placeholder.NamespaceId;
        key = placeholder.Key;
        start = placeholder.StartIndex;
        length = placeholder.Length;
        return true;
    }
    
    internal static bool ContainsIgnoreCase(string source, string value)
    {
        return !string.IsNullOrEmpty(source) &&
               !string.IsNullOrEmpty(value) &&
               source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

#endif
