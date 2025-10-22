using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using VirtualLens2.AV3EditorLib;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Constraint.Components;
using Object = UnityEngine.Object;

namespace VirtualLens2
{
    internal static class ObjectGenerator
    {
        private static readonly string[] RESOLUTION_SET = new[] { "1080p", "1440p", "2160p", "4320p" };
        
        private static GameObject CreateTransformReference(ImplementationSettings settings)
        {
            var origin = MarkerDetector.DetectOrigin(settings);
            var droppable = settings.CameraNonPreviewRoot;
            var parent = droppable.transform.parent;
            var reference = new GameObject
            {
                name = "_VirtualLens_TransformReference",
                transform =
                {
                    parent = parent,
                    position = droppable.transform.position,
                    rotation = origin.transform.rotation
                }
            };
            return reference;
        }

        private static VRCConstraintSourceKeyableList CreateConstraintSources(IEnumerable<VRCConstraintSource> list)
        {
            var result = new VRCConstraintSourceKeyableList();
            foreach (var e in list) { result.Add(e); }
            return result;
        }

        private static GameObject CreateSelfieMarkers(ImplementationSettings settings)
        {
            var animator = settings.Avatar.GetComponent<Animator>();
            if (!animator) { return null; }

            // Get related transforms
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var leftEye = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            var rightEye = animator.GetBoneTransform(HumanBodyBones.RightEye);
            if (head == null || leftEye == null || rightEye == null) { return null; }
            var leftMarkerObj = settings.SelfieMarkerLeft;
            var rightMarkerObj = settings.SelfieMarkerRight;
            if (leftMarkerObj == null || rightMarkerObj == null) { return null; }
            var leftMarker = leftMarkerObj.transform;
            var rightMarker = rightMarkerObj.transform;

            // VirtualLens2/Core/Prefabs/AutoFocus/SelfieFocusDetector.prefab
            var prefab = AssetUtility.LoadAssetByGUID<GameObject>("50c3bac1529a2e24cac4df738044650c");
            var root = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (root == null)
            {
                throw new ApplicationException(
                    "Failed to instantiate SelfieFocusDetector prefab.\n" +
                    "Please reimport VirtualLens2 package.");
            }
            root.name = "_VirtualLens_SelfieFocusDetector";

            // Update transforms
            root.transform.parent = head.parent;
            root.transform.position = head.position;
            root.transform.rotation = head.rotation;
            root.transform.localScale = Vector3.one;
            root.GetComponent<VRCRotationConstraint>().Sources.Add(
                new VRCConstraintSource { SourceTransform = head, Weight = 1.0f });
            // Left eye
            var copyLeftEye = HierarchyUtil.PathToTransform(root.transform, "LeftEye");
            copyLeftEye.position = leftEye.position;
            copyLeftEye.rotation = leftEye.rotation;
            copyLeftEye.GetComponent<VRCRotationConstraint>().Sources.Add(
                new VRCConstraintSource { SourceTransform = leftEye, Weight = 1.0f });
            // Right eye
            var copyRightEye = HierarchyUtil.PathToTransform(root.transform, "RightEye");
            copyRightEye.position = rightEye.position;
            copyRightEye.rotation = rightEye.rotation;
            copyRightEye.GetComponent<VRCRotationConstraint>().Sources.Add(
                new VRCConstraintSource { SourceTransform = rightEye, Weight = 1.0f });
            // Left marker
            var copyLeftMarker = HierarchyUtil.PathToTransform(copyLeftEye, "Detector");
            copyLeftMarker.position = leftMarker.position;
            copyLeftMarker.rotation = leftMarker.rotation;
            // Right marker
            var copyRightMarker = HierarchyUtil.PathToTransform(copyRightEye, "Detector");
            copyRightMarker.position = rightMarker.position;
            copyRightMarker.rotation = rightMarker.rotation;

            root.SetActive(false);
            return root;
        }

