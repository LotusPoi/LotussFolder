using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SmartFolder
{
    public class FolderContentAnalyzer
    {
        public class ContentStats
        {
            public int ScriptCount;    // 蓝色
            public int PrefabCount;    // 绿色
            public int TextureCount;   // 黄色
            public int AnimationCount; // 红色

            public bool HasContent => ScriptCount > 0 || PrefabCount > 0 || TextureCount > 0 || AnimationCount > 0;
        }

        private static Dictionary<string, ContentStats> _cachedStats = new Dictionary<string, ContentStats>();
        private static readonly string[] ScriptExtensions = { ".cs", ".js" };
        private static readonly string[] TextureExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd" };

        public static ContentStats AnalyzeFolder(string folderPath)
        {
            if (_cachedStats.TryGetValue(folderPath, out ContentStats stats))
            {
                return stats;
            }

            stats = new ContentStats();
            var guids = AssetDatabase.FindAssets("", new[] { folderPath });

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string ext = Path.GetExtension(assetPath).ToLower();

                // 检查文件类型
                if (ScriptExtensions.Contains(ext))
                {
                    stats.ScriptCount++;
                }
                else if (TextureExtensions.Contains(ext))
                {
                    stats.TextureCount++;
                }
                else
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (asset != null)
                    {
                        if (asset is GameObject && PrefabUtility.GetPrefabAssetType(asset) != PrefabAssetType.NotAPrefab)
                        {
                            stats.PrefabCount++;
                        }
                        else if (asset is AnimationClip || asset is Animator || asset is RuntimeAnimatorController)
                        {
                            stats.AnimationCount++;
                        }
                    }
                }
            }

            _cachedStats[folderPath] = stats;
            return stats;
        }

        public static void ClearCache()
        {
            _cachedStats.Clear();
        }

        public static void DrawStackingIndicator(Rect rect, string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath)) return;

            var stats = AnalyzeFolder(folderPath);
            if (!stats.HasContent) return;

            float barHeight = 3f;
            float spacing = 1f;
            float totalHeight = (barHeight + spacing) * 4;
            float startY = rect.y + rect.height - totalHeight - 2f;
            float width = 24f;
            float startX = rect.x + rect.width - width - 2f;

            // 绘制堆叠条
            if (stats.ScriptCount > 0)
                EditorGUI.DrawRect(new Rect(startX, startY, width, barHeight), new Color(0.2f, 0.6f, 1f, 0.8f));
            
            if (stats.PrefabCount > 0)
                EditorGUI.DrawRect(new Rect(startX, startY + barHeight + spacing, width, barHeight), new Color(0.4f, 0.8f, 0.4f, 0.8f));
            
            if (stats.TextureCount > 0)
                EditorGUI.DrawRect(new Rect(startX, startY + (barHeight + spacing) * 2, width, barHeight), new Color(1f, 0.8f, 0.2f, 0.8f));
            
            if (stats.AnimationCount > 0)
                EditorGUI.DrawRect(new Rect(startX, startY + (barHeight + spacing) * 3, width, barHeight), new Color(1f, 0.4f, 0.4f, 0.8f));
        }
    }
}
