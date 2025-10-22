using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualLens2.AV3EditorLib;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3A.Editor;
using VRC.SDKBase.Editor.BuildPipeline;

namespace VirtualLens2
{
    public class VirtualLensBuildHook : IVRCSDKPreprocessAvatarCallback
    {
        // NDMF Preprocess (1.6.8)       | -11000
        // VirtualLens2 Validator        | -10900
        // VRCFury Pre-upload (1.1189.0) | -10000
        // NDMF Optimize (1.6.8)         |  -1025
        public int callbackOrder => -10900;

        // Original object of the current build target
        private static GameObject _currentTarget = null;
        
        [InitializeOnLoadMethod]
        public static void RegisterSDKCallback()
        {
            VRCSdkControlPanel.OnSdkPanelEnable += AddBuildHook;
        }

        private static void AddBuildHook(object sender, EventArgs e)
        {
            if (VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder))
            {
                builder.OnSdkBuildStart += OnBuildStarted;
            }
        }

        private static void OnBuildStarted(object sender, object target)
        {
            _currentTarget = (GameObject)target;
        }

        private VirtualLensSettings FindDestructiveSettings()
        {
            for (var i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) { continue; }
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var component in root.GetComponentsInChildren<VirtualLensSettings>())
                    {
                        Debug.Log(component.avatar);
                        if (component.avatar == _currentTarget && component.buildMode == BuildMode.Destructive)
                        {
                            return component;
                        }
                    }
                }
            }
            return null;
        }

        private int FindVersionFromFX(GameObject avatar)
        {
            // Get FX layer
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null) { return -1; }
            AnimatorController controller = null;
            foreach (var playableLayer in descriptor.baseAnimationLayers)
            {
                if (playableLayer.type == VRCAvatarDescriptor.AnimLayerType.FX)
                {
                    controller = (AnimatorController)playableLayer.animatorController;
                    break;
                }
            }
            if (controller == null) { return -1; }

            // Extract VirtualLens2 version from initialization state
            AnimatorControllerLayer layer = null;
            try
            {
                layer = controller.layers.First(l => l.name == Constants.ParameterPrefix + "Initialize");
            }
            catch (InvalidOperationException)
            {
                // VirtualLens2 is not applied for this AnimatorController
                return -1;
            }
            AnimatorState state = null;
            try
            {
                state = layer.stateMachine.states.First(s => s.state.name == "Init").state;
            }
            catch (InvalidOperationException)
            {
                // VirtualLens2 is applied but too old.
                return 0;
            }
            // All behaviours should be checked because VRCFury may split VRCAvatarParameterDriver
            foreach (var behaviour in state.behaviours)
            {
                if (!(behaviour is VRCAvatarParameterDriver driver)) { continue; }
                foreach (var parameter in driver.parameters)
                {
                    if (parameter.name == Constants.ParameterPrefix + "Version")
                    {
                        return (int)parameter.value;
                    }
                }
            }
            // VirtualLens2 is applied but too old.
            return 0;
        }

        public bool OnPreprocessAvatar(GameObject avatar)
        {
            var component = FindDestructiveSettings();

            var root = HierarchyUtility.PathToObject(avatar, "_VirtualLens_Root");
            // Pass if VirtualLens2 is not implemented for the avatar.
            if (component == null && root == null) { return true; }
            
            // Check destructive settings not applied
            if (component != null && root == null)
            {
                return EditorUtility.DisplayDialog(
                    "VirtualLens2",
                    "Unapplied VirtualLens2 settings detected.\nDo you want to continue with the avatar build?",
                    "Yes", "No");
            }

            // Search and test version value in initialization state
            var version = FindVersionFromFX(avatar);
            if (version >= 0 && version != Constants.Version)
            {
                Debug.LogError($"Expected version = {Constants.Version}, Actual version = {version}");
                EditorUtility.DisplayDialog(
                    "VirtualLens2 Validator",
                    "Older VirtualLens2 is implemented for this avatar.\n" +
                    "You have to apply VirtualLensSettings again before building the avatar.",
                    "Cancel build");
                return false;
            }

            return true;
        }
    }
}