        private static void ConfigureNonPreviewConstraint(
            ImplementationSettings settings, GameObject root, GameObject transformReference)
        {
            var source = HierarchyUtility.PathToObject(root, "Local/Transform/WorldFixed/Result");
            var target = settings.CameraNonPreviewRoot;
            var constraint = target.AddComponent<VRCParentConstraint>();
            var so = new SerializedObject(constraint);
            so.FindProperty("IsActive").boolValue = false;
            so.FindProperty("GlobalWeight").floatValue = 0.0f;
            var localPosition = target.transform.localPosition;
            so.FindProperty("PositionAtRest.x").floatValue = localPosition.x;
            so.FindProperty("PositionAtRest.y").floatValue = localPosition.y;
            so.FindProperty("PositionAtRest.z").floatValue = localPosition.z;
            var localRotation = target.transform.localRotation.eulerAngles;
            so.FindProperty("RotationAtRest.x").floatValue = localRotation.x;
            so.FindProperty("RotationAtRest.y").floatValue = localRotation.y;
            so.FindProperty("RotationAtRest.z").floatValue = localRotation.z;
            var q1 = transformReference.transform.rotation;
            var q2 = target.transform.rotation;
            var rotationOffset = (Quaternion.Inverse(q1) * q2).eulerAngles;
            so.FindProperty("Sources.totalLength").intValue = 1;
            // VRC Constraints requires apply totalLength before updating sources?
            so.ApplyModifiedProperties();
            so.Update();
            so.FindProperty("Sources.source0.SourceTransform").objectReferenceValue = source.transform;
            so.FindProperty("Sources.source0.Weight").floatValue = 1.0f;
            so.FindProperty("Sources.source0.ParentPositionOffset.x").floatValue = 0.0f;
            so.FindProperty("Sources.source0.ParentPositionOffset.y").floatValue = 0.0f;
            so.FindProperty("Sources.source0.ParentPositionOffset.z").floatValue = 0.0f;
            so.FindProperty("Sources.source0.ParentRotationOffset.x").floatValue = rotationOffset.x;
            so.FindProperty("Sources.source0.ParentRotationOffset.y").floatValue = rotationOffset.y;
            so.FindProperty("Sources.source0.ParentRotationOffset.z").floatValue = rotationOffset.z;
            so.FindProperty("Locked").boolValue = true;
            so.FindProperty("IsActive").boolValue = true;
            so.ApplyModifiedProperties();
            so.Update();
        }

        private static void ConfigureTouchableCamera(ImplementationSettings settings, GameObject root)
        {
            var parent = MarkerDetector.DetectPreview(settings);
            if (parent == null) { return; }
            var scale = parent.transform.lossyScale.y;
            var camera = HierarchyUtility
                .PathToObject(root, "Local/Preview/Camera")
                .GetComponent<Camera>();
            camera.orthographicSize = scale * 0.5f * (16.0f / 9.0f);
            camera.nearClipPlane = scale * 0.02f;
            camera.farClipPlane = scale * 0.2f * settings.TouchScreenThickness;
        }

        private static IDictionary<string, RenderTexture> GetRGBTextureSet(ImplementationSettings settings)
        {
            var table = new Dictionary<string, string[]>
            {
                {
                    "1080p", new[]
                    {
                        // VirtualLens2/Core/Texture/LogiBokeh/1080p/RGB/RGB_1080p_*.renderTexture
                        "8010fd118f75a6141a2c3344eaafc736", // 1x
                        "863fe9e21f704a749b65d9e64e4068a7", // 2x
                        "041eb4f4737d1754babe0a7fc419ac25", // 4x
                        "db12c8d57f8790144bf72910114cd23b", // 8x
                    }
                },
                {
                    "1440p", new[]
                    {
                        // VirtualLens2/Core/Texture/LogiBokeh/1440p/RGB/RGB_1440p_*.renderTexture
                        "e16b4ed93db7dcc4bb72b33dd19c3c59", // 1x
                        "2bc7355b9c96e53458c7d6d36b4ea87f", // 2x
                        "0dc5936b7ffcf9d42b40e45701e36b79", // 4x
                        "e91004fbc65ec8f4b82f7a2e969ea341", // 8x
                    }
                },
                {
                    "2160p", new[]
                    {
                        // VirtualLens2/Core/Texture/LogiBokeh/2160p/RGB/RGB_2160p_*.renderTexture
                        "f0ad4aef49b6c7f43bfccaa16134bab2", // 1x
                        "79b9fb52641322849934ac6f133ecf85", // 2x
                        "89c966572e6b4934fba13df8352a3ca3", // 4x
                        "eb6adbf0797934648bcff50196a1d690", // 8x
                    }
                },
                {
                    "4320p", new[]
                    {
                        // VirtualLens2/Core/Texture/LogiBokeh/4320p/RGB/RGB_4320p_*.renderTexture
                        "319f9c050e5241f47aeac499bb623ff7", // 1x
                        "1b46d07e06eef5f4b893105bba5bc775", // 2x
                        "b6251ed211c356c46b077c4462c26ff4", // 4x
                        "dc82d50beabc0cf498abfb4207fadce9", // 8x
                    }
                },
            };
            var result = new Dictionary<string, RenderTexture>();
            foreach (var kv in table)
            {
                result.Add(kv.Key, AssetUtility.LoadAssetByGUID<RenderTexture>(kv.Value[settings.MSAASamples]));
            }
            return result;
        }

        private static IDictionary<string, RenderTexture> GetDepthTextureSet(ImplementationSettings settings)
        {
            var table = new Dictionary<string, string[]>
            {
                {
                    "1080p", new[]
                    {
                        // VirtualLens2/Core/Texture/LogiBokeh/1080p/Depth/Depth_1080p_*.renderTexture
                        "a02876c4db73c164f802ffbe5441b325", // 1x
                        "78b9541d59bc4ba4fa0e3436c13349ba", // 2x
                        "c25d0463b6a221e46b6a67e9cf56dd4f", // 4x
                        "65de0dce2cb8fa247ac2f6b14bf39e9c", // 8x
                    }
                },
                {
                    "1440p", new[]
                    {
                        // VirtualLens2/Core/Texture/LogiBokeh/1440p/Depth/Depth_1440p_*.renderTexture
                        "38a1676767f4552468ebab5882dd1f2d", // 1x
                        "44c1a10afdf9f4a4a8b436927c732b77", // 2x
                        "04a07d62831ff7a4dbc0a9a48720e4a7", // 4x
                        "d0cfc183834bc3047a3edc7bdaa96a05", // 8x
                    }
                },
                {
                    "2160p", new[]
                    {
                        // VirtualLens2/Core/Texture/LogiBokeh/2160p/Depth/Depth_2160p_*.renderTexture
                        "b4b7029ba3b672b4a81aa01a14f2f048", // 1x
                        "e708ce1aa76085443bcb02ba24df1112", // 2x
                        "565f8b5f7c30cd84591916fb3431277a", // 4x
                        "7c8640bd841dafe4080117c9ca8fe327", // 8x
                    }
                },
                {
                    "4320p", new[]
                    {
                        // VirtualLens2/Core/Texture/LogiBokeh/4320p/Depth/Depth_4320p_*.renderTexture
                        "df398a56714efff4d92483fd836252b5", // 1x
                        "917bfa51cdb65b24983bae30c120e437", // 2x
                        "aaee09493a6451e48afc98bcfd8cef63", // 4x
                        "59db6badb761783449b6e4d087ef40a7", // 8x
                    }
                },
            };
            var result = new Dictionary<string, RenderTexture>();
            foreach (var kv in table)
            {
                result.Add(kv.Key, AssetUtility.LoadAssetByGUID<RenderTexture>(kv.Value[settings.MSAASamples]));
            }
            return result;
        }

        private static void ConfigureCaptureEngine(ImplementationSettings settings, GameObject root)
        {
            var rgbTextures = GetRGBTextureSet(settings);
            var depthTextures = GetDepthTextureSet(settings);

            var cameraRoot = HierarchyUtility.PathToObject(root, "Local/Capture/Camera");
            foreach (var resolution in RESOLUTION_SET)
            {
                var group = HierarchyUtility.PathToObject(cameraRoot, resolution);
                foreach (var camera in group.GetComponentsInChildren<Camera>())
                {
                    if (camera.name == "RGB")
                    {
                        camera.targetTexture = rgbTextures[resolution];
                    }
                    else if (camera.name == "Depth")
                    {
                        camera.targetTexture = depthTextures[resolution];
                    }
                }
            }
        }

        private static void SetMultiSamplingKeyword(ImplementationSettings settings, Material material)
        {
            if (settings.MSAASamples == 0)
            {
                material.DisableKeyword("WITH_MULTI_SAMPLING");
            }
            else
            {
                material.EnableKeyword("WITH_MULTI_SAMPLING");
            }
        }

        private static Material[] CreateFaceFocusComputeMaterials(ImplementationSettings settings,
            ArtifactsFolder folder)
        {
            var materials = new List<Material>();
            var rgbTextures = GetRGBTextureSet(settings);
            foreach (var resolution in RESOLUTION_SET)
            {
                // VirtualLens2/Core/Materials/AutoFocus/FaceFocusCompute.mat
                var material = Object.Instantiate(
                    AssetUtility.LoadAssetByGUID<Material>("29e488319d742f346abb335808f74999"));
                SetMultiSamplingKeyword(settings, material);
                material.SetTexture("_InputTex", rgbTextures[resolution]);
                folder.CreateAsset(material);
                materials.Add(material);
            }
            return materials.ToArray();
        }

