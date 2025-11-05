#if UNITY_2022_1_OR_NEWER
using com.ams.avatarmodifysupport.callback;
using com.ams.avatarmodifysupport.setting;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace com.ams.avatarmodifysupport
{
    internal sealed class AMSAnimationSelectRendererPopup : PopupWindowContent
    {
        internal static AMSSetting amsSetting { get { return AMSSetting.instance; } }

        Vector2 scroll = new Vector2();
        Vector2 size = new Vector2(200, 300);
        List<SkinnedMeshRenderer> renderers;
        AMSPopupCallback callbacks;

        public override Vector2 GetWindowSize() => size;

        internal static bool IsLeftClicked(Rect rect, Event e)
        {
            return rect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0 && e.isMouse;
        }

        internal AMSAnimationSelectRendererPopup(IReadOnlyCollection<SkinnedMeshRenderer> _renderers, AMSPopupCallback _callbacks)
        {
            renderers = new List<SkinnedMeshRenderer>(_renderers);
            callbacks = _callbacks;
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField(amsSetting.texts.selectRendererForAnimation, EditorStyles.boldLabel);
            GUILayout.Space(5);

            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r.sharedMesh == null || r.sharedMesh.blendShapeCount == 0)
                    continue;

                EditorGUILayout.LabelField(r.name, EditorStyles.boldLabel);
                var _rect = GUILayoutUtility.GetLastRect();
                EditorGUIUtility.AddCursorRect(_rect, MouseCursor.Link);
                if (IsLeftClicked(_rect, Event.current))
                {
                    string savePath = EditorUtility.SaveFilePanelInProject(
                                amsSetting.texts.title,
                                "Default",
                                "anim",
                                amsSetting.texts.selectSaveAnimPath);

                    if (string.IsNullOrEmpty(savePath))
                        continue;

                    callbacks.onSelectRendererForSave?.Invoke(r, savePath);
                    editorWindow.Close();
                }
            }
            GUILayout.EndScrollView();
        }
    }
}
#endif