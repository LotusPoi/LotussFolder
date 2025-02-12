using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace SmartFolder
{
    public class HierarchyLineRenderer
    {
        private static Dictionary<string, float> pathDepthCache = new Dictionary<string, float>();
        private const float INDENT_WIDTH = 14f;
        private const float LINE_THICKNESS = 1f;
        private static Color lineColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        public static void DrawHierarchyLines(Rect rect, string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            float depth = GetPathDepth(path);
            if (depth <= 0) return;

            // 计算缩进基准点
            float baseX = rect.x - INDENT_WIDTH;

            // 绘制垂直连接线
            for (int i = 1; i <= depth; i++)
            {
                float x = baseX - (INDENT_WIDTH * (depth - i));
                
                // 获取父路径
                string parentPath = GetParentPath(path, depth - i);
                if (string.IsNullOrEmpty(parentPath)) continue;

                // 检查是否是父文件夹的最后一个子项
                bool isLastChild = IsLastChild(parentPath, GetParentPath(path, depth - i + 1));
                
                // 如果不是最后一个子项，绘制完整的垂直线
                if (!isLastChild)
                {
                    Rect verticalRect = new Rect(
                        x,
                        rect.y - 8f, // 向上延伸
                        LINE_THICKNESS,
                        rect.height + 16f // 向下延伸
                    );
                    EditorGUI.DrawRect(verticalRect, lineColor);
                }
                
                // 绘制水平连接线
                if (i == depth)
                {
                    Rect horizontalRect = new Rect(
                        x,
                        rect.y + rect.height * 0.5f,
                        INDENT_WIDTH - 2f,
                        LINE_THICKNESS
                    );
                    EditorGUI.DrawRect(horizontalRect, lineColor);
                }
            }
        }

        private static float GetPathDepth(string path)
        {
            if (pathDepthCache.TryGetValue(path, out float depth))
            {
                return depth;
            }

            depth = path.Split('/').Length - 1;
            pathDepthCache[path] = depth;
            return depth;
        }

        private static string GetParentPath(string path, int levelsUp)
        {
            string[] parts = path.Split('/');
            if (parts.Length <= levelsUp) return "";
            
            string[] parentParts = new string[parts.Length - levelsUp];
            System.Array.Copy(parts, 0, parentParts, 0, parts.Length - levelsUp);
            return string.Join("/", parentParts);
        }

        private static bool IsLastChild(string parentPath, string childPath)
        {
            if (!Directory.Exists(parentPath)) return true;

            string[] siblings = Directory.GetDirectories(parentPath);
            if (siblings.Length == 0) return true;

            // 获取按字母顺序排序的最后一个文件夹
            System.Array.Sort(siblings);
            string lastSibling = siblings[siblings.Length - 1].Replace('\\', '/');
            
            return lastSibling.EndsWith(childPath);
        }

        public static void ClearCache()
        {
            pathDepthCache.Clear();
        }

        public static void UpdateLineColor(bool isDarkTheme)
        {
            lineColor = isDarkTheme 
                ? new Color(0.5f, 0.5f, 0.5f, 0.3f) 
                : new Color(0.3f, 0.3f, 0.3f, 0.2f);
        }
    }
}
