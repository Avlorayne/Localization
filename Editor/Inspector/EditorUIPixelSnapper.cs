#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace Localization.Editor
{
    /// <summary>
    /// UI Toolkit 元素的整数像素对齐：把 margin/padding 钳到物理整数像素点。
    /// 每个元素只在首次布局时修正一次，防止递归触发 GeometryChangedEvent。
    /// </summary>
    internal static class EditorUIPixelSnapper
    {
        public static void MakePixelPerfect(VisualElement element)
        {
            if (element == null)
                return;

            element.style.unityTextAlign = TextAnchor.MiddleLeft;
            element.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private static void OnGeometryChanged(GeometryChangedEvent evt)
        {
            var element = evt.currentTarget as VisualElement;
            if (element == null)
                return;

            if (element.userData != null)
                return;

            element.userData = true;

            var rs = element.resolvedStyle;
            element.style.marginLeft = Mathf.FloorToInt(rs.marginLeft);
            element.style.marginRight = Mathf.FloorToInt(rs.marginRight);
            element.style.marginTop = Mathf.FloorToInt(rs.marginTop);
            element.style.marginBottom = Mathf.FloorToInt(rs.marginBottom);
            element.style.paddingLeft = Mathf.FloorToInt(rs.paddingLeft);
            element.style.paddingRight = Mathf.FloorToInt(rs.paddingRight);
            element.style.paddingTop = Mathf.FloorToInt(rs.paddingTop);
            element.style.paddingBottom = Mathf.FloorToInt(rs.paddingBottom);
        }
    }
}
#endif