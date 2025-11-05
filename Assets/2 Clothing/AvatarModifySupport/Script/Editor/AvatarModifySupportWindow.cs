//Avatar Modify Support Terms of Use
//Copyright(c) 2024 AvatarModifySupport Team

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to use the Software
//without restriction, including the rights to copy, modify, merge, publish, distribute, and sublicense copies of the Software. This permission allows others who receive
//the Software to do the same, subject to the following conditions:

//The above copyright notice and this permission notice must be included in all copies or substantial portions of the Software.

//Modification of the Software is permitted; however, redistribution of modified versions is prohibited. Redistribution is only permitted in its original, unmodified form.
//Redistribution of the Software is allowed when it is provided as part of a paid 3D model for VRChat (a social virtual reality platform), even if the result is a paid
//distribution.

//THE SOFTWARE IS PROVIDED “AS IS,” WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
//PURPOSE, AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES, OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT, OR OTHERWISE, ARISING FROM OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#if VRC_SDK_VRCSDK3 && UNITY_2022_1_OR_NEWER
#region dependencies
using com.ams.avatarmodifysupport.behaviour;
using com.ams.avatarmodifysupport.callback;
using com.ams.avatarmodifysupport.colormodify;
using com.ams.avatarmodifysupport.database;
using com.ams.avatarmodifysupport.gui;
using com.ams.avatarmodifysupport.language;
using com.ams.avatarmodifysupport.preset;
using com.ams.avatarmodifysupport.setting;
using com.ams.avatarmodifysupport.utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static com.ams.avatarmodifysupport.colormodify.AMSColorModifyPresetGroup;
#endregion

namespace com.ams.avatarmodifysupport.window
{
    public class AvatarModifySupportWindow : EditorWindow
    {
        internal static AMSSetting amsSetting { get { return AMSSetting.instance; } }

        internal static GUIStyle foldoutBoxStyle = null;
        internal static GUIStyle presetBoxStyle = null;
        internal static GUIStyle gridStyle;
        internal static GUIStyle colorPresetButton = null;

        internal static Texture2D presetIconTex = null;
        internal static Texture2D helpIconTex = null;
        internal static Texture2D colorModifyIconTex = null;
        internal static Texture2D headerTex;

        /// <summary>
        /// いずれエクスポートされたプリセットファイルの読み込みに使用
        /// </summary>
        internal static readonly string Version = "0.0.12";
        enum eReason
        {
            None,
            EmptyPath,
            InvalidPath,
            LoadError,
            DeserializeError
        }
        enum ePage
        {
            Preset,
            ColorModify,
        }

        internal static Vector2 windowSize = new Vector2(500, 800);

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
        internal static string[] languagePopupTexts = new string[] { "日本語", "English", "한국어" };
        internal string GUIDtoPath(string guid) => AssetDatabase.GUIDToAssetPath(guid);

        internal static void _Save()
        {
            amsSetting.saveFlag = true;
        }
        internal static void _Repaint()
        {
            amsSetting.repaintFlag = true;
        }
        internal static void _InternalSave()
        {
            //もし作業中にBehaviourが消されてた場合
            if (amsSetting.Behaviour == null && amsSetting.AvatarGO != null)
            {
                amsSetting.Behaviour = amsSetting.AvatarGO.AddComponent<AvatarModifySupportBehaviour>();
                return;
            }

            List<Action> actions = new List<Action>();

            foreach (var p in amsSetting.blendshapePresets)
                foreach (var g in p.blendshapePresetGroups)
                {
                    if (!g.CheckRendererExistsAndRefleshPath(amsSetting.Behaviour.transform))
                        actions.Add(() => { p.blendshapePresetGroups.Remove(g); });
                }

            foreach (var a in actions)
                a?.Invoke();

            foreach (var group in amsSetting.colorPresetGroups)
                group.RemoveIfInvalidTarget(amsSetting.colorPresets);

            amsSetting.Behaviour.exportedData = JsonConvert.SerializeObject(amsSetting.blendshapePresets);
            amsSetting.Behaviour.exportedColorData = JsonConvert.SerializeObject(amsSetting.colorPresets);
            amsSetting.Behaviour.exportedColorGroupData = JsonConvert.SerializeObject(amsSetting.colorPresetGroups);

            amsSetting.Behaviour.blendshapePresets = new List<AMSBlendshapePreset>(amsSetting.blendshapePresets);
            amsSetting.Behaviour.colorPresets = new List<AMSColorModifyPreset>(amsSetting.colorPresets);
            amsSetting.Behaviour.colorPresetGroups = new List<AMSColorModifyPresetGroup>(amsSetting.colorPresetGroups);

            amsSetting.Behaviour.lastEditedVersion = Version;

            EditorUtility.SetDirty(amsSetting.Behaviour);
        }
        /// <summary>
        /// JSON読み込み後に必要なデータを再生成
        /// </summary>
        internal static void _LoadBehaviourData()
        {
            amsSetting.Behaviour.LoadData();

            amsSetting.blendshapePresets = new List<AMSBlendshapePreset>(amsSetting.Behaviour.blendshapePresets);
            amsSetting.colorPresets = new List<AMSColorModifyPreset>(amsSetting.Behaviour.colorPresets);
            amsSetting.colorPresetGroups = new List<AMSColorModifyPresetGroup>(amsSetting.Behaviour.colorPresetGroups);
        }

