using UnityEngine;
using UnityEditor;
using System;

namespace SmartFolder
{
    public class ColorPickerWindow : EditorWindow
    {
        private Color selectedColor;
        private string hexColor = "#FFFFFF";
        private Vector3 hsvColor;
        private ColorMode currentMode = ColorMode.RGB;
        private Action<Color> onColorSelected;
        
        public enum ColorMode
        {
            RGB,
            HEX,
            HSV
        }

        public static void Show(Color initialColor, Action<Color> callback)
        {
            var window = GetWindow<ColorPickerWindow>("颜色选择器");
            window.selectedColor = initialColor;
            window.hexColor = "#" + ColorUtility.ToHtmlStringRGB(initialColor);
            Color.RGBToHSV(initialColor, out float h, out float s, out float v);
            window.hsvColor = new Vector3(h, s, v);
            window.onColorSelected = callback;
            window.minSize = new Vector2(300, 200);
            window.ShowAuxWindow();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // 模式选择
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            currentMode = (ColorMode)GUILayout.Toolbar((int)currentMode, 
                new string[] { "RGB", "HEX", "HSV" }, GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            // 颜色预览
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var previewRect = GUILayoutUtility.GetRect(100, 100);
            EditorGUI.DrawRect(previewRect, selectedColor);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            // 颜色输入
            switch (currentMode)
            {
                case ColorMode.RGB:
                    DrawRGBControls();
                    break;
                case ColorMode.HEX:
                    DrawHexControls();
                    break;
                case ColorMode.HSV:
                    DrawHSVControls();
                    break;
            }

            EditorGUILayout.Space(20);

            // 确认按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("确定", GUILayout.Width(100)))
            {
                onColorSelected?.Invoke(selectedColor);
                Close();
            }
            if (GUILayout.Button("取消", GUILayout.Width(100)))
            {
                Close();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRGBControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Vector4 rgba = new Vector4(selectedColor.r, selectedColor.g, selectedColor.b, selectedColor.a);
            EditorGUI.BeginChangeCheck();
            rgba = EditorGUILayout.Vector4Field("RGBA", rgba);
            if (EditorGUI.EndChangeCheck())
            {
                selectedColor = new Color(rgba.x, rgba.y, rgba.z, rgba.w);
                UpdateHexAndHSV();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawHexControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginChangeCheck();
            hexColor = EditorGUILayout.TextField("HEX", hexColor);
            if (EditorGUI.EndChangeCheck())
            {
                if (ColorUtility.TryParseHtmlString(hexColor, out Color newColor))
                {
                    selectedColor = newColor;
                    UpdateHSV();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawHSVControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginChangeCheck();
            hsvColor = EditorGUILayout.Vector3Field("HSV", hsvColor);
            if (EditorGUI.EndChangeCheck())
            {
                selectedColor = Color.HSVToRGB(
                    Mathf.Clamp01(hsvColor.x),
                    Mathf.Clamp01(hsvColor.y),
                    Mathf.Clamp01(hsvColor.z)
                );
                UpdateHex();
            }
            EditorGUILayout.EndVertical();
        }

        private void UpdateHexAndHSV()
        {
            hexColor = "#" + ColorUtility.ToHtmlStringRGB(selectedColor);
            Color.RGBToHSV(selectedColor, out float h, out float s, out float v);
            hsvColor = new Vector3(h, s, v);
        }

        private void UpdateHex()
        {
            hexColor = "#" + ColorUtility.ToHtmlStringRGB(selectedColor);
        }

        private void UpdateHSV()
        {
            Color.RGBToHSV(selectedColor, out float h, out float s, out float v);
            hsvColor = new Vector3(h, s, v);
        }
    }
}
