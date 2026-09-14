#if UNITY_EDITOR
using System.Runtime.CompilerServices;
using UnityEditor;

[assembly: InternalsVisibleTo("Dotline.Localization.Editor.Tests")]

namespace Localization.Editor
{
    [CustomEditor(typeof(LanguageConfigSO))]
    internal class LanguageConfigSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            LanguageConfigSettingsDrawer.Draw(serializedObject);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif