using UnityEditor;
using UnityEngine;

namespace net.satania_shopping.tabakosystem
{
    public static class SatabakoEditorUtils
    {
        private const float k_maxHeaderWidth = 200;
        private static Texture2D _headerImage = AssetDatabase.LoadAssetAtPath<Texture2D>(
            AssetDatabase.GUIDToAssetPath("34b0b9e5e2a893d45a2f158e9aab2ee3"));

        private static GUIContent boothIconContent = new GUIContent()
        {
            image = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath("e2431d7f80d0e154f8747627d7a22830")),
            text = "",
            tooltip = ""
        };
        public static GUIContent BoothIconContent => boothIconContent;

        public static void DrawHeader(float maxWidth = k_maxHeaderWidth)
        {
            if (_headerImage == null)
                return;

            float aspect = _headerImage.width / _headerImage.height;

            float w = Mathf.Min(EditorGUIUtility.currentViewWidth, maxWidth);
            float h = w / aspect;
            Rect rect2 = GUILayoutUtility.GetAspectRect(aspect, GUILayout.MaxWidth(w), GUILayout.Height(h));
            rect2.x = (EditorGUIUtility.currentViewWidth * 0.5f) - w / 2;
            GUI.DrawTexture(rect2, _headerImage, ScaleMode.ScaleToFit, true);
        }

        public static void MinMaxSliderWithValue(string label, ref float minRetValue, ref float maxRetValue, float min, float max)
        {
            Rect totalRect = EditorGUILayout.GetControlRect();
            bool isDrawFields = totalRect.width > 200;
            if (isDrawFields)
                totalRect.width -= 67;

            EditorGUI.MinMaxSlider(totalRect, new GUIContent(label), ref minRetValue, ref maxRetValue, min, max);

            if (isDrawFields)
            {
                Rect minFieldRect = new Rect()
                {
                    x = totalRect.x + totalRect.width - 10,
                    y = totalRect.y,
                    width = 45,
                    height = totalRect.height
                };

                Rect maxFieldRect = new Rect()
                {
                    x = minFieldRect.x + 30 + 2.5f,
                    y = minFieldRect.y,
                    width = 45,
                    height = minFieldRect.height
                };

                minRetValue = Mathf.Clamp(EditorGUI.FloatField(minFieldRect, minRetValue), min, max);
                maxRetValue = Mathf.Clamp(EditorGUI.FloatField(maxFieldRect, maxRetValue), min, max);
            }
        }

        public static void DrawWebButtonWithCustomIcon(string text, string url, GUIContent iconContent)
        {
            var area = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());

            EditorGUIUtility.AddCursorRect(area, MouseCursor.Link);
            iconContent.text = text;
            var style = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(),
                fontStyle = FontStyle.Bold,
            };
            style.hover.textColor = new Color32(0x3C, 0x99, 0xF5, 0xFF);

            if (GUI.Button(area, iconContent, style))
                Help.BrowseURL(url);
        }

        //https://github.com/lilxyzw/lilToon/blob/master/Assets/lilToon/Editor/lilEditorGUI.cs#L65
        public static void DrawWebButton(string text, string url)
        {
            DrawWebButtonWithCustomIcon(text, url, EditorGUIUtility.IconContent("BuildSettings.Web.Small"));
        }

        /// <summary>
        /// returns true if changed
        /// </summary>
        /// <param name="value"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public static bool ToggleLeftExpressionParameter(float value, string label, out bool newValue)
        {
            bool toggle = value != 0 ? true : false;
            newValue = EditorGUILayout.ToggleLeft(label, toggle);

            return toggle != newValue;
        }

        /// <summary>
        /// returns true if changed
        /// </summary>
        /// <param name="value"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public static bool ToggleForFloatParameter(float value, string label, out bool newValue)
        {
            bool toggle = value != 0 ? true : false;
            newValue = EditorGUILayout.Toggle(label, toggle);

            return toggle != newValue;
        }
    }
}