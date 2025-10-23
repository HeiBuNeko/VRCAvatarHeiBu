#if UNITY_2022_1_OR_NEWER
using com.ams.avatarmodifysupport.callback;
using com.ams.avatarmodifysupport.gui;
using com.ams.avatarmodifysupport.setting;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static com.ams.avatarmodifysupport.colormodify.AMSColorModifyPresetGroup;

namespace com.ams.avatarmodifysupport.colormodify
{
    internal class AMSAddPresetGroupPopup : PopupWindowContent
    {
        internal static AMSSetting amsSetting { get { return AMSSetting.instance; } }

        Vector2 size = new Vector2(300, 200);

        AMSPopupCallback callbacks;
        Color32 _color = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        string displayName = "New Preset";
        string[] someGroupColorNames = new string[0];

        List<ColorModifyPresetNamePair> namePair;

        public AMSAddPresetGroupPopup(string[] _colorNames, AMSPopupCallback callback)
        {
            callbacks = callback;
            someGroupColorNames = _colorNames;
        }

        public override Vector2 GetWindowSize()
        {
            return size;
        }

        void OnSelectColorPreset(List<ColorModifyPresetNamePair> pairs)
        {
            if (pairs == null || pairs.Count == 0)
            {
                namePair = null;
                return;
            }

            namePair = pairs;
        }

        public override void OnGUI(Rect rect)
        {
            _color = EditorGUILayout.ColorField(amsSetting.texts.color, _color);
            displayName = EditorGUILayout.TextField(amsSetting.texts.colorName, displayName);

            if (namePair == null)
                EditorGUILayout.HelpBox(amsSetting.texts.selectColorPresetError, MessageType.Error);

            if (someGroupColorNames.Length > 0 && someGroupColorNames.Contains(displayName))
                EditorGUILayout.HelpBox(amsSetting.texts.someNameGroupExists, MessageType.Error);

            //選択
            if (GUILayout.Button(amsSetting.texts.select))
            {
                var pos = Event.current.mousePosition;
                pos.x -= 200 / 2;
                pos.y += 30;

                Rect presetPopup = new Rect(pos, new Vector2());

                callbacks.onClickColorPreset = OnSelectColorPreset;

                PopupWindow.Show(presetPopup, new AMSSelectColorPreset(callbacks, namePair)); //プリセット作成画面
            }

            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(namePair == null && !string.IsNullOrEmpty(displayName));
            if (GUILayout.Button(amsSetting.texts.create))
            {
                string colorCode = AMSGuiUtility.ConvertToColorCode(_color);
                callbacks?.onCreateColorGroup.Invoke(new ColorGroup(colorCode, displayName, namePair));
                editorWindow.Close();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif