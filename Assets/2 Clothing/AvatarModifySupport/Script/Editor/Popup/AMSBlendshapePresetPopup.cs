using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace com.ams.avatarmodifysupport.preset
{
    internal sealed class AMSBlendshapePresetPopup : PopupWindowContent
    {
        internal static Vector2 size = new Vector2(200, 150);
        internal List<AMSSkinnedMeshRenderer> renderers = new List<AMSSkinnedMeshRenderer>();
        internal Action<SkinnedMeshRenderer, int> onClickedCallback;
        internal bool[] foldoutToggles = new bool[0];

        internal Vector2 scrollPosition;

        public AMSBlendshapePresetPopup(Vector2 _size, IReadOnlyCollection<AMSSkinnedMeshRenderer> _renderers, Action<SkinnedMeshRenderer, int> _onClickedCallback)
        {
            size = _size;
            renderers = new List<AMSSkinnedMeshRenderer>(_renderers);
            onClickedCallback = _onClickedCallback;

            foldoutToggles = new bool[_renderers.Count];
        }

        public void Clicked(SkinnedMeshRenderer renderer, AMSBlendshape blendshape)
        {
            onClickedCallback?.Invoke(renderer, blendshape.Index);
        }

        public override Vector2 GetWindowSize()
        {
            return size;
        }

        public override void OnGUI(Rect rect)
        {
            using (var s = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = s.scrollPosition;
                for (int r = 0; r < renderers.Count; r++)
                {
                    var renderer = renderers[r];
                    if (renderer == null) continue;

                    foldoutToggles[r] = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutToggles[r], renderer.Renderer.name);
                    if (foldoutToggles[r])
                    {
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < renderer.Blendshapes.Count; i++)
                        {
                            var shapekey = renderer.Blendshapes[i];

                            if (shapekey == null)
                                continue;

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(shapekey.DisplayName, GUILayout.Width(155));
                            if (GUILayout.Button("+", GUILayout.Width(20)))
                            {
                                Clicked(renderer.Renderer, shapekey);
                            }

                            GUILayout.EndHorizontal();
                        }
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
            }
        }

        public override void OnOpen() { }
        public override void OnClose() { }
    }
}