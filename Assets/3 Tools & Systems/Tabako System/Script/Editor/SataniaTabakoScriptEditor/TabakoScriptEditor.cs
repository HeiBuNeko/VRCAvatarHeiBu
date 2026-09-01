using System;
using UnityEditor;
using UnityEngine;
using net.satania_shopping.tabakosystem.runtime;
using nadena.dev.modular_avatar.core;
using System.Linq;
using System.Collections.Generic;
using net.satania_shopping.tabakosystem.editor;
using nadena.dev.ndmf.runtime;

namespace net.satania_shopping.tabakosystem
{
    [CustomEditor(typeof(TabakoScriptBehaviour))]
    public class TabakoScriptEditor : Editor
    {
        private const int MIN_SATANIA_TABAKO_EXPARAM_COUNT = 9 + 1; //Default + VRC_Fire(1)

        private TabakoScriptBehaviour script => target as TabakoScriptBehaviour;
        private bool dirty = false;
        private GUIStyle rightAligmentStyle;

        private class EditorSingleton : ScriptableSingleton<EditorSingleton>
        {
            //public bool isAdvancedExpanded = false;

            //エクスプッションメニューの詳細設定
            public bool isExMenuSettingExpanded = false;
            //エクスプッションメニューのタバコ吸うスピード
            public bool isExMenuSettingTabakoSpeedExpanded = false;
        }

        internal class MAParameterValue
        {
            private float defaultValue;
            private float fieldValue;

            public float FieldValue
            {
                get => fieldValue;
                set => fieldValue = value;
            }

            public float DefaultParameterValue => defaultValue;

            internal MAParameterValue(float defaultValue)
            {
                this.defaultValue = defaultValue;
                this.fieldValue = defaultValue;
            }
        }

        internal struct RadioButtonProperty
        {
            internal string title;
            internal float value;
        }

        internal Dictionary<string, MAParameterValue> ParametersDictionary = new Dictionary<string, MAParameterValue>()
        {
            //詳細設定
            { "Satabako_Local_Pokect_IsOn", new MAParameterValue(1) },
            { "Satabako_Local_Contact_Size", new MAParameterValue(0.5f) },
            { "Satabako_Local_Accept_Other_Contact", new MAParameterValue(1) },
            { "Satabako_Sync_Tabako_LipSync_Move_IsOn", new MAParameterValue(1) },
            { "Satabako_Local_Cancel_Snuff_out_Tabako", new MAParameterValue(1) },
            { "Satabako_Local_Accept_Swap_Tabako", new MAParameterValue(1) },

            //タバコ吸うスピード
            {"Satabako_Sync_Tabako_Smoke_Speed_Index", new MAParameterValue(0) }
        };


        private bool ToggleExpressionParameter(string parameterName, string label)
        {
            if (!ParametersDictionary.ContainsKey(parameterName))
                throw new KeyNotFoundException($"{parameterName}が含まれていません。");

            if (SatabakoEditorUtils.ToggleForFloatParameter(ParametersDictionary[parameterName].FieldValue, label, out bool newValue))
            {
                ParametersDictionary[parameterName].FieldValue = newValue ? 1f : 0f;
                dirty = true;
                return true;
            }

            return false;
        }

        private bool RadioExpressionParameter(string parameterName, RadioButtonProperty[] properties)
        {
            if (!ParametersDictionary.ContainsKey(parameterName))
                throw new KeyNotFoundException($"{parameterName}が含まれていません。");

            bool changed = false;
            float value = ParametersDictionary[parameterName].FieldValue;

            foreach (var p in properties)
            {
                bool before = Mathf.Approximately(value, p.value);
                bool after = EditorGUILayout.Toggle(p.title, before);
                if (before != after)
                {
                    ParametersDictionary[parameterName].FieldValue = p.value;
                    dirty = true;
                    changed = true;
                }
            }

            return changed;
        }

        private bool SliderExpressionParameter(string parameterName, string label, float min, float max)
        {
            if (!ParametersDictionary.ContainsKey(parameterName))
                throw new KeyNotFoundException($"{parameterName}が含まれていません。");

            float before = ParametersDictionary[parameterName].FieldValue;
            float after = EditorGUILayout.Slider(label, before, min, max);

            if (!Mathf.Approximately(before, after))
            {
                ParametersDictionary[parameterName].FieldValue = after;
                dirty = true;
                return true;
            }

            return false;
        }

