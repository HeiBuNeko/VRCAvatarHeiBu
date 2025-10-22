using nadena.dev.modular_avatar.core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace nHaruka.PCSS4VRC
{
    public class PCSS4VRC_MaterialAnimationCreator : EditorWindow
    {
        public VRCAvatarDescriptor avatarDescriptor;
        private string[] escapeChar = { "\\", " ", "#", "/", "!", "%", "'", "|", "?", "&", "\"", "~", "@", ";", ":", "<", ">", "=", ".", "," };

        public List<string> propNames = new List<string> { "_IsOn", "_EnvLightStrength", "_ShadowDensity" };
        public List<float> minVal = new List<float> {0f, 0f, 0f };
        public List<float> maxVal = new List<float> {1f, 1f, 1f };
        public List<bool> isToggle = new List<bool> { true, false, false};


        List<Renderer> excludeList = new List<Renderer>();

        List<string> allPCSSMaterialPaths;
        List<Type> rendererTypes;

        Vector2 scroll = Vector2.zero;
        Vector2 scroll2 = Vector2.zero;

        [MenuItem("nHaruka/PCSS For VRC MaterialAnimation Creator")]
        public static void ShowWindow()
        {
            PCSS4VRC_MaterialAnimationCreator window = GetWindow<PCSS4VRC_MaterialAnimationCreator>("PCSS4VRC MaterialAnimationCreator");
            window.minSize = new Vector2(300, 400);
        }

        void OnGUI()
        {
            avatarDescriptor = EditorGUILayout.ObjectField("Avatar", avatarDescriptor, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;

            GUILayout.Space(10);

            EditorGUI.indentLevel++;

            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("プロパティ名");
            for (int i = 0; i < propNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                propNames[i] = EditorGUILayout.TextField(propNames[i],GUILayout.Width(170));
                GUILayout.Label("IsToggle:", GUILayout.MinWidth(60));
                isToggle[i] = EditorGUILayout.Toggle(isToggle[i], GUILayout.MinWidth(20)); ;
                if (isToggle[i])
                {
                    minVal[i] = 0;
                    maxVal[i] = 1;
                }
                GUILayout.Label("Min:", GUILayout.MinWidth(30));
                minVal[i] = EditorGUILayout.FloatField(minVal[i], GUILayout.MinWidth(20));
                GUILayout.Label("Max:", GUILayout.MinWidth(30));
                maxVal[i] = EditorGUILayout.FloatField(maxVal[i], GUILayout.MinWidth(20));
                if (GUILayout.Button("削除", GUILayout.Width(60)))
                {
                    propNames.RemoveAt(i);
                    minVal.RemoveAt(i);
                    maxVal.RemoveAt(i);
                    isToggle.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("プロパティを追加"))
            {
                propNames.Add(null);
                minVal.Add(0f);
                maxVal.Add(1f);
                isToggle.Add(false);
            }

            EditorGUI.indentLevel--;

            GUILayout.Space(10);

            EditorGUI.indentLevel++;

            scroll2 = EditorGUILayout.BeginScrollView(scroll2);

            EditorGUILayout.LabelField("除外オブジェクト一覧");
            for (int i = 0; i < excludeList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                excludeList[i] = (Renderer)EditorGUILayout.ObjectField(excludeList[i], typeof(Renderer),true);
                if (GUILayout.Button("削除", GUILayout.Width(60)))
                {
                    excludeList.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("除外リストを追加"))
            {
                excludeList.Add(null);
            }

            EditorGUI.indentLevel--;

            GUILayout.Space(10);

            if (GUILayout.Button("Create"))
            {
                try
                {
                    if (avatarDescriptor != null)
                    {
                        Setup();
                        EditorUtility.DisplayDialog("Finished", "Finished!", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Not assigned Avatar", "OK");
                    }
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Error", "An error occurred. See console log.", "OK");
                    Debug.LogException(e);
                }
            }
            if (GUILayout.Button("Remove"))
            {
                try
                {
                    if (avatarDescriptor != null)
                    {
                        Remove();
                        EditorUtility.DisplayDialog("Finished", "Finished!", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Not assigned Avatar", "OK");
                    }
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Error", "An error occurred. See console log.", "OK");
                    Debug.LogException(e);
                }
            }
        }

        public void Setup()
        {
            ListAllPCSSMaterials(avatarDescriptor);

            var pcssMA = avatarDescriptor.transform.Find("PCSS_Setup_for_MA");

            var parameters = pcssMA.GetComponent<ModularAvatarParameters>().parameters;
            EditorUtility.SetDirty(pcssMA.GetComponent<ModularAvatarParameters>());

            var shader = Shader.Find("PCSS4VRC/PCSS4lilToon/lilToon");

            string[] propDisplayNames = new string[propNames.Count];

            for (int i = 0; i < propNames.Count; i++)
            {
                var defval = 0f;
                
                var index = shader.FindPropertyIndex(propNames[i]);
                if (index > 0)
                {
                    defval = shader.GetPropertyDefaultFloatValue(index);
                    propDisplayNames[i] = shader.GetPropertyDescription(index);
                }
                if (!parameters.Any(x => x.nameOrPrefix == "PCSS" + propNames[i]))
                {
                    Debug.Log("PCSS" + propNames[i]);
                    if (isToggle[i])
                    {
                        parameters.Add(new ParameterConfig() { nameOrPrefix = "PCSS" + propNames[i], defaultValue = defval, syncType = ParameterSyncType.Bool, saved = true });
                    }
                    else
                    {
                        parameters.Add(new ParameterConfig() { nameOrPrefix = "PCSS" + propNames[i], defaultValue = defval, syncType = ParameterSyncType.Float, saved = true });
                    }
                }
            }
            var escapedAvatarName = EscapeName(avatarDescriptor.name);

            if (!Directory.Exists("Assets/nHaruka/PCSS4VRC/AvatarData"))
            {
                Directory.CreateDirectory("Assets/nHaruka/PCSS4VRC/AvatarData");
            }

            if (!Directory.Exists("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName))
            {
                Directory.CreateDirectory("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName);
            }

            AssetDatabase.DeleteAsset("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/CustomPropMenu.asset");
            AssetDatabase.DeleteAsset("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/LightControl.asset");
            AssetDatabase.DeleteAsset("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/LightControl_Main.asset");
            AssetDatabase.CopyAsset("Assets/nHaruka/PCSS4VRC/CustomPropMenu.asset", "Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/CustomPropMenu.asset");
            AssetDatabase.CopyAsset("Assets/nHaruka/PCSS4VRC/LightControl.asset", "Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/LightControl.asset");
            AssetDatabase.CopyAsset("Assets/nHaruka/PCSS4VRC/LightControl_Main.asset", "Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/LightControl_Main.asset");

            var LightControl_Main = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/LightControl_Main.asset");
            var LightControl = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/LightControl.asset");
            var CustomPropMenu = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/CustomPropMenu.asset");
            EditorUtility.SetDirty(LightControl_Main);
            EditorUtility.SetDirty(LightControl);
            EditorUtility.SetDirty(CustomPropMenu);

            LightControl_Main.controls[0].subMenu = LightControl;
            LightControl.controls.Add(new VRCExpressionsMenu.Control() { name = "MaterialPropertyControl", type = VRCExpressionsMenu.Control.ControlType.SubMenu, subMenu = CustomPropMenu });

            for (int i = 0; i < propNames.Count; i++)
            {
                if (isToggle[i])
                {
                    CustomPropMenu.controls.Add(new VRCExpressionsMenu.Control() { name = propDisplayNames[i], type = VRCExpressionsMenu.Control.ControlType.Toggle, parameter =  new VRCExpressionsMenu.Control.Parameter() { name = "PCSS" + propNames[i] } });
                }
                else
                {
                    CustomPropMenu.controls.Add(new VRCExpressionsMenu.Control() { name = propDisplayNames[i], type = VRCExpressionsMenu.Control.ControlType.RadialPuppet, subParameters = new VRCExpressionsMenu.Control.Parameter[] { new VRCExpressionsMenu.Control.Parameter() { name = "PCSS" + propNames[i] } } });
                }
            }

            AssetDatabase.DeleteAsset("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/LightControl.controller");
            AssetDatabase.CopyAsset("Assets/nHaruka/PCSS4VRC/LightControl.controller", "Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/LightControl.controller");

            var animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/LightControl.controller");
            EditorUtility.SetDirty(animatorController);

            for (int i = 0; i < propNames.Count; i++)
            {
                animatorController.AddParameter("PCSS" + propNames[i], AnimatorControllerParameterType.Float);
            }

            AnimationClip[] clips = new AnimationClip[propNames.Count];
            for (int i = 0; i < propNames.Count; i++)
            {
                clips[i] = CreateAnimationClip(allPCSSMaterialPaths.ToArray(), rendererTypes.ToArray(), propNames[i], minVal[i], maxVal[i]);
                AssetDatabase.DeleteAsset("Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/PCSS" + propNames[i] + ".anim");
                AssetDatabase.CreateAsset(clips[i], "Assets/nHaruka/PCSS4VRC/AvatarData/" + escapedAvatarName + "/PCSS" + propNames[i] + ".anim");

                var layer = new AnimatorControllerLayer();
                layer.name = "PCSS" + propNames[i];
                layer.defaultWeight = 1;
                layer.stateMachine = new AnimatorStateMachine();
                var state = layer.stateMachine.AddState("PCSS" + propNames[i]);
                state.motion = clips[i];
                state.timeParameterActive = true;
                state.timeParameter = "PCSS" + propNames[i];
                animatorController.AddLayer(layer);
                AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(animatorController));
                AssetDatabase.AddObjectToAsset(state, AssetDatabase.GetAssetPath(animatorController));
                EditorUtility.SetDirty(layer.stateMachine);
                EditorUtility.SetDirty(state);
                EditorUtility.SetDirty(animatorController);
            }
            pcssMA.GetComponent<ModularAvatarMergeAnimator>().animator = animatorController;
            EditorUtility.SetDirty(pcssMA.GetComponent<ModularAvatarMergeAnimator>());

            var menuInstaller = pcssMA.GetComponent<ModularAvatarMenuInstaller>();
            menuInstaller.menuToAppend = LightControl_Main;
            EditorUtility.SetDirty(menuInstaller);

            var optLight = avatarDescriptor.transform.Find("OptionalLight");
            if (optLight != null)
            {
                var installer = optLight.GetComponent<ModularAvatarMenuInstaller>();
                EditorUtility.SetDirty(installer);
                installer.installTargetMenu = LightControl;
            }

            var castAddon = avatarDescriptor.transform.Find("ShadowCastAddon");
            if (castAddon != null)
            {
                var installer = castAddon.GetComponent<ModularAvatarMenuInstaller>();
                EditorUtility.SetDirty(installer);
                installer.installTargetMenu = LightControl;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void Remove()
        {
            var pcssMA = avatarDescriptor.transform.Find("PCSS_Setup_for_MA");

            var parameters = pcssMA.GetComponent<ModularAvatarParameters>().parameters;
            EditorUtility.SetDirty(pcssMA.GetComponent<ModularAvatarParameters>());

            for (int i = 0; i < propNames.Count; i++)
            {
                parameters.RemoveAll(x => x.nameOrPrefix == "PCSS" + propNames[i]);
            }

            var LightControl_Main = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>("Assets/nHaruka/PCSS4VRC/LightControl_Main.asset");
            var LightControl = AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>("Assets/nHaruka/PCSS4VRC/LightControl.asset");

            pcssMA.GetComponent<ModularAvatarMergeAnimator>().animator = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/nHaruka/PCSS4VRC/LightControl.controller"); ;
            EditorUtility.SetDirty(pcssMA.GetComponent<ModularAvatarMergeAnimator>());

            var menuInstaller = pcssMA.GetComponent<ModularAvatarMenuInstaller>();
            menuInstaller.menuToAppend = LightControl_Main;
            EditorUtility.SetDirty(menuInstaller);

            var optLight = avatarDescriptor.transform.Find("OptionalLight");
            if (optLight != null)
            {
                var installer = optLight.GetComponent<ModularAvatarMenuInstaller>();
                EditorUtility.SetDirty(installer);
                installer.installTargetMenu = LightControl;
            }

            var castAddon = avatarDescriptor.transform.Find("ShadowCastAddon");
            if (castAddon != null)
            {
                var installer = castAddon.GetComponent<ModularAvatarMenuInstaller>();
                EditorUtility.SetDirty(installer);
                installer.installTargetMenu = LightControl;
            }
        }

        void ListAllPCSSMaterials(VRCAvatarDescriptor vRCAvatarDescriptor )
        {
            allPCSSMaterialPaths = new List<string>();
            rendererTypes = new List<Type>();

            var renderers = vRCAvatarDescriptor.GetComponentsInChildren<Renderer>();

            for (int r = 0; r < renderers.Length; r++)
            {
                if (excludeList.Contains(renderers[r]))
                {
                    continue;
                }

                for (int i = 0; i < renderers[r].sharedMaterials.Length; i++)
                {
                    if (renderers[r].sharedMaterials[i] == null)
                    {
                        continue;
                    }

                    if (renderers[r].sharedMaterials[i].shader.name.Contains("PCSS4VRC") && !renderers[r].sharedMaterials[i].shader.name.Contains("ShadowCast"))
                    {
                        var path = GetPath(avatarDescriptor.transform, renderers[r].transform);
                        if (!allPCSSMaterialPaths.Contains(path))
                        {
                            allPCSSMaterialPaths.Add(path);
                            rendererTypes.Add(renderers[r].GetType());
                        }

                    }
                }
            }
        }

        AnimationClip CreateAnimationClip(string[] paths, Type[] type, string propName, float valueFrom , float valueTo)
        {
            AnimationClip clip = new AnimationClip();

            var curve = new AnimationCurve(new Keyframe(0f, valueFrom), new Keyframe(10f, valueTo));
            for (int i = 0; i< paths.Length; i++)
            {
                var binding = new EditorCurveBinding
                {
                    type = type[i],
                    path = paths[i],
                    propertyName = "material." + propName
                };
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
            return clip;
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

        private static string GetPath(Transform root, Transform self)
        {

            string path = self.gameObject.name;
            Transform parent = self.parent;

            while (root != parent)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
