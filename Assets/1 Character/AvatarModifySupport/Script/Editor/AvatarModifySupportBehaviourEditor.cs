#if UNITY_2022_1_OR_NEWER
using com.ams.avatarmodifysupport.database;
using com.ams.avatarmodifysupport.language;
using com.ams.avatarmodifysupport.setting;
using com.ams.avatarmodifysupport.window;
using UnityEditor.SceneManagement;
#endif
using UnityEditor;
using UnityEngine;

namespace com.ams.avatarmodifysupport.behaviour
{
    [CustomEditor(typeof(AvatarModifySupportBehaviour))]
    internal class AvatarModifySupportBehaviourEditor : Editor
    {
#if UNITY_2022_1_OR_NEWER
        internal eLanguage LanguageIndex
        {
            get
            {
                int index = EditorPrefs.GetInt("AMS_LANGUAGE_INDEX", -1);
                var osLanguage = Application.systemLanguage;
                if (index == -1)
                {
                    Debug.Log(osLanguage);
                    if (osLanguage == SystemLanguage.Japanese)
                    {
                        EditorPrefs.SetInt("AMS_LANGUAGE_INDEX", (int)eLanguage.Japanese);
                        return eLanguage.Japanese;
                    }
                    else if (osLanguage == SystemLanguage.Korean)
                    {
                        EditorPrefs.SetInt("AMS_LANGUAGE_INDEX", (int)eLanguage.Korean);
                        return eLanguage.Korean;
                    }
                    else
                    {
                        EditorPrefs.SetInt("AMS_LANGUAGE_INDEX", (int)eLanguage.English);
                        return eLanguage.English;
                    }
                }

                return (eLanguage)index;
            }
            set
            {
                int v = (int)value;
                EditorPrefs.SetInt("AMS_LANGUAGE_INDEX", v);
            }
        }
        internal static AMSSetting amsSetting { get { return AMSSetting.instance; } }
        AvatarModifySupportBehaviour ams => target as AvatarModifySupportBehaviour;

        public void OnEnable()
        {
            ams.LoadData();
        }
#endif

        public override void OnInspectorGUI()
        {
#if UNITY_2022_1_OR_NEWER
            if (amsSetting.texts == null)
            {
                amsSetting.Languages = LoadLanguageFiles();
                amsSetting.texts = amsSetting.Languages[(int)LanguageIndex];
            }

            if (PrefabStageUtility.GetCurrentPrefabStage() || !ams.gameObject.scene.IsValid())
            {
                GUILayout.Label(amsSetting.texts.cantusePreviewMode, EditorStyles.boldLabel);
                return;
            }

            if (ams.blendshapePresets.Count > 0)
            {
                GUILayout.Label(amsSetting.texts.blendshapePreset, EditorStyles.boldLabel);

                for (int i = 0; i < ams.blendshapePresets.Count; i++)
                {
                    var preset = ams.blendshapePresets[i];
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(preset.displayName);
                    if (GUILayout.Button(amsSetting.texts.apply, GUILayout.Width(LanguageIndex == eLanguage.English ? 50f : 40f)))
                        preset.ApplyToAvatar();
                    GUILayout.EndHorizontal();
                }
            }

            if (GUILayout.Button(amsSetting.texts.openSupportMenu, GUILayout.MaxHeight(30)))
            {
                AvatarModifySupportWindow.Open(ams.avatar, ams);
            }

            AMSLanguage[] LoadLanguageFiles()
            {
                var jp = CreateInstance<AMSLanguage>();
                var en = AssetDatabase.LoadAssetAtPath<AMSLanguage>(AssetDatabase.GUIDToAssetPath(Database.GUID_Language_En));
                if (en == null)
                    en = CreateInstance<AMSLanguage>();

                var kr = AssetDatabase.LoadAssetAtPath<AMSLanguage>(AssetDatabase.GUIDToAssetPath(Database.GUID_Language_Kr));
                if (kr == null)
                    kr = CreateInstance<AMSLanguage>();

                return new AMSLanguage[] { jp, en, kr };
            }
#else
            GUILayout.Label("Install VRChat SDK 3.6.0 or higher.");
#endif
        }
    }
}