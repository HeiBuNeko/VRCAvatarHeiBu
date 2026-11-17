using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using UnityEngine;
using JetBrains.Annotations;

namespace VRC.SDK3A.Editor
{
    public static class AvatarEditorHelper
    {
        #region Blendshapes
        
        [PublicAPI]
        public static string[] GetBlendshapes(SkinnedMeshRenderer renderer)
        {
            var names = new string[renderer.sharedMesh.blendShapeCount];
            for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
            {
                names[i] = renderer.sharedMesh.GetBlendShapeName(i);
            }
            return names;
        }
        
        [PublicAPI]
        public static int GetBlendshapeIndex(SkinnedMeshRenderer renderer, string blendshapeName)
        {
            if (string.IsNullOrEmpty(blendshapeName))
                return -1;
            
            for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
            {
                if (renderer.sharedMesh.GetBlendShapeName(i) == blendshapeName)
                {
                    return i;
                }
            }

            return -1;
        }
        
        [PublicAPI]
        public static void DrawBlendshapesPopupButton(SerializedProperty property, string[] names, GUIContent title = null, System.Action onChange = null)
        {
            string name = "";
            if (property.propertyType == SerializedPropertyType.String)
            {
                name = string.IsNullOrEmpty(property.stringValue) ? SimpleSearchWindow.NoneText : property.stringValue;
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                name = property.intValue >= 0 && property.intValue < names.Length ? names[property.intValue] : SimpleSearchWindow.NoneText;
            }
            else
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(title != null ? title : new GUIContent(property.displayName));
            if (GUILayout.Button(name, EditorStyles.popup))
            {
                OpenSearchWindowPopup(property, new GUIContent("Blendshapes"), names, onChange);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        [PublicAPI]
        public static ISearchWindowProvider OpenSearchWindowPopup(SerializedProperty property, GUIContent title, string[] names, System.Action onChange = null)
        {
            return OpenSearchWindowPopup(title, names, index =>
            {
                if (property.propertyType == SerializedPropertyType.String)
                {
                    property.stringValue = index < 0 ? "" : names[index];
                }
                else if (property.propertyType == SerializedPropertyType.Integer)
                {
                    property.intValue = index;
                }
                else
                {
                    return;
                }
                property.serializedObject.ApplyModifiedProperties();
                onChange?.Invoke();
            });
        }

        /// <summary>
        /// Opens a flat search list, reporting the selected index (-1 for None), with an optional action entry.
        /// </summary>
        internal static ISearchWindowProvider OpenSearchWindowPopup(GUIContent title, string[] names, System.Action<int> onSelect,
            GUIContent extraActionTitle = null, System.Action onExtraAction = null)
        {
            if (_simpleSearchWindow == null)
                _simpleSearchWindow = ScriptableObject.CreateInstance<SimpleSearchWindow>();
            _simpleSearchWindow.title = title;
            _simpleSearchWindow.names = names;
            _simpleSearchWindow.onSelect = onSelect;
            _simpleSearchWindow.extraActionTitle = extraActionTitle;
            _simpleSearchWindow.onExtraAction = onExtraAction;

            var pos = Event.current != null ? GUIUtility.GUIToScreenPoint(Event.current.mousePosition) : new Vector2(400f, 300f);
            SearchWindow.Open(new SearchWindowContext(pos, 360, 360), _simpleSearchWindow);

            return _simpleSearchWindow;
        }

        static SimpleSearchWindow _simpleSearchWindow;
        class SimpleSearchWindow : ScriptableObject, ISearchWindowProvider
        {
            static readonly object ExtraAction = new object();
            public static string NoneText = "- None -";
            public string[] names;
            public GUIContent title;
            public System.Action<int> onSelect;
            public GUIContent extraActionTitle;
            public System.Action onExtraAction;

            public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
            {
                var results = new List<SearchTreeEntry>();
                results.Add(new SearchTreeGroupEntry(title, 0));
                results.Add(new SearchTreeEntry(new GUIContent(NoneText)) { level = 1, userData = -1 });
                if (extraActionTitle != null)
                {
                    results.Add(new SearchTreeEntry(extraActionTitle) { level = 1, userData = ExtraAction });
                }
                for (int i = 0; i < names.Length; i++)
                {
                    results.Add(new SearchTreeEntry(new GUIContent(names[i])) { level = 1, userData = i });
                }
                return results;
            }
            public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
            {
                if (ReferenceEquals(entry.userData, ExtraAction))
                {
                    onExtraAction?.Invoke();
                }
                else
                {
                    onSelect?.Invoke((int)entry.userData);
                }
                return true;
            }
        }
        #endregion
    }
}