        static int IndexOf(Array arr, object val) => Array.IndexOf(arr, val);

#region Button
        internal static bool IsLeftClicked(Rect rect, Event e)
        {
            return rect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0 && e.isMouse;
        }
        internal static bool IsRightClicked(Rect rect, Event e)
        {
            return rect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 1 && e.isMouse;
        }
#endregion

#region GUI
        internal bool IsProSkin() => EditorGUIUtility.isProSkin;
        internal static void AlwaysOpenedFoldout(string title)
        {
            AlwaysOpenedFoldout(title, out Rect rect, null);
        }
        internal static void AlwaysOpenedFoldout(string title, Texture2D icon, float iconSize = 20f)
        {
            AlwaysOpenedFoldout(title, out Rect rect, icon, iconSize);
        }
        internal static void AlwaysOpenedFoldout(string title, out Rect rect, Texture2D icon, float iconSize = 20f)
        {
            float h = 30f;
            var style = AMSGuiUtility.ShurikenFoldoutStyle(h);

            Vector2 textSize = style.CalcSize(new GUIContent(title));

            rect = GUILayoutUtility.GetRect(0, 0, style, GUILayout.ExpandWidth(true), GUILayout.MinHeight(h), GUILayout.MaxHeight(h));
            rect.y += 4;

            Rect toggleRect = new Rect();

            if (icon != null && Event.current.type == EventType.Repaint)
            {
                toggleRect = new Rect(rect.x + (rect.width * 0.5f) - (textSize.x * 0.5f) - (iconSize * 0.5f) - 1f,
                                          rect.y + (rect.height * 0.5f) - (iconSize * 0.5f) - 1.5f,
                                          iconSize,
                                          iconSize);

                style.contentOffset += new Vector2(iconSize * 0.5f, 0);
            }

            GUI.Box(rect, title, style);

            if (icon != null && Event.current.type == EventType.Repaint)
                GUI.DrawTexture(toggleRect, icon);

        }
        internal static void CategoryHeader(string title, Texture2D icon, Action onHelpClicked, float iconSize = 20f)
        {
            float h = 35f;
            var style = AMSGuiUtility.ShurikenFoldoutStyle(h);

            Vector2 textSize = style.CalcSize(new GUIContent(title));

            var rect = GUILayoutUtility.GetRect(0, 0, style, GUILayout.ExpandWidth(true), GUILayout.MinHeight(h), GUILayout.MaxHeight(h));
            rect.y += 4;

            Rect toggleRect = new Rect();

            if (icon != null && Event.current.type == EventType.Repaint)
            {
                toggleRect = new Rect(rect.x + (rect.width * 0.5f) - (textSize.x * 0.5f) - (iconSize * 0.5f) - 1f,
                                          rect.y + (rect.height * 0.5f) - (iconSize * 0.5f) - 1.5f,
                                          iconSize,
                                          iconSize);

                style.contentOffset += new Vector2(iconSize * 0.5f, 0);
            }

            GUI.Box(rect, title, style);

            if (icon != null && Event.current.type == EventType.Repaint)
                GUI.DrawTexture(toggleRect, icon);

            float helpMarkSize = 19;
            Rect helpIconRect = new Rect(
                rect.x + rect.width - helpMarkSize - 5,
                rect.y + 6f,
                helpMarkSize,
                helpMarkSize);

            GUI.DrawTexture(helpIconRect, helpIconTex);
            EditorGUIUtility.AddCursorRect(helpIconRect, MouseCursor.Link);
            if (IsLeftClicked(helpIconRect, Event.current))
            {
                Event.current.Use();
                onHelpClicked?.Invoke();
            }
        }
        internal static bool Foldout(string title, bool display, out Rect rect, float adjustClickAreaSize = 1.0f, GenericMenu menu = null)
        {
            float h = 30f;
            var style = AMSGuiUtility.ShurikenFoldoutStyle(h);

            Vector2 textSize = style.CalcSize(new GUIContent(title));

            rect = GUILayoutUtility.GetRect(0, 0, style, GUILayout.ExpandWidth(true), GUILayout.MinHeight(h), GUILayout.MaxHeight(h));
            rect.y += 4;
            GUI.Box(rect, title, style);

            var e = Event.current;

            float iconSize = 13f;
            var toggleRect = new Rect(rect.x + (rect.width * 0.5f) - (textSize.x * 0.5f) - 15f, rect.y + iconSize * 0.5f, iconSize, iconSize);

            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }

            Rect mouseArea = rect;
            mouseArea.width *= Mathf.Min(adjustClickAreaSize, 1);
            EditorGUIUtility.AddCursorRect(mouseArea, MouseCursor.Link);
            if (IsLeftClicked(mouseArea, e))
            {
                display = !display;
                e.Use();
            }
            else if (menu != null && IsRightClicked(mouseArea, e))
            {
                menu.ShowAsContext();
            }

            return display;
        }
        internal static bool Foldout(string title, bool display, float adjustSize = 1.0f, GenericMenu menu = null)
        {
            return Foldout(title, display, out Rect rect, adjustSize, menu);
        }
        internal bool DrawLanguageButton(out Rect rect, eLanguage index)
        {
            float width = 85;
            rect = GUILayoutUtility.GetRect(
                0,
                0,
                GUILayout.ExpandWidth(true),
                GUILayout.MaxHeight(30),
                GUILayout.MinHeight(30));

            rect.x = rect.width - width - 4;
            rect.y += (30 - 20) / 2;
            rect.width = width;
            rect.height = 20;

            var style = AMSGuiUtility.LanguageLabelStyle(3);

            GUI.Label(rect, "▶", style);

            rect.x += 3f;

            if (GUI.Button(rect, $"{languagePopupTexts[(int)index]}"))
            {
                return true;
            }

            return false;
        }
        internal bool ShowMessage(string msg, string ok, string cancel) => EditorUtility.DisplayDialog(titleContent.text, msg, ok, cancel);
        internal void ShowMessage(string msg, string ok) => EditorUtility.DisplayDialog(titleContent.text, msg, ok);
        internal void ShowMessage(string msg) => EditorUtility.DisplayDialog(titleContent.text, msg, amsSetting.texts.ok);
        internal void InitializeGUIStyles()
        {
            if (foldoutBoxStyle == null)
            {
                string _guid = IsProSkin() ? Database.GUID_categoryElementFrame_Dark : Database.GUID_categoryElementFrame_White;
                Texture2D foldOutFrameTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(_guid));
                foldoutBoxStyle = AMSGuiUtility.CreateBoxStyle(5, 0, 5);
                foldoutBoxStyle.normal.background = foldOutFrameTex;
            }

            if (presetBoxStyle == null)
            {
                string _guid = IsProSkin() ? Database.GUID_blendshapePresetFrame_Dark : Database.GUID_blendshapePresetFrame_White;
                Texture2D presetBoxTex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(_guid));
                presetBoxStyle = AMSGuiUtility.CreateBoxStyle(5, 0, 5);
                presetBoxStyle.normal.background = presetBoxTex;
            }

            if (gridStyle == null)
                gridStyle = AMSGuiUtility.SelectionGridStyle();

            if (colorPresetButton == null)
            {
                colorPresetButton = new GUIStyle(GUI.skin.box);
                colorPresetButton.richText = true;
                colorPresetButton.fixedWidth = 109;
            }
        }
        internal void DestroyGUIStyles()
        {
            foldoutBoxStyle = null;
            presetBoxStyle = null;
            gridStyle = null;
            colorPresetButton = null;

            presetIconTex = null;
            helpIconTex = null;
            colorModifyIconTex = null;
            headerTex = null;
        }
        internal void OpenPopup(PopupWindowContent popupWindow, float width)
        {
            var pos = Event.current.mousePosition;
            pos.x -= width / 2;

            Rect presetPopup = new Rect(pos, new Vector2());

            PopupWindow.Show(presetPopup, popupWindow); //プリセット作成画面
        }
#endregion

#region Language
        internal AMSLanguage[] LoadLanguageFiles()
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
#endregion

#region BlendshapePreset
        internal static bool BlendshapePresetFoldout(string title, bool display, out Rect rect, AMSPopupCallback callback, float adjustClickAreaSize = 1)
        {
            float h = 30f;
            var style = AMSGuiUtility.ShurikenFoldoutStyle(h);

            Vector2 textSize = style.CalcSize(new GUIContent(title));

            rect = GUILayoutUtility.GetRect(0, 0, style, GUILayout.ExpandWidth(true), GUILayout.MinHeight(h), GUILayout.MaxHeight(h));
            rect.y += 4;
            GUI.Box(rect, title, style);

            var e = Event.current;

            float iconSize = 13f;
            var toggleRect = new Rect(rect.x + (rect.width * 0.5f) - (textSize.x * 0.5f) - 15f, rect.y + iconSize * 0.5f, iconSize, iconSize);

            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }

            Rect mouseArea = rect;
            mouseArea.width *= Mathf.Min(adjustClickAreaSize, 1);

            EditorGUIUtility.AddCursorRect(mouseArea, MouseCursor.Link);

            if (IsLeftClicked(mouseArea, e))
            {
                display = !display;
                e.Use();

                //TODO: isOpenedの保存場所をAMSSetting側にする
                _Save();
            }
            else if (IsRightClicked(mouseArea, e))
            {
                //右クリックでメニュー表示
                var genericMenu = new GenericMenu();
                genericMenu.AddItem(new GUIContent(amsSetting.texts.applyToAvatar), false, () => callback.onApplyToAvatar?.Invoke());
                genericMenu.AddSeparator("");
                genericMenu.AddItem(new GUIContent(amsSetting.texts.upPreset), false, () => callback.onPresetOrderUp?.Invoke());
                genericMenu.AddItem(new GUIContent(amsSetting.texts.downPreset), false, () => callback.onPresetOrderDown?.Invoke());
                genericMenu.AddSeparator("");
                genericMenu.AddItem(new GUIContent(amsSetting.texts.duplicate), false, () => callback.onPresetDuplicate?.Invoke());
                genericMenu.AddItem(new GUIContent(amsSetting.texts.remove), false, () => callback.onPresetRemove?.Invoke());
                genericMenu.AddSeparator("");
                genericMenu.AddItem(new GUIContent(amsSetting.texts.saveAsAnimation), false, () => callback.onPresetAnimationSave?.Invoke());
                genericMenu.AddItem(new GUIContent(amsSetting.texts.export), false, () => callback.onExportPreset?.Invoke());
                genericMenu.AddSeparator("");
                genericMenu.AddItem(new GUIContent(amsSetting.texts.clear), false, () => callback.onPresetClear?.Invoke());
                genericMenu.ShowAsContext();
            }

            return display;
        }

        internal void DrawBlendshapePreset(AMSBlendshapePreset preset, AMSPopupCallback callback, float adjustClickAreaSize = 1)
        {
            preset.IsOpened = BlendshapePresetFoldout(preset.displayName, preset.IsOpened, out Rect foldoutRect, callback, adjustClickAreaSize);
            Rect buttonRect = foldoutRect;

            float buttonWidth = LanguageIndex == eLanguage.English ? 50f : 40f;

            buttonRect.width = buttonWidth;
            buttonRect.height = 26.2f;
            buttonRect.x = foldoutRect.xMax - buttonWidth;
            buttonRect.y = foldoutRect.yMin + 0.4f;

            if (GUI.Button(buttonRect, amsSetting.texts.apply)) //アバターへ適用ボタン
                callback.onApplyToAvatar();

            if (preset.IsOpened)
            {
                using (new GUILayout.VerticalScope(presetBoxStyle))
                {
                    List<Action> action = new List<Action>();

                    if (GUILayout.Button(amsSetting.texts.addBlendshape))
                    {
                        var pos = Event.current.mousePosition;

                        Rect presetPopup = new Rect(pos, new Vector2());

                        List<AMSSkinnedMeshRenderer> renderers = new List<AMSSkinnedMeshRenderer>(amsSetting.renderers);

                        PopupWindow.Show(presetPopup, new AMSBlendshapePresetPopup(new Vector2(200, 300), renderers, AddBlendshapeToPreset));
                    }

                    preset.scrollPosition = GUILayout.BeginScrollView(preset.scrollPosition, GUILayout.Height(200));
                    foreach (var group in preset.blendshapePresetGroups)
                    {
                        if (group.Renderer == null)
                            continue;

                        EditorGUILayout.LabelField(group.Renderer.name, EditorStyles.boldLabel);

                        foreach (var data in group.BlendshapeDatas)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUI.BeginChangeCheck();
                            float weight = EditorGUILayout.Slider(data.BlendshapeKeyName, data.Weight, 0, 100);
                            if (EditorGUI.EndChangeCheck())
                            {
                                data.Set(weight);
                                _Save(); //Behaviour側に保存
                            }

                            if (GUILayout.Button("-", GUILayout.Width(20)))
                            {
                                action.Add(() =>
                                {
                                    group.Remove(data.BlendshapeKeyName);

                                    //グループ内にブレンドシェイプがない場合はグループごと削除
                                    if (group.BlendshapeDatas.Count == 0)
                                    {
                                        preset.blendshapePresetGroups.Remove(group);
                                    }
                                });
                            }
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.Space(5);
                    }

                    if (action.Count > 0)
                    {
                        foreach (var a in action)
                            a?.Invoke();
                    }

                    GUILayout.EndScrollView();
                }
            }

            void AddBlendshapeToPreset(SkinnedMeshRenderer renderer, int blendshapeIndex)
            {
                preset.Add(
                    renderer,
                    renderer?.sharedMesh.GetBlendShapeName(blendshapeIndex),
                    renderer.GetBlendShapeWeight(blendshapeIndex),
                    amsSetting.Avatar.transform);

                _Save();
                _Repaint();
            }
        }
#endregion

