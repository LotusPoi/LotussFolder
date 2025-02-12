using UnityEngine;
using UnityEditor;

namespace SmartFolder
{
    public class SmartFolderSettingsWindow : EditorWindow
    {
        private string targetFolderPath;
        private SmartFolderSettings settings;
        private Color selectedColor = Color.white;
        private string customIconPath;

        public void Initialize(string folderPath, SmartFolderSettings folderSettings)
        {
            targetFolderPath = folderPath;
            settings = folderSettings;

            if (settings.customIcons.TryGetValue(folderPath, out FolderCustomization customization))
            {
                if (!string.IsNullOrEmpty(customization.color))
                {
                    ColorUtility.TryParseHtmlString(customization.color, out selectedColor);
                }
                customIconPath = customization.icon;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("文件夹设置", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("路径:", targetFolderPath);
            EditorGUILayout.Space();

            // 颜色设置
            EditorGUILayout.LabelField("颜色设置");
            Color newColor = EditorGUILayout.ColorField(selectedColor);
            if (newColor != selectedColor)
            {
                selectedColor = newColor;
                SaveCustomization();
            }

            EditorGUILayout.Space();

            // 图标设置
            EditorGUILayout.LabelField("自定义图标");
            EditorGUILayout.HelpBox("拖拽图标到此处", MessageType.Info);
            
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "拖拽区域");

            // 处理拖拽
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (string path in DragAndDrop.paths)
                        {
                            if (path.EndsWith(".png") || path.EndsWith(".jpg"))
                            {
                                customIconPath = path;
                                SaveCustomization();
                                break;
                            }
                        }
                    }
                    break;
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("重置设置"))
            {
                if (settings.customIcons.ContainsKey(targetFolderPath))
                {
                    settings.customIcons.Remove(targetFolderPath);
                    EditorApplication.RepaintProjectWindow();
                    Close();
                }
            }
        }

        private void SaveCustomization()
        {
            FolderCustomization customization = new FolderCustomization
            {
                color = "#" + ColorUtility.ToHtmlStringRGB(selectedColor),
                icon = customIconPath
            };

            if (settings.customIcons.ContainsKey(targetFolderPath))
            {
                settings.customIcons[targetFolderPath] = customization;
            }
            else
            {
                settings.customIcons.Add(targetFolderPath, customization);
            }

            EditorApplication.RepaintProjectWindow();
        }
    }
}