        private EditorSingleton singleton => EditorSingleton.instance;

        private SerializedProperty gesture_case;
        private SerializedProperty gesture_lighter;
        private SerializedProperty gesture_fire;
        private SerializedProperty gesture_restore;
        private SerializedProperty gesture_exhalesmoke;
        private SerializedProperty gesture_swap;

        private SerializedProperty minContactSize;
        private SerializedProperty maxContactSize;
        private SerializedProperty firedAnimationLength;
        private SerializedProperty deactiveObjectOnBuild;

        private ModularAvatarParameters[] unionedParameters;

        private void OnEnable()
        {
            gesture_case = serializedObject.FindProperty("gesture_case");
            gesture_lighter = serializedObject.FindProperty("gesture_lighter");
            gesture_fire = serializedObject.FindProperty("gesture_fire");
            gesture_restore = serializedObject.FindProperty("gesture_restore");
            gesture_exhalesmoke = serializedObject.FindProperty("gesture_exhalesmoke");
            gesture_swap = serializedObject.FindProperty("gesture_swap");

            minContactSize = serializedObject.FindProperty("minContactSize");
            maxContactSize = serializedObject.FindProperty("maxContactSize");
            firedAnimationLength = serializedObject.FindProperty("firedAnimationLength");

            deactiveObjectOnBuild = serializedObject.FindProperty("deactiveObjectOnBuild");

            IEnumerable<ModularAvatarParameters> _parameters = script.SatabakoMAParameters;
            var plugins = script.Plugins;

            foreach (var plugin in plugins)
            {
                if (plugin == null)
                    continue;

                _parameters = _parameters.Union(plugin.Parameters);
            }

            unionedParameters = _parameters
                .Where(x => x != null)
                .Distinct()
                .ToArray();

            LoadExpressionMenuSetting(unionedParameters);

            //言語読み込み
            LanguageManager.InitializeLanguage();
        }

        private bool IsParentEditorOnly(Transform t)
        {
            Transform AvatarRoot = RuntimeUtil.FindAvatarInParents(t);

            Transform parent = t.parent;
            while (parent != null && parent != AvatarRoot)
            {
                if (parent.CompareTag("EditorOnly"))
                    return true;

                parent = parent.parent;
            }

            return false;
        }

        private void LoadExpressionMenuSetting(ModularAvatarParameters[] parameters)
        {
            if (parameters == null)
                return;

            IEnumerable<ModularAvatarParameters> _parameters = parameters;

            foreach (var plugin in script.Plugins)
            {
                if (plugin == null)
                    continue;

                _parameters = _parameters.Union(plugin.Parameters);
            }

            parameters = _parameters
                .Where(x => x != null)
                .Distinct()
                .ToArray();

            foreach (ModularAvatarParameters p in parameters)
            {
                if (p?.parameters == null)
                    continue;

                foreach (ParameterConfig pc in p.parameters)
                {
                    if (!ParametersDictionary.ContainsKey(pc.nameOrPrefix))
                        continue;

                    if (string.IsNullOrEmpty(pc.nameOrPrefix) || !ParametersDictionary.ContainsKey(pc.nameOrPrefix))
                        continue;

                    ParametersDictionary[pc.nameOrPrefix].FieldValue = pc.defaultValue;
                }
            }
        }


        private void SaveExpressionMenuSetting(ModularAvatarParameters[] parameters)
        {
            if (parameters == null)
                return;

            HashSet<string> checkedParameterList = new HashSet<string>();

            foreach (ModularAvatarParameters p in parameters)
            {
                bool dirty = false;
                ParameterConfig[] parameterConfigs = p.parameters.ToArray();

                for (int i = 0; i < parameterConfigs.Length; i++)
                {
                    ParameterConfig pc = parameterConfigs[i];
                    string parameterName = pc.nameOrPrefix;

                    if (checkedParameterList.Contains(parameterName))
                        continue;

                    checkedParameterList.Add(parameterName);

                    if (!ParametersDictionary.ContainsKey(parameterName))
                        continue;

                    //変更されてなかった場合はスキップ
                    if (!CheckParameterValueChanged(pc))
                        continue;

                    parameterConfigs[i] = new ParameterConfig()
                    {
                        nameOrPrefix = parameterName,
                        remapTo = pc.remapTo,
                        internalParameter = pc.internalParameter,
                        isPrefix = pc.isPrefix,
                        syncType = pc.syncType,
                        localOnly = pc.localOnly,
                        defaultValue = ParametersDictionary[parameterName].FieldValue,
                        saved = pc.saved,
                        hasExplicitDefaultValue = pc.hasExplicitDefaultValue
                    };
                    dirty = true;
                }

                if (dirty)
                {
                    Undo.RecordObject(p, "Default Value Changed");

                    p.parameters = parameterConfigs.ToList();
                    EditorUtility.SetDirty(p);
                }
            }
        }


        private void SetTagEditorOnly(GameObject go, bool isEditorOnly, bool setDirty)
        {
            go.tag = isEditorOnly ? "EditorOnly" : "Untagged";

            if (setDirty)
                EditorUtility.SetDirty(go);
        }
        private void SetTagEditorOnly(Transform t, bool isEditorOnly, bool setDirty)
        {
            t.tag = isEditorOnly ? "EditorOnly" : "Untagged";

            if (setDirty)
                EditorUtility.SetDirty(t);
        }
        private void SetTagEditorOnly(GameObject go, bool isEditorOnly, bool setDirty = true, bool withChildren = false)
        {
            if (withChildren)
            {
                foreach (var t in go.transform.GetComponentsInChildren<Transform>(true))
                {
                    if (IsEditorOnly(t) == isEditorOnly)
                        continue;

                    SetTagEditorOnly(t, isEditorOnly, setDirty);
                }
            }
            else
            {
                SetTagEditorOnly(go, isEditorOnly, setDirty);
            }
        }

        private bool IsEditorOnly(GameObject go)
        {
            //https://kan-kikuchi.hatenablog.com/entry/CompareTag
            return go.CompareTag("EditorOnly");
        }

        private bool IsEditorOnly(Transform t)
        {
            //https://kan-kikuchi.hatenablog.com/entry/CompareTag
            return t.CompareTag("EditorOnly");
        }

        private bool CheckParameterValueChanged(ParameterConfig pc)
        {
            if (string.IsNullOrEmpty(pc.nameOrPrefix))
                return false;

            //同じ値だった場合はセーブしない
            return !Mathf.Approximately(pc.defaultValue, ParametersDictionary[pc.nameOrPrefix].FieldValue);
        }

        public static bool IsNullOrDestroyed(System.Object obj)
        {

            if (object.ReferenceEquals(obj, null)) return true;

            if (obj is UnityEngine.Object) return (obj as UnityEngine.Object) == null;

            return false;
        }

        private void DrawActivatePluginsGUI()
        {
            int EstimationSatabakoExParamCount = MIN_SATANIA_TABAKO_EXPARAM_COUNT;

            bool newValue = false;

            if (script.Plugins != null && script.Plugins.Length > 0)
            {

                //追加プラグイン用
                foreach (ISatabakoPlugin plugin in script.Plugins)
                {
                    if (IsNullOrDestroyed(plugin) || IsNullOrDestroyed(plugin.gameObject))
                        continue;

                    string name = plugin.gameObject.name;
                    int bitCount = plugin.UseBitCount;
                    GameObject[] exMenus = plugin.ExpressionMenuObjects.Where(x => x != null).ToArray();

                    //EditorOnlyじゃない状態がtrueにする
                    bool isNotEditorOnly = !IsEditorOnly(plugin.gameObject);
                    foreach (GameObject exmenu in exMenus)
                    {
                        if (exmenu == null)
                            continue;

                        //EditorOnlyだった場合はbreak
                        if (!isNotEditorOnly)
                            break;

                        if (IsEditorOnly(exmenu.gameObject))
                        {
                            isNotEditorOnly = false;
                            break;
                        }
                    }

                    newValue = EditorGUILayout.ToggleLeft($"{name} ({bitCount}bit)", isNotEditorOnly);

                    if (newValue != isNotEditorOnly)
                    {
                        SetTagEditorOnly(plugin.gameObject, !newValue, withChildren: true);

                        foreach (GameObject ex in exMenus)
                        {
                            SetTagEditorOnly(ex.gameObject, !newValue, withChildren: true);
                        }
                    }

                    if (newValue)
                        EstimationSatabakoExParamCount += bitCount;
                }
            }

            EditorGUILayout.LabelField($"{GetText("t_Estimated_Parameter_Usage")}: {EstimationSatabakoExParamCount}bit");
        }

