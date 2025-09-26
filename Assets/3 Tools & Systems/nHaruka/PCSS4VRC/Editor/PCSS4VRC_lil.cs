using lilToon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.Constraint.Components;

namespace nHaruka.PCSS4VRC
{
    public class PCSS4VRC_lil : EditorWindow
    {
        private VRCAvatarDescriptor avatar;
        private int isEng = 0;
        private bool toggle = false;
        private bool WriteDefault = true;
        private bool rmMats = false;
        private string[] escapeChar = { "\\", " ", "#", "/", "!", "%", "'", "|", "?", "&", "\"", "~", "@", ";", ":", "<", ">", "=", ".", "," };
        private bool copyFX = false;
        private bool useMA = true;
        private bool useOnOff = true;
        private List<Renderer> excludeList = new List<Renderer>();
        private bool foldout = false;
        private bool useOpLight = false;
        Vector2 scroll1 = Vector2.zero;
        Vector2 scroll2 = Vector2.zero;

        [MenuItem("nHaruka/PCSS For VRC (For lilToon)")]
        private static void Init()
        {
            var window = GetWindowWithRect<PCSS4VRC_lil>(new Rect(0, 0, 600, 600));
            window.Show();
        }

        private void OnGUI()
        {
            GUIStyle style0 = new GUIStyle();
            style0.normal.textColor = Color.white;
            style0.fontSize = 16;
            style0.wordWrap = true;
            style0.fontStyle = FontStyle.Bold;

            scroll1 = EditorGUILayout.BeginScrollView(scroll1);

            if (isEng == 0)
            {
                EditorGUILayout.LabelField("リアル影システムセットアップツール for lilToon", style0);
            }
            else
            {
                EditorGUILayout.LabelField("PCSS for VRC setup tool for lilToon", style0);
            }
            GUILayout.Space(10);
            avatar =
                (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", avatar, typeof(VRCAvatarDescriptor), true);

            GUILayout.Space(5);

            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
            style.fontSize = 12;
            style.wordWrap = true;
            style.fontStyle = FontStyle.Normal;

            GUIStyle style3 = new GUIStyle(EditorStyles.helpBox);
            style3.fontSize = 14;
            style3.normal.textColor = Color.red;
            style3.wordWrap = true;
            style3.fontStyle = FontStyle.Normal;

            if (isEng == 0)
            {
                EditorGUILayout.LabelField("【PCSS For VRC 導入手順】\n" +
                    "① lilToon(Ver1.7.2以上)を導入する。\n" +
                    "③ リアル影を適用したいアバターのマテリアルは事前にシェーダーをlilToonにしておく。\n" +
                    "③ 本ツールを実行してアバターをセットアップする。\n" +
                    "※アバターや画面がマテリアルエラーになる場合は本ツールの[Clear Shader Cache]ボタンを押してみてください。\nそれでも治らない場合は、lilToonおよびPCSS4VRC（本ツール）をプロジェクトから削除し、もう一度インポートしなおしてから最初から導入しなおしてみてください。", style);
                EditorGUILayout.LabelField("***以前のバージョンを上書きインポートした場合は、\"Assets/nHaruka/PCSS4VRC\"フォルダを削除してからインポートし直してください！***", style3);
            }
            else
            {
                EditorGUILayout.LabelField("[PCSS For VRC Introduction Procedure] \n" +
                    "① Import  lilToon v1.7.2 or later.\n" +
                    "② Unify the shaders of the avatar's material to which you want to apply PCSS4VRC with lilToon.\n" +
                    "③ Run this tool to set up your avatar.\n" +
                    "※If your avatar or screen is experiencing material errors, try pressing the [Clear Shader Cache] button in this tool.\nIf this does not cure the problem, delete lilToon and PCSS4VRC (this tool) from the project, re-import them and try installing them again from scratch.", style);
                EditorGUILayout.LabelField("***If you have overwritten imported an older version, delete \"Assets/nHaruka/PCSS4VRC\"folder and re-import it!***", style3);
            }
            GUILayout.Space(5);
            if (isEng == 0)
            {
                EditorGUILayout.LabelField("マテリアルを選択するとインスペクターから設定値を調整することができます。\n" +
                 "[カスタムプロパティ]の[PCSS Shadow Settings]という項目です。\n" +
                "設定した値は一番下の[ApplyProperty All PCSS Material]ボタンを押すことで、\n全てのPCSS Shaderのマテリアルに反映させることができます。", style);
            }
            else
            {
                EditorGUILayout.LabelField("When you select a material, you can adjust its settings from the inspector.\n" +
                                "[PCSS Shadow Settings] under [Custom Properties]. \n" +
                "The values you set can be reflected in all PCSS Shader materials by pressing the [ApplyProperty All PCSS Material] button at the bottom.", style);
            }
            GUILayout.Space(5);

            if (isEng == 0)
            {
                EditorGUILayout.LabelField("※レンダータイプ\"Gem, Refraction, Overlay, FakeShadow, Fur\"はサポート外です。", style);
            }
            else
            {
                EditorGUILayout.LabelField("*The render types \"Gem, Refraction, Overlay, FakeShadow, Fur\" are not supported.", style);
            }
            GUILayout.Space(5);

            if (isEng == 0)
            {
                EditorGUILayout.LabelField("※本ツールはマテリアルを複製したうえで改変を行うものの、念のためバックアップを行うことを推奨します。", style);
            }
            else
            {
                EditorGUILayout.LabelField("*This tool will duplicate the material and then modify it, but it is recommended that you back it up just in case.", style);
            }
            GUILayout.Space(5);

            if (isEng == 0)
            {
                EditorGUILayout.LabelField("大概のトラブルへの対処法は商品ページに書いてあります。困ったらまずは商品ページを再度ご確認ください。", style3);
            }
            else
            {
                EditorGUILayout.LabelField("The solution to most problems is written on the product page. If you have any trouble, please check the product page again first.", style3);
            }

            GUILayout.Space(5);

            if (!toggle)
            {
                if (isEng == 0)
                {
                    toggle = GUILayout.Toggle(toggle, "導入手順を読みました", GUI.skin.button);
                }
                else
                {
                    toggle = GUILayout.Toggle(toggle, "I read the installation instructions.", GUI.skin.button);
                }

            }

            if (toggle)
            {
                var style4 = new GUIStyle(EditorStyles.label);
                style4.normal.textColor = Color.red;
                if (isEng == 0)
                {
                    useMA = GUILayout.Toggle(true, "Modular Avatarを使用してセットアップする（※必須になりました）");
                    GUILayout.Label("※Animator関連のみ。マテリアルは差し替えられます。", style4);
                }
                else
                {
                    useMA = GUILayout.Toggle(true, "Setup with Modular Avatar (*Now required)");
                    GUILayout.Label("*Animator related only. Materials will be replaced.", style4);
                }
                
                if (isEng == 0)
                {
                    useOnOff = GUILayout.Toggle(useOnOff, "ExメニューにOn/Offスイッチをインストールする");
                    GUILayout.Label("※ヒエラルキーに変更があった場合機能しなくなることがあります", style4);
                }
                else
                {
                    useOnOff = GUILayout.Toggle(useOnOff, "Install an On/Off switch in the Ex menu.");
                    GUILayout.Label("*May not work if there are changes to the hierarchy.", style4);
                }
                
                if (isEng == 0)
                {
                    useOpLight = GUILayout.Toggle(useOpLight, "追加ライトをインストールする");
                    GUILayout.Label("※主に撮影用を想定しています。常用は負荷の観点からお勧めしません。ExpressionMenuからOn/Offが可能です。", style4);
                }
                else
                {
                    useOpLight = GUILayout.Toggle(useOpLight, "Install Additional light.");
                    GUILayout.Label("*For photography purposes primarily. Not recommended for daily use due to performance.This can be turned on or off from the ExpressionMenu.", style4);
                }
                
                if (isEng == 0)
                {

                    rmMats = GUILayout.Toggle(rmMats, "以前生成したマテリアルがある場合削除する（Setup & Rmoveで有効）");
                    GUILayout.Label("※他のアバターと生成したマテリアルを共有している場合、当該アバターのマテリアル参照が外れてしまうので注意！", style4);
                }
                else
                {
                    rmMats = GUILayout.Toggle(rmMats, "Remove old materials. (Affects Setup & Rmove)");
                    GUILayout.Label("*Note that if the generated material is shared with another avatar, the material reference of the other avatar will be lost!", style4);
                }

                GUILayout.Space(5);

                foldout = EditorGUILayout.Foldout(foldout, "除外オブジェクト一覧 / Exclude Objects");

                if (foldout)
                {
                    EditorGUI.indentLevel++;

                    scroll2 = EditorGUILayout.BeginScrollView(scroll2, GUILayout.Height(80));

                    for (int i = 0; i < excludeList.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        excludeList[i] = (Renderer)EditorGUILayout.ObjectField(excludeList[i], typeof(Renderer), true);
                        if (GUILayout.Button("削除 / Del", GUILayout.Width(60)))
                        {
                            excludeList.RemoveAt(i);
                            EditorGUILayout.EndHorizontal();
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();

                    if (GUILayout.Button("除外リストを追加 / Add"))
                    {
                        excludeList.Add(null);
                    }

                    EditorGUI.indentLevel--;
                }

                GUILayout.Space(5);

                GUI.backgroundColor = new Color(0.3f, 0.3f, 1f);

                if (GUILayout.Button("Setup"))
                {
                    GUI.backgroundColor = Color.white;

                    if (avatar == null)
                    {
                        EditorUtility.DisplayDialog("Error", "Avatar is not set.", "OK");
                        return;
                    }
                    else if(avatar.expressionsMenu == null)
                    {
                        EditorUtility.DisplayDialog("Error", "Expressions Menu iis not set on avatar.", "OK");
                        return;
                    }
                    else if (avatar.expressionParameters == null)
                    {
                        EditorUtility.DisplayDialog("Error", "Expression Parameters is not set on avatar.", "OK");
                        return;
                    }
                    else if (avatar.baseAnimationLayers[4].animatorController == null)
                    {
                        EditorUtility.DisplayDialog("Error", "FX layer is not set on avatar.", "OK");
                        return;
                    }

                    try
                    {
                        if (Setup(avatar, rmMats))
                        {
                            if (useOnOff)
                            {
                                SetMaterialAnim();
                            }
                            EditorUtility.DisplayDialog("Finished", "Finished!", "OK");
                            
                        }
                        else
                        {
                            Remove(avatar, rmMats);
                            EditorUtility.DisplayDialog("Error", "An error occurred. See console log.", "OK");
                        }
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("Error", "An error occurred. See console log.", "OK");
                        Debug.LogException(e);
                        try
                        {
                            Remove(avatar, true);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                }

                GUI.backgroundColor = Color.red;

                if (GUILayout.Button("Remove"))
                {

                    GUI.backgroundColor = Color.white;
                    if (avatar == null)
                    {
                        EditorUtility.DisplayDialog("Error", "Avatar is not set.", "OK");
                        return;
                    }

                    try
                    {
                        Remove(avatar, rmMats);
                        EditorUtility.DisplayDialog("Finished", "Finished!", "OK");
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("Error", "An error occurred. See console log.", "OK");
                        Debug.LogException(e);
                    }
                }

                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("Clear Shader Cache"))
                {
                    try
                    {
                        Refresh();
                        EditorUtility.DisplayDialog("Finished", "Finished!", "OK");
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("Error", "An error occurred. See console log.", "OK");
                        Debug.LogException(e);
                    }
                }

                GUILayout.Space(5);

                GUI.backgroundColor = Color.cyan;

                if (isEng == 0)
                {
                    if (GUILayout.Button("簡単影設定ツールを起動する"))
                    {
                        PCSS4VRC_ParameterSetter.Init();
                    }
                }
                else
                {
                    if (GUILayout.Button("Launch Easy Configuration Tool"))
                    {
                        PCSS4VRC_ParameterSetter.Init();
                    }
                }
                GUI.backgroundColor = Color.white;

            }
            GUILayout.Space(5);

            isEng = GUILayout.SelectionGrid(isEng, new string[] { "Japanese", "English" }, 2, GUI.skin.toggle);

            GUIStyle style2 = new GUIStyle(EditorStyles.linkLabel);
            style2.fontSize = 16;
            style2.normal.textColor = Color.magenta;
            style2.wordWrap = true;
            style2.fontStyle = FontStyle.Normal;

            GUILayout.Space(5);

            if (isEng == 0)
            {
                if (GUILayout.Button("サポートリクエスト/バグ報告はこちら（Discord）", style2))
                {
                    Application.OpenURL("https://discord.gg/zuaYSC5FHg");
                }

                GUILayout.Space(5);

                if (GUILayout.Button("商品説明ページを開く", style2))
                {
                    Application.OpenURL("https://nharuka.booth.pm/items/4493526");
                }
            }
            else
            {
                if (GUILayout.Button("Click here for support requests/bug reports. (Discord)", style2))
                {
                    Application.OpenURL("https://discord.gg/zuaYSC5FHg");
                }

                GUILayout.Space(5);

                if (GUILayout.Button("Click here to open item instruction page", style2))
                {
                    Application.OpenURL("https://nharuka.booth.pm/items/4493526");
                }
            }

            GUILayout.EndScrollView();
        }

        void Refresh()
        {
            typeof(ShaderUtil).GetMethod("ClearCurrentShaderVariantCollection", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[0]);
        }

        void SetMaterialAnim()
        {
            try
            {
                var animCreater = new PCSS4VRC_MaterialAnimationCreator();
                animCreater.avatarDescriptor = avatar;
                animCreater.propNames = new List<string> { "_IsOn" };
                animCreater.minVal = new List<float> { 0f };
                animCreater.maxVal = new List<float> { 1f };
                animCreater.isToggle = new List<bool> { true };
                animCreater.Setup();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        void SetupLayers()
        {
            string[] requiredLayers =
            {
            "Player",
            "PlayerLocal",
            "MirrorReflection",
        };
            int[] requiredLayerIds =
            {
            9,
            10,
            18,
        };
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layersProp = tagManager.FindProperty("layers");
            var index = 0;
            foreach (var layerId in requiredLayerIds)
            {
                if (layersProp.arraySize > layerId)
                {
                    var sp = layersProp.GetArrayElementAtIndex(layerId);
                    if (sp != null && sp.stringValue != requiredLayers[index])
                    {
                        sp.stringValue = requiredLayers[index];
                        Debug.Log("Adding layer " + requiredLayers[index]);
                    }
                }

                index++;
            }
            tagManager.ApplyModifiedProperties();
        }

        string EscapeName(string name)
        {
            string res = name;
            foreach (var c in escapeChar)
            {
                res.Replace(c, "");
            }
            return res;
        }

        public bool Setup(VRCAvatarDescriptor avatarDescriptor, bool removeMaterials)
        {
            try
            {
                Remove(avatarDescriptor, removeMaterials);

            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }

            var escapedAvatarName = EscapeName(avatarDescriptor.name);

            SetupLayers();

            SetAvatarMaterials(avatarDescriptor);

            var shaderSettingPath = "ProjectSettings/lilToonSetting.json";
            lilToonSetting settings = CreateInstance<lilToonSetting>();
            if (File.Exists(shaderSettingPath)) JsonUtility.FromJsonOverwrite(File.ReadAllText(shaderSettingPath), settings);
            settings.LIL_OPTIMIZE_USE_FORWARDADD = true;
            settings.LIL_OPTIMIZE_USE_FORWARDADD_SHADOW = true;
            settings.LIL_OPTIMIZE_APPLY_SHADOW_FA = true;
            //lilToonSetting.BuildShaderSettingString(settings, true);
            lilToonSetting.SaveShaderSetting(settings);
            string shaderSettingString = lilToonSetting.BuildShaderSettingString(settings, true);

            var rootPath = new[] { "Assets/nHaruka/PCSS4VRC/PCSS4lilToon/Shaders" };
            var guids =  AssetDatabase.FindAssets("t:Shader", rootPath);

            foreach (var guid in guids)
            {
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceSynchronousImport);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();


            if (useMA)
            {
                var prefabMA = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/nHaruka/PCSS4VRC/PCSS_Setup_for_MA.prefab");
                var MA = (GameObject)PrefabUtility.InstantiatePrefab(prefabMA);
                MA.transform.parent = avatarDescriptor.transform;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/nHaruka/PCSS4VRC/SelfLight.prefab");
            var SelfLight = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            SelfLight.name = "SelfLight";
            SelfLight.transform.parent = avatarDescriptor.transform;

            var animator = avatar.GetComponent<Animator>();

            var rootVRCconst = SelfLight.GetComponent<VRCPositionConstraint>();
            rootVRCconst.Sources[0] = new VRC.Dynamics.VRCConstraintSource { Weight = 1.0f, SourceTransform = animator.GetBoneTransform(HumanBodyBones.Hips) };

            var targetConst = SelfLight.transform.Find("TargetSphere").GetComponent<ParentConstraint>();
            targetConst.SetSource(0, new ConstraintSource() { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Head) });
            targetConst.SetSource(1, new ConstraintSource() { weight = 0, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Chest) });

            var AimConst = SelfLight.transform.Find("AimSphere").GetComponent<ParentConstraint>();
            AimConst.SetSource(0, new ConstraintSource() { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Head) });
            AimConst.SetSource(1, new ConstraintSource() { weight = 0, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Chest) });

            var OffsetedRootConst = SelfLight.transform.Find("OffsettedRoot").GetComponent<PositionConstraint>();
            OffsetedRootConst.SetSource(0, new ConstraintSource() { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Head) });

            var AutoLightingConst = SelfLight.transform.Find("AutoLighting").GetComponent<PositionConstraint>();
            AutoLightingConst.SetSource(0, new ConstraintSource() { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Neck) });

            if(useOpLight)
            {
                var oPprefab = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/nHaruka/PCSS4VRC/OptionalLight.prefab");
                var oPSelfLight = (GameObject)PrefabUtility.InstantiatePrefab(oPprefab);
                oPSelfLight.name = "OptionalLight";
                oPSelfLight.transform.parent = avatarDescriptor.transform;

                var oProotVRCconst = oPSelfLight.GetComponent<VRCPositionConstraint>();
                oProotVRCconst.Sources[0] = new VRC.Dynamics.VRCConstraintSource { Weight = 1.0f, SourceTransform = animator.GetBoneTransform(HumanBodyBones.Hips) };

                var oPtargetConst = oPSelfLight.transform.Find("TargetSphere").GetComponent<ParentConstraint>();
                oPtargetConst.SetSource(0, new ConstraintSource() { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Head) });
                oPtargetConst.SetSource(1, new ConstraintSource() { weight = 0, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Chest) });

                var oPAimConst = oPSelfLight.transform.Find("AimSphere").GetComponent<ParentConstraint>();
                oPAimConst.SetSource(0, new ConstraintSource() { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Head) });
                oPAimConst.SetSource(1, new ConstraintSource() { weight = 0, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Chest) });

