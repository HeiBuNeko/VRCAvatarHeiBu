#if UNITY_2022_1_OR_NEWER
using com.ams.avatarmodifysupport.callback;
using com.ams.avatarmodifysupport.setting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.ams.avatarmodifysupport.colormodify
{
    internal class AMSCreateColorPresetPopup : PopupWindowContent
    {
        internal class AMSCreateColorSelectRendererPopup : PopupWindowContent
        {
            Vector2 scroll = new Vector2();

            internal static AMSSetting amsSetting { get { return AMSSetting.instance; } }
            Vector2 size = new Vector2(200, 300);
            AMSPopupCallback callbacks;
            Renderer[] renderers;
            List<AMSColorModifyPreset> alreadySelectedList;

            GUIStyle richTextStyle;

            bool[] isOpened;

            public AMSCreateColorSelectRendererPopup(AMSPopupCallback _callbacks, Renderer[] _renderers, IReadOnlyCollection<AMSColorModifyPreset> _alreadySelectedList)
            {
                callbacks = _callbacks;

                renderers = _renderers;
                alreadySelectedList = (List<AMSColorModifyPreset>)_alreadySelectedList;
                isOpened = new bool[_renderers.Length];
            }

            internal static bool IsLeftClicked(Rect rect, Event e)
            {
                return rect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0 && e.isMouse;
            }

            public override Vector2 GetWindowSize()
            {
                return size;
            }

            public override void OnGUI(Rect rect)
            {
                if (richTextStyle == null)
                {
                    richTextStyle = new GUIStyle(GUI.skin.label);
                    richTextStyle.richText = true;
                }

                scroll = GUILayout.BeginScrollView(scroll);
                for (int i = 0; i < renderers.Length; i++)
                {
                    var r = renderers[i];
                    isOpened[i] = EditorGUILayout.Foldout(isOpened[i], r.name);
                    if (isOpened[i])
                    {
                        EditorGUI.indentLevel++;
                        for (int m = 0; m < r.sharedMaterials.Length; m++)
                        {
                            var material = r.sharedMaterials[m];
                            bool isAdded = alreadySelectedList.Where(x => x.renderer == r && x.MaterialIndex == m).Any();

                            if (isAdded)
                                EditorGUILayout.LabelField($"<color=gray><b>{material.name} ({amsSetting.texts.alreadyAdded})</b></color>", richTextStyle);
                            else
                                EditorGUILayout.LabelField(material.name);

                            //既に追加されている場合はクリック出来ない
                            if (isAdded)
                                continue;

                            var lastRect = GUILayoutUtility.GetLastRect();
                            EditorGUIUtility.AddCursorRect(lastRect, MouseCursor.Link);
                            if (IsLeftClicked(lastRect, Event.current))
                            {
                                callbacks.onClickedSelectMaterial?.Invoke(i, m);
                                editorWindow.Close();
                            }
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                GUILayout.EndScrollView();
            }
        }
        Vector2 size = new Vector2(400, 200);

        AMSPopupCallback callbacks;

        /// <summary>
        /// プリセットの名前
        /// </summary>
        string name = "Name";
        /// <summary>
        /// マテリアルが変更されるRenderer
        /// </summary>
        Renderer _renderer;
        public int _materialIndex = -1;
        List<Renderer> renderers;
        internal static AMSSetting amsSetting { get { return AMSSetting.instance; } }

        public AMSCreateColorPresetPopup(AMSPopupCallback _callbacks)
        {
            callbacks = _callbacks;
            name = amsSetting.texts.newPreset;
            renderers = amsSetting.Avatar.GetComponentsInChildren<Renderer>(true).ToList();
        }

        public override Vector2 GetWindowSize()
        {
            return size;
        }

        public override void OnGUI(Rect rect)
        {
            name = EditorGUILayout.TextField(amsSetting.texts.presetName, name);

            using (new GUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(true);

                //選択された物を表示
                EditorGUILayout.ObjectField(amsSetting.texts.renderer, _renderer, typeof(SkinnedMeshRenderer), true);
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button(amsSetting.texts.select, GUILayout.Width(40)))
                    OpenSelectRendererPopup();
            }

            using (new GUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(true);

                //選択された物を表示
                EditorGUILayout.ObjectField(
                    amsSetting.texts.material,
                    (_renderer != null && _renderer.sharedMaterials.Length > _materialIndex) ? _renderer.sharedMaterials[_materialIndex] : null,
                    typeof(Material),
                    true);
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button(amsSetting.texts.select, GUILayout.Width(40)))
                    OpenSelectRendererPopup();
            }

            GUILayout.Space(5);

            bool isRendererOrIndexIsInvalid = _renderer == null || _materialIndex == -1;

            if (isRendererOrIndexIsInvalid)
            {
                EditorGUILayout.HelpBox(amsSetting.texts.createGroupError_PleaseSelect, MessageType.Error, true);
                if (GUILayout.Button(amsSetting.texts.selectMaterial))
                    OpenSelectRendererPopup();
            }

            EditorGUI.BeginDisabledGroup(isRendererOrIndexIsInvalid || string.IsNullOrEmpty(name));
            if (GUILayout.Button(amsSetting.texts.createGroup))
            {
                if (name.Length > 12)
                    name = name.Substring(0, 12);

                callbacks.onClickCreateColorPreset?.Invoke(new AMSColorModifyPreset(_renderer, amsSetting.Avatar.transform, name, _materialIndex));
                editorWindow.Close();
            }
            EditorGUI.EndDisabledGroup();
        }

        public void OpenSelectRendererPopup()
        {
            var pos = Event.current.mousePosition;
            pos.x -= 300 / 2;
            Rect presetPopup = new Rect(pos, new Vector2());
            callbacks.onClickedSelectMaterial = (rendererIndex, materialIndex) =>
            {
                _renderer = renderers[rendererIndex];
                if (_renderer == null)
                    return;

                _materialIndex = materialIndex;
            };

            PopupWindow.Show(presetPopup, new AMSCreateColorSelectRendererPopup(callbacks, renderers.ToArray(), amsSetting.colorPresets));
        }
    }
}
#endif