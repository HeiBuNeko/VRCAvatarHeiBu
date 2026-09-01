using UnityEditor;
using net.satania_shopping.tabakosystem.runtime;
using net.satania_shopping.tabakosystem.editor;
using UnityEngine;

namespace net.satania_shopping.tabakosystem
{
    [CustomEditor(typeof(ParametersCompressor))]
    public class ParametersCompressorEditor : Editor
    {
        public string GetText(string id)
        {
            return LanguageManager.GetText(id);
        }

        public GUIContent GetGUIContent(string id)
        {
            return new GUIContent(GetText(id));
        }

        private ParametersCompressor script => target as ParametersCompressor;

        private SerializedProperty compressSettings;
        //private string[] bitSizeTexts = new string[]
        //{
        //    "圧縮しない (最大: 255)",
        //    "1Bit (最大: 1)",
        //    "2Bit (最大: 3)",
        //    "3Bit (最大: 7)",
        //    "4Bit (最大: 15)",
        //    "5Bit (最大: 31)",
        //    "6Bit (最大: 63)",
        //    "7Bit (最大: 127)",
        //};

        private void OnEnable()
        {
            compressSettings = serializedObject.FindProperty("compressSettings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUILayout.PropertyField(compressSettings);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.HelpBox(GetText("t_MsgDonotParameterRename"), MessageType.Info);
            GUILayout.Space(10);

            int newLanguage = EditorGUILayout.Popup(GetText("t_Language"), LanguageManager.LanguageIndex, LanguageManager.LanguageNames);
            if (newLanguage != LanguageManager.LanguageIndex)
            {
                LanguageManager.ChangeLanguage(newLanguage);
            }
        }
    }
}