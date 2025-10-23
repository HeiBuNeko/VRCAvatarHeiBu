#if UNITY_2022_1_OR_NEWER
using com.ams.avatarmodifysupport.language;
using com.ams.avatarmodifysupport.setting;
using UnityEditor;
using UnityEngine;

namespace com.ams.avatarmodifysupport.gui
{
    internal sealed class AMSHelpPopup : PopupWindowContent
    {
        internal static AMSSetting amsSetting { get { return AMSSetting.instance; } }
        private string _title;
        private string _message;
        Vector2 windowSize = new Vector2(350, 200f);

        GUIStyle middleLabelStyle;
        GUIStyle messageMiddleLabelStyle;
        GUIStyle messageLeftLabelStyle;
        internal eLanguage LanguageIndex
        {
            get
            {
                eLanguage index = (eLanguage)EditorPrefs.GetInt("AMS_LANGUAGE_INDEX", 0);
                return index;
            }
            set
            {
                int v = (int)value;
                EditorPrefs.SetInt("AMS_LANGUAGE_INDEX", v);
            }
        }

        internal AMSHelpPopup(string title, string message)
        {
            _title = title;
            _message = message;
        }
        public override Vector2 GetWindowSize()
        {
            return windowSize;
        }

        void InitializeGUIStyles()
        {
            if (middleLabelStyle == null)
                middleLabelStyle = AMSGuiUtility.MiddleLabelStyle(true, 15);

            if (messageMiddleLabelStyle == null)
            {
                messageMiddleLabelStyle = AMSGuiUtility.MiddleLabelStyle(true, 12);
                messageMiddleLabelStyle.wordWrap = true; //文字を改行して表示
            }

            if (messageLeftLabelStyle == null)
            {
                messageLeftLabelStyle = AMSGuiUtility.LabelStyle(true, 12);
                messageLeftLabelStyle.wordWrap = true;
            }
        }

        public override void OnGUI(Rect rect)
        {
            InitializeGUIStyles();

            GUILayout.Space(15);

            EditorGUILayout.LabelField(_title, middleLabelStyle);

            GUILayout.Space(15);
            EditorGUILayout.LabelField(_message, LanguageIndex == eLanguage.English ? messageLeftLabelStyle : messageMiddleLabelStyle);
        }
    }
}
#endif