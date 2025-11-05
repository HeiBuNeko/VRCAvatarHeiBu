#if UNITY_2022_1_OR_NEWER
using com.ams.avatarmodifysupport.callback;
using com.ams.avatarmodifysupport.setting;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace com.ams.avatarmodifysupport
{
    internal sealed class AMSSelectRendererPopup : PopupWindowContent
    {
        internal static AMSSetting amsSetting { get { return AMSSetting.instance; } }

        static Vector2 size = new Vector2(210, 300);
        Vector2 scroll = new Vector2();

        GameObject avatarRoot;
        AMSPopupCallback callback;

        List<SkinnedMeshRenderer> selectedRenderers = new List<SkinnedMeshRenderer>();

        public AMSSelectRendererPopup(
            GameObject _avatarRoot,
            IReadOnlyCollection<SkinnedMeshRenderer> _selectedRenderers,
            AMSPopupCallback _callback)
        {
            avatarRoot = _avatarRoot;
            selectedRenderers = new List<SkinnedMeshRenderer>(_selectedRenderers);
            callback = _callback;
        }

        public override Vector2 GetWindowSize()
        {
            return size;
        }

        public void DrawRendererLoop(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (renderers.Length == 0)
                return;

            EditorGUI.indentLevel++;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r.gameObject == root)
                    continue;

                if (r.sharedMesh == null || r.sharedMesh.blendShapeCount == 0)
                    continue;

                bool isChecked = selectedRenderers.Contains(r);
                EditorGUI.BeginChangeCheck();
                isChecked = EditorGUILayout.ToggleLeft(r.name, isChecked);
                if (EditorGUI.EndChangeCheck())
                {
                    if (!isChecked && selectedRenderers.Contains(r))
                        selectedRenderers.Remove(r);
                    else if (isChecked && !selectedRenderers.Contains(r))
                        selectedRenderers.Add(r);
                }

                DrawRendererLoop(r.gameObject);
            }
            EditorGUI.indentLevel--;
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Label(amsSetting.texts.selectSkinnedMeshRendererTitle, EditorStyles.boldLabel);
            GUILayout.Space(5);

            scroll = GUILayout.BeginScrollView(scroll);
            DrawRendererLoop(avatarRoot);
            GUILayout.EndScrollView();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(amsSetting.texts.select))
                callback.onClickSelectRenderer?.Invoke(selectedRenderers);

            if (GUILayout.Button(amsSetting.texts.cancel))
                callback.onCancelSelectRenderers?.Invoke();

            GUILayout.EndHorizontal();
        }
    }
}
#endif