using UnityEngine;
using UnityEditor;
using System.Linq;

namespace SmartFolder
{
    public class TwoLineRenderer
    {
        private const float MIN_WIDTH_FOR_SPLIT = 24f;
        private static GUIStyle _secondLineStyle;

        private static GUIStyle SecondLineStyle
        {
            get
            {
                if (_secondLineStyle == null)
                {
                    _secondLineStyle = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = new Color(1f, 1f, 1f, 0.6f) },
                        fontSize = EditorStyles.label.fontSize - 1
                    };
                }
                return _secondLineStyle;
            }
        }

        public static void DrawTwoLineLabel(Rect rect, string text, bool isSelected)
        {
            if (string.IsNullOrEmpty(text)) return;

            // 计算可用宽度
            float availableWidth = rect.width - MIN_WIDTH_FOR_SPLIT;
            if (availableWidth <= 0) return;

            // 分割文本
            string[] parts = text.Split(' ');
            if (parts.Length <= 1)
            {
                // 如果没有空格，使用普通绘制
                EditorGUI.LabelField(rect, text);
                return;
            }

            // 计算第一行和第二行
            string firstLine = "";
            string secondLine = "";
            float currentWidth = 0;
            bool firstLineDone = false;

            foreach (string part in parts)
            {
                float partWidth = EditorStyles.label.CalcSize(new GUIContent(part + " ")).x;

                if (!firstLineDone && currentWidth + partWidth <= availableWidth)
                {
                    firstLine += (firstLine.Length > 0 ? " " : "") + part;
                    currentWidth += partWidth;
                }
                else
                {
                    firstLineDone = true;
                    secondLine += (secondLine.Length > 0 ? " " : "") + part;
                }
            }

            // 如果第二行为空，就只显示一行
            if (string.IsNullOrEmpty(secondLine))
            {
                EditorGUI.LabelField(rect, firstLine);
                return;
            }

            // 绘制两行文本
            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect firstLineRect = new Rect(rect.x, rect.y, rect.width, lineHeight);
            Rect secondLineRect = new Rect(rect.x, rect.y + lineHeight, rect.width, lineHeight);

            // 根据选中状态调整颜色
            if (isSelected)
            {
                SecondLineStyle.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            }
            else
            {
                SecondLineStyle.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
            }

            EditorGUI.LabelField(firstLineRect, firstLine);
            EditorGUI.LabelField(secondLineRect, secondLine, SecondLineStyle);
        }
    }
}
