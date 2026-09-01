using net.satania_shopping.tabakosystem.editor;
using net.satania_shopping.tabakosystem.runtime;
using UnityEditor;
using UnityEngine;

namespace net.satania_shopping.tabakosystem
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(IFollowSatabakoObject))]
    public class IFollowSatabakoObjectEditor : Editor
    {
        public string GetText(string id)
        {
            return LanguageManager.GetText(id);
        }

        public GUIContent GetGUIContent(string id)
        {
            return new GUIContent(GetText(id));
        }

        private IFollowSatabakoObject script => target as IFollowSatabakoObject;
        private SerializedProperty _object;
        private SerializedProperty autoFixLocalScale;

        private void OnEnable()
        {
            _object = serializedObject.FindProperty("_object");
            autoFixLocalScale = serializedObject.FindProperty("autoFixLocalScale");
        }

        public sealed override void OnInspectorGUI()
        {
            SatabakoEditorUtils.DrawHeader(100);

            if (!script.gameObject.activeInHierarchy)
            {
                EditorGUILayout.HelpBox(GetText("t_ObjectisDeactive_NotFollow"), MessageType.Error);
            }
            else if (!script.enabled)
            {
                EditorGUILayout.HelpBox(GetText("t_ScriptIsDisable_NotFollow"), MessageType.Error);
            }
            else if (script.AvatarTransform == null)
            {
                EditorGUILayout.HelpBox(GetText("t_ErrorNotFoundAvatar"), MessageType.Error);
            }
            else if (script.TabakoScriptBehaviour == null)
            {
                //そもそもゲームオブジェクトがOFFな場合はBehaviour見つからないっぽいので、オンの時だけエラー出し
                EditorGUILayout.HelpBox(GetText("t_ErrorNotHasSatabako"), MessageType.Error);
            }

            if (script.TargetObject == IFollowSatabakoObject.SatabakoObject.Case ||
                script.TargetObject == IFollowSatabakoObject.SatabakoObject.Tabako ||
                script.TargetObject == IFollowSatabakoObject.SatabakoObject.Lighter)
            {
                var someTargetObjectComponents = script.SomeTargetObjectComponents;
                if (someTargetObjectComponents.Length > 0)
                {
                    if (EditorGUIUtils.AutoFixHelpBox(string.Format(GetText("t_ErrorSomeTargetObject"), $"{'"'}I Follow Satania Tabako Object{'"'}", someTargetObjectComponents.Length + 1),
                        GetText("t_Delete")))
                    {
                        //削除する場合
                        foreach (var someTarget in someTargetObjectComponents)
                        {
                            if (someTarget != null)
                                Undo.DestroyObjectImmediate(someTarget.gameObject);
                        }

                        Debug.Log($"[<color=green>Satania Tabako System</color>] 削除しました。");
                    }
                }
            }

            serializedObject.UpdateIfRequiredOrScript();
            EditorGUILayout.PropertyField(_object, GetGUIContent("t_TargetFollowObject"));
            EditorGUILayout.PropertyField(autoFixLocalScale, GetGUIContent("t_AlsoTrackScale"));
            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(10);

            int newLanguage = EditorGUILayout.Popup(GetText("t_Language"), LanguageManager.LanguageIndex, LanguageManager.LanguageNames);
            if (newLanguage != LanguageManager.LanguageIndex)
            {
                LanguageManager.ChangeLanguage(newLanguage);
            }
        }
    }
}