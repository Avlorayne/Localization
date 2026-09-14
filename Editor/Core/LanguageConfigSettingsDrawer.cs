#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using static Localization.Editor.LocalizationEditorText;

namespace Localization.Editor
{
    internal static class LanguageConfigSettingsDrawer
    {
        public static void Draw(SerializedObject serializedObject)
        {
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Max(
                previousLabelWidth,
                CalcLongestLabelWidth() + 4f);
            try
            {
                DrawContent(serializedObject);
            }
            finally
            {
                EditorGUIUtility.labelWidth = previousLabelWidth;
            }
        }

        private static float CalcLongestLabelWidth()
        {
            GUIContent[] labels =
            {
                new(T("settings.source.folder")),
                new(T("settings.so.folder")),
                new(T("settings.addressables.group")),
                new(T("settings.addressables.group.custom")),
            };
            float widest = 0f;
            foreach (GUIContent label in labels)
                widest = Mathf.Max(widest, EditorStyles.label.CalcSize(label).x);
            return widest;
        }

        private static void DrawContent(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField(T("source.configuration"), EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(LanguageConfigSO.sourceFolderPath)),
                new GUIContent(T("settings.source.folder"), T("settings.source.folder.tooltip")));

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(LanguageConfigSO.soFolderPath)),
                new GUIContent(T("settings.so.folder"), T("settings.so.folder.tooltip")));

            string soFolder = serializedObject
                .FindProperty(nameof(LanguageConfigSO.soFolderPath))
                .stringValue?
                .Replace('\\', '/') ?? string.Empty;
            if (!soFolder.Contains("/Resources/") &&
                !soFolder.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
            {
                EditorGUILayout.HelpBox(T("settings.so.folder.not.resources"), MessageType.Warning);
            }

            SerializedProperty addressablesGroupProperty =
                serializedObject.FindProperty(nameof(LanguageProjectSettings.languageDataAddressablesGroupName));
            if (addressablesGroupProperty != null)
                DrawAddressablesGroupField(addressablesGroupProperty);

            EditorGUILayout.Space();
            DrawSeparator();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(T("language.configuration"), EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(LanguageConfigSO.defaultLanguage)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(LanguageConfigSO.languages)));
        }

        private static void DrawAddressablesGroupField(SerializedProperty property)
        {
            var label = new GUIContent(T("settings.addressables.group"), T("settings.addressables.group.tooltip"));

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
            {
                EditorGUILayout.PropertyField(property, label);
                EditorGUILayout.HelpBox(T("settings.addressables.group.no.addressables"), MessageType.Warning);
                return;
            }

            string groupName = property.stringValue;
            AddressableAssetGroup selectedGroup = string.IsNullOrWhiteSpace(groupName)
                ? null
                : settings.FindGroup(groupName);

            var openLabel = new GUIContent(T("settings.addressables.group.open.window"));
            Vector2 openButtonSize = EditorStyles.miniButton.CalcSize(openLabel);
            Rect row = EditorGUILayout.GetControlRect(true);
            Rect labelRect = new Rect(row.x, row.y, EditorGUIUtility.labelWidth, row.height);
            Rect fieldRect = new Rect(
                row.x + EditorGUIUtility.labelWidth, row.y,
                row.width - EditorGUIUtility.labelWidth - openButtonSize.x - 2f, row.height);
            Rect buttonRect = new Rect(row.xMax - openButtonSize.x, row.y, openButtonSize.x, row.height);

            GUI.Label(labelRect, label);

            GUIContent groupContent = CreateGroupFieldContent(selectedGroup, groupName);
            if (EditorGUI.DropdownButton(fieldRect, groupContent, FocusType.Keyboard, EditorStyles.objectField))
            {
                OpenAddressablesGroupSelectionWindow(
                    settings,
                    property.serializedObject.targetObject,
                    property.propertyPath,
                    selectedGroup != null ? selectedGroup.name : groupName,
                    fieldRect);
            }

            if (GUI.Button(buttonRect, openLabel, EditorStyles.miniButton))
                OpenAddressablesGroupsWindow();

            if (selectedGroup == null)
            {
                EditorGUILayout.PropertyField(
                    property,
                    new GUIContent(T("settings.addressables.group.custom"), T("settings.addressables.group.tooltip")));
            }

            string finalName = property.stringValue;
            if (string.IsNullOrWhiteSpace(finalName))
            {
                EditorGUILayout.HelpBox(T("settings.addressables.group.empty"), MessageType.Warning);
            }
            else if (settings.FindGroup(finalName) == null)
            {
                EditorGUILayout.HelpBox(F("settings.addressables.group.not.found", finalName), MessageType.Info);
            }
        }

        private static GUIContent CreateGroupFieldContent(AddressableAssetGroup selectedGroup, string groupName)
        {
            string text;
            if (selectedGroup != null)
                text = selectedGroup.name;
            else if (!string.IsNullOrWhiteSpace(groupName))
                text = groupName.Trim();
            else
                text = "None";

            return new GUIContent(text, EditorGUIUtility.IconContent("Folder Icon").image);
        }

        private static void OpenAddressablesGroupSelectionWindow(
            AddressableAssetSettings settings,
            UnityEngine.Object targetObject,
            string propertyPath,
            string currentGroupName,
            Rect fieldRect)
        {
            var window = EditorWindow.GetWindow<AddressablesGroupSelectionWindow>(
                utility: true,
                title: "Select Addressable Group",
                focus: true);

            window.Initialize(settings, currentGroupName, group =>
            {
                if (targetObject == null)
                    return;

                Undo.RecordObject(targetObject, "Select Addressables Group");
                var serializedTarget = new SerializedObject(targetObject);
                serializedTarget.Update();
                SerializedProperty targetProperty = serializedTarget.FindProperty(propertyPath);
                if (targetProperty == null)
                    return;

                targetProperty.stringValue = group != null ? group.name : string.Empty;
                serializedTarget.ApplyModifiedProperties();
                EditorUtility.SetDirty(targetObject);

                if (targetObject is LanguageProjectSettings projectSettings)
                    projectSettings.SaveProjectSettings();
            });

            Vector2 screenPosition =
                GUIUtility.GUIToScreenPoint(new Vector2(fieldRect.xMax - window.position.width, fieldRect.y));
            Rect position = window.position;
            position.position = new Vector2(Mathf.Max(0f, screenPosition.x), Mathf.Max(0f, screenPosition.y));
            window.position = position;
        }

        private static void OpenAddressablesGroupsWindow()
        {
            if (!EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups"))
                EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Addressables Groups");
        }

        private static void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.32f, 0.32f, 0.32f, 1f));
        }

        private sealed class AddressablesGroupSelectionWindow : EditorWindow
        {
            private AddressableAssetSettings settings;
            private Action<AddressableAssetGroup> onSelected;
            private string searchText = string.Empty;
            private string currentGroupName = string.Empty;
            private Vector2 scroll;
            private Texture folderIcon;

            public void Initialize(
                AddressableAssetSettings addressableSettings,
                string selectedGroupName,
                Action<AddressableAssetGroup> selectedCallback)
            {
                settings = addressableSettings;
                currentGroupName = selectedGroupName ?? string.Empty;
                onSelected = selectedCallback;
                folderIcon = EditorGUIUtility.IconContent("Folder Icon").image;
                minSize = new Vector2(320f, 220f);
                maxSize = new Vector2(640f, 520f);
            }

            private void OnLostFocus()
            {
                Close();
            }

            private void OnGUI()
            {
                const float padding = 6f;

                EditorGUILayout.BeginVertical();
                GUILayout.Space(4f);
                searchText = GUILayout.TextField(searchText,
                    GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.toolbarSearchField);
                GUILayout.Space(4f);

                scroll = EditorGUILayout.BeginScrollView(scroll);
                foreach (AddressableAssetGroup group in EnumerateGroups())
                {
                    if (group == null || group.ReadOnly)
                        continue;

                    if (!string.IsNullOrEmpty(searchText) &&
                        group.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + padding);
                    row.x += 2f;
                    row.width -= 4f;

                    if (string.Equals(group.name, currentGroupName, StringComparison.OrdinalIgnoreCase))
                        EditorGUI.DrawRect(row, new Color(0.24f, 0.24f, 0.24f, 1f));

                    var content = new GUIContent(group.name, folderIcon);
                    if (GUI.Button(row, content, EditorStyles.label))
                    {
                        onSelected?.Invoke(group);
                        Close();
                    }
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }

            private IEnumerable<AddressableAssetGroup> EnumerateGroups()
            {
                if (settings == null)
                    yield break;

                foreach (AddressableAssetGroup group in settings.groups)
                    yield return group;
            }
        }
    }
}
#endif