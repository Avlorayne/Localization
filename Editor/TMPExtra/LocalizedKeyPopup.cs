#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using static LocalizedTextMeshProTool;

internal sealed class LocalizedKeyPopup : PopupWindowContent
{
    private readonly Action<LocalizedKeyOption> onSelected;
    private readonly Action repaint;
    private readonly List<LocalizedKeyOption> visibleOptions = new();

    private string currentNamespace = "";
    private string searchText = "";
    private Vector2 scrollPosition;
    private bool searchFieldFocused;

    private string tooltipToDraw;
    private Vector2 tooltipMousePos;

    private const float PopupWidth = 520f;
    private const float PopupHeight = 450f;
    private const float SearchRowHeight = 26f;
    private const float TitleRowHeight = 26f;
    private const float TopBarHeight = SearchRowHeight + TitleRowHeight;
    private const float RowHeight = 26f;
    private const float NamespaceColWidth = 90f;
    private const float KeyColWidth = 140f;

    // 高亮色（黄色，加粗）
    private const string HighlightColor = "#FFD54F";

    private static GUIStyle rowNormalStyle;
    private static GUIStyle rowHoverStyle;
    private static GUIStyle namespaceStyle;
    private static GUIStyle keyStyle;
    private static GUIStyle previewStyle;
    private static GUIStyle tooltipStyle;

    private static GUIContent arrowRightIcon;
    private static GUIContent arrowLeftIcon;

    public LocalizedKeyPopup(Action<LocalizedKeyOption> onSelected, Action repaint)
    {
        this.onSelected = onSelected;
        this.repaint = repaint;
    }

    public override Vector2 GetWindowSize()
    {
        return new Vector2(PopupWidth, PopupHeight);
    }

    public override void OnOpen()
    {
        searchFieldFocused = false;
        InitStyles();
        InitIcons();
    }

    private static void InitStyles()
    {
        if (rowNormalStyle != null) return;

        const int fontSize = 12;

        rowNormalStyle = new GUIStyle(EditorStyles.label)
        {
            padding = new RectOffset(8, 8, 0, 0),
            alignment = TextAnchor.MiddleLeft,
            fontSize = fontSize
        };

        rowHoverStyle = new GUIStyle(rowNormalStyle);
        Texture2D hoverTex = new Texture2D(1, 1);
        hoverTex.SetPixel(0, 0, EditorGUIUtility.isProSkin
            ? new Color(0.24f, 0.37f, 0.58f, 0.6f)
            : new Color(0.24f, 0.37f, 0.58f, 0.4f));
        hoverTex.Apply();
        rowHoverStyle.normal.background = hoverTex;

        // 关键：所有会高亮的样式都打开 richText
        namespaceStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = fontSize,
            alignment = TextAnchor.MiddleLeft,
            richText = true
        };
        namespaceStyle.normal.textColor = EditorGUIUtility.isProSkin
            ? new Color(0.6f, 0.6f, 0.6f)
            : new Color(0.4f, 0.4f, 0.4f);

        keyStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = fontSize,
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            richText = true
        };
        keyStyle.normal.textColor = EditorGUIUtility.isProSkin
            ? new Color(0.4f, 0.7f, 1f)
            : new Color(0.1f, 0.4f, 0.8f);

        previewStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = fontSize,
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip,
            richText = true
        };
        previewStyle.normal.textColor = EditorGUIUtility.isProSkin
            ? new Color(0.85f, 0.85f, 0.85f)
            : Color.black;

        tooltipStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 12,
            wordWrap = true,
            padding = new RectOffset(6, 6, 6, 6),
            alignment = TextAnchor.MiddleLeft,
            richText = true
        };
        tooltipStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

        const int radius = 1;
        const int texSize = radius * 2 + 4;
        Color tooltipBgColor = EditorGUIUtility.isProSkin
            ? new Color(0.16f, 0.16f, 0.16f, 1f)
            : new Color(0.94f, 0.94f, 0.94f, 1f);

        tooltipStyle.normal.background = CreateRoundedRectTexture(texSize, radius, tooltipBgColor);
        tooltipStyle.border = new RectOffset(radius, radius, radius, radius);
    }

    /// <summary>
    /// 把 source 中所有匹配 query 的片段（不区分大小写）包裹为黄色加粗。
    /// query 为空时原样返回。
    /// </summary>
    private static string HighlightMatch(string source, string query)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(query))
            return source;

        var sb = new StringBuilder(source.Length + 32);
        int startIndex = 0;
        int queryLen = query.Length;

        while (startIndex < source.Length)
        {
            int matchIndex = source.IndexOf(query, startIndex, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0)
            {
                sb.Append(source, startIndex, source.Length - startIndex);
                break;
            }

            // 添加匹配前的部分
            if (matchIndex > startIndex)
                sb.Append(source, startIndex, matchIndex - startIndex);

            // 添加高亮部分
            sb.Append("<color=").Append(HighlightColor).Append("><b>");
            sb.Append(source, matchIndex, queryLen);
            sb.Append("</b></color>");

            startIndex = matchIndex + queryLen;
        }

        return sb.ToString();
    }

    private static Texture2D CreateRoundedRectTexture(int size, int radius, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = 1f;

                int cx = -1, cy = -1;
                if (x < radius && y < radius)
                {
                    cx = radius;
                    cy = radius;
                }
                else if (x >= size - radius && y < radius)
                {
                    cx = size - radius - 1;
                    cy = radius;
                }
                else if (x < radius && y >= size - radius)
                {
                    cx = radius;
                    cy = size - radius - 1;
                }
                else if (x >= size - radius && y >= size - radius)
                {
                    cx = size - radius - 1;
                    cy = size - radius - 1;
                }

                if (cx >= 0)
                {
                    float dx = x - cx + 0.5f;
                    float dy = y - cy + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    alpha = Mathf.Clamp01(radius - dist + 0.5f);
                }

                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    private static void InitIcons()
    {
        if (arrowRightIcon == null)
        {
            Texture2D rightTex = EditorGUIUtility.FindTexture("d_ArrowRight")
                                 ?? EditorGUIUtility.FindTexture("d_Forward");
            arrowRightIcon = rightTex != null ? new GUIContent(rightTex) : new GUIContent(">");
        }

        if (arrowLeftIcon == null)
        {
            Texture2D leftTex = EditorGUIUtility.FindTexture("d_ArrowLeft")
                                ?? EditorGUIUtility.FindTexture("d_Back");
            arrowLeftIcon = leftTex != null ? new GUIContent(leftTex) : new GUIContent("<");
        }
    }

    public override void OnGUI(Rect rect)
    {
        tooltipMousePos = Event.current.mousePosition;
        tooltipToDraw = null;

        DrawTopBar(rect);

        Rect listRect = new Rect(rect.x, TopBarHeight, rect.width,
            Mathf.Max(0f, rect.height - TopBarHeight));
        DrawList(listRect);

        if (!string.IsNullOrEmpty(tooltipToDraw))
        {
            float maxWidth = Mathf.Min(280f, rect.width - 12f);
            tooltipStyle.fixedWidth = 0f;

            GUIContent content = new GUIContent(tooltipToDraw);
            Vector2 size = tooltipStyle.CalcSize(content);

            float width = Mathf.Min(size.x, maxWidth);
            if (size.x > maxWidth)
            {
                width = maxWidth;
                size.y = tooltipStyle.CalcHeight(content, maxWidth);
            }

            float height = size.y;

            float x = tooltipMousePos.x + 15f;
            float y = tooltipMousePos.y + 15f;

            if (x + width > rect.width - 4f) x = tooltipMousePos.x - width - 15f;
            if (y + height > rect.height - 4f) y = tooltipMousePos.y - height - 15f;

            x = Mathf.Clamp(x, 4f, rect.width - width - 4f);
            y = Mathf.Clamp(y, 4f, rect.height - height - 4f);

            Rect tooltipRect = new Rect(x, y, width, height);
            GUI.Box(tooltipRect, tooltipToDraw, tooltipStyle);
        }
    }

    private void DrawTopBar(Rect rect)
    {
        Rect topBarRect = new Rect(rect.x, rect.y, rect.width, TopBarHeight);
        GUI.Box(topBarRect, GUIContent.none, EditorStyles.toolbar);

        Rect searchRowRect = new Rect(topBarRect.x, topBarRect.y, topBarRect.width, SearchRowHeight);
        Rect searchRect = new Rect(searchRowRect.x + 4f, searchRowRect.y + 3f, searchRowRect.width - 8f,
            SearchRowHeight - 6f);

        EditorGUI.BeginChangeCheck();
        GUI.SetNextControlName("LocalizedKeySearch");
        searchText = EditorGUI.TextField(searchRect, searchText, EditorStyles.toolbarSearchField);
        if (EditorGUI.EndChangeCheck())
            scrollPosition = Vector2.zero;

        if (!searchFieldFocused)
        {
            EditorGUI.FocusTextInControl("LocalizedKeySearch");
            searchFieldFocused = true;
        }

        Rect separatorRect = new Rect(topBarRect.x, topBarRect.y + SearchRowHeight, topBarRect.width, 1f);
        EditorGUI.DrawRect(separatorRect, EditorGUIUtility.isProSkin
            ? new Color(0f, 0f, 0f, 0.35f)
            : new Color(0f, 0f, 0f, 0.15f));

        Rect titleRowRect = new Rect(topBarRect.x, topBarRect.y + SearchRowHeight + 1f,
            topBarRect.width, TitleRowHeight - 1f);

        float titleLeftOffset = 10f;

        if (!string.IsNullOrEmpty(currentNamespace) && string.IsNullOrEmpty(searchText))
        {
            Rect backRect = new Rect(titleRowRect.x + 4f, titleRowRect.y + 3f, 24f, titleRowRect.height - 6f);
            if (GUI.Button(backRect, arrowLeftIcon, EditorStyles.toolbarButton))
            {
                currentNamespace = "";
                scrollPosition = Vector2.zero;
                GUIUtility.ExitGUI();
            }

            titleLeftOffset = 32f;
        }

        Rect refreshRect = new Rect(titleRowRect.xMax - 76f, titleRowRect.y + 3f, 72f, titleRowRect.height - 6f);
        bool wantRefresh = GUI.Button(refreshRect, "刷新缓存", EditorStyles.toolbarButton);

        string title = string.IsNullOrEmpty(searchText)
            ? (string.IsNullOrEmpty(currentNamespace) ? "所有命名空间 (Namespaces)" : $"命名空间: {currentNamespace}")
            : $"搜索: {searchText}";
        Rect titleRect = new Rect(
            titleRowRect.x + titleLeftOffset,
            titleRowRect.y,
            refreshRect.x - (titleRowRect.x + titleLeftOffset) - 4f,
            titleRowRect.height);
        GUI.Label(titleRect, title, EditorStyles.boldLabel);

        if (wantRefresh)
        {
            LoadKeys(true);
            scrollPosition = Vector2.zero;
            repaint?.Invoke();
            GUIUtility.ExitGUI();
        }
    }

    private void DrawList(Rect rect)
    {
        visibleOptions.Clear();
        if (string.IsNullOrEmpty(searchText))
        {
            if (string.IsNullOrEmpty(currentNamespace))
            {
                DrawNamespaceList(rect);
                return;
            }

            if (OptionsByNamespace.TryGetValue(currentNamespace, out List<LocalizedKeyOption> namespaceOptions))
                visibleOptions.AddRange(namespaceOptions);
        }
        else
        {
            foreach (LocalizedKeyOption option in AllOptions)
            {
                if (ContainsIgnoreCase(option.NamespaceId, searchText) ||
                    ContainsIgnoreCase(option.Key, searchText) ||
                    ContainsIgnoreCase(option.Preview, searchText) ||
                    ContainsIgnoreCase(option.Comment, searchText))
                {
                    visibleOptions.Add(option);
                }
            }
        }

        DrawOptionList(rect);
    }

    private void DrawNamespaceList(Rect rect)
    {
        if (cachedNamespaces.Length == 0)
        {
            GUI.Label(rect, "没有找到任何命名空间", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        float contentHeight = cachedNamespaces.Length * RowHeight;
        Rect contentRect = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(rect.height, contentHeight));
        scrollPosition = GUI.BeginScrollView(rect, scrollPosition, contentRect);

        for (int i = 0; i < cachedNamespaces.Length; i++)
        {
            string namespaceId = cachedNamespaces[i];
            Rect rowRect = new Rect(4f, i * RowHeight, contentRect.width - 8f, RowHeight);

            bool isHover = rowRect.Contains(Event.current.mousePosition);
            GUIStyle style = isHover ? rowHoverStyle : rowNormalStyle;

            if (Event.current.type == EventType.Repaint)
                style.Draw(rowRect, false, false, false, false);

            GUI.Label(new Rect(rowRect.x + 8, rowRect.y, rowRect.width - 32, rowRect.height),
                namespaceId, keyStyle);

            Rect arrowRect = new Rect(rowRect.xMax - 24f, rowRect.y, 20f, rowRect.height);
            GUI.Label(arrowRect, arrowRightIcon, EditorStyles.centeredGreyMiniLabel);

            if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
            {
                currentNamespace = namespaceId;
                scrollPosition = Vector2.zero;
                GUIUtility.ExitGUI();
            }
        }

        GUI.EndScrollView();
    }

    private void DrawOptionList(Rect rect)
    {
        if (visibleOptions.Count == 0)
        {
            string tip = string.IsNullOrEmpty(searchText) ? "该命名空间下没有键值" : "没有匹配的搜索结果";
            GUI.Label(rect, tip, EditorStyles.centeredGreyMiniLabel);
            return;
        }

        bool hasSearch = !string.IsNullOrEmpty(searchText);

        float contentHeight = visibleOptions.Count * RowHeight;
        Rect contentRect = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(rect.height, contentHeight));
        scrollPosition = GUI.BeginScrollView(rect, scrollPosition, contentRect);

        for (int i = 0; i < visibleOptions.Count; i++)
        {
            LocalizedKeyOption option = visibleOptions[i];
            Rect rowRect = new Rect(4f, i * RowHeight, contentRect.width - 8f, RowHeight);

            bool isHover = rowRect.Contains(Event.current.mousePosition);
            GUIStyle style = isHover ? rowHoverStyle : rowNormalStyle;

            if (Event.current.type == EventType.Repaint)
                style.Draw(rowRect, false, false, false, false);

            float nsWidth = NamespaceColWidth;
            float keyWidth = KeyColWidth;
            float previewWidth = rowRect.width - nsWidth - keyWidth - 16f;

            // 有搜索时做高亮，否则原样显示
            string nsText = hasSearch ? HighlightMatch(option.NamespaceId, searchText) : option.NamespaceId;
            string keyText = hasSearch ? HighlightMatch(option.Key, searchText) : option.Key;
            string previewText = string.IsNullOrEmpty(option.Preview)
                ? "-"
                : (hasSearch ? HighlightMatch(option.Preview, searchText) : option.Preview);

            GUI.Label(new Rect(rowRect.x + 8, rowRect.y, nsWidth, rowRect.height),
                nsText, namespaceStyle);
            GUI.Label(new Rect(rowRect.x + 8 + nsWidth, rowRect.y, keyWidth, rowRect.height),
                keyText, keyStyle);
            GUI.Label(new Rect(rowRect.x + 8 + nsWidth + keyWidth, rowRect.y, previewWidth, rowRect.height),
                previewText, previewStyle);

            if (isHover && !string.IsNullOrEmpty(option.Comment))
            {
                tooltipToDraw = option.Comment;
            }

            if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
            {
                onSelected?.Invoke(option);
                repaint?.Invoke();
                editorWindow?.Close();
                GUIUtility.ExitGUI();
            }
        }

        GUI.EndScrollView();
    }
}

#endif
