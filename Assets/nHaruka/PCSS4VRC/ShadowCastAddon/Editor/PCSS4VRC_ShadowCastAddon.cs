using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace nHaruka.PCSS4VRC
{
    public class PCSS4VRC_ShadowCastAddon : EditorWindow
    {
        private VRCAvatarDescriptor avatarDescriptor;
        private bool WriteDefault = true;
        private int isEng = 0;
        private string[] escapeChar = { "\\", " ", "#", "/", "!", "%", "'", "|", "?", "&", "\"", "~", "@", ";", ":", "<", ">", "=", ".", "," };
        private bool useMA = true;

        [MenuItem("nHaruka/PCSS For VRC ShadowCastAddon")]
        private static void Init()
        {

            var window = GetWindowWithRect<PCSS4VRC_ShadowCastAddon>(new Rect(0, 0, 500, 260));
            window.Show();
        }

        private void OnGUI()
        {

            GUIStyle style0 = new GUIStyle();
            style0.normal.textColor = Color.white;
            style0.fontSize = 16;
            style0.wordWrap = true;
            style0.fontStyle = FontStyle.Bold;

            if (isEng == 0)
            {
                EditorGUILayout.LabelField("リアル影システム Shadow Cast Addon", style0);
            }
            else
            {
                EditorGUILayout.LabelField("PCSS for VRC Shadow Cast Addon", style0);
            }
            GUILayout.Space(10);

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.wordWrap = true;

            avatarDescriptor =
                (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", avatarDescriptor, typeof(VRCAvatarDescriptor), true);

            WriteDefault = GUILayout.Toggle(WriteDefault, "WriteDefaults");
            if (isEng == 1)
            {
                EditorGUILayout.LabelField("※Choose according to which FX layer of the avatar you are installing is unified. \nIf they are not unified, the facial expressions may look strange or not function properly.", style);
            }
            else
            {
                EditorGUILayout.LabelField("※導入アバターのFXレイヤーがどちらで統一されているかによって選択してください。\n統一されていないと表情がおかしくなったり正しく機能しなかったりします。", style);
            }
            GUILayout.Space(10);

            if (isEng == 0)
            {
                useMA = GUILayout.Toggle(useMA, "Modular Avatarを使用してセットアップする");
            }
            else
            {
                useMA = GUILayout.Toggle(useMA, "Setup with Modular Avatar ");
            }
            GUILayout.Space(10);

            if (GUILayout.Button("Setup"))
            {
                if (avatarDescriptor == null)
                {
                    EditorUtility.DisplayDialog("Error", "Avatar is not set.", "OK");
                    return;
                }
                else if (avatarDescriptor.expressionsMenu == null)
                {
                    EditorUtility.DisplayDialog("Error", "Expressions Menu iis not set on avatar.", "OK");
                    return;
                }
                else if (avatarDescriptor.expressionParameters == null)
                {
                    EditorUtility.DisplayDialog("Error", "Expression Parameters is not set on avatar.", "OK");
                    return;
                }
                else if (avatarDescriptor.baseAnimationLayers[4].animatorController == null)
                {
                    EditorUtility.DisplayDialog("Error", "FX layer is not set on avatar.", "OK");
                    return;
                }
                try
                {
                    Remove();
                    Setup();
                    EditorUtility.DisplayDialog("Finished", "Finished!", "OK");
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Error", "An error occurred. See console log.", "OK");
                    Debug.LogError(e);
                }

            }

            if (GUILayout.Button("Remove"))
            {
                if (avatarDescriptor == null)
                {
                    EditorUtility.DisplayDialog("Error", "Avatar is not set.", "OK");
                    return;
                }
                try
                {
                    Remove();
                    EditorUtility.DisplayDialog("Finished", "Finished!", "OK");
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Error", "An error occurred. See console log.", "OK");
                    Debug.LogError(e);
                }
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

        public void Setup()
        {
            try
            {
                Remove();

            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return;
            }


            if (useMA)
            {
                var prefabMA = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/nHaruka/PCSS4VRC/ShadowCastAddon/ShadowCastAddon_MA.prefab");
                var MA = (GameObject)PrefabUtility.InstantiatePrefab(prefabMA);
                MA.name = "ShadowCastAddon";
                MA.transform.parent = avatarDescriptor.transform;
            }
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/nHaruka/PCSS4VRC/ShadowCastAddon/ShadowCastAddon.prefab");
                var Prefab_Unpack = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Prefab_Unpack.name = "ShadowCastAddon";
                Prefab_Unpack.transform.parent = avatarDescriptor.transform;


                var escapedAvatarName = EscapeName(avatarDescriptor.name);

                if (!Directory.Exists("Assets/nHaruka/PCSS4VRC/AvatarData"))
                {
                    Directory.CreateDirectory("Assets/nHaruka/PCSS4VRC/AvatarData");
                }

                if (!Directory.Exists("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName))
                {
                    Directory.CreateDirectory("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName);
                }

                AssetDatabase.CopyAsset("Assets/nHaruka/PCSS4VRC/ShadowCastAddon/PCSS_ShadowCast.controller", "Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/PCSS_ShadowCast_copy.controller");

                var AddAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/PCSS_ShadowCast_copy.controller");

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
                }
                catch (Exception ex)
                {
                    Debug.LogError("Something wrong in FX!");
                    Debug.LogException(ex);
                    return;
                }
                EditorUtility.SetDirty(FxAnimator);

                FxAnimator.parameters = FxAnimator.parameters.Union(AddAnimatorController.parameters).ToArray();
                foreach (var layer in AddAnimatorController.layers)
                {
                    FxAnimator.AddLayer(layer);
                }
                var AddExpParam = AssetDatabase.LoadAssetAtPath<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters>("Assets/nHaruka/PCSS4VRC/ShadowCastAddon/ShadowCast_Params.asset");

                avatarDescriptor.expressionParameters.parameters = avatarDescriptor.expressionParameters.parameters.Union(AddExpParam.parameters).ToArray();

                EditorUtility.SetDirty(avatarDescriptor.expressionParameters);

                var AddSubMenu = AssetDatabase.LoadAssetAtPath<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>("Assets/nHaruka/PCSS4VRC/ShadowCastAddon/LightControl_Addon.asset");

                var lightControl = avatarDescriptor.expressionsMenu.controls.FirstOrDefault(item => item.name == "LightControl");

                avatarDescriptor.expressionsMenu.controls.Remove(lightControl);

                var newMenu = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control();
                newMenu.name = "LightControl";
                newMenu.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu;
                newMenu.subMenu = AddSubMenu;

                avatarDescriptor.expressionsMenu.controls.Add(newMenu);


                EditorUtility.SetDirty(avatarDescriptor.expressionsMenu);
            }

            AssetDatabase.SaveAssets();

            return;
        }
        /*
        void SetMaterialsStencil()
        {
            var renderers = avatarDescriptor.GetComponentsInChildren<Renderer>(true);

            List<string> processedMatPath = new List<string>();

            for (int r = 0; r < renderers.Length; r++)
            {
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

                        if (renderers[r].sharedMaterials[i].shader.name.Contains("PCSS", StringComparison.OrdinalIgnoreCase) && !renderers[r].sharedMaterials[i].shader.name.Contains("Gem", StringComparison.OrdinalIgnoreCase) && !renderers[r].sharedMaterials[i].shader.name.Contains("Refraction", StringComparison.OrdinalIgnoreCase) && !renderers[r].sharedMaterials[i].shader.name.Contains("Optional", StringComparison.OrdinalIgnoreCase))
                        {
                            if (renderers[r].sharedMaterials[i].GetFloat("_StencilRef") != 0)
                            {
                                renderers[r].sharedMaterials[i].SetFloat("_StencilComp", 8);
                                renderers[r].sharedMaterials[i].SetFloat("_StencilPass", 0);
                                renderers[r].sharedMaterials[i].SetFloat("_StencilRef", 0);
                            }
                        }
                    }

                    EditorUtility.SetDirty(renderers[r].sharedMaterials[i]);
                }
                EditorUtility.SetDirty(renderers[r]);
            }
            AssetDatabase.SaveAssets();
        }
        */
        void Remove()
        {
            if (avatarDescriptor.transform.Find("ShadowCastAddon") != null)
            {
                DestroyImmediate(avatarDescriptor.transform.Find("ShadowCastAddon").gameObject);
            }

            try
            {
                var FxAnimatorLayer =
                    avatarDescriptor.baseAnimationLayers.First(item => item.type == VRCAvatarDescriptor.AnimLayerType.FX && item.animatorController != null);
                var FxAnimator = (AnimatorController)FxAnimatorLayer.animatorController;

                var FxAnimatorPath = AssetDatabase.GetAssetPath(FxAnimator);

                FxAnimator.layers = FxAnimator.layers.Where(item => !item.name.Contains("PCSS_A_")).ToArray();
                FxAnimator.parameters = FxAnimator.parameters.Where(item => !item.name.Contains("PCSS_A_")).ToArray();
                EditorUtility.SetDirty(FxAnimator);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            try
            {
                var AddSubMenu = AssetDatabase.LoadAssetAtPath<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>("Assets/nHaruka/PCSS4VRC/LightControl.asset");

                var lightControl = avatarDescriptor.expressionsMenu.controls.FirstOrDefault(item => item.name == "LightControl");

                if (lightControl != null)
                {
                    avatarDescriptor.expressionsMenu.controls.Remove(lightControl);

                    var newMenu = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control();
                    newMenu.name = "LightControl";
                    newMenu.type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu;
                    newMenu.subMenu = AddSubMenu;

                    avatarDescriptor.expressionsMenu.controls.Add(newMenu);
                }

                avatarDescriptor.expressionParameters.parameters = avatarDescriptor.expressionParameters.parameters.Where(item => !item.name.Contains("PCSS_A_")).ToArray();
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }
    }
}