        private void ResetExpressionParameter(string paramName)
        {
            if (!ParametersDictionary.ContainsKey(paramName))
                return;

            ParametersDictionary[paramName].FieldValue = ParametersDictionary[paramName].DefaultParameterValue;
            dirty = true;
        }

        public string GetText(string id)
        {
            return LanguageManager.GetText(id);
        }

        public GUIContent GetGUIContent(string id)
        {
            return new GUIContent(GetText(id));
        }

        private void DrawBuildSettingGUI()
        {
            float newMinContactSize = minContactSize.floatValue;
            float newMaxContactSize = maxContactSize.floatValue;

            SatabakoEditorUtils.MinMaxSliderWithValue(GetText("t_MinMaxContactSize"),
                ref newMinContactSize, ref newMaxContactSize,
                TabakoScriptBehaviour.k_minContactSize, TabakoScriptBehaviour.k_maxContactSize);

            EditorGUILayout.PropertyField(firedAnimationLength, new GUIContent(GetText("t_Cigarette_Burning_Animation_Duration")));
            EditorGUILayout.PropertyField(deactiveObjectOnBuild, new GUIContent(GetText("t_Hide_Mesh_on_Build")));

            //変更されていた場合は書き込み
            if (!Mathf.Approximately(newMinContactSize, minContactSize.floatValue))
            {
                newMinContactSize = (float)Math.Round(newMinContactSize, 2, MidpointRounding.AwayFromZero);
                minContactSize.floatValue = newMinContactSize;
            }

            if (!Mathf.Approximately(newMaxContactSize, maxContactSize.floatValue))
            {
                newMaxContactSize = (float)Math.Round(newMaxContactSize, 2, MidpointRounding.AwayFromZero);
                maxContactSize.floatValue = newMaxContactSize;
            }
        }

        private void DrawExpressionMenuSettingGUI()
        {
            SatabakoEditorUtils.DrawWebButton(
             GetText("t_ExpressionMenuExplanation"),
             @"https://saturnianjp.github.io/satania_shopping_document/docs/SataniaTabako/features#%E3%82%A8%E3%82%AF%E3%82%B9%E3%83%97%E3%83%AC%E3%83%83%E3%82%B7%E3%83%A7%E3%83%B3%E3%83%A1%E3%83%8B%E3%83%A5%E3%83%BC");

            //追加する時はMAParameterDictionaryと

            singleton.isExMenuSettingExpanded = EditorGUILayout.Foldout(singleton.isExMenuSettingExpanded, GetGUIContent("t_Advanced_Settings"));
            if (singleton.isExMenuSettingExpanded)
            {
                EditorGUI.indentLevel++;

                ToggleExpressionParameter("Satabako_Local_Pokect_IsOn", GetText("t_Pocket_Detection"));
                SliderExpressionParameter("Satabako_Local_Contact_Size", GetText("t_Contact_Size"), 0, 1);
                ToggleExpressionParameter("Satabako_Local_Accept_Other_Contact", GetText("t_Receive_from_Others"));
                ToggleExpressionParameter("Satabako_Sync_Tabako_LipSync_Move_IsOn", GetText("t_Jiggle_Cigarette_with_LipSync"));
                ToggleExpressionParameter("Satabako_Local_Cancel_Snuff_out_Tabako", GetText("t_Disable_Extinguishing_Cigarette"));
                ToggleExpressionParameter("Satabako_Local_Accept_Swap_Tabako", GetText("t_Hand-Switching_Feature"));

                EditorGUI.indentLevel--;
            }

            singleton.isExMenuSettingTabakoSpeedExpanded = EditorGUILayout.Foldout(singleton.isExMenuSettingTabakoSpeedExpanded,
                GetGUIContent("t_Smoking_Speed"));

            if (singleton.isExMenuSettingTabakoSpeedExpanded)
            {
                EditorGUI.indentLevel++;
                RadioExpressionParameter("Satabako_Sync_Tabako_Smoke_Speed_Index", new RadioButtonProperty[]
                {
                    new RadioButtonProperty()
                    {
                         title = GetText("t_Normal"),
                         value = 0
                    },
                    new RadioButtonProperty()
                    {
                         title = GetText("t_Fast"),
                         value = 1
                    },
                    new RadioButtonProperty()
                    {
                         title = GetText("t_Stop"),
                         value = 2
                    },
                    new RadioButtonProperty()
                    {
                         title = GetText("t_Restore_Cigarette_Length"),
                         value = 3
                    },
                });
                EditorGUI.indentLevel--;
            }

            if (dirty)
            {
                SaveExpressionMenuSetting(unionedParameters);
                dirty = false;
            }
        }