        private static Material[] CreateStateUpdaterMaterials(ImplementationSettings settings, ArtifactsFolder folder)
        {
            var materials = new List<Material>();
            var depthTextures = GetDepthTextureSet(settings);
            foreach (var resolution in RESOLUTION_SET)
            {
                // VirtualLens2/Core/Materials/System/StateUpdaterWithExtensions.mat
                var material = Object.Instantiate(
                    AssetUtility.LoadAssetByGUID<Material>("abbc98ae909d3f945bda1eb2f7b8708f"));
                SetMultiSamplingKeyword(settings, material);
                material.SetTexture("_DepthTex", depthTextures[resolution]);
                folder.CreateAsset(material);
                materials.Add(material);
            }
            return materials.ToArray();
        }

        private static Material[] CreateDisplayRendererMaterials(ImplementationSettings settings,
            ArtifactsFolder folder)
        {
            var materials = new List<Material>();
            var depthTextures = GetDepthTextureSet(settings);
            foreach (var resolution in RESOLUTION_SET)
            {
                // VirtualLens2/Core/Materials/System/DisplayRenderer.mat
                var material = Object.Instantiate(
                    AssetUtility.LoadAssetByGUID<Material>("f90d649c3764726458e6844c6c377319"));
                SetMultiSamplingKeyword(settings, material);
                material.SetTexture("_DepthTex", depthTextures[resolution]);
                for (var i = 0; i < settings.CustomGrids.Count; ++i)
                {
                    material.SetTexture($"_CustomGrid{i}Tex", settings.CustomGrids[i].Texture);
                }
                folder.CreateAsset(material);
                materials.Add(material);
            }
            return materials.ToArray();
        }

        private static void ConfigureDroneSpeed(ImplementationSettings settings, GameObject root)
        {
            var s = settings.DroneLinearSpeedScale;
            var tf = HierarchyUtility
                .PathToObject(root, "Local/Transform/WorldFixed/Accumulator/DroneSpeed")
                .transform;
            tf.localScale = new Vector3(s, s, s);
        }

        private static void ConfigureDroneDeadZone(ImplementationSettings settings, GameObject root)
        {
            var x = settings.DroneLinearDeadZoneSize;
            var tf = HierarchyUtility
                .PathToObject(root, "Local/Transform/Controller/Enabler/Arm0/Arm1")
                .transform;
            tf.localPosition = new Vector3(x, 0.0f, 0.0f);
        }

        private static void CreateScreenToucherMeshes(ImplementationSettings settings)
        {
            // VirtualLens2/Core/Prefabs/ScreenToucherMesh.prefab
            var prefab = AssetUtility.LoadAssetByGUID<GameObject>("b9efe750bc19fe94797ce4166f15d90f");
            foreach (var container in settings.ScreenTouchers)
            {
                var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null)
                {
                    throw new ApplicationException(
                        "Failed to instantiate ScreenToucherMesh prefab.\n" +
                        "Please reimport VirtualLens2 package");
                }
                instance.name = "_VirtualLens_ScreenToucher";
                instance.transform.parent = container.transform;
                TransformUtil.ResetLocalTransform(instance.transform);
                PrefabUtility.UnpackPrefabInstance(
                    instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Undo.RegisterCreatedObjectUndo(instance, "Create ScreenToucherMesh");
            }
        }