                var oPOffsetedRootConst = oPSelfLight.transform.Find("OffsettedRoot").GetComponent<PositionConstraint>();
                oPOffsetedRootConst.SetSource(0, new ConstraintSource() { weight = 1, sourceTransform = animator.GetBoneTransform(HumanBodyBones.Head) });
            }

            if (!useMA)
            {
                if (!Directory.Exists("Assets/nHaruka/PCSS4VRC/AvatarData"))
                {
                    Directory.CreateDirectory("Assets/nHaruka/PCSS4VRC/AvatarData");
                }

                if (!Directory.Exists("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName))
                {
                    Directory.CreateDirectory("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName);
                }

                AssetDatabase.CopyAsset("Assets/nHaruka/PCSS4VRC/LightControl.controller", "Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/LightControl_copy.controller");

                var AddAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/LightControl_copy.controller");

                EditorUtility.SetDirty(AddAnimatorController);

                if (WriteDefault == false)
                {
                    foreach (var layer in AddAnimatorController.layers)
                    {
                        foreach (var state in layer.stateMachine.states)
                        {
                            state.state.writeDefaultValues = false;
                        }
                    }
                }

                AnimatorController FxAnimator = null;
                try
                {
                    var FxAnimatorLayer =
                            avatarDescriptor.baseAnimationLayers.FirstOrDefault(item => item.type == VRCAvatarDescriptor.AnimLayerType.FX && item.animatorController != null);
                    FxAnimator = (AnimatorController)FxAnimatorLayer.animatorController;

                    if (copyFX)
                    {
                        AnimatorController Copied_FX = null;

                        var FxAnimatorPath = AssetDatabase.GetAssetPath(FxAnimator);
                        if (FxAnimatorPath != null && FxAnimatorPath.EndsWith("controller"))
                        {
                            if (File.Exists(FxAnimatorPath.Replace(".controller", "") + "_copy.controller"))
                            {
                                AssetDatabase.DeleteAsset(FxAnimatorPath.Replace(".controller", "") + "_copy.controller");
                            }
                            AssetDatabase.CopyAsset(FxAnimatorPath, FxAnimatorPath.Replace(".controller", "") + "_copy.controller");
                            Copied_FX = AssetDatabase.LoadAssetAtPath<AnimatorController>(FxAnimatorPath.Replace(".controller", "") + "_copy.controller");
                        }
                        else if (FxAnimatorPath != null && FxAnimatorPath.EndsWith("asset"))
                        {
                            if (File.Exists(FxAnimatorPath.Replace(".controller", "") + "_copy.asset"))
                            {
                                AssetDatabase.DeleteAsset(FxAnimatorPath.Replace(".asset", "") + "_copy.asset");
                            }
                            AssetDatabase.CopyAsset(FxAnimatorPath, FxAnimatorPath.Replace(".asset", "") + "_copy.asset");
                            Copied_FX = AssetDatabase.LoadAssetAtPath<AnimatorController>(FxAnimatorPath.Replace(".asset", "") + "_copy.asset");
                        }
                        else
                        {
                            Debug.LogError("No FX was found");
                            return false;
                        }

                        EditorUtility.SetDirty(Copied_FX);
                        avatarDescriptor.baseAnimationLayers[4].animatorController = Copied_FX;
                        FxAnimator = (AnimatorController)avatarDescriptor.baseAnimationLayers[4].animatorController;

                        DuplicateExpressions(avatarDescriptor);

                        EditorUtility.SetDirty(avatarDescriptor);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Something wrong in FX!");
                    Debug.LogException(ex);
                    return false;
                }

                EditorUtility.SetDirty(FxAnimator);

                FxAnimator.parameters = FxAnimator.parameters.Union(AddAnimatorController.parameters).ToArray();
                foreach (var layer in AddAnimatorController.layers)
                {
                    FxAnimator.AddLayer(layer);
                }
                var AddExpParam = AssetDatabase.LoadAssetAtPath<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters>("Assets/nHaruka/PCSS4VRC/LightControl_params.asset");

                avatarDescriptor.expressionParameters.parameters = avatarDescriptor.expressionParameters.parameters.Union(AddExpParam.parameters).ToArray();

                EditorUtility.SetDirty(avatarDescriptor.expressionParameters);

                if (avatarDescriptor.expressionsMenu == null)
                {
                    Debug.LogError("Expression Menu Asset is Not set!");
                    return false;
                }
                if (avatarDescriptor.expressionParameters == null)
                {
                    Debug.LogError("Expression Parameter Asset is Not set!");
                    return false;
                }

                if (avatarDescriptor.expressionsMenu.controls.Count != 8)
                {
                    var AddSubMenu = AssetDatabase.LoadAssetAtPath<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>("Assets/nHaruka/PCSS4VRC/LightControl.asset");

                    var newMenu = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control();
                    newMenu.name = "LightControl";
                    newMenu.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu;
                    newMenu.subMenu = AddSubMenu;

                    avatarDescriptor.expressionsMenu.controls.Add(newMenu);
                }
                else
                {
                    Debug.LogError("Expression Menu is full!");
                    return false;
                }
            }

            EditorUtility.SetDirty(avatarDescriptor.expressionsMenu);

            AssetDatabase.SaveAssets();

            Refresh();

            return true;
        }

        void SetAvatarMaterials(VRCAvatarDescriptor avatarDescriptor)
        {
            var renderers = avatarDescriptor.GetComponentsInChildren<Renderer>(true);

            List<string> processedMatPath = new List<string>();

            for (int r = 0; r < renderers.Length; r++)
            {
                if (excludeList.Contains(renderers[r]))
                {
                    continue;
                }

                renderers[r].receiveShadows = true;
                renderers[r].lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

                Material[] mats = new Material[renderers[r].sharedMaterials.Length];
                for (int i = 0; i < renderers[r].sharedMaterials.Length; i++)
                {
                    if (renderers[r].sharedMaterials[i] == null)
                    {
                        continue;
                    }

                    string MatPath = AssetDatabase.GetAssetPath(renderers[r].sharedMaterials[i]);

                    if (!processedMatPath.Contains<string>(MatPath))
                    {
                        processedMatPath.Add(MatPath);

                        if (renderers[r].sharedMaterials[i].shader.name.Contains("lilToon", StringComparison.OrdinalIgnoreCase) && !renderers[r].sharedMaterials[i].shader.name.Contains("Gem", StringComparison.OrdinalIgnoreCase) && !renderers[r].sharedMaterials[i].shader.name.Contains("Refraction", StringComparison.OrdinalIgnoreCase) && !renderers[r].sharedMaterials[i].shader.name.Contains("Optional", StringComparison.OrdinalIgnoreCase))
                        {
                            if (MatPath.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) && !MatPath.EndsWith("_pcss.mat", StringComparison.OrdinalIgnoreCase))
                            {
                                if (AssetDatabase.LoadAssetAtPath<Material>(MatPath.Replace(".mat", "_pcss.mat", StringComparison.OrdinalIgnoreCase)) == null)
                                {
                                    AssetDatabase.CopyAsset(MatPath, MatPath.Replace(".mat", "_pcss.mat", StringComparison.OrdinalIgnoreCase));
                                }
                                mats[i] = AssetDatabase.LoadAssetAtPath<Material>(MatPath.Replace(".mat", "_pcss.mat", StringComparison.OrdinalIgnoreCase));
                            }
                            else if (MatPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) && !MatPath.EndsWith("_pcss.asset", StringComparison.OrdinalIgnoreCase))
                            {
                                if (AssetDatabase.LoadAssetAtPath<Material>(MatPath.Replace(".asset", "_pcss.asset", StringComparison.OrdinalIgnoreCase)) == null)
                                {
                                    AssetDatabase.CopyAsset(MatPath, MatPath.Replace(".asset", "_pcss.asset", StringComparison.OrdinalIgnoreCase));
                                }
                                mats[i] = AssetDatabase.LoadAssetAtPath<Material>(MatPath.Replace(".asset", "_pcss.asset", StringComparison.OrdinalIgnoreCase));
                            }

                            if (mats[i] != null)
                            {
                                PCSS4lilToonInspector inspector = new PCSS4lilToonInspector();
                                inspector.ConvertMaterialProxy(mats[i]);
                                mats[i].SetFloat("_AlphaBoostFA", 1);

                                if (mats[i].GetFloat("_StencilRef") == 0 || mats[i].GetFloat("_StencilPass") == 0)
                                {
                                    mats[i].SetFloat("_StencilComp", 8);
                                    mats[i].SetFloat("_StencilPass", 2);
                                    mats[i].SetFloat("_StencilRef", 125);
                                }
                            }
                        }
                        else if (renderers[r].sharedMaterials[i].shader.name.Contains("lil") && renderers[r].sharedMaterials[i].shader.name.Contains("NGSS4lilToon"))
                        {
                            mats[i] = renderers[r].sharedMaterials[i];
                            var shader = Shader.Find(mats[i].shader.name.Replace("NGSS4lilToon", "PCSS4lilToon"));
                            if (shader != null)
                            {
                                mats[i].shader = shader;
                                mats[i].SetFloat("_AlphaBoostFA", 1);
                                SetDefaultTextures(mats[i]);
                                EditorUtility.SetDirty(mats[i]);
                            }
                        }
                    }
                    else
                    {
                        if (MatPath.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                        {
                            mats[i] = AssetDatabase.LoadAssetAtPath<Material>(MatPath.Replace(".mat", "_pcss.mat", StringComparison.OrdinalIgnoreCase));
                        }
                        else if (MatPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                        {
                            mats[i] = AssetDatabase.LoadAssetAtPath<Material>(MatPath.Replace(".asset", "_pcss.asset", StringComparison.OrdinalIgnoreCase));
                        }
                    }

                    if (mats[i] == null)
                    {
                        mats[i] = renderers[r].sharedMaterials[i];
                    }
                    EditorUtility.SetDirty(mats[i]);
                    EditorUtility.SetDirty(renderers[r].sharedMaterials[i]);
                }
                renderers[r].sharedMaterials = mats;
                EditorUtility.SetDirty(renderers[r]);
            }
            AssetDatabase.SaveAssets();
        }

        private static void DuplicateExpressions(VRCAvatarDescriptor originalDescriptor)
        {
            var expressionsMenu = originalDescriptor.expressionsMenu;
            if (expressionsMenu != null)
            {
                string expressionsMenuPath = AssetDatabase.GetAssetPath(expressionsMenu);
                string copiedExpressionsMenuPath = $"{Path.GetDirectoryName(expressionsMenuPath)}/{expressionsMenu.name}_copy.asset";

                if(File.Exists(copiedExpressionsMenuPath))
                {
                    AssetDatabase.DeleteAsset(copiedExpressionsMenuPath);
                }

                AssetDatabase.CopyAsset(expressionsMenuPath, copiedExpressionsMenuPath);
                AssetDatabase.Refresh();

                var copiedExpressionsMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(copiedExpressionsMenuPath);
                if (copiedExpressionsMenu != null)
                {
                    originalDescriptor.expressionsMenu = copiedExpressionsMenu;
                    Debug.Log($"Expressions Menu duplicated: {copiedExpressionsMenu.name}");
                }
                else
                {
                    Debug.LogError("Failed to load the copied Expressions Menu.");
                }
            }

            var expressionParameters = originalDescriptor.expressionParameters;
            if (expressionParameters != null)
            {
                string expressionParametersPath = AssetDatabase.GetAssetPath(expressionParameters);
                string copiedExpressionParametersPath = $"{Path.GetDirectoryName(expressionParametersPath)}/{expressionParameters.name}_copy.asset";

                if (File.Exists(copiedExpressionParametersPath))
                {
                    AssetDatabase.DeleteAsset(copiedExpressionParametersPath);
                }

                AssetDatabase.CopyAsset(expressionParametersPath, copiedExpressionParametersPath);
                AssetDatabase.Refresh();

                var copiedExpressionParameters = AssetDatabase.LoadAssetAtPath<VRCExpressionParameters>(copiedExpressionParametersPath);
                if (copiedExpressionParameters != null)
                {
                    originalDescriptor.expressionParameters = copiedExpressionParameters;
                    Debug.Log($"Expression Parameters duplicated: {copiedExpressionParameters.name}");
                }
                else
                {
                    Debug.LogError("Failed to load the copied Expression Parameters.");
                }
            }
        }

        void RevertMaterials(VRCAvatarDescriptor avatarDescriptor, bool removeMaterials)
        {
            var renderers = avatarDescriptor.GetComponentsInChildren<Renderer>(true);

            var oldMaterialsPath = new List<string>();

            for (int r = 0; r < renderers.Length; r++)
            {
                Material[] mats = new Material[renderers[r].sharedMaterials.Length];
                for (int i = 0; i < renderers[r].sharedMaterials.Length; i++)
                {
                    if (renderers[r].sharedMaterials[i] == null)
                    {
                        continue;
                    }

                    string MatPath = AssetDatabase.GetAssetPath(renderers[r].sharedMaterials[i]);

                    if (renderers[r].sharedMaterials[i] != null && renderers[r].sharedMaterials[i].shader.name.Contains("lil", StringComparison.OrdinalIgnoreCase))
                    {
                        if (MatPath.EndsWith("_pcss.mat", StringComparison.OrdinalIgnoreCase))
                        {
                            var orig = AssetDatabase.LoadAssetAtPath<Material>(MatPath.Replace("_pcss.mat", ".mat", StringComparison.OrdinalIgnoreCase));
                            if (orig != null)
                            {
                                mats[i] = orig;

                                if (removeMaterials)
                                {
                                    oldMaterialsPath.Add(MatPath);
                                }
                            }
                        }
                        else if (MatPath.EndsWith("_pcss.asset", StringComparison.OrdinalIgnoreCase))
                        {
                            var orig = AssetDatabase.LoadAssetAtPath<Material>(MatPath.Replace("_pcss.asset", ".asset", StringComparison.OrdinalIgnoreCase));
                            if (orig != null)
                            {
                                mats[i] = orig;

                                if (removeMaterials)
                                {
                                    oldMaterialsPath.Add(MatPath);
                                }
                            }
                        }

                    }
                    if (mats[i] == null)
                    {
                        mats[i] = renderers[r].sharedMaterials[i];
                    }
                    EditorUtility.SetDirty(mats[i]);
                    EditorUtility.SetDirty(renderers[r].sharedMaterials[i]);
                }
                renderers[r].sharedMaterials = mats;
                EditorUtility.SetDirty(renderers[r]);
            }
            AssetDatabase.SaveAssets();

            for (int i = 0; i < oldMaterialsPath.Count; i++)
            {
                AssetDatabase.DeleteAsset(oldMaterialsPath[i]);
            }
            AssetDatabase.SaveAssets();
        }

        void SetDefaultTextures(Material mat)
        {
            if (mat.GetTexture("_EnvLightLevelTexture") == null)
            {
                var EnvLightLevelTexture = AssetDatabase.LoadAssetAtPath<CustomRenderTexture>("Assets/nHaruka/PCSS4VRC/EnvLightLevelSensor/EnvLightColor.asset");
                if (EnvLightLevelTexture != null)
                {
                    mat.SetTexture("_EnvLightLevelTexture", EnvLightLevelTexture);
                }
            }

            if (mat.GetTexture("_IgnoreCookieTexture") == null)
            {
                var ignoreCookieTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/nHaruka/PCSS4VRC/Soft.png");
                if (ignoreCookieTexture != null)
                {
                    mat.SetTexture("_IgnoreCookieTexture", ignoreCookieTexture);
                }
            }

        EditorUtility.SetDirty(mat);
        }
        void Remove(VRCAvatarDescriptor avatarDescriptor, bool removeMaterials)
        {
            if (avatarDescriptor.transform.Find("SelfLight") != null)
            {
                DestroyImmediate(avatarDescriptor.transform.Find("SelfLight").gameObject);
            } 
            if (avatarDescriptor.transform.Find("PCSS_Setup_for_MA") != null)
            {
                DestroyImmediate(avatarDescriptor.transform.Find("PCSS_Setup_for_MA").gameObject);
            }

            try
            {
                var FxAnimatorLayer =
                    avatarDescriptor.baseAnimationLayers.First(item => item.type == VRCAvatarDescriptor.AnimLayerType.FX && item.animatorController != null);
                var FxAnimator = (AnimatorController)FxAnimatorLayer.animatorController;

                var FxAnimatorPath = AssetDatabase.GetAssetPath(FxAnimator);

                if (FxAnimatorPath != null && File.Exists(FxAnimatorPath.Replace("_copy", "")))
                {
                    var revertFX = AssetDatabase.LoadAssetAtPath<AnimatorController>(FxAnimatorPath.Replace("_copy", ""));
                    avatarDescriptor.baseAnimationLayers[4].animatorController = revertFX;
                    FxAnimator = (AnimatorController)avatarDescriptor.baseAnimationLayers[4].animatorController;
                }
                else
                {
                    Debug.LogWarning("No Copied FX was found");
                }

                FxAnimator.layers = FxAnimator.layers.Where(item => !item.name.Contains("PCSS_")).ToArray();
                FxAnimator.parameters = FxAnimator.parameters.Where(item => !item.name.Contains("PCSS_")).ToArray();
                EditorUtility.SetDirty(FxAnimator);

                string expressionParametersPath = AssetDatabase.GetAssetPath(avatarDescriptor.expressionParameters);

                if (expressionParametersPath != null && File.Exists(expressionParametersPath.Replace("_copy", "")))
                {
                     var revertParams = AssetDatabase.LoadAssetAtPath<VRCExpressionParameters>(expressionParametersPath.Replace("_copy", ""));
                    avatarDescriptor.expressionParameters = revertParams;
                }
                else
                {
                    Debug.LogWarning("No Copied ExpressionParameters was found");
                }

                string expressionsMenuPath = AssetDatabase.GetAssetPath(avatarDescriptor.expressionsMenu);

                if (expressionsMenuPath != null && File.Exists(expressionsMenuPath.Replace("_copy", "")))
                {
                    var revertMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(expressionsMenuPath.Replace("_copy", ""));
                    avatarDescriptor.expressionsMenu = revertMenu;
                }
                else
                {
                    Debug.LogWarning("No Copied ExpressionMenus was found");
                }
                EditorUtility.SetDirty(avatarDescriptor);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            try
            {
                avatarDescriptor.expressionsMenu.controls.RemoveAll(item => item.name == "LightControl");
                avatarDescriptor.expressionParameters.parameters = avatarDescriptor.expressionParameters.parameters.Where(item => !item.name.Contains("PCSS_")).ToArray();
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            try
            {
                RevertMaterials(avatarDescriptor, removeMaterials);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
            AssetDatabase.SaveAssets();

            Refresh();
        }
    }
}
