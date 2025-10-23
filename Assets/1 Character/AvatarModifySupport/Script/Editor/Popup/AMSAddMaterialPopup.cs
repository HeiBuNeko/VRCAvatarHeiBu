#if UNITY_2022_1_OR_NEWER
using com.ams.avatarmodifysupport.callback;
using com.ams.avatarmodifysupport.setting;
using UnityEditor;
using UnityEngine;

namespace com.ams.avatarmodifysupport.colormodify
{
    internal class AMSAddMaterialPopup : PopupWindowContent
    {
        internal static AMSSetting amsSetting { get { return AMSSetting.instance; } }

        Vector2 size = new Vector2(300, 200);

        AMSPopupCallback callbacks;
        Color32 _color = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        string displayName = "";
        Material _material;

        public override Vector2 GetWindowSize()
        {
            return size;
        }

        public AMSAddMaterialPopup(AMSPopupCallback _callback)
        {
            callbacks = _callback;
        }

        public override void OnGUI(Rect rect)
        {
            displayName = EditorGUILayout.TextField(amsSetting.texts.colorName, displayName);
            if (displayName.Length > 10)
                displayName = displayName.Substring(0, 10);

            _material = EditorGUILayout.ObjectField(amsSetting.texts.material, _material, typeof(Material), false) as Material;
            _color = EditorGUILayout.ColorField(amsSetting.texts.color, _color);

            GUILayout.Space(5);

            bool isMaterialInvalid = _material == null;

            if (isMaterialInvalid)
            {
                EditorGUILayout.HelpBox(amsSetting.texts.createGroupError_PleaseSelect, MessageType.Error, true);
            }

            EditorGUI.BeginDisabledGroup(isMaterialInvalid || string.IsNullOrEmpty(displayName));
            if (GUILayout.Button(amsSetting.texts.addColor))
            {
                if (displayName.Length > 10)
                    displayName = displayName.Substring(0, 10);

                callbacks.onClickAddMaterial?.Invoke(_material, _color, displayName);
                editorWindow.Close();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif