using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using VirtualLens2.AV3EditorLib;

#if WITH_MODULAR_AVATAR
using nadena.dev.modular_avatar.core;
#endif

namespace VirtualLens2
{
    internal static class AnimatorControllerGenerator
    {
        private const string ParameterPrefix = Constants.ParameterPrefix;

        public static void Generate(ImplementationSettings settings, ArtifactsFolder folder,
            GeneratedObjectSet objectSet)
        {
            Clear(settings);

            var avatar = settings.Avatar;
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            var original = AvatarDescriptorUtility.GetPlayableLayer(descriptor, VRCAvatarDescriptor.AnimLayerType.FX);

            AnimatorController controller;
            if (settings.BuildAsModule || settings.BuildMode == BuildMode.NonDestructive)
            {
                if (string.IsNullOrEmpty(folder.Path))
                {
                    controller = new AnimatorController();
                }
                else
                {
                    var path = folder.GenerateAssetPath<AnimatorController>();
                    controller = AnimatorController.CreateAnimatorControllerAtPath(path);
                    Undo.RegisterCreatedObjectUndo(controller, "Create animator controller");
                    controller.RemoveLayer(0); // Remove Base Layer
                }
#if WITH_MODULAR_AVATAR
                var component = objectSet.VirtualLensRoot.GetComponent<ModularAvatarMergeAnimator>();
                Undo.RecordObject(component, "Update modular avatar merge animator");
                var so = new SerializedObject(component);
                so.FindProperty("animator").objectReferenceValue = controller;
                so.ApplyModifiedProperties();
#endif
            }
            else
            {
                controller = AvatarDescriptorUtility.GetOrCreatePlayableLayer(
                    descriptor, VRCAvatarDescriptor.AnimLayerType.FX, folder);
            }

            // Prepare avatar mask
            var avatarMask = CreateAvatarMask(settings, folder);
            PatchFirstAvatarMask(settings, folder, avatarMask);

            // VirtualLens2/Core/Animations/FX.controller
            var source = AnimatorControllerEditor.Clone(
                AssetUtility.LoadAssetByGUID<AnimatorController>("21f6fdb6102ff1045a1eb146ac402102"));

            // Replace avatar masks
            {
                var so = new SerializedObject(source);
                var numLayers = source.layers.Length;
                for (var i = 0; i < numLayers; ++i) { SetAvatarMaskForLayer(so, i, avatarMask); }
                so.ApplyModifiedProperties();
            }

            // Process motions
            var motions = new MotionTemplateParameters();
            RegisterZoomMotion(motions, settings, folder);
            RegisterApertureMotion(motions, settings, folder);
            RegisterDistanceMotion(motions, settings, folder);
            RegisterExposureMotion(motions, settings, folder);
            RegisterDroneYawSpeedMotion(motions, settings, folder);
            RegisterFarPlaneMotion(motions, settings, folder);
            RegisterAvatarScalingMotion(motions, settings, folder);
            RegisterMaxBlurrinessMotion(motions, settings, folder);
            RegisterPreviewMaterialsMotion(motions, settings, folder);
            RegisterResolutionMotion(motions, settings, folder, objectSet);
            AnimatorControllerEditor.ProcessMotionTemplate(source, motions);

            var objects = new AnimationTemplateParameters();
            objects.Add("VirtualLensOrigin", MarkerDetector.DetectOrigin(settings));
            var preview = MarkerDetector.DetectPreview(settings);
            if (preview)
            {
                objects.Add("VirtualLensPreview", MarkerDetector.DetectPreview(settings));
            }
            objects.Add("CameraRoot", settings.CameraRoot);
            objects.Add("CameraNonPreviewRoot", settings.CameraNonPreviewRoot);

            var hideableMeshes = settings.HideableMeshes
                .Select(obj => obj.GetComponent<MeshRenderer>())
                .Where(renderer => renderer != null)
                .ToArray();
            var hideableSkinnedMeshes = settings.HideableMeshes
                .Select(obj => obj.GetComponent<SkinnedMeshRenderer>())
                .Where(renderer => renderer != null)
                .ToArray();

            var nonPreviewMeshes = settings.CameraNonPreviewRoot
                .GetComponentsInChildren<MeshRenderer>(true)
                .Where(c => c.enabled)
                .ToArray();
            var nonPreviewSkinnedMeshes = settings.CameraNonPreviewRoot
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(c => c.enabled)
                .ToArray();

            var previewMeshes = settings.CameraRoot
                .GetComponentsInChildren<MeshRenderer>(true)
                .Except(nonPreviewMeshes)
                .Where(c => c.enabled)
                .ToArray();
            var previewSkinnedMeshes = settings.CameraRoot
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Except(nonPreviewSkinnedMeshes)
                .Where(c => c.enabled)
                .ToArray();

            var simpleHideableMeshes = hideableMeshes
                .Except(nonPreviewMeshes)
                .Except(previewMeshes)
                .ToArray();
            var simpleHideableSkinnedMeshes = hideableSkinnedMeshes
                .Except(nonPreviewSkinnedMeshes)
                .Except(previewSkinnedMeshes)
                .ToArray();

            foreach (var renderer in nonPreviewMeshes.Except(hideableMeshes))
            {
                objects.Add("NonPreviewMeshes", renderer.gameObject);
            }
            foreach (var renderer in nonPreviewSkinnedMeshes.Except(hideableSkinnedMeshes))
            {
                objects.Add("NonPreviewSkinnedMeshes", renderer.gameObject);
            }
            foreach (var renderer in nonPreviewMeshes.Intersect(hideableMeshes))
            {
                objects.Add("HideableNonPreviewMeshes", renderer.gameObject);
            }
            foreach (var renderer in nonPreviewSkinnedMeshes.Intersect(hideableSkinnedMeshes))
            {
                objects.Add("HideableNonPreviewSkinnedMeshes", renderer.gameObject);
            }

            foreach (var renderer in previewMeshes.Except(hideableMeshes))
            {
                objects.Add("PreviewMeshes", renderer.gameObject);
            }
            foreach (var renderer in previewSkinnedMeshes.Except(hideableSkinnedMeshes))
            {
                objects.Add("PreviewSkinnedMeshes", renderer.gameObject);
            }
            foreach (var renderer in previewMeshes.Intersect(hideableMeshes))
            {
                objects.Add("HideablePreviewMeshes", renderer.gameObject);
            }
            foreach (var renderer in previewSkinnedMeshes.Intersect(hideableSkinnedMeshes))
            {
                objects.Add("HideablePreviewSkinnedMeshes", renderer.gameObject);
            }

            foreach (var renderer in simpleHideableMeshes)
            {
                objects.Add("HideableMeshes", renderer.gameObject);
            }
            foreach (var renderer in simpleHideableSkinnedMeshes)
            {
                objects.Add("HideableSkinnedMeshes", renderer.gameObject);
            }

            if (objectSet.SelfieDetectorMarkers)
            {
                objects.Add("SelfieMarker", objectSet.SelfieDetectorMarkers);
            }
            foreach (var container in settings.ScreenTouchers)
            {
                var toucher = HierarchyUtility.PathToObject(container, "_VirtualLens_ScreenToucher");
                if (toucher)
                {
                    objects.Add("ScreenTouchers", toucher);
                }
            }
            foreach (var optional in settings.OptionalObjects)
            {
                var key = optional.DefaultState ? "OptionalObjectsNegated" : "OptionalObjects";
                objects.Add(key, optional.GameObject);
            }

            AnimatorControllerEditor.ProcessAnimationTemplates(source, settings.Avatar, objects, folder);

            var writeDefaults = AV3EditorLib.WriteDefaultsOverrideMode.None;
            switch (settings.WriteDefaults)
            {
                case WriteDefaultsOverrideMode.Auto:
                    writeDefaults = original == null
                        ? AV3EditorLib.WriteDefaultsOverrideMode.ForceEnable
                        : SelectWriteDefaultsMode(original);
                    break;
                case WriteDefaultsOverrideMode.ForceDisable:
                    writeDefaults = AV3EditorLib.WriteDefaultsOverrideMode.ForceDisable;
                    break;
                case WriteDefaultsOverrideMode.ForceEnable:
                    writeDefaults = AV3EditorLib.WriteDefaultsOverrideMode.ForceEnable;
                    break;
            }
            AnimatorControllerEditor.Merge(controller, source, writeDefaults);

            // Controller must be persistent to add StateMachineBehaviour
            RegisterDefaultParameters(controller, settings);
            RegisterQuickCalls(controller, settings);

            EditorUtility.SetDirty(controller);
        }


        public static void Clear(GameObject avatar)
        {
            if (!avatar) { return; }
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            if (!descriptor) { return; }
            var controller = AvatarDescriptorUtility.GetPlayableLayer(descriptor, VRCAvatarDescriptor.AnimLayerType.FX);
            if (!controller) { return; }
            AssetUtility.RemoveSubAssets(controller, o => o.name.StartsWith("zAutogenerated__VirtualLens2__"));
            var re = new Regex(@"^VirtualLens2 .*");
            AnimatorControllerEditor.RemoveLayers(controller, re, descriptor);
            AnimatorControllerEditor.RemoveParameters(controller, re);
            EditorUtility.SetDirty(controller);
            UnpatchAvatarMask(avatar);
        }

        private static void Clear(ImplementationSettings settings) { Clear(settings.Avatar); }


        #region Object Paths

        private static readonly string[] CaptureCameraPaths = new[]
        {
            "_VirtualLens_Root/Local/Capture/Camera/1080p",
            "_VirtualLens_Root/Local/Capture/Camera/1440p",
            "_VirtualLens_Root/Local/Capture/Camera/2160p",
            "_VirtualLens_Root/Local/Capture/Camera/4320p",
            "_VirtualLens_Root/Local/Capture/Camera/AvatarDepth",
            "_VirtualLens_Root/Local/Capture/Camera/SelfieDetector",
        };

        private static readonly string[] ComputeRendererPaths = new[]
        {
            "_VirtualLens_Root/Local/Compute/FaceFocusCompute/Quad",
            "_VirtualLens_Root/Local/Compute/StateUpdater/Quad",
            "_VirtualLens_Root/Local/Compute/DisplayRenderer/Quad",
            "_VirtualLens_Root/Local/Compute/DepthOfField/ComputeCoc/Quad",
            "_VirtualLens_Root/Local/Compute/DepthOfField/ComputeTiles/Quad",
            "_VirtualLens_Root/Local/Compute/DepthOfField/Downsample/Quad",
            "_VirtualLens_Root/Local/Compute/DepthOfField/RealtimeCompute/Quad",
            "_VirtualLens_Root/Local/Writer/Renderer",
        };

        #endregion
        
        #region Avatar Mask Modification

        private static AvatarMask CreateAvatarMask(ImplementationSettings settings, ArtifactsFolder folder)
        {
            // VirtualLens2/Core/AvatarMasks/VirtualLens2_DefaultMask.mask
            var mask = Object.Instantiate(AssetUtility.LoadAssetByGUID<AvatarMask>("a051bbb9cae570d4093db925c409f187"));
            
            // VirtualLens2/Materials/VirtualLensPreview.mat
            var placeholderMaterial = AssetUtility.LoadAssetByGUID<Material>("f9d9632b4c0a6f7439776c9bf3f64ad1");

            var so = new SerializedObject(mask);
            var elementsProp = so.FindProperty("m_Elements");
            foreach (var renderer in settings.Avatar.GetComponentsInChildren<Renderer>(true))
            {
                var isPreview = false;
                var materials = renderer.sharedMaterials;
                foreach (var mat in materials)
                {
                    if (mat == placeholderMaterial) { isPreview = true; }
                }
                if (isPreview)
                {
                    var index = elementsProp.arraySize;
                    elementsProp.InsertArrayElementAtIndex(index);
                    var element = elementsProp.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("m_Path").stringValue =
                        HierarchyUtility.RelativePath(settings.Avatar.transform, renderer.transform);
                    element.FindPropertyRelative("m_Weight").floatValue = 1.0f;
                }
            }
            so.ApplyModifiedProperties();
            folder.CreateAsset(mask);

            return mask;
        }

        private static AvatarMask GetFirstAvatarMask(AnimatorController controller)
        {
            if (controller == null) { return null; }
            if (controller.layers.Length <= 0) { return null; }
            return controller.layers[0].avatarMask;
        }

        private static void PatchFirstAvatarMask(ImplementationSettings settings, ArtifactsFolder folder, AvatarMask source)
        {
            var fx = AvatarDescriptorUtility.GetPlayableLayer(
                settings.Avatar.GetComponent<VRCAvatarDescriptor>(), VRCAvatarDescriptor.AnimLayerType.FX);
            var mask = GetFirstAvatarMask(fx);
            if (mask == null) { return; }
            if (settings.BuildMode == BuildMode.Destructive)
            {
                AvatarMaskEditor.Merge(mask, source);
            }
            else
            {
                mask = Object.Instantiate(mask);
                AvatarMaskEditor.Merge(mask, source);

                // `mask` must be saved as a sub-asset to use as objectReferenceValue.
                // https://discussions.unity.com/t/after-assigned-to-serializedproperty-objectreferencevalue-it-is-still-null/661614/3
                folder.CreateAsset(mask);

                var so = new SerializedObject(fx);
                SetAvatarMaskForLayer(so, 0, mask);
                so.ApplyModifiedProperties();
            }
        }

        private static void UnpatchAvatarMask(GameObject avatar)
        {
            // TODO remove transforms for previews?
            var fx = AvatarDescriptorUtility.GetPlayableLayer(
                avatar.GetComponent<VRCAvatarDescriptor>(), VRCAvatarDescriptor.AnimLayerType.FX);
            var mask = GetFirstAvatarMask(fx);
            if (mask == null) { return; }
            
            var so = new SerializedObject(mask);
            var transformsProp = so.FindProperty("m_Elements");
            var marks = new List<int>();
            for (var i = transformsProp.arraySize - 1; i >= 0; --i)
            {
                var elementProp = transformsProp.GetArrayElementAtIndex(i);
                var path = elementProp.FindPropertyRelative("m_Path").stringValue;
                if (path.StartsWith("_VirtualLens_Root/")) { marks.Add(i); }
            }
            foreach (var i in marks) { transformsProp.DeleteArrayElementAtIndex(i); }
            so.ApplyModifiedProperties();
        }

        private static void SetAvatarMaskForLayer(SerializedObject controller, int index, AvatarMask mask)
        {
            var layersProp = controller.FindProperty("m_AnimatorLayers");
            var layerProp = layersProp.GetArrayElementAtIndex(index);
            var maskProp = layerProp.FindPropertyRelative("m_Mask");
            maskProp.objectReferenceValue = mask;
        }

        #endregion

        #region Motion Generators

        private static void RegisterZoomMotion(
            MotionTemplateParameters parameters, ImplementationSettings settings, ArtifactsFolder folder)
        {
            var root = settings.Avatar;
            var clip = new AnimationClip();
            var values = settings.ZoomFovs();
            foreach (var groupPath in CaptureCameraPaths)
            {
                var group = HierarchyUtility.PathToObject(root, groupPath);
                if (group == null) { continue; }
                foreach (var camera in group.GetComponentsInChildren<Camera>(true))
                {
                    var cameraPath = HierarchyUtility.RelativePath(root, camera.gameObject);
                    AnimationClipEditor.AppendValue(clip, cameraPath, typeof(Camera), "field of view", values);
                }
            }
            foreach (var path in ComputeRendererPaths)
            {
                AnimationClipEditor.AppendValue(clip, path, typeof(MeshRenderer), "material._FieldOfView", values);
            }
            folder.CreateAsset(clip);
            // VirtualLens2/Core/Animations/Placeholders/Zoom.anim
            parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>("4605d0f9cfdde2d4c80f18af6265c401"), clip);
        }

        private static void RegisterApertureMotion(
            MotionTemplateParameters parameters, ImplementationSettings settings, ArtifactsFolder folder)
        {
            var values = new[] { settings.ApertureMinParameter, settings.ApertureMaxParameter };
            var thresh = new[] { settings.ApertureMinParameter, settings.ApertureMinParameter };
            var flags = new[] { 0.0f, 1.0f };
            var clips = new[] { new AnimationClip(), new AnimationClip() };
            foreach (var path in ComputeRendererPaths)
            {
                for (var i = 0; i < 2; ++i)
                {
                    AnimationClipEditor.AppendValue(
                        clips[i], path, typeof(MeshRenderer), "material._LogFNumber", values[i]);
                    AnimationClipEditor.AppendValue(
                        clips[i], path, typeof(MeshRenderer), "material._BlurringThresh", thresh[i]);
                    AnimationClipEditor.AppendValue(
                        clips[i], path, typeof(MeshRenderer), "material._Blurring", flags[i]);
                }
            }
            folder.CreateAsset(clips[0]);
            folder.CreateAsset(clips[1]);
            
            // VirtualLens2/Core/Animations/Placeholders/ApertureMin.anim
            parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>("5be4c8a7f6a92ab4cbb4ec8d8e7ef5db"), clips[0]);
            // VirtualLens2/Core/Animations/Placeholders/ApertureMax.anim
            parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>("113089bf8eec90542ab08f080bfa460c"), clips[1]);
        }

        private static void RegisterDistanceMotion(
            MotionTemplateParameters parameters, ImplementationSettings settings, ArtifactsFolder folder)
        {
            var values = new[]
                { settings.ManualFocusingDistanceMinParameter, settings.ManualFocusingDistanceMaxParameter };
            var thresh = new[]
                { settings.ManualFocusingDistanceMinParameter, settings.ManualFocusingDistanceMinParameter };
            var clips = new[] { new AnimationClip(), new AnimationClip() };
            foreach (var path in ComputeRendererPaths)
            {
                for (var i = 0; i < 2; ++i)
                {
                    AnimationClipEditor.AppendValue(
                        clips[i], path, typeof(MeshRenderer), "material._LogFocusDistance", values[i]);
                    AnimationClipEditor.AppendValue(
                        clips[i], path, typeof(MeshRenderer), "material._FocusingThresh", thresh[i]);
                }
            }
            folder.CreateAsset(clips[0]);
            folder.CreateAsset(clips[1]);
            
            // VirtualLens2/Core/Animations/Placeholders/DistanceMin.anim
            parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>("0252f150775f383449351e783fa0279f"), clips[0]);
            // VirtualLens2/Core/Animations/Placeholders/DistanceMax.anim
            parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>("d6421ea187b66994197cbf16d653e9e1"), clips[1]);
        }

        private static void RegisterExposureMotion(
            MotionTemplateParameters parameters, ImplementationSettings settings, ArtifactsFolder folder)
        {
            var values = new[] { settings.ExposureMinParameter, settings.ExposureMaxParameter };
            var clips = new[] { new AnimationClip(), new AnimationClip() };
            foreach (var path in ComputeRendererPaths)
            {
                for (var i = 0; i < 2; ++i)
                {
                    AnimationClipEditor.AppendValue(
                        clips[i], path, typeof(MeshRenderer), "material._Exposure", values[i]);
                }
            }
            folder.CreateAsset(clips[0]);
            folder.CreateAsset(clips[1]);
            
            // VirtualLens2/Core/Animations/Placeholders/ExposureMin.anim
            parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>("10d1b7ec22a81aa41bc9e77c18d78164"), clips[0]);
            // VirtualLens2/Core/Animations/Placeholders/ExposureMax.anim
            parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>("860f373828cb7f64597c80252183779e"), clips[1]);
        }

        private static void RegisterDroneYawSpeedMotion(
            MotionTemplateParameters parameters, ImplementationSettings settings, ArtifactsFolder folder)
        {
            var path = "_VirtualLens_Root/Local/Transform/WorldFixed/Accumulator/DroneSpeed/Compensation/Controller";
            var speed = settings.DroneYawSpeedScale * 0.1f;
            var negative = Quaternion.Euler(0.0f, -speed, 0.0f);
            var positive = Quaternion.Euler(0.0f, speed, 0.0f);
            var negClip = new AnimationClip();
            AnimationClipEditor.AppendValue(negClip, path, typeof(Transform), "m_LocalRotation.x", negative.x);
            AnimationClipEditor.AppendValue(negClip, path, typeof(Transform), "m_LocalRotation.y", negative.y);
            AnimationClipEditor.AppendValue(negClip, path, typeof(Transform), "m_LocalRotation.z", negative.z);
            AnimationClipEditor.AppendValue(negClip, path, typeof(Transform), "m_LocalRotation.w", negative.w);
            var posClip = new AnimationClip();
            AnimationClipEditor.AppendValue(posClip, path, typeof(Transform), "m_LocalRotation.x", positive.x);
            AnimationClipEditor.AppendValue(posClip, path, typeof(Transform), "m_LocalRotation.y", positive.y);
            AnimationClipEditor.AppendValue(posClip, path, typeof(Transform), "m_LocalRotation.z", positive.z);
            AnimationClipEditor.AppendValue(posClip, path, typeof(Transform), "m_LocalRotation.w", positive.w);
            folder.CreateAsset(negClip);
            folder.CreateAsset(posClip);
            // VirtualLens2/Core/Animations/Placeholders/DroneYaw*.anim
            parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>("b4050538ebd7d8e458e33ca45dbd3843"), posClip);
            parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>("952ad7fdd2030264d9ade8b34e56ce8c"), negClip);
        }

        private static void RegisterFarPlaneMotion(
            MotionTemplateParameters parameters, ImplementationSettings settings, ArtifactsFolder folder)
        {
            var root = settings.Avatar;
            var displayRendererPath = "_VirtualLens_Root/Local/Compute/DisplayRenderer/Quad";
            var clips = new[] { new AnimationClip(), new AnimationClip(), new AnimationClip() };
            var scale = 1.0f;
            var mode = 0.0f;
            foreach (var clip in clips)
            {
                foreach (var groupPath in CaptureCameraPaths)
                {
                    var group = HierarchyUtility.PathToObject(root, groupPath);
                    if (group == null) { continue; }
                    foreach (var camera in group.GetComponentsInChildren<Camera>(true))
                    {
                        var cameraPath = HierarchyUtility.RelativePath(root, camera.gameObject);
                        AnimationClipEditor.AppendValue(
                            clip, cameraPath, typeof(Camera), "far clip plane", scale * settings.ClippingFar);
                    }
                }
                foreach (var path in ComputeRendererPaths)
                {
                    AnimationClipEditor.AppendValue(
                        clip, path, typeof(MeshRenderer), "material._Far", scale * settings.ClippingFar);
                }
                AnimationClipEditor.AppendValue(
                    clip, displayRendererPath, typeof(MeshRenderer), "material._FarOverride", mode);
                folder.CreateAsset(clip);
                scale *= 10.0f;
                mode += 1.0f;
            }
            // VirtualLens2/Core/Animations/Placeholders/FarPlane_[1,10,100]x.anim
            parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>("218573be9fc8f3c4cab9db15b50f632e"), clips[0]);
            parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>("0ef0ad9869d03e6488c695ae8debf683"), clips[1]);
            parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>("b0f40d73030ae20429dd8544c42cdfbe"), clips[2]);
        }

        private static void RegisterAvatarScalingMotion(
            MotionTemplateParameters parameters, ImplementationSettings settings, ArtifactsFolder folder)
        {
            var root = settings.Avatar;
            var maxScale = Constants.MaxScaling;
            var negativeClip = new AnimationClip();
            var neutralClip = new AnimationClip();
            var positiveClip = new AnimationClip();
            var invNegativeClip = new AnimationClip();
            var invNeutralClip = new AnimationClip();
            var invPositiveClip = new AnimationClip();

            {
                // Camera: Near plane
                foreach (var groupPath in CaptureCameraPaths)
                {
                    var group = HierarchyUtility.PathToObject(root, groupPath);
                    if (group == null) { continue; }
                    foreach (var camera in group.GetComponentsInChildren<Camera>(true))
                    {
                        var cameraPath = HierarchyUtility.RelativePath(root, camera.gameObject);
                        AnimationClipEditor.AppendValue(
                            negativeClip, cameraPath, typeof(Camera), "near clip plane", settings.ClippingNear);
                        AnimationClipEditor.AppendValue(
                            neutralClip, cameraPath, typeof(Camera), "near clip plane", settings.ClippingNear);
                        AnimationClipEditor.AppendValue(
                            positiveClip, cameraPath, typeof(Camera), "near clip plane",
                            maxScale * settings.ClippingNear);
                    }
                }
                foreach (var path in ComputeRendererPaths)
                {
                    AnimationClipEditor.AppendValue(
                        negativeClip, path, typeof(MeshRenderer), "material._Near", settings.ClippingNear);
                    AnimationClipEditor.AppendValue(
                        neutralClip, path, typeof(MeshRenderer), "material._Near", settings.ClippingNear);
                    AnimationClipEditor.AppendValue(
                        positiveClip, path, typeof(MeshRenderer), "material._Near", maxScale * settings.ClippingNear);
                }
            }
            {
                // Camera: Touch screen
                var parent = MarkerDetector.DetectPreview(settings);
                var scale = parent == null ? 1.0f : parent.transform.lossyScale.y;
                var path = "_VirtualLens_Root/Local/Preview/Camera";
                var orthographicSize = scale * 0.5f * (16.0f / 9.0f);
                var nearClipPlane = scale * 0.02f;
                var farClipPlane = scale * 0.2f * settings.TouchScreenThickness;
                AnimationClipEditor.AppendValue(
                    negativeClip, path, typeof(Camera), "orthographic size", 0.0f);
                AnimationClipEditor.AppendValue(
                    neutralClip, path, typeof(Camera), "orthographic size", orthographicSize);
                AnimationClipEditor.AppendValue(
                    positiveClip, path, typeof(Camera), "orthographic size", maxScale * orthographicSize);
                AnimationClipEditor.AppendValue(
                    negativeClip, path, typeof(Camera), "near clip plane", 0.0f);
                AnimationClipEditor.AppendValue(
                    neutralClip, path, typeof(Camera), "near clip plane", nearClipPlane);
                AnimationClipEditor.AppendValue(
                    positiveClip, path, typeof(Camera), "near clip plane", maxScale * nearClipPlane);
                AnimationClipEditor.AppendValue(
                    negativeClip, path, typeof(Camera), "far clip plane", 0.0f);
                AnimationClipEditor.AppendValue(
                    neutralClip, path, typeof(Camera), "far clip plane", farClipPlane);
                AnimationClipEditor.AppendValue(
                    positiveClip, path, typeof(Camera), "far clip plane", maxScale * farClipPlane);
            }

            folder.CreateAsset(negativeClip);
            folder.CreateAsset(neutralClip);
            folder.CreateAsset(positiveClip);
            folder.CreateAsset(invNegativeClip);
            folder.CreateAsset(invNeutralClip);
            folder.CreateAsset(invPositiveClip);

            parameters.Add(
                AssetUtility.LoadAssetByGUID<AnimationClip>("517f8b3e9b5e9ba48b82a5fac6cb2f00"),
                neutralClip);
            parameters.Add(
                AssetUtility.LoadAssetByGUID<AnimationClip>("6c21e36a857210446b973ec421e67205"),
                negativeClip);
            parameters.Add(
                AssetUtility.LoadAssetByGUID<AnimationClip>("ff621afe1778d064fb295ab4e051dd0e"),
                positiveClip);
            parameters.Add(
                AssetUtility.LoadAssetByGUID<AnimationClip>("bf3b63f0b74c38643a62ee3c8aac1ab6"),
                invNeutralClip);
            parameters.Add(
                AssetUtility.LoadAssetByGUID<AnimationClip>("4914a1e1c43c25244af2ce714f57271e"),
                invNegativeClip);
            parameters.Add(
                AssetUtility.LoadAssetByGUID<AnimationClip>("f7be67ecd714d984297105a912e47c3a"),
                invPositiveClip);
        }

        private static void RegisterMaxBlurrinessMotion(
            MotionTemplateParameters parameters, ImplementationSettings settings, ArtifactsFolder folder)
        {
            var clip = new AnimationClip();
            foreach (var path in ComputeRendererPaths)
            {
                AnimationClipEditor.AppendValue(
                    clip, path, typeof(MeshRenderer), "material._MaxNumRings", settings.MaxBlurriness);
            }
            folder.CreateAsset(clip);
            // VirtualLens2/Core/Animations/Placeholders/MaxBlurriness.anim
            parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>("3fa459b4565178345b1c4741517645ad"), clip);
        }

        private static void RegisterPreviewMaterialsMotion(
            MotionTemplateParameters parameters, ImplementationSettings settings, ArtifactsFolder folder)
        {
            // VirtualLens2/Materials/VirtualLensPreview.mat
            var placeholderMaterial = AssetUtility.LoadAssetByGUID<Material>("f9d9632b4c0a6f7439776c9bf3f64ad1");
            // VirtualLens2/Core/Materials/System/VirtualLensPreview.mat
            var actualMaterial = AssetUtility.LoadAssetByGUID<Material>("2bd4e12658720e549b0ccd10968d9ad9");

            var clip = new AnimationClip();
            foreach (var renderer in settings.Avatar.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; ++i)
                {
                    if (materials[i] != placeholderMaterial) { continue; }
                    var path = HierarchyUtility.RelativePath(settings.Avatar, renderer.gameObject);
                    var property = $"m_Materials.Array.data[{i}]";
                    AnimationClipEditor.AppendValue(clip, path, renderer.GetType(), property, actualMaterial);
                }
            }
            folder.CreateAsset(clip);
            // VirtualLens2/Core/Animations/Placeholders/ReplacePreviewMaterials.anim
            parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>("c05e3ba36ad7e9a4d99677c51a5abdc1"), clip);
        }

        private static void RegisterResolutionMotion(
            MotionTemplateParameters parameters, ImplementationSettings settings, ArtifactsFolder folder, GeneratedObjectSet objectSet)
        {
            var clips = new[] { new AnimationClip(), new AnimationClip(), new AnimationClip(), new AnimationClip() };
            // ComputeCoc
            var computeCocMaterials = new[]
            {
                new[] // 1x MSAA
                {
                    "57fec08909d49114381687240368e899",
                    "88e6e74112660f6489bf56ee32ea4602",
                    "e09db7053546e4f4cb723bf539b2fd1b",
                    "284e4a6915f757d4292e509f54aa8deb",
                },
                new[] // 2x MSAA
                {
                    "7a3d3610f9854f043bbb8787850de7a9",
                    "c9b4dbac9dc712a4b8a7a198727768d1",
                    "ff62f4cf92c0ab240afe1b055df2ba83",
                    "809efb570e1f2df48b43d8c947c2fdb7",
                },
                new[] // 4x MSAA
                {
                    "aa98d9a4d269dc24baf6660d98173258",
                    "50aa65f850f9c4f4b9eb03b097bbac28",
                    "ef500406f220def44b84c48218f795cf",
                    "490b08cc23f63a041bb3cadc95f5d6ff",
                },
                new[] // 8x MSAA
                {
                    "58685708646416d449238d86d98a64c0",
                    "73827a571803b914fb11e2fb41f066f8",
                    "058b0d4d75a36624a88df72a28b91ed7",
                    "b83e7a6b49d77be4683ef10adb9f1c59",
                },
            }[settings.MSAASamples];
            for (var i = 0; i < clips.Length; ++i)
            {
                var path = "_VirtualLens_Root/Local/Compute/DepthOfField/ComputeCoc/Quad";
                var property = "m_Materials.Array.data[0]";
                var material = AssetUtility.LoadAssetByGUID<Material>(computeCocMaterials[i]);
                AnimationClipEditor.AppendValue(clips[i], path, typeof(MeshRenderer), property, material);
            }
            // ComputeTiles
            var computeTilesMaterials = new[]
            {
                "c46e9c47426fd334d8236cf6215f1c5c",
                "74fccba92f08a8a458d0a46155cf4d58",
                "0ee08376c11434f409007d5009537362",
                "043527fb5ecb4854abe79c29c742636e",
            };
            for (var i = 0; i < clips.Length; ++i)
            {
                var path = "_VirtualLens_Root/Local/Compute/DepthOfField/ComputeTiles/Quad";
                var property = "m_Materials.Array.data[0]";
                var material = AssetUtility.LoadAssetByGUID<Material>(computeTilesMaterials[i]);
                AnimationClipEditor.AppendValue(clips[i], path, typeof(MeshRenderer), property, material);
            }
            // Downsample
            var downsampleMaterials = new[]
            {
                new[] // 1x MSAA
                {
                    "",
                    "7b68a00914d209d438294a283b0687ae",
                    "1c5263db01765564ca721ca03402d0df",
                    "d165ccd535ede404397f92e0e8b9ef97",
                },
                new[] // 2x MSAA
                {
                    "",
                    "f0d99c516f2341445bf4f352c1971ab1",
                    "da42396b987f5f64cab5ab19d3a9286e",
                    "ee096c35af0ee1f4d8617d0ec6368137",
                },
                new[] // 4x MSAA
                {
                    "",
                    "4d214d8066b4e3f42b9fab40945474a2",
                    "f95092bcb924f5048a93bb498a960fad",
                    "062d372b865ccdc4fb07d93849ab6f15",
                },
                new[] // 8x MSAA
                {
                    "",
                    "43311c8252417a5478aef1273976df15",
                    "6532f7d8633c97448ba811eea410ed0d",
                    "462ea58777c2a5849b93cf08d30796d3",
                },
            }[settings.MSAASamples];
            for (var i = 0; i < clips.Length; ++i)
            {
                var path = "_VirtualLens_Root/Local/Compute/DepthOfField/Downsample/Quad";
                var property = "m_Materials.Array.data[0]";
                var material = string.IsNullOrEmpty(downsampleMaterials[i])
                    ? null
                    : AssetUtility.LoadAssetByGUID<Material>(downsampleMaterials[i]);
                AnimationClipEditor.AppendValue(clips[i], path, typeof(MeshRenderer), property, material);
            }
            // Preview 
            var previewMaterials = new[]
            {
                new[] // no post antialiasing
                {
                    new[] // 1x MSAA
                    {
                        "0971c9f887b493145bb5606e33cebe3d",
                        "f23111588973c6e4cb0a4a7475a55d9c",
                        "4289b434fbc04ba41a3796c0cec9a345",
                        "3fbc8eaab8a125b4eb08c015e771baf9",
                    },
                    new[] // 2x MSAA
                    {
                        "4686d69bef0dfd642832f326274eb6a3",
                        "cbc6dd6a9e723ea4a94f22eb6a1346fb",
                        "30a551e7decedd04abded4cf7a054e68",
                        "f81ae3120caf113489375dc3cf47ff5f",
                    },
                    new[] // 4x MSAA
                    {
                        "4817dddff51a40641857231a9aa01226",
                        "4a258b0e7b23fe849840f54599b20fe8",
                        "ee3c538a00619394a8df5429ea37aa8e",
                        "3a794b84b5bdef64ca09a2cad193712c",
                    },
                    new[] // 8x MSAA
                    {
                        "af561af7d7e4fa64080b9fcb0f72f34c",
                        "41f9da6e0473e4a47998a2a048d55979",
                        "e6b01eae87384e44780c03dc9a51af8c",
                        "c6b6dbbe3d8536148b84ee9b0e5f23f8",
                    },
                },
                new[] // FXAA
                {
                    new[] // 1x MSAA
                    {
                        "0a2b9649a3338d64faabc2553c0f16f7",
                        "eac136e9cbdaa084dad79dfb0beaec1d",
                        "2706f2019cb8c0f41b6fafe5e6a83ceb",
                        "01be5ddb29a61294a899158a86afabbf",
                    },
                    new[] // 2x MSAA
                    {
                        "1467488d7ef4a0f4aa707916ae7a398f",
                        "12da4c8ca5378dc4f97385c7c0b79258",
                        "4bfc8879499c4db49af891a3a899a355",
                        "b92523f2872b48a41b22fae45f25bb81",
                    },
                    new[] // 4x MSAA
                    {
                        "8c5c0ccd55fe7b740937cf9b95172fcf",
                        "ed1dc9c8f6b52b948886a8ad50dea18d",
                        "438a7031888022f4798ce7fa1a09683e",
                        "b724ada063028824fbdc4785e77a1994",
                    },
                    new[] // 8x MSAA
                    {
                        "49953548a121d6846a2ff286817510a7",
                        "b5a66ead01d10b840aa55eadd4a18320",
                        "e556328b6e572d74e9ea0b2fc723c710",
                        "9a323ba3d420a1e459bec7e2b1b01cf8",
                    },
                },
                new[] // SMAA
                {
                    new[] // 1x MSAA
                    {
                        "704d6168b8e45a5489b7b1cfa0f7baea",
                        "9fb1ccd4ed5f83f41a91c71a177e83bd",
                        "14333c5022d900742a14148ea1a455b5",
                        "a332c62441df2124aba60a73bb49ae82",
                    },
                    new[] // 2x MSAA
                    {
                        "7849d2d133db1ea43a807beb116d817e",
                        "6a90a01253c3e4047b408b4f3cb3a925",
                        "29d9f4aee495cae4d812615e7c5ef79e",
                        "44774ad0c08584042acaf9b30c9741fa",
                    },
                    new[] // 4x MSAA
                    {
                        "84d4e6a9909bf2a4bb0b6c9bcd63d13b",
                        "dfd98c6743383d640887bc5838889d7a",
                        "2137b992d14e5294d8ea6792126b01d1",
                        "b3aca3ae94ceeeb41b5fbd295e257714",
                    },
                    new[] // 8x MSAA
                    {
                        "dfda7a733da16a047ad7adf9dd85e374",
                        "ada85fb624d4faa43ab8167bfb0a7978",
                        "4636c59e845231d4e8596845cf94f215",
                        "e5fc5cb5adb61984c9496280ff233bae",
                    },
                },
            }[(int)settings.AntialiasingMode][settings.MSAASamples];
            for (var i = 0; i < clips.Length; ++i)
            {
                var path = "_VirtualLens_Root/Local/Compute/DepthOfField/RealtimeCompute/Quad";
                var property = "m_Materials.Array.data[0]";
                var material = AssetUtility.LoadAssetByGUID<Material>(previewMaterials[i]);
                AnimationClipEditor.AppendValue(clips[i], path, typeof(MeshRenderer), property, material);
            }
            // Renderer
            var rendererMaterials = new[]
            {
                new[] // no post antialiasing
                {
                    new[] // 1x MSAA
                    {
                        "86cda67d1a994f54194f5897fa59d144",
                        "aefd70b79ed11dd4dafd5ab9f61f5b1b",
                        "a93268f513548a54bbe9012fabf288be",
                        "f71b4851a7e22234b8b27d0cb6335d05",
                    },
                    new[] // 2x MSAA
                    {
                        "f74623a10b95d0e4f9f1ecdf0cfb13ac",
                        "1db674f592b2dc24b969dc00bcf56165",
                        "68bd65cd5e7327d4e810b476506350f6",
                        "801c772fffdac374bb518acc51a8d106",
                    },
                    new[] // 4x MSAA
                    {
                        "160366e63e7ed5f428b0c8ece718e569",
                        "e15917a284d7b8d478c29b2bfd51276d",
                        "51901cbe3dc2d07418a8283ab7864789",
                        "ab1ecfc8068bbd94fb60234a9f911ef1",
                    },
                    new[] // 8x MSAA
                    {
                        "32d90879ecb54584e84621a96ebbd3a5",
                        "2fca3b04ec033a143b721f1f839d131f",
                        "4ec3555298891ee40a9f88f2f25d5ae3",
                        "cd8ead0d68360f946b50b65573260687",
                    },
                },
                new[] // FXAA
                {
                    new[] // 1x MSAA
                    {
                        "8aa02cad843f7f94bbe67300e25b568b",
                        "37d4db02da000dc48ad1e1d074892dfc",
                        "5dfd2dece6e735446b2a7fc54e1bf460",
                        "e2d1118fa57c44f43b80426155cac902",
                    },
                    new[] // 2x MSAA
                    {
                        "002c65816bb467d4aba73cdf2414b5ca",
                        "4320dac109022704b84f80e7939a4ba0",
                        "473cee656069d654a8d6d2e85b10f1b8",
                        "75c66f94d6f52bd4e9c28d88ff34896e",
                    },
                    new[] // 4x MSAA
                    {
                        "e465d6f849d3b9942ba1d57af806333f",
                        "7320d609e61a35545a2ee6c09dcceecd",
                        "9abb18dbc1b7a1a4a8f4ad14183204fa",
                        "f7bcc25a5dcdd374487f4c655602291a",
                    },
                    new[] // 8x MSAA
                    {
                        "1a4d0e5c0cf69614bb1d70c098fc5f96",
                        "c7d4b72dff1299a488f11f688e19fb9f",
                        "8a6eb9696f814a246ae6d684e70c93d4",
                        "643f76882509a9a4d8ec73313d0843bb",
                    },
                },
                new[] // SMAA
                {
                    new[] // 1x MSAA
                    {
                        "dc6de774eb4d9e04fac201b813d6eb48",
                        "9b7ee7acfaa47874496f1a1df695d57e",
                        "c5bdc782dced1b545ad8035b8efa3318",
                        "8ee74f84abeebc14ea17e19b0aa370fd",
                    },
                    new[] // 2x MSAA
                    {
                        "7ffa5bb7f9d7b6d458fdc191d9d0c971",
                        "2dee94b8828c8554ab65e0e01c600275",
                        "1bbffdc03341bd44c930933a3bce7680",
                        "d869ca498ffc6ac45a3652315102f124",
                    },
                    new[] // 4x MSAA
                    {
                        "a2b461dd2758b4d40a013f0f111fc259",
                        "8a73a7215ae562944b7018d213ffc2b7",
                        "718c3bc231390b94c8966ef9e72a9583",
                        "8ee2ea287f57b054eb6f829b28d8abcc",
                    },
                    new[] // 8x MSAA
                    {
                        "bb738c257d8e3d54689cca09aca3abf4",
                        "9078fc1b9b3359d4f95c1c397ac33822",
                        "086ecf0165c39ca41a99e3fcc2261db4",
                        "7fcacbf9e3a9bc4419f0f49f57af0de9",
                    },
                },
            }[(int)settings.AntialiasingMode][settings.MSAASamples];
            for (var i = 0; i < clips.Length; ++i)
            {
                var path = "_VirtualLens_Root/Local/Writer/Renderer";
                var property = "m_Materials.Array.data[0]";
                var material = AssetUtility.LoadAssetByGUID<Material>(rendererMaterials[i]);
                AnimationClipEditor.AppendValue(clips[i], path, typeof(MeshRenderer), property, material);
            }
            
            // FaceFocusCompute
            if (objectSet.FaceFocusComputeMaterials != null)
            {
                for (var i = 0; i < clips.Length; ++i)
                {
                    var path = "_VirtualLens_Root/Local/Compute/FaceFocusCompute/Quad";
                    var property = "m_Materials.Array.data[0]";
                    var material = objectSet.FaceFocusComputeMaterials[i];
                    AnimationClipEditor.AppendValue(clips[i], path, typeof(MeshRenderer), property, material);
                }
            }

            // StateUpdater
            if (objectSet.StateUpdaterMaterials != null)
            {
                for (var i = 0; i < clips.Length; ++i)
                {
                    var path = "_VirtualLens_Root/Local/Compute/StateUpdater/Quad";
                    var property = "m_Materials.Array.data[0]";
                    var material = objectSet.StateUpdaterMaterials[i];
                    AnimationClipEditor.AppendValue(clips[i], path, typeof(MeshRenderer), property, material);
                }
            }

            // DisplayRenderer
            if (objectSet.DisplayRendererMaterials != null)
            {
                for (var i = 0; i < clips.Length; ++i)
                {
                    var path = "_VirtualLens_Root/Local/Compute/DisplayRenderer/Quad";
                    var property = "m_Materials.Array.data[0]";
                    var material = objectSet.DisplayRendererMaterials[i];
                    AnimationClipEditor.AppendValue(clips[i], path, typeof(MeshRenderer), property, material);
                }
            }

            // VirtualLens2/Core/Animations/Placeholders/Resolution[1080, 1440, 2160, 4320]p.anim
            var placeholders = new[]
            {
                "5ea813845fa645a4597f227fae261fc4",
                "2a450deb2ff77fb4b92111e051e02ea6",
                "e924c23abfe26fb49a926f834bc6cd28",
                "bb6eb09c42fe35740959d7dab56a20cd"
            };
            for (var i = 0; i < clips.Length; ++i)
            {
                folder.CreateAsset(clips[i]);
                parameters.Add(AssetUtility.LoadAssetByGUID<AnimationClip>(placeholders[i]), clips[i]);
            }
        }

        #endregion

        #region Parameter Drivers

        private static void RegisterDefaultParameters(AnimatorController controller, ImplementationSettings settings)
        {
            var layer = controller.layers.First(l => l.name == "VirtualLens2 Initialize");
            var state = layer.stateMachine.states.First(s => s.state.name == "Init").state;
            var driver = (VRCAvatarParameterDriver)state.behaviours[0];

            void RegisterValue(string name, float value)
            {
                driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                {
                    name = ParameterPrefix + name, value = value
                });
            }

            var zoom = settings.FocalToValue(settings.FocalLengthDefault);
            var aperture = settings.ApertureEnabled
                ? settings.ApertureToValue(settings.ApertureFNumberDefault)
                : 0.0f;
            var distance = 0.0f;
            var exposure = settings.ExposureEnabled
                ? settings.ExposureToValue(settings.ExposureDefault)
                : 0.5f;

            RegisterValue("Control", 0);
            RegisterValue("Zoom", zoom);
            RegisterValue("Aperture", aperture);
            RegisterValue("Distance", distance);
            RegisterValue("Exposure", exposure);
            RegisterValue("X", 0.0f);
            RegisterValue("DroneQuickTurn", settings.DroneQuickTurnEnabled ? 1.0f : 0.0f);
            RegisterValue("DroneZoom", settings.DroneZoomEnabled ? 1.0f : 0.0f);
            RegisterValue("AFMode", (float)settings.DefaultAutoFocusMode);
            RegisterValue("TrackingSpeed", (float)settings.DefaultAutoFocusTrackingSpeed);
            RegisterValue("AFSpeed", (float)settings.DefaultFocusingSpeed);
            RegisterValue("Grid", (float)settings.DefaultGrid);
            RegisterValue("GridOpacity", settings.DefaultGridOpacity);
            RegisterValue("Information", settings.DefaultInformation ? 1.0f : 0.0f);
            RegisterValue("Level", settings.DefaultLevelMeter ? 1.0f : 0.0f);
            RegisterValue("Peaking", (float)settings.DefaultPeakingMode);
            RegisterValue("Resolution", (float)settings.DefaultResolution);
            RegisterValue("DepthEnabler", settings.DefaultDepthEnabler ? 1.0f : 0.0f);
            RegisterValue("DepthCleaner", 2.0f); // Farthest
            RegisterValue("PreviewHUD", 0.0f);
            RegisterValue("LocalPlayerMask", 1.0f);
            RegisterValue("RemotePlayerMask", 1.0f);
            RegisterValue("UIMask", 0.0f);
            
            RegisterValue("Version", Constants.Version);
            RegisterValue("Zoom Min", settings.FocalLengthMin);
            RegisterValue("Zoom Max", settings.FocalLengthMax);
            RegisterValue("Aperture Min", settings.ApertureFNumberMin);
            RegisterValue("Aperture Max", settings.ApertureFNumberMax);
            RegisterValue("Exposure Range", settings.ExposureRange);
        }

        private static void RegisterQuickCalls(AnimatorController controller, ImplementationSettings settings)
        {
            var layer = controller.layers.First(l => l.name == "VirtualLens2 QuickCall");
            for (var i = 0; i < settings.QuickCalls.Count; ++i)
            {
                var item = settings.QuickCalls[i];
                var state = layer.stateMachine.states.First(s => s.state.name == i.ToString()).state;
                var driver = (VRCAvatarParameterDriver)state.behaviours[0];
                if (item.Focal != null)
                {
                    driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                    {
                        name = ParameterPrefix + "Zoom",
                        value = settings.FocalToValue((float)item.Focal)
                    });
                }
                if (item.Aperture != null)
                {
                    driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                    {
                        name = ParameterPrefix + "Aperture",
                        value = settings.ApertureToValue((float)item.Aperture)
                    });
                }
                if (item.Exposure != null)
                {
                    driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
                    {
                        name = ParameterPrefix + "Exposure",
                        value = settings.ExposureToValue((float)item.Exposure)
                    });
                }
            }
        }

        #endregion

        #region Utilities

        private static AV3EditorLib.WriteDefaultsOverrideMode SelectWriteDefaultsMode(AnimatorController controller)
        {
            bool hasWdOn = false;
            foreach (var layer in controller.layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    if (state.state.name.Contains("(WD On)")) { continue; }
                    if (state.state.name.Contains("(WD Off)")) { continue; }
                    if (state.state.writeDefaultValues) { hasWdOn = true; }
                }
            }
            return hasWdOn
                ? AV3EditorLib.WriteDefaultsOverrideMode.ForceEnable
                : AV3EditorLib.WriteDefaultsOverrideMode.ForceDisable;
        }

        #endregion
    }
}
