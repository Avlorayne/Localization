#if UNITY_EDITOR
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;

[CustomEditor(typeof(TextMeshPro), true), CanEditMultipleObjects]
public class LocalizedTextMeshProEditor : TMP_EditorPanel
{
    private readonly LocalizedTextMeshProTool localizationTool = new();

    public override void OnInspectorGUI()
    {
        localizationTool.Draw(targets, Repaint);
        EditorGUILayout.Space(8f);
        base.OnInspectorGUI();
    }
}

#endif
