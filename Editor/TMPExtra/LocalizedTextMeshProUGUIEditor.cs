#if UNITY_EDITOR
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;

[CustomEditor(typeof(TextMeshProUGUI), true), CanEditMultipleObjects]
public class LocalizedTextMeshProUGUIEditor : TMP_EditorPanelUI
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
