using UnityEditor;
using UnityEngine;

namespace RedNightWorks.NadeSystem
{
    [CustomEditor(typeof(NadeSystemSettings))]
    public class NadeSystemEditor : Editor
    {
        private string headerGUID = "6b54478850692ef48ae413dce0a414fb";
        private Texture2D header;
        [SerializeField] private bool _advancedSettingsFoldout;

        private void OnEnable()
        {
            header = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(headerGUID));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (header != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(header, GUILayout.Width(300), GUILayout.Height(40));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(NadeSystemVersion.DisplayText, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            var languageIndex = Application.systemLanguage == SystemLanguage.Japanese ? 1
                : Application.systemLanguage == SystemLanguage.Korean ? 2
                : Application.systemLanguage == SystemLanguage.ChineseTraditional ? 4
                : Application.systemLanguage == SystemLanguage.ChineseSimplified
                    || Application.systemLanguage == SystemLanguage.Chinese ? 3 : 0;
            var menuLanguage = serializedObject.FindProperty("menuLanguage");
            var menuLanguageRect = EditorGUILayout.GetControlRect();
            var menuLanguageLabel = EditorGUI.BeginProperty(menuLanguageRect,
                new GUIContent(Localize.menuLanguageLabel[languageIndex]), menuLanguage);
            EditorGUI.BeginChangeCheck();
            int selectedLanguage = EditorGUI.Popup(menuLanguageRect, menuLanguageLabel.text,
                menuLanguage.intValue, Localize.MenuLanguageOptions(languageIndex));
            if (EditorGUI.EndChangeCheck()) menuLanguage.intValue = selectedLanguage;
            EditorGUI.EndProperty();
            EditorGUILayout.Space(4);

            SerializedProperty audioClipsProperty = serializedObject.FindProperty("audioClips");
            if (audioClipsProperty.arraySize != 16)
                audioClipsProperty.arraySize = 16;

            EditorGUILayout.LabelField(Localize.soundSettings[languageIndex], EditorStyles.boldLabel);
            for (int i = 0; i < audioClipsProperty.arraySize; i++)
            {
                var element = audioClipsProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(element,
                    new GUIContent($"{Localize.soundSlot[languageIndex]} {i + 1}"));
            }
            EditorGUILayout.Space(4);

            _advancedSettingsFoldout = EditorGUILayout.Foldout(_advancedSettingsFoldout,
                Localize.compatibilitySettings[languageIndex], true, EditorStyles.foldoutHeader);
            if (_advancedSettingsFoldout)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16);
                EditorGUILayout.BeginVertical();
                var writeDefaults = serializedObject.FindProperty("writeDefaultsMode");
                var rect = EditorGUILayout.GetControlRect();
                var label = EditorGUI.BeginProperty(rect, new GUIContent("Write Defaults"), writeDefaults);
                EditorGUI.BeginChangeCheck();
                int value = EditorGUI.Popup(rect, label.text, writeDefaults.intValue,
                    Localize.writeDefaultsOptions[languageIndex]);
                if (EditorGUI.EndChangeCheck()) writeDefaults.intValue = value;
                EditorGUI.EndProperty();
                if (writeDefaults.intValue != (int)WriteDefaultsMode.UsePrefabSettings)
                    EditorGUILayout.HelpBox(Localize.writeDefaultsHelp[languageIndex], MessageType.Info);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
