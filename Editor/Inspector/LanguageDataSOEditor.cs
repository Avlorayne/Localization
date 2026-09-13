#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Localization.Editor.LocalizationEditorText;

namespace Localization.Editor
{
    [CustomEditor(typeof(LanguageDataSO))]
    public class LanguageDataSOEditor : UnityEditor.Editor
    {
        private VisualElement root;
        private ScrollView entriesScrollView;
        private VisualElement entriesContent;
        private VisualElement errorBox;
        private Label errorLabel;
        private Button normalizeInvalidKeysButton;
        private readonly List<VisualElement> entryCards = new List<VisualElement>();
        private int entryColumnCount = 1;
        private int visibleEntryCount;
        private float entriesLayoutWidth;
        private string searchFilter = "";
        private IVisualElementScheduledItem filterDebounce;
        private IVisualElementScheduledItem deferredInspectorErrorCheck;

        private LanguageDataSO Data => (LanguageDataSO)target;

        public override VisualElement CreateInspectorGUI()
        {
            LocalizationSourceSchema.InvalidateLanguageConfigCache();
            // 1. 创建根容器
            root = new VisualElement
            {
                usageHints = UsageHints.GroupTransform
            };
            root.AddToClassList("main-container");

            // 2. 载入 USS 样式表：相对本脚本所在目录解析，目录整体移动时不断链
            var ussAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(GetInspectorUssPath());
            if (ussAsset != null)
            {
                root.styleSheets.Add(ussAsset);
            }
            else
            {
                Debug.LogWarning("[Localization] LanguageDataSO.uss 未找到，Inspector 样式将缺失。");
            }

            // 3. 绘制 CSV 配置区域
            EnsureSourcePathFromLegacyCsv();

            var sourceTitle = new Label(T("source.configuration"));
            sourceTitle.AddToClassList("section-title");
            EditorUIPixelSnapper.MakePixelPerfect(sourceTitle);
            root.Add(sourceTitle);

            var sourceRow = new VisualElement();
            sourceRow.AddToClassList("toolbar-row");
            sourceRow.AddToClassList("source-row");

            // isDelayed：回车/失焦才提交，避免每敲一个字符就 SetDirty + 全资产落盘
            var sourceField = new TextField(T("source.file")) { value = Data.sourceFilePath, isDelayed = true };
            sourceField.AddToClassList("source-field");
            var sourceLabel = sourceField.Q<Label>();
            if (sourceLabel != null) EditorUIPixelSnapper.MakePixelPerfect(sourceLabel);

            sourceField.RegisterValueChangedCallback(evt =>
            {
                Data.sourceFilePath = evt.newValue;
                EditorUtility.SetDirty(Data);
                AssetDatabase.SaveAssets();
            });
            sourceRow.Add(sourceField);

            var btnBrowse = new Button(() => BrowseSourceFile(sourceField)) { text = T("browse") };
            btnBrowse.AddToClassList("modern-button");
            btnBrowse.AddToClassList("btn-browse");
            sourceRow.Add(btnBrowse);

            root.Add(sourceRow);

            var namespaceField = new TextField(T("key.namespace")) { value = Data.NamespaceId, isReadOnly = true };
            namespaceField.AddToClassList("source-field");
            namespaceField.AddToClassList("namespace-field");
            var namespaceLabel = namespaceField.Q<Label>();
            if (namespaceLabel != null) EditorUIPixelSnapper.MakePixelPerfect(namespaceLabel);
            root.Add(namespaceField);

            // 4. 绘制功能按钮组
            var toolbar = new VisualElement();
            toolbar.AddToClassList("toolbar-row");
            toolbar.AddToClassList("action-row");

            var btnImport = new Button(ImportSource) { text = T("import.source") };
            btnImport.AddToClassList("modern-button");
            btnImport.AddToClassList("btn-import");
            toolbar.Add(btnImport);

            var btnExport = new Button(ExportToFile) { text = T("export.source") };
            btnExport.AddToClassList("modern-button");
            btnExport.AddToClassList("btn-export");
            toolbar.Add(btnExport);

            root.Add(toolbar);

            // 5. 致命错误提示区域
            errorBox = new VisualElement();
            errorBox.AddToClassList("error-box");
            errorBox.style.display = DisplayStyle.None;
            errorLabel = new Label();
            errorLabel.AddToClassList("error-text");
            EditorUIPixelSnapper.MakePixelPerfect(errorLabel);
            errorBox.Add(errorLabel);
            normalizeInvalidKeysButton = new Button(NormalizeInvalidKeys) { text = T("normalize.invalid.keys") };
            normalizeInvalidKeysButton.AddToClassList("modern-button");
            normalizeInvalidKeysButton.AddToClassList("btn-normalize-keys");
            normalizeInvalidKeysButton.style.display = DisplayStyle.None;
            errorBox.Add(normalizeInvalidKeysButton);
            root.Add(errorBox);

            // 6. 新增条目按钮
            var btnAdd = new Button(AddNewEntry) { text = T("add.entry") };
            btnAdd.AddToClassList("modern-button");
            btnAdd.AddToClassList("btn-add");
            root.Add(btnAdd);

            // 7. 搜索过滤栏
            var filterField = new TextField(T("filter"));
            filterField.AddToClassList("filter-bar");

            var filterLabel = filterField.Q<Label>();
            if (filterLabel != null) EditorUIPixelSnapper.MakePixelPerfect(filterLabel);

            var filterInput = filterField.Q<VisualElement>("unity-text-input");
            if (filterInput != null) EditorUIPixelSnapper.MakePixelPerfect(filterInput);

            filterField.RegisterValueChangedCallback(evt =>
            {
                searchFilter = evt.newValue;
                // 300ms 防抖：用户停止输入后再刷新筛选结果
                filterDebounce?.Pause();
                filterDebounce = entriesScrollView.schedule.Execute(RefreshList).StartingIn(300);
            });
            root.Add(filterField);

            // 8. 滚动列表视图
            entriesScrollView = new ScrollView(ScrollViewMode.Vertical);
            entriesScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            entriesScrollView.AddToClassList("entries-scroll");
            entriesContent = entriesScrollView.contentContainer;
            entriesContent.AddToClassList("entries-grid");
            // flexDirection/alignItems 已由 USS .entries-grid 控制（flex-wrap 自动行布局）
            // 卡片实际排布在 contentViewport 中；不能使用 ScrollView 外框宽度，
            // 否则垂直滚动条出现时，最后一张卡会被挤到下一行。
            entriesScrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(evt =>
                UpdateEntryColumnLayout(evt.newRect.width));
            root.Add(entriesScrollView);

            // 推迟到下一帧执行初始构建，让 UI Toolkit 先完成布局算出 resolved 宽度
            // 避免首次布局用 fallback 宽度再由 GeometryChangedEvent 修正
            entriesScrollView.schedule.Execute(RefreshList).StartingIn(0);
            return root;
        }

