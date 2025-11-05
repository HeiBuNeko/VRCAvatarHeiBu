using UnityEditor;
using UnityEngine;

namespace com.ams.avatarmodifysupport.gui
{
    internal sealed class AMSGuiUtility
    {
        internal static float MarginX = 15;
        internal static float MarginY = 7.5f;
        internal static float AdjustSizeForHelpMark = 0.947f;

        #region Color
        public static string ConvertToColorCode(Color32 color)
        {
            var r = color.r.ToString("X2").ToLower();
            var g = color.g.ToString("X2").ToLower();
            var b = color.b.ToString("X2").ToLower();
            var a = color.a.ToString("X2").ToLower();

            return $"#{r}{g}{b}{a}";
        }
        #endregion

        #region GUI Style
        internal static GUIStyle CreateBoxStyle(int b, int m, int p)
        {
            return new GUIStyle
            {
                border = new RectOffset(b, b, 0, b),
                margin = new RectOffset(m, m, 0, m),
                padding = new RectOffset(p, p, 0, p),
                overflow = new RectOffset(0, 0, 0, 0)
            };
        }
        internal static GUIStyle SetColorStyle(GUIStyle style, Color color)
        {
            GUIStyle _style = new GUIStyle(style);
            GUIStyleState styleState = _style.normal;
            styleState.textColor = color;

            _style.normal = styleState;
            _style.hover = styleState;
            _style.onNormal = styleState;
            _style.onHover = styleState;
            _style.onActive = styleState;
            _style.focused = styleState;
            _style.onFocused = styleState;

            return _style;
        }
        internal static GUIStyle LanguageLabelStyle(int sub = 2, bool isCenter = false)
        {
            GUIStyle buttonStyle = SetColorStyle(new GUIStyle(GUI.skin.label), Color.white);
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.alignment = isCenter ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            buttonStyle.fontSize = buttonStyle.fontSize - sub;

            return buttonStyle;
        }
        internal static GUIStyle SelectionGridStyle()
        {
            var GridSelectionStyle = new GUIStyle(GUI.skin.button);
            GridSelectionStyle.fixedHeight = 21;
            GridSelectionStyle.fontStyle = FontStyle.Bold;
            GridSelectionStyle.fontSize = 10;
            GridSelectionStyle.alignment = TextAnchor.MiddleCenter;
            GridSelectionStyle.padding = new RectOffset(0, 0, 0, 0);
            GridSelectionStyle.border = new RectOffset(0, 0, 0, 0);
            GridSelectionStyle.margin = new RectOffset(0, 0, 0, 0);
            GridSelectionStyle.overflow = new RectOffset(0, 0, 0, 0);
            return GridSelectionStyle;
        }
        internal static GUIStyle MiddleLabelStyle(bool isBold, int size)
        {
            GUIStyle middleLabel = new GUIStyle(GUI.skin.label);
            middleLabel.fontStyle = isBold ? FontStyle.Bold : FontStyle.Normal;
            middleLabel.fontSize = size;
            middleLabel.alignment = TextAnchor.MiddleCenter;

            return middleLabel;
        }
        internal static GUIStyle LabelStyle(bool isBold, int size)
        {
            GUIStyle label = new GUIStyle(GUI.skin.label);
            label.fontStyle = isBold ? FontStyle.Bold : FontStyle.Normal;
            label.fontSize = size;

            return label;
        }
        internal static GUIStyle ShurikenFoldoutStyle(float h)
        {
            var style = new GUIStyle("ShurikenModuleTitle");
            style.font = new GUIStyle(EditorStyles.label).font;
            style.border = new RectOffset(20, 7, 4, 4);
            style.fixedHeight = h;
            style.contentOffset = new Vector2(0, -2f);

            style.fontStyle = FontStyle.Bold;
            style.fontSize = 12;
            style.alignment = TextAnchor.MiddleCenter;

            return style;
        }
        #endregion

        #region Rect
        internal static Vector2 GetAspectedSize(float windowWidth, float heightRatio)
        {
            float height = windowWidth * heightRatio;

            return new Vector2(windowWidth, height);
        }
        internal static Rect GetAspectRect(float windowWidth, float heightRatio)
        {
            Vector2 size = GetAspectedSize(windowWidth, heightRatio);
            var rect = GUILayoutUtility.GetRect(
                0, 0,
                GUILayout.MaxWidth(size.x),
                GUILayout.MaxHeight(size.y),
                GUILayout.MinWidth(size.x),
                GUILayout.MinHeight(size.y));

            return rect;
        }
        internal Rect GetRectExpandWidth(float height)
        {
            return GUILayoutUtility.GetRect(
                0,
                0,
                GUILayout.ExpandWidth(true),
                GUILayout.MaxHeight(height),
                GUILayout.MinHeight(height));
        }
        internal static Vector2 GetMarginAspectedSize(float windowWidth, float heightRatio)
        {
            float width = windowWidth - MarginX * 2;
            float height = width * heightRatio;

            return new Vector2(width, height);
        }
        internal static Rect GetMarginAspectRect(float windowWidth, float heightRatio)
        {
            Vector2 size = GetMarginAspectedSize(windowWidth, heightRatio);
            var rect = GUILayoutUtility.GetRect(
                0, 0,
                GUILayout.MaxWidth(size.x),
                GUILayout.MaxHeight(size.y),
                GUILayout.MinWidth(size.x),
                GUILayout.MinHeight(size.y));
            rect.x += MarginX;

            return rect;
        }
        internal static Rect GetMarginRect(float height, float adjustment = 0f)
        {
            if (adjustment > 14.5f)
                adjustment = 14.5f;

            var rect =
                GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(height), GUILayout.MinHeight(height));

            rect.x += MarginX - adjustment;
            rect.width -= (MarginX - adjustment) * 2;

            return rect;
        }
        #endregion

        #region Editor GUI Layout
        internal static void Space()
        {
            GUILayout.Space(MarginY);
        }
        internal static void Label(string text, GUIStyle style, bool isSetColor = true)
        {
            if (style == null)
                style = new GUIStyle(GUI.skin.label);

            if (isSetColor) style = AMSGuiUtility.SetColorStyle(style, Color.white);

            style.richText = true;
            EditorGUILayout.LabelField(text, style);
        }
        #endregion

        internal static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

    }
}