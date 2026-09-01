using net.satania_shopping.tabakosystem;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static satania.tabakosystem.TabakoScript;

namespace satania.tabakosystem
{
    [CustomEditor(typeof(TabakoScript))]
    public class TabakoScriptEditor : Editor
    {
        private const int MAX_SOUND_LENGTH = 2;
        private const int MAX_MATERIAL_LENGTH = 4;

        private enum eObject
        {
            Tabako,
            Case,
            Lighter
        }

        private TabakoScript script => target as TabakoScript;

        private string[] guid_sound_array = new string[] {
            "82e2fb4da2e2d0f48ab39223e7feff2d", //Tabako System/Resource/Sound/TobaccoGimmickSE_Lighter,Fire_Real_V1.wav
            "0e181c785203d2c42be4d0ce3c58221c"  //Tabako System/Resource/Sound/TobaccoGimmickSE_Lighter,Fire_V1.1.wav
        };

        private string[] guid_material_array = new string[] {
                "24f00faa892b9e64d894cea1ec2a9d81", //Tabako System/Resource/Tabacco/Material/tabacco_black_EN.mat
                "76e726da4d7c7fc43a1a43c70909e1a5", //Tabako System/Resource/Tabacco/Material/tabacco_black_JP.mat
                "a887ff41a2e4ada449df2ec9f371d359", //Tabako System/Resource/Tabacco/Material/tabacco_white_EN.mat
                "34210222859e8bf479d87e66de1806f0"  //Tabako System/Resource/Tabacco/Material/tabacco_white_JP.mat
        };

        private AudioClip[] presetSoundArray = new AudioClip[2];
        private Material[] presetMaterialArray = new Material[4];

        private GUIStyle rightAligmentStyle;

        private SerializedProperty _gesture_case;
        private SerializedProperty _gesture_lighter;
        private SerializedProperty _gesture_fire;
        private SerializedProperty _gesture_restore;
        private SerializedProperty _gesture_smoke;
        private SerializedProperty _gesture_swap;
        private SerializedProperty _case_mat;
        private SerializedProperty _tabako_mat;
        private SerializedProperty _lighter_mat;
        private SerializedProperty _custom_case_mat;
        private SerializedProperty _custom_tabako_mat;
        private SerializedProperty _custom_lighter_mat;
        private SerializedProperty __audio;
        private SerializedProperty _clip;

        private void OnEnable()
        {
            _gesture_case = serializedObject.FindProperty("gesture_case");
            _gesture_lighter = serializedObject.FindProperty("gesture_lighter");
            _gesture_fire = serializedObject.FindProperty("gesture_fire");
            _gesture_restore = serializedObject.FindProperty("gesture_restore");
            _gesture_smoke = serializedObject.FindProperty("gesture_smoke");
            _gesture_swap = serializedObject.FindProperty("gesture_swap");
            _custom_case_mat = serializedObject.FindProperty("custom_case_mat");
            _custom_tabako_mat = serializedObject.FindProperty("custom_tabako_mat");
            _custom_lighter_mat = serializedObject.FindProperty("custom_lighter_mat");
            _case_mat = serializedObject.FindProperty("case_mat");
            _tabako_mat = serializedObject.FindProperty("tabako_mat");
            _lighter_mat = serializedObject.FindProperty("lighter_mat");
            __audio = serializedObject.FindProperty("_audio");
            _clip = serializedObject.FindProperty("_clip");

            //Load Sound
            for (int i = 0; i < MAX_SOUND_LENGTH; i++)
            {
                presetSoundArray[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid_sound_array[i]));
            }