        /// <summary>USS 与本脚本同目录，路径由脚本资产位置推导，避免硬编码绝对路径。</summary>
        private string GetInspectorUssPath()
        {
            string scriptDirectory =
                Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
            return Path.Combine(scriptDirectory ?? string.Empty, "LanguageDataSO.uss").Replace("\\", "/");
        }

        /// <summary>在相关字段失焦后刷新校验状态，绝不在输入法组合输入过程中重建 UI。</summary>
        private void ScheduleInspectorErrorCheck()
        {
            deferredInspectorErrorCheck?.Pause();
            deferredInspectorErrorCheck = entriesScrollView.schedule.Execute(() =>
            {
                if (Data == null || Data.entries == null) return;
                if (HasFocusedEntryTextField()) return;
                RefreshList();
            }).StartingIn(100);
        }

        private bool HasFocusedEntryTextField()
        {
            var focusedElement = root?.panel?.focusController?.focusedElement as VisualElement;
            if (focusedElement == null || entriesContent == null || !entriesContent.Contains(focusedElement))
                return false;

            return focusedElement is TextField ||
                   focusedElement.GetFirstAncestorOfType<TextField>() != null;
        }

        private void RefreshList()
        {
            entryCards.Clear();

            if (Data == null || Data.entries == null) return;

            var duplicateReports = LanguageDataSODuplicateKeyValidator.CollectReportsInvolving(Data);
            var duplicateKeys = duplicateReports
                .Select(report => report.Key)
                .ToHashSet(System.StringComparer.OrdinalIgnoreCase);
            var invalidKeyIssues = CollectInvalidKeyIssues();
            var invalidKeyFields = invalidKeyIssues
                .Select(issue => issue.EntryIndex)
                .ToHashSet();
            var contentKeyIssues = CollectContentKeyFieldIssues();
            var invalidContentFields = contentKeyIssues
                .Select(issue => BuildContentFieldKey(issue.EntryIndex, issue.LanguageCode))
                .ToHashSet(System.StringComparer.OrdinalIgnoreCase);

            UpdateInspectorErrors(duplicateReports, invalidKeyIssues, contentKeyIssues, true);

            if (duplicateReports.Count == 0 && invalidKeyIssues.Count == 0 && contentKeyIssues.Count == 0)
                LanguageDataSOAddressableRegistrar.TryRegisterIfValid(Data);

            List<int> filteredIndices = GetFilteredEntryIndices();
            visibleEntryCount = filteredIndices.Count;
            entryColumnCount =
                EntryCardLayout.CalculateColumnCount(GetEntryLayoutWidth(), visibleEntryCount, entryColumnCount);

            // 一次性清空 entriesContent，清除旧卡片
            entriesContent.Clear();

            float cardWidth = GetCurrentEntryCardWidth();
            for (int i = 0; i < filteredIndices.Count; i++)
            {
                int entryIndex = filteredIndices[i];
                var entry = Data.entries[entryIndex];
                var card = new VisualElement();
                card.AddToClassList("entry-card");
                ApplyEntryCardLayout(card, i, cardWidth);

                var header = new VisualElement();
                header.AddToClassList("entry-header");

                var indexLabel = new Label($"#{i + 1}");
                indexLabel.AddToClassList("entry-index");
                EditorUIPixelSnapper.MakePixelPerfect(indexLabel);
                header.Add(indexLabel);

                var btnDelete = new Button(() => DeleteEntry(entryIndex)) { text = T("delete") };
                btnDelete.AddToClassList("btn-delete");
                btnDelete.style.flexShrink = 0f;
                header.Add(btnDelete);
                card.Add(header);

                // Key 与语言内容只更新数据和脏标记；校验重建延后到相关字段失焦后执行。
                string trimmedEntryKey = entry.key?.Trim();
                bool hasDuplicateKeyIssue =
                    !string.IsNullOrEmpty(trimmedEntryKey) && duplicateKeys.Contains(trimmedEntryKey);
                bool hasInvalidKeyIssue = invalidKeyFields.Contains(entryIndex);
                SetupInputField(card, T("key"), entry.key, duplicateKeys, evt =>
                {
                    UpdateEntry(entryIndex, data =>
                    {
                        data.key = evt;
                        return data;
                    });
                }, true, hasDuplicateKeyIssue || hasInvalidKeyIssue, validateOnFocusOut: true);
                // 语言列由 LanguageConfigSO 经 LocalizationSourceSchema 驱动，新增语言无需增改这里的 UI 代码
                LocalizationLanguageColumn[] languageColumns = LocalizationSourceSchema.LanguageColumns;
                for (int c = 0; c < languageColumns.Length; c++)
                {
                    LocalizationLanguageColumn column = languageColumns[c];
                    bool hasContentKeyIssue =
                        invalidContentFields.Contains(BuildContentFieldKey(entryIndex, column.LanguageCode));
                    SetupInputField(card, column.DisplayName, column.GetValue(entry), duplicateKeys, evt =>
                    {
                        UpdateEntry(entryIndex, data =>
                        {
                            column.SetValue(ref data, evt);
                            return data;
                        });
                    }, highlightContentError: hasContentKeyIssue, validateOnFocusOut: true);
                }

                SetupInputField(card, T("comment"), entry.comment ?? "", duplicateKeys, evt =>
                {
                    UpdateEntry(entryIndex, data =>
                    {
                        data.comment = evt;
                        return data;
                    });
                });

                entryCards.Add(card);
                entriesContent.Add(card);
            }
        }

