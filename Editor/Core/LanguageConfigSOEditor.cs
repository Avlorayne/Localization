#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using static Localization.Editor.LocalizationEditorText;

namespace Localization.Editor
{
    [CustomEditor(typeof(LanguageConfigSO))]
    public class LanguageConfigSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField(T("source.configuration"), EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(LanguageConfigSO.sourceFolderPath)),
                new GUIContent(T("settings.source.folder"), T("settings.source.folder.tooltip")));

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(LanguageConfigSO.soFolderPath)),
                new GUIContent(T("settings.so.folder"), T("settings.so.folder.tooltip")));

            var config = (LanguageConfigSO)target;
            string soFolder = config.soFolderPath?.Replace('\\', '/') ?? string.Empty;
            if (!soFolder.Contains("/Resources/") &&
                !soFolder.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
            {
                EditorGUILayout.HelpBox(T("settings.so.folder.not.resources"), MessageType.Warning);
            }

            EditorGUILayout.Space();
            DrawSeparator();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(T("language.configuration"), EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(LanguageConfigSO.defaultLanguage)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(LanguageConfigSO.languages)));

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.32f, 0.32f, 0.32f, 1f));
        }
    }
}
#endif