        public static GeneratedObjectSet Generate(ImplementationSettings settings, ArtifactsFolder folder)
        {
            const string rootName = "_VirtualLens_Root";
            
            Clear(settings);

            if (settings.RemoteOnly)
            {
                // VirtualLens2/Core/Prefabs/RemoteRootMA.prefab
                var remoteRoot = PrefabUtility.InstantiatePrefab(
                    AssetUtility.LoadAssetByGUID<GameObject>("a81733ce65e5f2d489a76fffe4bddb52")) as GameObject;
                remoteRoot.name = rootName;
                remoteRoot.transform.parent = settings.Avatar.transform;
                return new GeneratedObjectSet
                {
                    VirtualLensRoot = remoteRoot,
                    SelfieDetectorMarkers = null
                };
            }

            var transformReference = CreateTransformReference(settings);
            var selfieMarkers = CreateSelfieMarkers(settings);

            var prefabParams = new GameObjectTemplateParameters();
            var origin = MarkerDetector.DetectOrigin(settings);
            var preview = MarkerDetector.DetectPreview(settings);
            prefabParams.Add("CaptureOrigin", origin);
            if (preview) { prefabParams.Add("Preview", MarkerDetector.DetectPreview(settings)); }
            prefabParams.Add("RepositionOrigin", settings.RepositionOrigin);
            prefabParams.Add("DroneController", settings.DroneController);
            prefabParams.Add("TransformReference", transformReference);
            prefabParams.Add(
                "ExternalSource", settings.ExternalPoseSource ? settings.ExternalPoseSource.gameObject : origin);

            var useModularAvatar = settings.BuildAsModule || settings.BuildMode == BuildMode.NonDestructive;
            var prefab = AssetUtility.LoadAssetByGUID<GameObject>(useModularAvatar
                ? "7eb6041c7f2c759499bedf567239930b"   // VirtualLens2/Core/Prefabs/RootMA.prefab
                : "236ed580f6b14b044961ddd465461d2b"); // VirtualLens2/Core/Prefabs/Root.prefab
            var root = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (root == null)
            {
                throw new ApplicationException(
                    "Failed to instantiate root prefab.\n" +
                    "Please reimport VirtualLens2 package");
            }
            Undo.RegisterCreatedObjectUndo(root, "Create VirtualLensRoot");
            root.name = rootName;
            root.transform.parent = settings.Avatar.transform;
            TransformUtil.ResetLocalTransform(root.transform);
            GameObjectTemplateEngine.Apply(root, prefabParams);

            ConfigureNonPreviewConstraint(settings, root, transformReference);
            ConfigureTouchableCamera(settings, root);
            ConfigureCaptureEngine(settings, root);
            ConfigureDroneSpeed(settings, root);
            ConfigureDroneDeadZone(settings, root);
            CreateScreenToucherMeshes(settings);
            
            return new GeneratedObjectSet
            {
                VirtualLensRoot = root,
                SelfieDetectorMarkers = selfieMarkers,
                FaceFocusComputeMaterials = CreateFaceFocusComputeMaterials(settings, folder),
                StateUpdaterMaterials = CreateStateUpdaterMaterials(settings, folder),
                DisplayRendererMaterials = CreateDisplayRendererMaterials(settings, folder),
            };
        }


        private static void RemoveConstraints(GameObject obj)
        {
            if (obj == null) { return; }
            {
                var position = obj.GetComponent<PositionConstraint>();
                if (position) { Object.DestroyImmediate(position); }
                var rotation = obj.GetComponent<RotationConstraint>();
                if (rotation) { Object.DestroyImmediate(rotation); }
                var parent = obj.GetComponent<ParentConstraint>();
                if (parent) { Object.DestroyImmediate(parent); }
            }
            {
                var position = obj.GetComponent<VRCPositionConstraint>();
                if (position) { Object.DestroyImmediate(position); }
                var rotation = obj.GetComponent<VRCRotationConstraint>();
                if (rotation) { Object.DestroyImmediate(rotation); }
                var parent = obj.GetComponent<VRCParentConstraint>();
                if (parent) { Object.DestroyImmediate(parent); }
            }
        }

        private static void RemoveDescendants(GameObject obj, Regex regex)
        {
            if (obj == null) { return; }
            var candidates = new List<GameObject>();
            foreach (Transform transform in obj.transform)
            {
                var child = transform.gameObject;
                if (regex.IsMatch(child.name))
                {
                    candidates.Add(child);
                }
                else
                {
                    RemoveDescendants(child, regex);
                }
            }
            foreach (var child in candidates)
            {
                Object.DestroyImmediate(child);
            }
        }

        private static void RemoveDescendants(GameObject obj)
        {
            RemoveDescendants(obj, new Regex(@"^.*$"));
        }

        public static void Clear(GameObject avatar, GameObject cameraRoot, GameObject nonPreviewRoot)
        {
            // Remove constraints from camera objects
            RemoveConstraints(cameraRoot);
            RemoveConstraints(nonPreviewRoot);
            // Remove auto-generated objects
            RemoveDescendants(avatar, new Regex(@"^_VirtualLens_.*$"));
            RemoveDescendants(MarkerDetector.DetectOrigin(cameraRoot));
            RemoveDescendants(MarkerDetector.DetectPreview(cameraRoot));
        }

        private static void Clear(ImplementationSettings settings)
        {
            Clear(settings.Avatar, settings.CameraRoot, settings.CameraNonPreviewRoot);
        }
    }
    
}