        /// <summary>
        /// 创建并高阶封装满足"像素完美"对齐的 TextField 组件
        /// </summary>
        private void SetupInputField(VisualElement parent, string labelText, string value,
            HashSet<string> duplicateKeys, System.Action<string> onValueChanged, bool isKeyField = false,
            bool highlightKeyError = false,
            bool highlightContentError = false,
            bool validateOnFocusOut = false)
        {
            var field = new TextField(labelText) { value = value };

            // 1. 强制对齐左侧 Label 标签
            var lbl = field.Q<Label>();
            if (lbl != null) EditorUIPixelSnapper.MakePixelPerfect(lbl);

            // 2. 核心狙击：定位并强行对齐 TextField 内部的真实文字输入渲染区
            var inputArea = field.Q<VisualElement>("unity-text-input");
            if (inputArea != null) EditorUIPixelSnapper.MakePixelPerfect(inputArea);

            field.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt.newValue));

            if (validateOnFocusOut)
                field.RegisterCallback<FocusOutEvent>(_ => ScheduleInspectorErrorCheck());

            if (isKeyField && highlightKeyError)
            {
                if (inputArea != null)
                {
                    inputArea.AddToClassList("invalid-key-input");
                }
                else
                {
                    field.AddToClassList("duplicate-key-field");
                    var duplicateOutline = new VisualElement { pickingMode = PickingMode.Ignore };
                    duplicateOutline.AddToClassList("duplicate-key-outline");
                    field.Insert(0, duplicateOutline);
                }
            }

