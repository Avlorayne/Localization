#if UNITY_EDITOR
using UnityEngine;

namespace Localization.Editor
{
    /// <summary>条目卡片的列布局数学：按可用宽度在"拆列/合并"阈值间计算列数与卡宽。</summary>
    internal static class EntryCardLayout
    {
        public const float SplitWidth = 400f;
        public const float MergeWidth = 260f;
        public const float ColumnGap = 10f;

        public static int CalculateColumnCount(float width, int entryCount, int currentColumns)
        {
            int maxColumns = Mathf.Max(1, entryCount);
            int columns = Mathf.Clamp(currentColumns, 1, maxColumns);

            while (columns < maxColumns &&
                   GetCardWidth(width, columns) > SplitWidth &&
                   GetCardWidth(width, columns + 1) >= MergeWidth)
            {
                columns++;
            }

            while (columns > 1 && GetCardWidth(width, columns) < MergeWidth)
            {
                columns--;
            }

            return columns;
        }

        public static float GetCardWidth(float width, int columns)
        {
            columns = Mathf.Max(1, columns);
            return (width - (columns - 1) * ColumnGap) / columns;
        }
    }
}
#endif