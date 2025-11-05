#if UNITY_2022_1_OR_NEWER
using com.ams.avatarmodifysupport.callback;
using com.ams.avatarmodifysupport.setting;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace com.ams.avatarmodifysupport.preset
{
    internal sealed class AMSCreatePresetPopup : PopupWindowContent
    {
        internal static AMSSetting amsSetting { get { return AMSSetting.instance; } }

        private string presetName = "";
        internal static Vector2 size = new Vector2(200, 150);
        AMSPopupCallback callbacks;
        AMSSetting ams;

        internal AMSCreatePresetPopup(Vector2 _size, AMSPopupCallback _callbacks, AMSSetting _ams)
        {
            size = _size;
            callbacks = _callbacks;
            ams = _ams;

            presetName = ams.texts.newPreset;
        }

        public override Vector2 GetWindowSize()
        {
            return size;
        }

        public override void OnGUI(Rect rect)
        {
            presetName = EditorGUILayout.TextField(ams.texts.presetName, presetName);
            if (presetName.Length > 14)
                presetName = presetName.Substring(0, 14);

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(ams.texts.create, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(195f)))
            {
                if (presetName.Length > 14)
                    presetName = presetName.Substring(0, 14);

                callbacks.onCreatePresetClicked?.Invoke(presetName);
                editorWindow.Close();
            }
            if (GUILayout.Button(ams.texts.createByAvatarValue, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(195f)))
            {
                callbacks.onClickSelectRenderer = (list) =>
                {
                    if (presetName.Length > 14)
                        presetName = presetName.Substring(0, 14);

                    ams.selectedRenderers = new List<SkinnedMeshRenderer>(list);
                    editorWindow.Close();

                    callbacks.onCreatePresetByAvatarClicked?.Invoke(presetName);
                };

                callbacks.onCancelSelectRenderers = () =>
                {
                    editorWindow.Close();
                };

                var pos = Event.current.mousePosition;
                Rect presetPopup = new Rect(pos, new Vector2());
                PopupWindow.Show(presetPopup, new AMSSelectRendererPopup(
                    ams.Avatar.gameObject,
                    ams.selectedRenderers,
                    callbacks));
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            if (GUILayout.Button(ams.texts.importPresetFromFile))
            {
                callbacks.onImportPreset?.Invoke();
                editorWindow.Close();
            }
        }
    }
}
#endif