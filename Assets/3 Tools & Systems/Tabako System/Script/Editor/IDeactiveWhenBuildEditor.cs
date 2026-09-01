using UnityEditor;
using UnityEngine;
using net.satania_shopping.tabakosystem.runtime;
using net.satania_shopping.tabakosystem.editor;

namespace net.satania_shopping.tabakosystem
{
    [CustomEditor(typeof(IDeactiveWhenBuild))]
    public class IDeactiveWhenBuildEditor : Editor
    {
        public string GetText(string id)
        {
            return LanguageManager.GetText(id);
        }

        public GUIContent GetGUIContent(string id)
        {
            return new GUIContent(GetText(id));
        }

        private IDeactiveWhenBuild script => target as IDeactiveWhenBuild;
        public override void OnInspectorGUI()
        {
            SatabakoEditorUtils.DrawHeader(100);

            if (script.gameObject.activeInHierarchy)
            {
                if (script.AvatarTransform == null)
                {
                    EditorGUILayout.HelpBox(GetText("t_ErrorNotFoundAvatar"), MessageType.Error);
                }
                else if (script.TabakoScriptBehaviour == null)
                {
                    EditorGUILayout.HelpBox(GetText("t_ErrorNotHasSatabako"), MessageType.Error);
                }
            }

            GUILayout.Space(10);

            int newLanguage = EditorGUILayout.Popup(GetText("t_Language"), LanguageManager.LanguageIndex, LanguageManager.LanguageNames);
            if (newLanguage != LanguageManager.LanguageIndex)
            {
                LanguageManager.ChangeLanguage(newLanguage);
            }
        }
    }
}