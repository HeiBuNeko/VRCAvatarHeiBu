using UnityEditor;
using UnityEngine;

namespace RedNightWorks.NadeSystem
{
    [CustomEditor(typeof(NadeSystemSettings))]
    public class NadeSystemEditor : Editor
    {
        private string headerGUID = "6b54478850692ef48ae413dce0a414fb";
        private Texture2D header;

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

            EditorGUILayout.Space();

            // "audioClips"プロパティを取得
            SerializedProperty audioClipsProperty = serializedObject.FindProperty("audioClips");

            // 配列のサイズが16であることを確認・修正
            if (audioClipsProperty.arraySize != 16)
            {
                audioClipsProperty.arraySize = 16;
            }

            // ヘッダーラベル
            EditorGUILayout.LabelField("Nade Sounds", EditorStyles.boldLabel);

            // 16個の要素を個別に描画
            for (int i = 0; i < audioClipsProperty.arraySize; i++)
            {
                SerializedProperty elementProperty = audioClipsProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(elementProperty, new GUIContent($"Sound {i + 1}"));
            }

            //EditorGUILayout.PropertyField(serializedObject.FindProperty("nadeSoundListTarget"), new GUIContent("Nade Sound List Target"));
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("naderareSoundListTarget"), new GUIContent("Naderare Sound List Target"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}