            //Load Material
            for (int i = 0; i < MAX_MATERIAL_LENGTH; i++)
            {
                presetMaterialArray[i] = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid_material_array[i]));
            }

            //バージョンロードし直し
            TabakoVersionChecker.LoadVersion();

            TabakoLanguageManager.InitializeLanguage();
            TabakoLanguageManager.UpdateLanguage();
        }

        private void DrawLogoTexture(string guid)
        {
            Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guid));

            float w = EditorGUIUtility.currentViewWidth;
            Rect rect = new Rect
            {
                width = w - 20f //40f
            };
            rect.height = rect.width / 3.0f;
            Rect rect2 = GUILayoutUtility.GetRect(rect.width, rect.height);
            rect.x = ((EditorGUIUtility.currentViewWidth - rect.width) * 0.5f) + 10f - 2f;
            rect.y = rect2.y;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
        }

        public void SwapMaterialMain()
        {
            foreach (var skin in script.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (skin.name == "タバコ本体" || skin.name == "cigarette")
                {
                    skin.sharedMaterials = new Material[] { GetTabakoMaterialByIndex(script.tabako_mat, eObject.Tabako) };
                }

                if (skin.name == "ケース本体")
                {
                    skin.sharedMaterials = new Material[] { GetTabakoMaterialByIndex(script.case_mat, eObject.Case) };
                }

                if (skin.name == "ライター本体")
                {
                    skin.sharedMaterials = new Material[] { GetTabakoMaterialByIndex(script.lighter_mat, eObject.Lighter) };
                }

                EditorUtility.SetDirty(skin);
            }

            Material GetTabakoMaterialByIndex(MaterialColor materialColor, eObject @object)
            {
                if (materialColor == MaterialColor.custom)
                {
                    if (@object == eObject.Case)
                        return script.custom_case_mat;
                    else if (@object == eObject.Lighter)
                        return script.custom_lighter_mat;
                    else if (@object == eObject.Tabako)
                        return script.custom_tabako_mat;
                }

                return presetMaterialArray[(int)materialColor];
            }
        }
        public void SwapSound()
        {
            AudioSource source = null;

            Transform sound_transform = script.transform.Find("オブジェクト/ライター/ライター本体/炎コライダー/TobaccoGimmickSE");

            if (sound_transform != null)
                source = sound_transform.GetComponent<AudioSource>();

            if (source != null)
            {
                if (script._audio == LighterAudio.v1_Real)
                {
                    source.clip = presetSoundArray[0];
                }
                else if (script._audio == LighterAudio.v1_1)
                {
                    source.clip = presetSoundArray[1];
                }
                else if (script._audio == LighterAudio.custom)
                {
                    source.clip = script._clip;
                }
                else
                {
                    source.clip = null;
                }

                EditorUtility.SetDirty(source);
            }
        }

        public static string GetText(string id) => TabakoLanguageManager.GetText(id);
        public static int LanguageIndex => TabakoLanguageManager.LanguageIndex;
        public static string[] LanguageNames => TabakoLanguageManager.LanguageNames;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (rightAligmentStyle == null)
            {
                rightAligmentStyle = new GUIStyle(GUI.skin.label);
                rightAligmentStyle.alignment = TextAnchor.MiddleRight;
                rightAligmentStyle.fontStyle = FontStyle.Bold;
            }

            EditorGUI.BeginChangeCheck();
            int newLanguage = EditorGUILayout.Popup(
                GetText("t_Language"),
                LanguageIndex,
                LanguageNames);
            if (newLanguage != TabakoLanguageManager.LanguageIndex)
            {
                TabakoLanguageManager.ChangeLanguage(newLanguage);
            }

            DrawLogoTexture("0ec7e63ff037b1e46b74731d93fdbdcf");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(GetText("t_HandgestureForUse"), EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("このプレハブは過去バージョン用のプレハブです。\n現在のバージョンのさたにあ式タバコでは動作しません。\nTabako Prefab Updaterを使用してアップデートしてください。", MessageType.Warning);

            EditorGUI.BeginDisabledGroup(true);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_gesture_case, new GUIContent(GetText("t_Case")));
            EditorGUILayout.PropertyField(_gesture_lighter, new GUIContent(GetText("t_Lighter")));
            EditorGUILayout.PropertyField(_gesture_fire, new GUIContent(GetText("t_UseFire")));
            EditorGUILayout.PropertyField(_gesture_restore, new GUIContent(GetText("t_PutoutCigarette")));
            EditorGUILayout.PropertyField(_gesture_smoke, new GUIContent(GetText("t_ExhaleSmoke")));
            EditorGUILayout.PropertyField(_gesture_swap, new GUIContent(GetText("t_MoveCigarette")));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }

            EditorGUILayout.Space(5);

            //Material
            EditorGUILayout.LabelField(GetText("t_Materials"), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            _case_mat.intValue = EditorGUILayout.Popup(GetText("t_Case"), _case_mat.intValue, GetText("t_MaterialColorNames").Split('|').ToArray());
            if ((MaterialColor)_case_mat.enumValueIndex == MaterialColor.custom)
            {
                EditorGUILayout.PropertyField(_custom_case_mat, new GUIContent(GetText("t_CaseMaterial")));
                EditorGUILayout.Space(5);
            }

            _tabako_mat.intValue = EditorGUILayout.Popup(GetText("t_Cigarette"), _tabako_mat.intValue, GetText("t_MaterialColorNames").Split('|').ToArray());
            if ((MaterialColor)_tabako_mat.enumValueIndex == MaterialColor.custom)
            {
                EditorGUILayout.PropertyField(_custom_tabako_mat, new GUIContent(GetText("t_CigaretteMaterial")));
                EditorGUILayout.Space(5);
            }

            _lighter_mat.intValue = EditorGUILayout.Popup(GetText("t_Lighter"), _lighter_mat.intValue, GetText("t_MaterialColorNames").Split('|').ToArray());
            if ((MaterialColor)_lighter_mat.enumValueIndex == MaterialColor.custom)
            {
                EditorGUILayout.PropertyField(_custom_lighter_mat, new GUIContent(GetText("t_LighterMaterial")));
                EditorGUILayout.Space(5);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                SwapMaterialMain();
            }

            //Sound
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(GetText("t_Sounds"), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            __audio.intValue = EditorGUILayout.Popup(GetText("t_LighterSound"), __audio.intValue, GetText("t_LighterSoundAudioNames").Split('|').ToArray());

            if ((LighterAudio)__audio.enumValueIndex == LighterAudio.custom)
                EditorGUILayout.PropertyField(_clip, new GUIContent("AudioClip"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                SwapSound();
            }

            if (TabakoVersionChecker.CanUpdate)
                EditorGUILayout.HelpBox(GetText("t_Message_CanUpdate"), MessageType.Info);

            GUILayout.Label(new GUIContent($"{GetText("t_Version")} : {TabakoVersionChecker.InstalledVersion}"), rightAligmentStyle);

            EditorGUI.EndDisabledGroup();
        }
    }
}