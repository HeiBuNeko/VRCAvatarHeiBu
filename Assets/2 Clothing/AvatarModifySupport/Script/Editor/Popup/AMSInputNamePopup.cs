#if UNITY_2022_1_OR_NEWER
using com.ams.avatarmodifysupport.callback;
using UnityEditor;
using UnityEngine;

namespace com.ams.avatarmodifysupport
{
    internal class AMSInputNamePopup : PopupWindowContent
    {
        Vector2 size = new Vector2(300, 100);

        string label = "Name";
        string output = "";
        string buttonLabel = "Button";

        AMSPopupCallback callbacks;

        internal AMSInputNamePopup(string _label, string _buttonLabel, AMSPopupCallback _callbacks)
        {
            label = _label;
            buttonLabel = _buttonLabel;
            callbacks = _callbacks;
        }

        public override Vector2 GetWindowSize()
        {
            return size;
        }

        public override void OnGUI(Rect rect)
        {
            output = EditorGUILayout.TextField(label, output);

            GUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(output));
            if (GUILayout.Button(buttonLabel))
            {
                callbacks.onClickSelectName?.Invoke(output);
                editorWindow.Close();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif