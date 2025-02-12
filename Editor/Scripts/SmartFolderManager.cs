using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SmartFolder
{
    [InitializeOnLoad]
    public class SmartFolderManager
    {
        private static SmartFolderSettings settings;
        private static string hoveredPath;
        private static float hoverStartTime;
        private const float HOVER_DELAY = 0.5f;
        private static int itemIndex = 0; // 用于斑马条纹
        private static bool isDarkTheme;

        static SmartFolderManager()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            EditorApplication.update += OnEditorUpdate;
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += CheckThemeChange;
            LoadSettings();
            
            // 初始化连接线颜色
            HierarchyLineRenderer.UpdateLineColor(EditorGUIUtility.isProSkin);
        }

        private static void LoadSettings()
        {
            string settingsPath = "ProjectSettings/SmartFolder.json";
            if (File.Exists(settingsPath))
            {
                string jsonContent = File.ReadAllText(settingsPath);
                settings = JsonUtility.FromJson<SmartFolderSettings>(jsonContent);
            }
            else
            {
                settings = new SmartFolderSettings();
                SaveSettings();
            }
        }

        private static void SaveSettings()
        {
            string settingsPath = "ProjectSettings/SmartFolder.json";
            string jsonContent = JsonUtility.ToJson(settings, true);
            File.WriteAllText(settingsPath, jsonContent);
        }

        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            Event current = Event.current;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            bool isSelected = Selection.assetGUIDs.Contains(guid);
            
            // 绘制层级连接线
            if (settings.IsModuleActive("hierarchyLine") && current.type == EventType.Repaint)
            {
                HierarchyLineRenderer.DrawHierarchyLines(selectionRect, path);
            }

            // 绘制斑马条纹
            if (settings.IsModuleActive("zebraStripe") && current.type == EventType.Repaint)
            {
                DrawZebraStripe(selectionRect, itemIndex++ % 2 == 0);
            }

            // 处理双行文本显示
            if (settings.IsModuleActive("twoLine"))
            {
                string fileName = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(fileName))
                {
                    // 创建一个新的矩形，与原始矩形相同但向右偏移以留出图标空间
                    Rect labelRect = new Rect(selectionRect);
                    labelRect.x += 16f; // 为图标留出空间
                    labelRect.width -= 16f;

                    if (current.type == EventType.Repaint)
                    {
                        TwoLineRenderer.DrawTwoLineLabel(labelRect, fileName, isSelected);
                        // 返回以防止Unity默认的标签绘制
                        return;
                    }
                }
            }
            
            // 处理悬停效果
            if (selectionRect.Contains(current.mousePosition))
            {
                if (hoveredPath != path)
                {
                    hoveredPath = path;
                    hoverStartTime = (float)EditorApplication.timeSinceStartup;
                }

                // 显示悬停提示
                if (EditorApplication.timeSinceStartup - hoverStartTime >= HOVER_DELAY)
                {
                    Rect tooltipRect = new Rect(selectionRect.x, selectionRect.y - 20, selectionRect.width, 20);
                    EditorGUI.DrawRect(tooltipRect, new Color(0.1f, 0.1f, 0.1f, 0.7f));
                    GUI.Label(tooltipRect, path, new GUIStyle(EditorStyles.label) { normal = { textColor = Color.white } });
                }

                // 绘制文件夹轮廓
                if (AssetDatabase.IsValidFolder(path))
                {
                    Color outlineColor = new Color(1f, 0.92f, 0.016f, 0.4f);
                    DrawOutline(selectionRect, outlineColor);
                }
            }
            
            if (AssetDatabase.IsValidFolder(path))
            {
                // 绘制堆叠指示器
                if (settings.IsModuleActive("stacking"))
                {
                    FolderContentAnalyzer.DrawStackingIndicator(selectionRect, path);
                }

                // 处理Alt点击事件
                if (current.alt && current.type == EventType.MouseDown)
                {
                    if (current.button == 0) // 左键
                    {
                        ShowQuickSettingsPanel(path, selectionRect);
                        current.Use();
                    }
                    else if (current.button == 1) // 右键
                    {
                        ResetFolderCustomization(path);
                        current.Use();
                    }
                }
            }
        }

        private static void OnEditorUpdate()
        {
            // 如果鼠标移出了当前悬停的项，重置悬停状态
            if (hoveredPath != null && !EditorWindow.mouseOverWindow)
            {
                hoveredPath = null;
                EditorApplication.RepaintProjectWindow();
            }
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.E)
            {
                bool handled = false;

                if (e.control)
                {
                    // 显示预设菜单
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("基础模式"), false, () => ApplyPreset1());
                    menu.AddItem(new GUIContent("完整模式"), false, () => ApplyPreset2());
                    menu.AddItem(new GUIContent("加载自定义预设"), false, () => ApplyPreset3());
                    menu.ShowAsContext();
                    handled = true;
                }
                else
                {
                    bool isExpanded = IsAnySelectedFolderExpanded();
                    if (e.shift)
                    {
                        // Shift+E：递归展开/折叠
                        handled = isExpanded ? RecursiveCollapseFolder() : RecursiveExpandFolder();
                    }
                    else
                    {
                        // E：展开/折叠
                        handled = isExpanded ? CollapseFolder() : ExpandFolder();
                    }
                }

                if (handled)
                {
                    e.Use();
                    EditorApplication.RepaintProjectWindow();
                }
            }
        }

        private static bool IsAnySelectedFolderExpanded()
        {
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                {
                    // 使用反射获取文件夹展开状态
                    foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                    {
                        if (window.GetType().Name == "ProjectBrowser")
                        {
                            var isExpandedMethod = window.GetType().GetMethod(
                                "IsExpanded",
                                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                                null,
                                new[] { typeof(string) },
                                null
                            );

                            if (isExpandedMethod != null)
                            {
                                bool isExpanded = (bool)isExpandedMethod.Invoke(window, new object[] { path });
                                if (isExpanded) return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static void CheckThemeChange()
        {
            bool currentTheme = EditorGUIUtility.isProSkin;
            if (isDarkTheme != currentTheme)
            {
                isDarkTheme = currentTheme;
                HierarchyLineRenderer.UpdateLineColor(isDarkTheme);
                EditorApplication.RepaintProjectWindow();
            }
        }

        private static void DrawZebraStripe(Rect rect, bool isEven)
        {
            if (!isEven) return;

            Color stripeColor = EditorGUIUtility.isProSkin
                ? new Color(0.3f, 0.3f, 0.3f, 0.3f)
                : new Color(0.9f, 0.9f, 0.9f, 0.3f);

            // 如果有自定义背景色，调整条纹颜色
            if (settings.customIcons.TryGetValue(hoveredPath, out var customization) && 
                !string.IsNullOrEmpty(customization.color))
            {
                Color customColor;
                if (ColorUtility.TryParseHtmlString(customization.color, out customColor))
                {
                    stripeColor = Color.Lerp(stripeColor, customColor, 0.3f);
                }
            }

            EditorGUI.DrawRect(rect, stripeColor);
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            float thickness = 1f;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);                     // 上
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), color); // 下
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);                    // 左
            EditorGUI.DrawRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), color); // 右
        }

        private static void ShowQuickSettingsPanel(string folderPath, Rect position)
        {
            var window = EditorWindow.GetWindow<SmartFolderSettingsWindow>();
            window.Initialize(folderPath, settings);
            window.position = new Rect(position.x, position.y, 250, 300);
            window.ShowPopup();
        }

        private static void ResetFolderCustomization(string folderPath)
        {
            if (settings.customIcons.ContainsKey(folderPath))
            {
                settings.customIcons.Remove(folderPath);
                SaveSettings();
                EditorApplication.RepaintProjectWindow();
            }
        }

        private static bool ExpandFolder()
        {
            if (Selection.assetGUIDs.Length == 0) return false;
            
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                {
                    SetFolderExpanded(path, true);
                }
            }
            return true;
        }

        private static bool RecursiveExpandFolder()
        {
            if (Selection.assetGUIDs.Length == 0) return false;
            
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                {
                    SetFolderExpandedRecursive(path, true);
                }
            }
            return true;
        }

        private static bool CollapseFolder()
        {
            if (Selection.assetGUIDs.Length == 0) return false;
            
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                {
                    SetFolderExpanded(path, false);
                }
            }
            return true;
        }

        private static bool RecursiveCollapseFolder()
        {
            if (Selection.assetGUIDs.Length == 0) return false;
            
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                {
                    SetFolderExpandedRecursive(path, false);
                }
            }
            return true;
        }

        private static void SetFolderExpanded(string folderPath, bool expanded)
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window.GetType().Name == "ProjectBrowser")
                {
                    var expandMethodInfo = window.GetType().GetMethod(
                        "SetExpandedRecursive",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    );
                    
                    if (expandMethodInfo != null)
                    {
                        expandMethodInfo.Invoke(window, new object[] { folderPath, expanded });
                    }
                }
            }
        }

        private static void SetFolderExpandedRecursive(string folderPath, bool expanded)
        {
            SetFolderExpanded(folderPath, expanded);
            
            string[] subFolders = Directory.GetDirectories(folderPath, "*", SearchOption.TopDirectoryOnly);
            foreach (string subFolder in subFolders)
            {
                SetFolderExpandedRecursive(subFolder, expanded);
            }
        }

        private static bool ApplyPreset1()
        {
            // 仅基础图标+文件名
            settings.activeModules.Clear();
            EditorApplication.RepaintProjectWindow();
            return true;
        }

        private static bool ApplyPreset2()
        {
            // 启用所有可视化辅助
            settings.activeModules = new List<string> { 
                "stacking", 
                "twoLine", 
                "zebraStripe",
                "hierarchyLine"
            };
            EditorApplication.RepaintProjectWindow();
            return true;
        }

        private static bool ApplyPreset3()
        {
            // 加载自定义配置存档
            LoadCustomPreset();
            return true;
        }

        private static void LoadCustomPreset()
        {
            string presetPath = EditorUtility.OpenFilePanel(
                "加载自定义预设",
                "Assets",
                "json"
            );
            
            if (!string.IsNullOrEmpty(presetPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(presetPath);
                    var customSettings = JsonUtility.FromJson<SmartFolderSettings>(jsonContent);
                    if (customSettings != null)
                    {
                        settings = customSettings;
                        SaveSettings();
                        EditorApplication.RepaintProjectWindow();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"加载预设失败: {e.Message}");
                }
            }
        }
    }
}