            if (highlightContentError)
            {
                if (inputArea != null)
                {
                    inputArea.AddToClassList("invalid-content-input");
                }
                else
                {
                    field.AddToClassList("invalid-content-field");
                    var invalidOutline = new VisualElement { pickingMode = PickingMode.Ignore };
                    invalidOutline.AddToClassList("invalid-content-outline");
                    field.Insert(0, invalidOutline);
                }
            }

            parent.Add(field);
        }

        private void UpdateEntryColumnLayout(float width)
        {
            if (width <= 0f)
                return;

            entriesLayoutWidth = width;
            entryColumnCount = EntryCardLayout.CalculateColumnCount(width, GetVisibleEntryCount(), entryColumnCount);
            ApplyEntryCardWidth();
        }

        private int GetVisibleEntryCount()
        {
            if (Data == null || Data.entries == null)
                return visibleEntryCount;

            if (string.IsNullOrEmpty(searchFilter))
                return Data.entries.Count;

            return GetFilteredEntryIndices().Count;
        }

        private List<int> GetFilteredEntryIndices()
        {
            var result = new List<int>();
            if (Data == null || Data.entries == null)
                return result;

            for (int i = 0; i < Data.entries.Count; i++)
            {
                if (MatchesFilter(Data.entries[i]))
                    result.Add(i);
            }

            return result;
        }

        // 卡片作为 entriesContent 的直接子元素，flex-wrap 自动换行。
        // Unity 2022 的 flex column-gap 在这里不会稳定参与布局计算，
        // 所以列间距由每个 card 的 marginRight 显式控制。

        private void ApplyEntryCardWidth()
        {
            float cardWidth = GetCurrentEntryCardWidth();
            for (int i = 0; i < entryCards.Count; i++)
                ApplyEntryCardLayout(entryCards[i], i, cardWidth);
        }

        private void ApplyEntryCardLayout(VisualElement card, int index, float cardWidth)
        {
            int columns = Mathf.Max(1, entryColumnCount);
            bool isLastColumn = index % columns == columns - 1;

            card.style.flexGrow = 0f;
            card.style.flexShrink = 0f;
            card.style.width = cardWidth;
            card.style.marginRight = isLastColumn ? 0f : EntryCardLayout.ColumnGap;
            card.style.marginBottom = EntryCardLayout.ColumnGap;
        }

        private float GetCurrentEntryCardWidth()
        {
            int columns = Mathf.Max(1, entryColumnCount);
            float width = GetEntryLayoutWidth();
            return Mathf.Max(0f, EntryCardLayout.GetCardWidth(width, columns));
        }

        private float GetEntryLayoutWidth()
        {
            float width = entriesLayoutWidth > 0f
                ? entriesLayoutWidth
                : entriesScrollView?.contentViewport.resolvedStyle.width ?? 0f;
            if (width <= 0f || float.IsNaN(width))
                width = root?.resolvedStyle.width ?? 0f;
            if (width <= 0f || float.IsNaN(width))
                width = EntryCardLayout.SplitWidth;

            return width;
        }

        private void AddNewEntry()
        {
            Undo.RecordObject(Data, T("undo.add.entry"));
            string baseKey = "NEW_KEY_";
            string finalKey = baseKey + System.DateTime.Now.Ticks.ToString().Substring(10);
            int index = 1;

            while (Data.entries.Any(e => e.key.Equals(finalKey, System.StringComparison.OrdinalIgnoreCase)))
            {
                finalKey = $"{baseKey}{index}";
                index++;
            }

            var newEntry = new LocalizationData()
            {
                key = finalKey, texts = LocalizationSourceSchema.CreateEmptyTextArray(), comment = ""
            };
            Data.entries.Insert(0, newEntry);
            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets();
            searchFilter = "";
            RefreshList();
        }

        private void DeleteEntry(int entryIndex)
        {
            if (entryIndex < 0 || entryIndex >= Data.entries.Count)
                return;

            LocalizationData entry = Data.entries[entryIndex];
            if (EditorUtility.DisplayDialog(T("delete.entry.title"), F("delete.entry.message", entry.key), T("yes"),
                    T("no")))
            {
                Undo.RecordObject(Data, T("undo.delete.entry"));
                Data.entries.RemoveAt(entryIndex);
                EditorUtility.SetDirty(Data);
                AssetDatabase.SaveAssets();
                RefreshList();
            }
        }

        private void UpdateEntry(int entryIndex, System.Func<LocalizationData, LocalizationData> update)
        {
            if (entryIndex < 0 || entryIndex >= Data.entries.Count || update == null)
                return;

            Undo.RecordObject(Data, T("undo.edit.entry"));
            Data.entries[entryIndex] = update(Data.entries[entryIndex]);
            EditorUtility.SetDirty(Data);
        }

        private bool MatchesFilter(LocalizationData e)
        {
            if (string.IsNullOrEmpty(searchFilter)) return true;
            var comp = System.StringComparison.OrdinalIgnoreCase;
            return (e.key?.IndexOf(searchFilter, comp) ?? -1) >= 0 ||
                   LocalizationSourceSchema.LanguageColumns.Any(column =>
                       (column.GetValue(e)?.IndexOf(searchFilter, comp) ?? -1) >= 0) ||
                   (e.comment?.IndexOf(searchFilter, comp) ?? -1) >= 0;
        }

        private string lastDuplicateLogText;
        private string lastInvalidKeyLogText;
        private string lastContentKeyLogText;

        private void UpdateInspectorErrors(
            List<LanguageDataSODuplicateKeyValidator.DuplicateKeyReport> duplicateReports,
            List<KeyFieldIssue> invalidKeyIssues,
            List<ContentKeyFieldIssue> contentKeyIssues,
            bool logToConsole)
        {
            var errorTexts = new List<string>();

            if (duplicateReports.Count > 0)
            {
                string errorText = LanguageDataSODuplicateKeyValidator.BuildErrorText(duplicateReports);
                // 同一批重复键只在内容变化时记录一次，避免每次 RefreshList/防抖检查都刷屏
                if (logToConsole && !string.Equals(errorText, lastDuplicateLogText, System.StringComparison.Ordinal))
                {
                    lastDuplicateLogText = errorText;
                    Debug.LogError(errorText, Data);
                }

                errorTexts.Add(errorText);
            }
            else
            {
                lastDuplicateLogText = null;
            }

            if (invalidKeyIssues.Count > 0)
            {
                string errorText = BuildInvalidKeyErrorText(invalidKeyIssues);
                if (logToConsole && !string.Equals(errorText, lastInvalidKeyLogText, System.StringComparison.Ordinal))
                {
                    lastInvalidKeyLogText = errorText;
                    Debug.LogError(errorText, Data);
                }

                errorTexts.Add(errorText);
                normalizeInvalidKeysButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                lastInvalidKeyLogText = null;
                normalizeInvalidKeysButton.style.display = DisplayStyle.None;
            }

            if (contentKeyIssues.Count > 0)
            {
                string errorText = BuildContentKeyErrorText(contentKeyIssues);
                if (logToConsole && !string.Equals(errorText, lastContentKeyLogText, System.StringComparison.Ordinal))
                {
                    lastContentKeyLogText = errorText;
                    Debug.LogError(errorText, Data);
                }

                errorTexts.Add(errorText);
            }
            else
            {
                lastContentKeyLogText = null;
            }

            if (errorTexts.Count > 0)
            {
                errorLabel.text = string.Join("\n\n", errorTexts);
                errorBox.style.display = DisplayStyle.Flex;
                return;
            }

            errorBox.style.display = DisplayStyle.None;
            normalizeInvalidKeysButton.style.display = DisplayStyle.None;
        }

        private List<KeyFieldIssue> CollectInvalidKeyIssues()
        {
            var result = new List<KeyFieldIssue>();
            if (Data == null || Data.entries == null)
                return result;

            for (int entryIndex = 0; entryIndex < Data.entries.Count; entryIndex++)
            {
                string key = Data.entries[entryIndex].key ?? "";
                if (!IsValidKeyLiteral(key))
                    result.Add(new KeyFieldIssue(entryIndex, key));
            }

            return result;
        }

        private static bool IsValidKeyLiteral(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                bool isUpperLetter = c is >= 'A' and <= 'Z';
                bool isDigit = c is >= '0' and <= '9';
                if (!isUpperLetter && !isDigit && c != '_')
                    return false;
            }

            return true;
        }

        private void NormalizeInvalidKeys()
        {
            if (Data == null || Data.entries == null)
                return;

            bool changed = false;
            Undo.RecordObject(Data, T("undo.normalize.invalid.keys"));
            for (int i = 0; i < Data.entries.Count; i++)
            {
                LocalizationData entry = Data.entries[i];
                string normalizedKey = NormalizeKeyLiteral(entry.key);
                if (string.Equals(entry.key, normalizedKey, System.StringComparison.Ordinal))
                    continue;

                entry.key = normalizedKey;
                Data.entries[i] = entry;
                changed = true;
            }

            if (!changed)
                return;

            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets();
            RefreshList();
        }

        private static string NormalizeKeyLiteral(string key)
        {
            if (string.IsNullOrEmpty(key))
                return "";

            var result = new StringBuilder(key.Length);
            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                if (char.IsWhiteSpace(c))
                    continue;

                if (c is >= 'a' and <= 'z')
                    c = (char)(c - 'a' + 'A');

                result.Append(c);
            }

            return result.ToString();
        }

        private List<ContentKeyFieldIssue> CollectContentKeyFieldIssues()
        {
            var result = new List<ContentKeyFieldIssue>();
            if (Data == null || Data.entries == null)
                return result;

            LocalizationLanguageColumn[] columns = LocalizationSourceSchema.LanguageColumns;
            for (int entryIndex = 0; entryIndex < Data.entries.Count; entryIndex++)
            {
                LocalizationData entry = Data.entries[entryIndex];
                foreach (LocalizationLanguageColumn column in columns)
                {
                    string text = column.GetValue(entry);
                    if (string.IsNullOrEmpty(text))
                        continue;

                    foreach (LocalizationPlaceholder placeholder in LocalizationTemplateParser.Parse(text))
                    {
                        result.Add(new ContentKeyFieldIssue(
                            entryIndex,
                            entry.key,
                            column.LanguageCode,
                            column.DisplayName,
                            placeholder));
                    }
                }
            }

            return result;
        }

        private static string BuildContentFieldKey(int entryIndex, string languageCode)
        {
            return $"{entryIndex}:{languageCode ?? string.Empty}";
        }

        private static string BuildInvalidKeyErrorText(List<KeyFieldIssue> issues)
        {
            const int maxShown = 20;
            var details = new StringBuilder();
            for (int i = 0; i < issues.Count && i < maxShown; i++)
            {
                if (i > 0)
                    details.AppendLine();

                details.Append(FormatInvalidKeyIssue(issues[i]));
            }

            if (issues.Count > maxShown)
            {
                details.AppendLine();
                details.Append($"... and {issues.Count - maxShown} more.");
            }

            return F("invalid.key.error", issues.Count, $"\n{details}");
        }

        private static string FormatInvalidKeyIssue(KeyFieldIssue issue)
        {
            string key = string.IsNullOrEmpty(issue.Key) ? "<Empty Key>" : issue.Key;
            return $"#{issue.EntryIndex + 1}: {key}";
        }

        private static string BuildContentKeyErrorText(List<ContentKeyFieldIssue> issues)
        {
            const int maxShown = 20;
            var details = new StringBuilder();
            for (int i = 0; i < issues.Count && i < maxShown; i++)
            {
                if (i > 0)
                    details.AppendLine();

                details.Append(FormatContentKeyIssue(issues[i]));
            }

            if (issues.Count > maxShown)
            {
                details.AppendLine();
                details.Append($"... and {issues.Count - maxShown} more.");
            }

            return F("embedded.content.key.error", issues.Count, $"\n{details}");
        }

        private static string FormatContentKeyIssue(ContentKeyFieldIssue issue)
        {
            string key = string.IsNullOrWhiteSpace(issue.EntryKey) ? "<Empty Key>" : issue.EntryKey.Trim();
            string language = string.IsNullOrWhiteSpace(issue.LanguageDisplayName)
                ? issue.LanguageCode
                : issue.LanguageDisplayName;
            return $"{key} / {language} ({issue.LanguageCode}): <{issue.Placeholder.RawContent}>";
        }

        private readonly struct KeyFieldIssue
        {
            public KeyFieldIssue(int entryIndex, string key)
            {
                EntryIndex = entryIndex;
                Key = key ?? "";
            }

            public int EntryIndex { get; }
            public string Key { get; }
        }

        private readonly struct ContentKeyFieldIssue
        {
            public ContentKeyFieldIssue(
                int entryIndex,
                string entryKey,
                string languageCode,
                string languageDisplayName,
                LocalizationPlaceholder placeholder)
            {
                EntryIndex = entryIndex;
                EntryKey = entryKey ?? "";
                LanguageCode = languageCode ?? "";
                LanguageDisplayName = languageDisplayName ?? "";
                Placeholder = placeholder;
            }

            public int EntryIndex { get; }
            public string EntryKey { get; }
            public string LanguageCode { get; }
            public string LanguageDisplayName { get; }
            public LocalizationPlaceholder Placeholder { get; }
        }

        private void EnsureSourcePathFromLegacyCsv()
        {
            if (!string.IsNullOrEmpty(Data.sourceFilePath) || Data.legacyCsvFile == null)
                return;

            Data.sourceFilePath = AssetDatabase.GetAssetPath(Data.legacyCsvFile);
            EditorUtility.SetDirty(Data);
        }

        private void BrowseSourceFile(TextField sourceField)
        {
            string initialDirectory = LanguageDataSOPathService.GetInitialDirectory(Data.sourceFilePath);
            string selectedPath = EditorUtility.OpenFilePanel(T("select.source.file"), initialDirectory, "");
            if (string.IsNullOrEmpty(selectedPath))
                return;

            Data.sourceFilePath = LanguageDataSOPathService.StoreSourcePath(selectedPath);
            sourceField.SetValueWithoutNotify(Data.sourceFilePath);
            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets();
        }

        private void ImportSource()
        {
            if (string.IsNullOrEmpty(Data.sourceFilePath))
            {
                EditorUtility.DisplayDialog(T("error"), T("assign.source.first"), T("ok"));
                return;
            }

            string sourcePath = LanguageDataSOPathService.ResolveSourcePath(Data.sourceFilePath);
            if (!File.Exists(sourcePath))
            {
                EditorUtility.DisplayDialog(T("error"), F("source.not.exist", Data.sourceFilePath), T("ok"));
                return;
            }

            if (!LocalizationSourceParser.IsSupported(sourcePath))
            {
                EditorUtility.DisplayDialog(T("error"), F("unsupported.source", sourcePath), T("ok"));
                return;
            }

            if (!EditorUtility.DisplayDialog(T("import.source.title"),
                    F("import.source.confirm", Data.name, Data.sourceFilePath), T("import"), T("cancel")))
                return;

            var importedEntries = LocalizationSourceParser.Import(sourcePath);
            if (importedEntries.Count == 0)
            {
                EditorUtility.DisplayDialog(T("import.source.title"), T("no.entries.imported"), T("ok"));
                return;
            }

            Undo.RecordObject(Data, T("undo.import.source"));
            Data.entries = importedEntries;
            EditorUtility.SetDirty(Data);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshList();

            Debug.Log(F("log.imported.source", Data.name, Path.GetFileName(sourcePath)));
        }

        private void ExportToFile()
        {
            string path = GetExportPath();
            if (string.IsNullOrEmpty(path))
                return;

            if (!LocalizationSourceExporter.IsSupported(path))
            {
                EditorUtility.DisplayDialog(T("export.file.title"), F("unsupported.export", path), T("ok"));
                return;
            }

            if (!EditorUtility.DisplayDialog(T("export.file.title"), F("export.confirm", path), T("export"),
                    T("cancel")))
                return;

            if (!LocalizationSourceExporter.Export(Data.entries, path))
            {
                EditorUtility.DisplayDialog(T("export.file.title"), F("export.failed", path), T("ok"));
                return;
            }

            AssetDatabase.Refresh();
            RefreshList();
        }

        private string GetExportPath()
        {
            if (!string.IsNullOrEmpty(Data.sourceFilePath))
                return LanguageDataSOPathService.ResolveSourcePath(Data.sourceFilePath);

            string initialDirectory = LanguageDataSOPathService.GetInitialDirectory(Data.sourceFilePath);
            string defaultName = GetDefaultExportFileName();
            return EditorUtility.SaveFilePanel(T("export.localization.file"), initialDirectory, defaultName, "xlsx");
        }

        private string GetDefaultExportFileName()
        {
            string sourcePath = string.IsNullOrEmpty(Data.sourceFilePath)
                ? ""
                : LanguageDataSOPathService.ResolveSourcePath(Data.sourceFilePath);
            string baseName = string.IsNullOrEmpty(sourcePath)
                ? Data.name
                : Path.GetFileNameWithoutExtension(sourcePath);

            if (string.IsNullOrEmpty(baseName))
                baseName = "Localization";

            return $"{baseName}.xlsx";
        }
    }
}
#endif
