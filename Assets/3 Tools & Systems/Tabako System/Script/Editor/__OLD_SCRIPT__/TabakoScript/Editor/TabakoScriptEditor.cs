using net.satania_shopping.tabakosystem.editor;
using UnityEditor;
using UnityEngine;

namespace satania.tabakosystem
{
    [CustomEditor(typeof(TabakoScript))]
    public class TabakoScriptEditor : Editor
    {
        private static readonly string[] k_gestureFields = { "gesture_case", "gesture_lighter", "gesture_fire", "gesture_restore", "gesture_smoke", "gesture_swap" };

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(LanguageManager.GetText("t_OldScript_Notice"), MessageType.Warning);
            if (GUILayout.Button(LanguageManager.GetText("t_OldScript_OpenUpdater")))
                EditorApplication.ExecuteMenuItem(MenuItemDictionary.p_PrefabUpdater);

            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
            {
                foreach (var name in k_gestureFields)
                {
                    var property = serializedObject.FindProperty(name);
                    if (property != null)
                        EditorGUILayout.PropertyField(property);
                }
            }
        }
    }
}
