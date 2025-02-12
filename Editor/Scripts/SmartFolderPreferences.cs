using UnityEngine;
using UnityEditor;

namespace SmartFolder
{
    public class SmartFolderPreferences : EditorWindow
    {
        private static SmartFolderSettings settings;
        private static string[] functionKeys = { "F1", "F2", "F3" };
        private static Vector2 scrollPosition;

        [MenuItem("Edit/Project Settings/Smart Folder")]
        public static void ShowWindow()
        {
            var window = GetWindow<SmartFolderPreferences>("Smart Folder Settings");
            window.minSize = new Vector2(300, 200);
        }

        private void OnEnable()
        {
            if (settings == null)
            {
                string settingsPath = "ProjectSettings/SmartFolder.json";
                if (System.IO.File.Exists(settingsPath))
                {
                    string jsonContent = System.IO.File.ReadAllText(settingsPath);
                    settings = JsonUtility.FromJson<SmartFolderSettings>(jsonContent);
                }
                else
                {
                    settings = new SmartFolderSettings();
                }
            }
        }

        private void OnGUI()
        {
            if (settings == null) return;

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("功能模块设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawModuleToggle("动态堆叠指示器", "stacking");
            DrawModuleToggle("双行文本显示", "twoLine");
            DrawModuleToggle("层级连接线", "hierarchyLine");
            DrawModuleToggle("斑马背景条纹", "zebraStripe");

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("快捷键设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("E: 展开/折叠选中文件夹");
            EditorGUILayout.LabelField("Shift+E: 递归展开/折叠所有子文件夹");
            EditorGUILayout.LabelField("Ctrl+E: 打开预设菜单");

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("性能设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox("为确保性能，每帧最大更新元素数量限制为50个", MessageType.Info);

            if (GUILayout.Button("清除缓存"))
            {
                FolderContentAnalyzer.ClearCache();
                EditorApplication.RepaintProjectWindow();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawModuleToggle(string label, string moduleName)
        {
            bool isActive = settings.IsModuleActive(moduleName);
            bool newValue = EditorGUILayout.Toggle(label, isActive);
            
            if (newValue != isActive)
            {
                settings.SetModuleActive(moduleName, newValue);
                SaveSettings();
                EditorApplication.RepaintProjectWindow();
            }
        }

        private void SaveSettings()
        {
            string settingsPath = "ProjectSettings/SmartFolder.json";
            string jsonContent = JsonUtility.ToJson(settings, true);
            System.IO.File.WriteAllText(settingsPath, jsonContent);
        }
    }
}