#region ColorModify
        internal static bool ColorGroupPresetFoldout(string title, bool display, out Rect rect, AMSPopupCallback callback)
        {
            float h = 30f;
            var style = AMSGuiUtility.ShurikenFoldoutStyle(h);

            Vector2 textSize = style.CalcSize(new GUIContent(title));

            rect = GUILayoutUtility.GetRect(0, 0, style, GUILayout.ExpandWidth(true), GUILayout.MinHeight(h), GUILayout.MaxHeight(h));
            rect.y += 4;
            GUI.Box(rect, title, style);

            var e = Event.current;

            float iconSize = 13f;
            var toggleRect = new Rect(rect.x + (rect.width * 0.5f) - (textSize.x * 0.5f) - 15f, rect.y + iconSize * 0.5f, iconSize, iconSize);

            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }

            Rect mouseArea = rect;

            EditorGUIUtility.AddCursorRect(mouseArea, MouseCursor.Link);

            if (IsLeftClicked(mouseArea, e))
            {
                display = !display;
                e.Use();
                _Save();
            }
            //else if (IsRightClicked(mouseArea, e))
            //{
            //    //右クリックでメニュー表示
            //    var genericMenu = new GenericMenu();
            //    genericMenu.AddItem(new GUIContent(amsSetting.texts.remove), false, () => { callback.onClickRemoveColorPreset?.Invoke(); });
            //    genericMenu.AddItem(new GUIContent(amsSetting.texts.clear), false, () => { callback.onClickClearColorPreset?.Invoke(); });
            //    genericMenu.ShowAsContext();
            //}

            return display;
        }
        internal static bool ColorPresetFoldout(string title, bool display, out Rect rect, AMSPopupCallback callback)
        {
            float h = 30f;
            var style = AMSGuiUtility.ShurikenFoldoutStyle(h);

            Vector2 textSize = style.CalcSize(new GUIContent(title));

            rect = GUILayoutUtility.GetRect(0, 0, style, GUILayout.ExpandWidth(true), GUILayout.MinHeight(h), GUILayout.MaxHeight(h));
            rect.y += 4;
            GUI.Box(rect, title, style);

            var e = Event.current;

            float iconSize = 13f;
            var toggleRect = new Rect(rect.x + (rect.width * 0.5f) - (textSize.x * 0.5f) - 15f, rect.y + iconSize * 0.5f, iconSize, iconSize);

            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
            }

            Rect mouseArea = rect;

            EditorGUIUtility.AddCursorRect(mouseArea, MouseCursor.Link);

            if (IsLeftClicked(mouseArea, e))
            {
                display = !display;
                e.Use();
                _Save();
            }
            else if (IsRightClicked(mouseArea, e))
            {
                //右クリックでメニュー表示
                var genericMenu = new GenericMenu();
                genericMenu.AddItem(new GUIContent(amsSetting.texts.remove), false, () => { callback.onClickRemoveColorPreset?.Invoke(); }); //削除
                genericMenu.AddItem(new GUIContent(amsSetting.texts.clear), false, () => { callback.onClickClearColorPreset?.Invoke(); }); //クリア
                genericMenu.AddSeparator("");
                genericMenu.AddItem(new GUIContent(amsSetting.texts.upPreset), false, () => { callback.onPresetOrderUp?.Invoke(); }); //上
                genericMenu.AddItem(new GUIContent(amsSetting.texts.downPreset), false, () => { callback.onPresetOrderDown?.Invoke(); }); //下
                genericMenu.ShowAsContext();
            }

            return display;
        }
        internal void DrawColorPreset(AMSColorModifyPreset colorP, AMSPopupCallback callback)
        {
            List<Action> actions = new List<Action>();

            if (colorP.isOpened)
            {
                using (new GUILayout.VerticalScope(presetBoxStyle))
                {
                    if (GUILayout.Button(amsSetting.texts.addColor))
                    {
                        callback.onClickAddMaterial = (material, color, name) =>
                        {
                            colorP.AddMaterial(material, color, name);

                            _Save();
                            _Repaint();
                        };

                        OpenPopup(new AMSAddMaterialPopup(callback), 300);
                    }

                    if (colorPresetButton == null)
                    {
                        colorPresetButton = new GUIStyle(GUI.skin.box);
                        colorPresetButton.richText = true;
                        colorPresetButton.fixedWidth = 109;
                    }

                    EditorGUILayout.LabelField($"Renderer:{colorP.renderer.name}, MaterialIndex:{colorP.MaterialIndex}", EditorStyles.boldLabel);
                    int count = colorP.amsMaterials.Count;
                    colorP.scroll = GUILayout.BeginScrollView(colorP.scroll, GUILayout.Height((40 * ((count / 4) + 1)))); //200
                    int index = 0;

                    if (count > 0)
                    {
                        for (int m = 0; m < count; m++)
                        {
                            if (index == 0)
                                GUILayout.BeginHorizontal();

                            if (index >= 4)
                            {
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal();
                                index = 0;
                            }

                            var amsMaterial = colorP.amsMaterials[m];
                            var label = $"<color={amsMaterial.colorCode}>●</color> {amsMaterial.displayName}";
                            var buttonRect = GUILayoutUtility.GetRect(109f, 23, colorPresetButton, GUILayout.MaxWidth(109f));
                            colorPresetButton.normal.textColor = IsProSkin() ? Color.white : Color.black;
                            GUI.Box(buttonRect, label, colorPresetButton);

                            EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
                            if (IsLeftClicked(buttonRect, Event.current))
                            {
                                if (colorP.TrySetMaterial(m))
                                    ShowMessage(amsSetting.texts.changedToMaterial.Replace("[[MATERIALNAME]]", colorP.amsMaterials[m].material?.name));
                            }
                            else if (IsRightClicked(buttonRect, Event.current))
                            {
                                //右クリックでメニュー表示
                                var genericMenu = new GenericMenu();

                                //削除
                                genericMenu.AddItem(new GUIContent(amsSetting.texts.remove), false, () => { callback.onClickRemoveMaterial(amsMaterial); });
                                genericMenu.AddSeparator("");

                                //Ping
                                genericMenu.AddItem(new GUIContent(amsSetting.texts.pingMaterial), false, () => { EditorGUIUtility.PingObject(amsMaterial.material); });
                                genericMenu.ShowAsContext();
                            }

                            index++;
                        }

                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();
                }
            }

            foreach (var act in actions)
                act?.Invoke();
        }
#endregion

#region Shapekey
        public static bool IsVRCLipSync(string name) => IndexOf(Database.vrcLipSyncKeys, name) != -1;
        public static bool IsVRCBlink(string name) => IndexOf(Database.vrcBlinkKeys, name) != -1;
        public static bool IsMMDLipSync(string name) => IndexOf(Database.mmdLipSyncKeys, name) != -1;
        public static bool IsMMDEye(string name) => IndexOf(Database.mmdEyeKeys, name) != -1;
        public static bool IsMMDEyeBlow(string name) => IndexOf(Database.mmdEyeBlowKeys, name) != -1;
        public static bool IsMMDOther(string name) => IndexOf(Database.mmdOtherKeys, name) != -1;
        public static bool IsOfficialKey(string name)
        {
            return IsVRCLipSync(name) ||
                IsVRCBlink(name) ||
                IsMMDLipSync(name) ||
                IsMMDEye(name) ||
                IsMMDEyeBlow(name) ||
                IsMMDOther(name);
        }
#endregion

#region DefaultFace Animation
        public static void AddKeyframe(AnimationClip clip, float time, float value, string propertyName, string path, Type type)
        {
            EditorCurveBinding curveBinding;
            AnimationCurve curve;

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (binding.propertyName.Equals(propertyName) && binding.path == path)
                {
                    Debug.Log(propertyName);
                    for (int j = 0; j < curve.keys.Length; j++)
                    {
                        curve.AddKey(time, value);
                        AnimationUtility.SetEditorCurve(clip, binding, curve);
                    }

                    return;
                }
            }

            curveBinding = new EditorCurveBinding();
            curveBinding.path = path;
            curveBinding.type = type;
            curveBinding.propertyName = propertyName;

            curve = new AnimationCurve();
            curve.AddKey(time, value);
            AnimationUtility.SetEditorCurve(clip, curveBinding, curve);
        }