        public override void OnInspectorGUI()
        {
            if (rightAligmentStyle == null)
            {
                rightAligmentStyle = new GUIStyle(GUI.skin.label);
                rightAligmentStyle.alignment = TextAnchor.MiddleRight;
                rightAligmentStyle.fontStyle = FontStyle.Bold;
            }

            SatabakoEditorUtils.DrawHeader(300);
            SatabakoEditorUtils.DrawWebButton(
                GetText("t_FAQ"),
                @"https://saturnianjp.github.io/satania_shopping_document/docs/SataniaTabako/FAQ");
            SatabakoEditorUtils.DrawWebButton(
                GetText("t_HowToUse"),
                @"https://saturnianjp.github.io/satania_shopping_document/docs/SataniaTabako/Install/for_supported_avatar");
            SatabakoEditorUtils.DrawWebButtonWithCustomIcon(
                GetText("BOOTH"),
                EditorGUIUtils.BoothUrl,
                SatabakoEditorUtils.BoothIconContent);

            serializedObject.UpdateIfRequiredOrScript();

            //Hand Gestures
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label(GetText("t_HandGesture"), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(gesture_case, GetGUIContent("t_Taking_out_the_case"));
                EditorGUILayout.PropertyField(gesture_lighter, GetGUIContent("t_Taking_out_the_lighter"));
                EditorGUILayout.PropertyField(gesture_fire, GetGUIContent("t_Lighting_the_lighter"));
                EditorGUILayout.PropertyField(gesture_restore, GetGUIContent("t_Putting_out_the_cigarette"));
                EditorGUILayout.PropertyField(gesture_exhalesmoke, GetGUIContent("t_Exhaling_smoke"));
                EditorGUILayout.PropertyField(gesture_swap, GetGUIContent("t_Switching_hands"));
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(5);

            //Build Settings
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label(GetText("t_BuildSettings"), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                DrawBuildSettingGUI();
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(5);

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label(GetText("t_Expression_Menu"), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                DrawExpressionMenuSettingGUI();
                EditorGUI.indentLevel--;
            }

            //追加機能
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label(GetText("t_Add-on_Features"), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                DrawActivatePluginsGUI();
                EditorGUI.indentLevel--;
            }
            GUILayout.Space(5);

            if (script.SomeComponents != null && script.SomeComponents.Length > 0)
                EditorGUILayout.HelpBox(GetText("t_ErrorMultipleTabaccoPrefab"), MessageType.Error);


            serializedObject.ApplyModifiedProperties();

            if (VersionChecker.ComparisonResult == VersionComparisonResult.LessThan) //アプデがある場合
            {
                //Open Pageボタンが押された場合
                if (EditorGUIUtils.HelpBoxWithButton(GetText("t_MsgLatestVersion"), "Open Page", EditorGUIUtility.IconContent("console.infoicon")))
                {
                    EditorGUIUtils.OpenBoothUrl();
                }
            }
            else if (VersionChecker.ComparisonResult == VersionComparisonResult.Invalid) //取得失敗した場合
            {
                EditorGUILayout.HelpBox(GetText("t_ErrorCheckVersion"), MessageType.Error);
            }

            if (VersionChecker.ComparisonResult != VersionComparisonResult.Invalid)
                GUILayout.Label(new GUIContent($"{GetText("t_Version")} : {VersionChecker.CurrentVersion}"), rightAligmentStyle);

            int newLanguage = EditorGUILayout.Popup(GetText("t_Language"), LanguageManager.LanguageIndex, LanguageManager.LanguageNames);
            if (newLanguage != LanguageManager.LanguageIndex)
            {
                LanguageManager.ChangeLanguage(newLanguage);
            }
        }
    }
}