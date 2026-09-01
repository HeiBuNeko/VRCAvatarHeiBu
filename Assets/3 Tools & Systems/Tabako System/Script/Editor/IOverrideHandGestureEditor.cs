using net.satania_shopping.tabakosystem.runtime;
using UnityEditor;
using UnityEngine;

namespace net.satania_shopping.tabakosystem.editor
{
    [CustomEditor(typeof(IOverrideHandGesture))]
    public class IOverrideHandGestureEditor : Editor
    {
        private SerializedProperty overrideMotion;
        private SerializedProperty customMotion;

        public string GetText(string id)
        {
            return LanguageManager.GetText(id);
        }

        public GUIContent GetGUIContent(string id)
        {
            return new GUIContent(GetText(id));
        }

        private void OnEnable()
        {
            overrideMotion = serializedObject.FindProperty("overrideMotion");
            customMotion = serializedObject.FindProperty("customMotion");
        }

        public override void OnInspectorGUI()
        {
            SatabakoEditorUtils.DrawHeader(100);
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUILayout.PropertyField(overrideMotion, GetGUIContent("t_overrideMotion"));
            EditorGUILayout.PropertyField(customMotion, GetGUIContent("t_Motion"));

            GUILayout.Space(10);

            int newLanguage = EditorGUILayout.Popup(GetText("t_Language"), LanguageManager.LanguageIndex, LanguageManager.LanguageNames);
            if (newLanguage != LanguageManager.LanguageIndex)
            {
                LanguageManager.ChangeLanguage(newLanguage);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}