#endregion

        internal static void Open(VRCAvatarDescriptor avatar, AvatarModifySupportBehaviour behaviour)
        {
            //既にウィンドウが開いている場合はSettingの書き込みをブロック
            if (amsSetting.isWindowOpened)
            {
                return;
            }

            amsSetting.Avatar = avatar;
            amsSetting.Behaviour = behaviour;
            amsSetting.AvatarGO = avatar.gameObject;

            var w = GetWindow<AvatarModifySupportWindow>("Avatar Modify Support");
            w.minSize = w.maxSize = windowSize;
            w.wantsMouseMove = true;
            w.Show();
        }

        private void OnGUI()
        {
            InitializeGUIStyles();

            //-------------------------------------------------------------------------------------------------------------
            //GUI area limit
            Rect windowArea = new Rect(new Vector2(AMSGuiUtility.MarginX, 0), windowSize - new Vector2(AMSGuiUtility.MarginX * 2, 0));
            GUILayout.BeginArea(windowArea);

            //-------------------------------------------------------------------------------------------------------------
            //Language
            if (DrawLanguageButton(out Rect languageWindowRect, LanguageIndex))
            {
                float diff = 20 * languagePopupTexts.Length - languageWindowRect.height;

                languageWindowRect.height = 20 * languagePopupTexts.Length;
                languageWindowRect.y -= diff;

                PopupWindow.Show(languageWindowRect, new AMSLanguagePopup(
                    languageWindowRect.size,
                    languagePopupTexts,
                    LanguageIndex,
                    OnLangPopupChanged));
            }

            //-------------------------------------------------------------------------------------------------------------
            //Draw Header
            var headerrect = AMSGuiUtility.GetAspectRect(windowArea.width, 0.1704f);
            GUI.DrawTexture(headerrect, headerTex);
            AMSGuiUtility.Space();

            //-------------------------------------------------------------------------------------------------------------
            //Page Selection
            amsSetting.pageIndex = GUILayout.SelectionGrid(amsSetting.pageIndex, new string[] { amsSetting.texts.preset, amsSetting.texts.colorModify }, 2, gridStyle);

            AMSGuiUtility.Space();

            //-------------------------------------------------------------------------------------------------------------
            //Draw Pages
            switch ((ePage)amsSetting.pageIndex)
            {
                case ePage.Preset: BlendshapePresetMenu(); break;
                case ePage.ColorModify: ColorModifyMenu(); break;
            }
            AMSGuiUtility.Space();
            GUILayout.EndArea();

#region Sub methods
            //-------------------------------------------------------------------------------------------------------------
            //Page

            //ブレンドシェイププリセットメニュー
            void BlendshapePresetMenu()
            {
                CategoryHeader(amsSetting.texts.preset, presetIconTex, () =>
                {
                    OnHelpClicked(new AMSHelpPopup(amsSetting.texts.whatPreset, amsSetting.texts.whatPresetEx)); //プリセットとは
                });

                using (new GUILayout.VerticalScope(foldoutBoxStyle))
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(amsSetting.texts.createNewPreset, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(228))) //プリセットを新規作成
                        ShowPresetAddMenu();

                    if (GUILayout.Button(amsSetting.texts.resetAvatarShapekeys, GUILayout.ExpandWidth(true))) //アバターのシェイプキーの値をリセット
                        ResetShapekeys();

                    GUILayout.EndHorizontal();

                    //メッシュの状態を元にAnimationClip作成
                    if (GUILayout.Button(amsSetting.texts.saveAsDefaultAnimation))
                    {
                        AMSPopupCallback callback = new AMSPopupCallback();
                        callback.onSelectRendererForSave = SaveDefaultAnimation;
                        OpenPopup(new AMSAnimationSelectRendererPopup(amsSetting.renderers.Select(x => x.Renderer).ToList(), callback), 200);
                    }

                    amsSetting.presetMenuScroll = EditorGUILayout.BeginScrollView(amsSetting.presetMenuScroll);
                    AMSGuiUtility.Space();

                    if (amsSetting.blendshapePresets != null)
                    {
                        for (int i = 0; i < amsSetting.blendshapePresets.Count; i++)
                        {
                            AMSPopupCallback callback = new AMSPopupCallback();
                            var preset = amsSetting.blendshapePresets[i];

                            callback.onPresetRemove = () => RemovePreset(preset);
                            callback.onPresetDuplicate = () => DuplicatePreset(preset);
                            callback.onPresetOrderUp = () => OrderUpPreset(preset);
                            callback.onPresetOrderDown = () => OrderDownPreset(preset);
                            callback.onPresetAnimationSave = () => SaveAsAnimationClip(preset);
                            callback.onPresetClear = () => ClearPreset(preset);
                            callback.onExportPreset = () => ExportPreset(preset);
                            callback.onApplyToAvatar = () => ApplyToAvatar(preset);

                            DrawBlendshapePreset(preset, callback, 0.90f);
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }

                void ShowPresetAddMenu()
                {
                    AMSPopupCallback callback = new AMSPopupCallback();
                    callback.onCreatePresetClicked = AddPresetByName;
                    callback.onCreatePresetByAvatarClicked = CreatePresetByAvatar;
                    callback.onImportPreset = ImportPreset;

                    OpenPopup(new AMSCreatePresetPopup(new Vector2(400, 200), callback, amsSetting), 400); //プリセット作成画面
                }
                void AddPresetByName(string presetName)
                {
                    AddPreset(new AMSBlendshapePreset(presetName));
                }
                void AddPreset(AMSBlendshapePreset preset)
                {
                    amsSetting.blendshapePresets.Add(preset);
                    _Repaint();
                    _Save();
                }
                void RemovePreset(AMSBlendshapePreset preset)
                {
                    if (!ShowMessage(amsSetting.texts.removePreset, amsSetting.texts.ok, amsSetting.texts.cancel)) //プリセットを削除してもよろしいでしょうか？
                        return;

                    amsSetting.blendshapePresets.Remove(preset);
                    _Save();
                }
                void DuplicatePreset(AMSBlendshapePreset preset)
                {
                    var cloned = preset.DeepClone();
                    AddPreset(cloned);
                }
                void OrderUpPreset(AMSBlendshapePreset preset)
                {
                    var list = amsSetting.blendshapePresets;
                    int index = list.IndexOf(preset);

                    if (index > 0 && index < list.Count)
                    {
                        var temp = list[index - 1];
                        list[index - 1] = list[index];
                        list[index] = temp;
                    }

                    _Save();
                }
                void OrderDownPreset(AMSBlendshapePreset preset)
                {
                    var list = amsSetting.blendshapePresets;
                    int index = list.IndexOf(preset);

                    if (index >= 0 && index < list.Count - 1)
                    {
                        var temp = list[index + 1];
                        list[index + 1] = list[index];
                        list[index] = temp;
                    }

                    _Save();
                }
                void SaveAsAnimationClip(AMSBlendshapePreset preset)
                {
                    //アニメーションファイルの保存場所を選んでください
                    string savePath = EditorUtility.SaveFilePanelInProject(
                        titleContent.text,
                        preset.displayName,
                        "anim",
                        amsSetting.texts.selectSaveAnimPath);

                    if (string.IsNullOrEmpty(savePath))
                        return;

                    AnimationClip clip = preset.CreateClip(preset.displayName, amsSetting.Avatar.transform);
                    IOUtility.SaveFile(clip, savePath, true);
                }
                void ClearPreset(AMSBlendshapePreset preset)
                {
                    //プリセットを初期化してよろしいでしょうか？
                    if (!ShowMessage(amsSetting.texts.resetAvatarShapekeys, amsSetting.texts.ok, amsSetting.texts.cancel))
                        return;

                    preset.blendshapePresetGroups.Clear();
                }
                void ApplyToAvatar(AMSBlendshapePreset preset)
                {
                    //そもそも中身ない時はスキップ
                    if (preset.blendshapePresetGroups.Count == 0)
                    {
                        ShowMessage(amsSetting.texts.applyPresetEmpty); //プリセットの中身がないエラー
                        return;
                    }

                    if (!ShowMessage(amsSetting.texts.applyPresetQuestion, amsSetting.texts.ok, amsSetting.texts.cancel)) //プリセットを適用して良いか
                        return;

                    preset.ApplyToAvatar();
                }
                void CreatePresetByAvatar(string presetName)
                {
                    AMSBlendshapePreset preset = new AMSBlendshapePreset(presetName, true);

                    for (int i = 0; i < amsSetting.renderers.Count; i++)
                    {
                        var amsR = amsSetting.renderers[i];
                        if (amsR == null || amsR.Renderer == null || !amsSetting.selectedRenderers.Contains(amsR.Renderer))
                            continue;

                        SkinnedMeshRenderer skinnedMeshRenderer = amsR.Renderer;
                        int count = amsR.blendshapeCount;
                        for (int c = 0; c < count; c++)
                        {
                            float value = skinnedMeshRenderer.GetBlendShapeWeight(c);

                            if (value > Mathf.Epsilon)
                                preset.Add(skinnedMeshRenderer, amsR.GetName(c), value, amsSetting.Avatar.transform);
                        }
                    }

                    AddPreset(preset);
                }
                void ExportPreset(AMSBlendshapePreset preset)
                {
                    if (preset == null)
                        return;

                    if (preset.blendshapePresetGroups.Count == 0)
                    {
                        ShowMessage(amsSetting.texts.exportPresetEmpty); //ブレンドシェイプが空のため...エラー
                        return;
                    }

                    string savePath = EditorUtility.SaveFilePanelInProject(
                        titleContent.text,
                        preset.displayName,
                        "preset",
                        amsSetting.texts.selectExportPresetPath);

                    if (string.IsNullOrEmpty(savePath))
                        return;

                    string json = JsonConvert.SerializeObject(preset);
                    AMSBlendshapePresetObject bpo = new AMSBlendshapePresetObject(json, Version);
                    IOUtility.SaveFile(bpo, savePath, true);
                }
                bool LoadPreset(out AMSBlendshapePreset preset, out eReason errorrReason)
                {
                    preset = null;
                    errorrReason = eReason.None;

                    string path = EditorUtility.OpenFilePanel(titleContent.text, "Assets/", "preset");
                    if (string.IsNullOrEmpty(path))
                    {
                        errorrReason = eReason.EmptyPath;
                        return false;
                    }

                    path = PathUtility.ConvertToUnityPath(path);
                    if (string.IsNullOrEmpty(path))
                    {
                        errorrReason = eReason.InvalidPath;
                        return false;
                    }

                    AMSBlendshapePresetObject bpo = AssetDatabase.LoadAssetAtPath<AMSBlendshapePresetObject>(path);
                    if (bpo == null)
                    {
                        errorrReason = eReason.LoadError;
                        return false;
                    }

                    preset = JsonConvert.DeserializeObject<AMSBlendshapePreset>(bpo.json);
                    if (preset == null)
                    {
                        errorrReason = eReason.DeserializeError;
                        return false;
                    }
                    foreach (var p in preset.blendshapePresetGroups)
                        p.LoadRenderer(amsSetting.Avatar.transform);

                    return true;
                }
                void ImportPreset()
                {
                    if (LoadPreset(out AMSBlendshapePreset preset, out eReason reason))
                    {
                        preset.IsOpened = true;
                        AddPreset(preset);
                    }
                    else
                    {
                        if (reason == eReason.InvalidPath)
                        {
                            string errorMsg = amsSetting.texts.errorExportPreset;
                            errorMsg += $"\n\n {amsSetting.texts.reason} : {amsSetting.texts.invalidPath}";
                            ShowMessage(errorMsg);
                        }
                        else if (reason == eReason.LoadError || reason == eReason.DeserializeError)
                        {
                            string errorMsg = amsSetting.texts.errorExportPreset;
                            errorMsg += $"\n\n {amsSetting.texts.reason} : {amsSetting.texts.loadError}";
                            ShowMessage(errorMsg);
                        }
                    }
                }
                void SaveDefaultAnimation(SkinnedMeshRenderer renderer, string filePath)
                {
                    AnimationClip clip = new AnimationClip();
                    Mesh mesh = renderer.sharedMesh;
                    if (mesh == null)
                        throw new NullReferenceException("Mesh is null.");

                    int keyLen = renderer.sharedMesh.blendShapeCount;

                    SkinnedMeshRenderer visemeRenderer = amsSetting.Avatar.VisemeSkinnedMesh;
                    SkinnedMeshRenderer eyelidsRenderer = amsSetting.Avatar.customEyeLookSettings.eyelidsSkinnedMesh;

                    string[] vrcLipsyncNames = amsSetting.Avatar.VisemeBlendShapes;
                    string vrcMouseOpenName = amsSetting.Avatar.MouthOpenBlendShapeName;
                    var vrcEyeLidBlendshapes = amsSetting.Avatar.customEyeLookSettings.eyelidsBlendshapes;

                    //Eyelidに設定されているシェイプキーを取得
                    string[] vrcEyelidNames = new string[0];
                    if (eyelidsRenderer != null && eyelidsRenderer.sharedMesh != null)
                    {
                        vrcEyelidNames = vrcEyeLidBlendshapes.Where(x => x > -1).Select(x => eyelidsRenderer.sharedMesh.GetBlendShapeName(x)).ToArray();
                    }

                    string transformPath = AnimationUtility.CalculateTransformPath(renderer.transform, amsSetting.Avatar.transform);

                    for (int i = 0; i < keyLen; i++)
                    {
                        string name = mesh.GetBlendShapeName(i);

                        //Avatarに設定されているリップシンクだった場合は除外
                        if (visemeRenderer == renderer && IndexOf(vrcLipsyncNames, name) != -1) continue;
                        if (IndexOf(vrcEyelidNames, name) != -1) continue;
                        if (IsOfficialKey(name)) continue;

                        float param = renderer.GetBlendShapeWeight(i);
                        AddKeyframe(clip, 0, param, "blendShape." + name, transformPath, typeof(SkinnedMeshRenderer));
                    }

                    EditorUtility.SetDirty(clip);
                    IOUtility.SaveFile(clip, filePath, true);
                }
            }

            //色改変メニュー
            void ColorModifyMenu()
            {
                CategoryHeader(amsSetting.texts.colorModify, colorModifyIconTex, () =>
                {
                    OnHelpClicked(new AMSHelpPopup(
                                    amsSetting.texts.whatColorModify,
                                    amsSetting.texts.whatColorModifyEx));
                });
                using (new GUILayout.VerticalScope(foldoutBoxStyle))
                {
                    GUILayout.BeginHorizontal();

                    //カラーグループを追加 (Rendererの1Materialにつき1グループ)
                    if (GUILayout.Button(amsSetting.texts.addGroup, GUILayout.MaxWidth(228)))
                    {
                        AMSPopupCallback callback = new AMSPopupCallback();
                        callback.onClickCreateColorPreset = AddColorPreset;

                        OpenPopup(new AMSCreateColorPresetPopup(callback), 400);
                    }

                    //アバターのマテリアルをリセット
                    if (GUILayout.Button(amsSetting.texts.resetMaterials, GUILayout.ExpandWidth(true)))
                        ResetMaterials();

                    GUILayout.EndHorizontal();

                    AMSGuiUtility.Space();

                    amsSetting.colorModifyListScroll = GUILayout.BeginScrollView(amsSetting.colorModifyListScroll);

                    //カラーを一気に適用するプリセットを描画
                    amsSetting.IsColorGroupPresetOpened = Foldout(amsSetting.texts.multiApply, amsSetting.IsColorGroupPresetOpened, out Rect rect1);
                    if (amsSetting.IsColorGroupPresetOpened)
                    {
                        DrawColorGroups();
                    }

                    AMSGuiUtility.Space();

                    //個別カラーセットのプリセットを描画
                    DrawColorPresets();
                    GUILayout.EndScrollView();
                }
            }

            //-------------------------------------------------------------------------------------------------------------
            //Functions
            void OnLangPopupChanged(eLanguage i)
            {
                LanguageIndex = i;
                amsSetting.texts = amsSetting.Languages[(int)i];
            }
            void OnHelpClicked(AMSHelpPopup helpPopup)
            {
                OpenPopup(helpPopup, 300);
            }
            void ResetShapekeys()
            {
                if (!ShowMessage(amsSetting.texts.resetShapekeyQuestion, amsSetting.texts.ok, amsSetting.texts.cancel))
                    return;

                if (string.IsNullOrEmpty(amsSetting.fbxPath))
                {
                    ShowMessage(amsSetting.texts.errorResetAvatarByEmptyFBXPath);
                    return;
                }

                GameObject fbxAvatar = AssetDatabase.LoadAssetAtPath<GameObject>(amsSetting.fbxPath);
                var renderers = fbxAvatar.GetComponentsInChildren<SkinnedMeshRenderer>().Select(x => new AMSSkinnedMeshRenderer(x)).ToList();

                foreach (var fbxRenderer in renderers)
                {
                    string fbxRendererPath = AnimationUtility.CalculateTransformPath(fbxRenderer.Renderer.transform, fbxAvatar.transform);

                    foreach (var r in amsSetting.renderers)
                    {
                        string amsRendererPath = AnimationUtility.CalculateTransformPath(r.Renderer.transform, amsSetting.Avatar.transform);

                        if (fbxRendererPath == amsRendererPath)
                        {
                            fbxRenderer.SetWeights(r.Renderer);
                            EditorUtility.SetDirty(r.Renderer);
                        }
                    }
                }

                ShowMessage(amsSetting.texts.finishResetAvatarShapekeys);
            }
            void ResetMaterials()
            {
                if (!ShowMessage(amsSetting.texts.resetMaterialsQuestion, amsSetting.texts.ok, amsSetting.texts.cancel))
                    return;

                if (string.IsNullOrEmpty(amsSetting.fbxPath))
                {
                    ShowMessage(amsSetting.texts.errorResetAvatarByEmptyFBXPath);
                    return;
                }

                GameObject fbxAvatar = AssetDatabase.LoadAssetAtPath<GameObject>(amsSetting.fbxPath);
                var renderers = fbxAvatar.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToList();

                foreach (var fbxRenderer in renderers)
                {
                    string fbxRendererPath = AnimationUtility.CalculateTransformPath(fbxRenderer.transform, fbxAvatar.transform);

                    foreach (var r in amsSetting.allRenderers)
                    {
                        string amsRendererPath = AnimationUtility.CalculateTransformPath(r.Renderer.transform, amsSetting.Avatar.transform);
                        if (fbxRendererPath.Equals(amsRendererPath))
                        {
                            r.Renderer.sharedMaterials = fbxRenderer.sharedMaterials;
                            EditorUtility.SetDirty(r.Renderer);
                        }
                    }
                }

                ShowMessage(amsSetting.texts.finishResetAvatarMaterials);
            }
            void AddColorPreset(AMSColorModifyPreset preset)
            {
                if (amsSetting.colorPresets.Where(x => x.DisplayName.Equals(preset.DisplayName)).Any())
                {
                    ShowMessage(amsSetting.texts.createGroupError_ExsitsSomeName);
                    return;
                }

                if (amsSetting.colorPresets.Where(x => x.renderer == preset.renderer && x.MaterialIndex == preset.MaterialIndex).Any())
                {
                    ShowMessage(amsSetting.texts.createGroupError_ExistsSomeGroup);
                    return;
                }

                amsSetting.colorPresets.Add(preset);
                _Save();
                _Repaint();
            }
            void DrawColorGroups()
            {
                GUILayout.BeginVertical(presetBoxStyle, GUILayout.Height(300));
                //グループ追加ボタン
                if (GUILayout.Button(amsSetting.texts.addGroup))
                {
                    AMSPopupCallback callbacks = new AMSPopupCallback();
                    callbacks.onClickSelectName = (name) =>
                    {
                        var group = new AMSColorModifyPresetGroup(name);
                        group.isOpened = true;
                        amsSetting.colorPresetGroups.Add(group);
                    };

                    OpenPopup(new AMSInputNamePopup(amsSetting.texts.nameOfGroup, amsSetting.texts.create, callbacks), 300);
                }

                AMSGuiUtility.Space();

                //リスト描画
                amsSetting.colorGroupListScroll = GUILayout.BeginScrollView(amsSetting.colorGroupListScroll);
                foreach (AMSColorModifyPresetGroup group in amsSetting.colorPresetGroups)
                {
                    GenericMenu menu = new GenericMenu();
                    //削除
                    menu.AddItem(new GUIContent(amsSetting.texts.remove), false, () => { RemoveGroup(group); });

                    group.isOpened = Foldout(group.groupName, group.isOpened, 1, menu);
                    if (group.isOpened)
                    {
                        GUILayout.BeginVertical(foldoutBoxStyle);
                        if (GUILayout.Button(amsSetting.texts.addColor))
                        {
                            AMSPopupCallback callbacks = new AMSPopupCallback();
                            callbacks.onCreateColorGroup = (colorGroup) =>
                            {
                                group.Add(colorGroup);
                                _Save();
                                _Repaint();
                            };
                            OpenPopup(new AMSAddPresetGroupPopup(group.colors.Select(x => x.presetName).ToArray(), callbacks), 300);
                        }
                        AMSGuiUtility.Space();

                        int count = group.colors.Count;
                        group.scroll = GUILayout.BeginScrollView(group.scroll, GUILayout.Height((40 * ((count / 4) + 1)))); //200
                        int index = 0;

                        if (count > 0)
                        {
                            foreach (ColorGroup color in group.colors)
                            {
                                if (index == 0)
                                    GUILayout.BeginHorizontal();

                                if (index >= 4)
                                {
                                    GUILayout.EndHorizontal();
                                    GUILayout.BeginHorizontal();
                                    index = 0;
                                }

                                //var color = group.colors[c];
                                var label = $"<color={color.colorCode}>●</color> {color.presetName}";
                                var buttonRect = GUILayoutUtility.GetRect(109f, 23, colorPresetButton, GUILayout.MaxWidth(109f));
                                colorPresetButton.normal.textColor = IsProSkin() ? Color.white : Color.black;
                                GUI.Box(buttonRect, label, colorPresetButton);

                                EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
                                if (IsLeftClicked(buttonRect, Event.current))
                                {
                                    color.InvokePreset(amsSetting.colorPresets);
                                }
                                else if (IsRightClicked(buttonRect, Event.current))
                                {
                                    //右クリックでメニュー表示
                                    var genericMenu = new GenericMenu();

                                    //削除
                                    genericMenu.AddItem(new GUIContent(amsSetting.texts.remove), false, () =>
                                    {
                                        group.Remove(color);
                                        _Save();
                                        _Repaint();
                                    });

                                    genericMenu.ShowAsContext();
                                }

                                index++;
                            }

                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndScrollView();
                        GUILayout.EndVertical();

                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            void DrawColorPresets()
            {
                //カラーグループ
                for (int i = 0; i < amsSetting.colorPresets.Count; i++)
                {
                    AMSColorModifyPreset colorP = amsSetting.colorPresets[i];
                    AMSPopupCallback callback = new AMSPopupCallback();

                    callback.onClickRemoveColorPreset = () =>
                    {
                        if (!ShowMessage(amsSetting.texts.removePreset, amsSetting.texts.ok, amsSetting.texts.cancel)) //プリセットを削除してもよろしいでしょうか？
                            return;

                        amsSetting.colorPresets.Remove(colorP);
                        _Save();
                    };
                    callback.onClickClearColorPreset = () =>
                    {
                        if (!ShowMessage(amsSetting.texts.resetPresetQuestion, amsSetting.texts.ok, amsSetting.texts.cancel)) //プリセットを削除してもよろしいでしょうか？
                            return;

                        colorP.amsMaterials.Clear();
                        _Save();
                    };
                    callback.onClickRemoveMaterial = (amsMaterial) => { colorP.amsMaterials.Remove(amsMaterial); _Save(); };
                    callback.onPresetOrderUp = () => { OrderUpPreset(colorP); };
                    callback.onPresetOrderDown = () => { OrderDownPreset(colorP); };

                    colorP.isOpened = ColorPresetFoldout(colorP.DisplayName, colorP.isOpened, out Rect rect, callback);
                    DrawColorPreset(colorP, callback);
                }
            }
            void OrderUpPreset(AMSColorModifyPreset preset)
            {
                var list = amsSetting.colorPresets;
                int index = list.IndexOf(preset);

                if (index > 0 && index < list.Count)
                {
                    var temp = list[index - 1];
                    list[index - 1] = list[index];
                    list[index] = temp;
                }

                _Save();
            }
            void OrderDownPreset(AMSColorModifyPreset preset)
            {
                var list = amsSetting.colorPresets;
                int index = list.IndexOf(preset);

                if (index >= 0 && index < list.Count - 1)
                {
                    var temp = list[index + 1];
                    list[index + 1] = list[index];
                    list[index] = temp;
                }

                _Save();
            }
            void RemoveGroup(AMSColorModifyPresetGroup group)
            {
                if (!ShowMessage(amsSetting.texts.removeGroupQues, amsSetting.texts.ok, amsSetting.texts.cancel))
                    return;

                amsSetting.colorPresetGroups.Remove(group);
                _Save();
            }
#endregion
        }
        private void OnEnable()
        {
            if (!amsSetting.isWindowOpened)
                amsSetting.isWindowOpened = true;

            if (amsSetting.Avatar == null || amsSetting.Behaviour == null)
            {
                Close();
                return;
            }

            _LoadBehaviourData();

            //--------------------------------------------------------------------------
            //Language読み込み
            amsSetting.Languages = LoadLanguageFiles();
            amsSetting.texts = amsSetting.Languages[(int)LanguageIndex];

            //--------------------------------------------------------------------------
            //Texture読み込み
            if (presetIconTex == null)
                presetIconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(GUIDtoPath(Database.GUID_BlendshapeIcon));

            if (helpIconTex == null)
                helpIconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(GUIDtoPath(Database.GUID_HelpIcon));

            if (colorModifyIconTex == null)
                colorModifyIconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(GUIDtoPath(Database.GUID_ColorModifyIcon));

            if (headerTex == null)
                headerTex = AssetDatabase.LoadAssetAtPath<Texture2D>(GUIDtoPath(Database.GUID_header1));

            amsSetting.allRenderers = new List<AMSSkinnedMeshRenderer>();
            amsSetting.allRenderers.Clear();

            amsSetting.renderers = new List<AMSSkinnedMeshRenderer>();
            amsSetting.renderers.Clear();

            amsSetting.selectedRenderers = new List<SkinnedMeshRenderer>();
            amsSetting.selectedRenderers.Clear();

            //Blendshapeが存在するSkinnedMeshRendererだけをリスト化
            var renderers = amsSetting.Avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToList();

            //シェイプキーリセット機能のために元のシェイプキーの値を取得
            amsSetting.fbxPath = AssetDatabase.GetAssetPath(amsSetting.Avatar.GetComponent<Animator>()?.avatar);
            for (int i = 0; i < renderers.Count; i++)
            {
                SkinnedMeshRenderer r = renderers[i];
                AMSSkinnedMeshRenderer amsR = new AMSSkinnedMeshRenderer(r);
                amsR.categoriedBlendshape = amsR.GetCategorizedBlendshapes();
                amsR.categoryIsOpened = new bool[amsR.categoriedBlendshape.Count];

                amsSetting.allRenderers.Add(amsR);

                if (r.sharedMesh?.blendShapeCount > 0)
                    amsSetting.renderers.Add(amsR);

            }
        }
        private void OnDisable()
        {
            if (amsSetting.isWindowOpened)
                amsSetting.isWindowOpened = false;

            _InternalSave();
            DestroyGUIStyles();
        }
        private void Update()
        {
            if (amsSetting.saveFlag)
            {
                _InternalSave();
                amsSetting.saveFlag = false;
            }

            if (amsSetting.repaintFlag)
            {
                Repaint();
                amsSetting.repaintFlag = false;
            }
        }
    }
}
#endif